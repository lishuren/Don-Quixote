using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Models;
using MapPlannerApi.Services;

namespace MapPlannerApi.Endpoints;

public static class MapEndpoints
{
    public static void MapMapEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/map").WithTags("Map");

        // GET /api/map
        group.MapGet("/", (MapStore store) =>
        {
            var map = store.GetMap();
            if (map is null)
            {
                return Results.NotFound(new { error = "No map uploaded. Call POST /api/map first." });
            }
            return Results.Ok(new { mapId = store.CurrentMapId, map });
        })
        .WithName("GetMap")
        .WithDescription("Get current map snapshot");

        // POST /api/map - accept MapConfigDto, create Zone and Table entities, store a MapPayload
        group.MapPost("/", async (MapConfigDto config, MapStore store, ZoneRepository zoneRepo, TableRepository tableRepo) =>
        {
            // create zones
            var createdZones = new List<ZoneDto>();
            var zoneEntities = new Dictionary<string, Zone>();

            if (config.Zones != null)
            {
                for (int i = 0; i < config.Zones.Count; i++)
                {
                    var zreq = config.Zones[i];
                    var key = string.IsNullOrWhiteSpace(zreq.Name) ? $"zone{i}" : zreq.Name;

                    // determine zone name (use provided name or fallback to generated key)
                    var zoneName = string.IsNullOrWhiteSpace(zreq.Name) ? key : zreq.Name;

                    // try to find existing zone by name to avoid UNIQUE constraint errors
                    var existing = await zoneRepo.GetZoneByNameAsync(zoneName);
                    if (existing != null)
                    {
                        // If the client supplied updated geometry or metadata, apply it to the existing zone
                        if (double.IsFinite(zreq.X)) existing.X = zreq.X;
                        if (double.IsFinite(zreq.Y)) existing.Y = zreq.Y;
                        if (double.IsFinite(zreq.Width)) existing.Width = zreq.Width;
                        if (double.IsFinite(zreq.Height)) existing.Height = zreq.Height;
                        existing.Color = zreq.Color ?? existing.Color;
                        existing.IsNavigable = zreq.IsNavigable;
                        existing.SpeedLimit = zreq.SpeedLimit ?? existing.SpeedLimit;
                        if (Enum.TryParse<ZoneType>(zreq.Type, true, out var parsedType)) existing.Type = parsedType;

                        var updated = await zoneRepo.UpdateZoneAsync(existing);
                        zoneEntities[key] = updated;
                        createdZones.Add(new ZoneDto(updated.Id, updated.Name, updated.Type.ToString(), updated.X, updated.Y, updated.Width, updated.Height, updated.Color, updated.IsNavigable, updated.SpeedLimit, updated.Tables.Count, updated.Checkpoints.Count));
                        continue;
                    }

                    var zoneEntity = new Zone
                    {
                        Name = zoneName,
                        Type = Enum.TryParse<ZoneType>(zreq.Type, true, out var zt) ? zt : ZoneType.Dining,
                        X = zreq.X,
                        Y = zreq.Y,
                        Width = zreq.Width,
                        Height = zreq.Height,
                        Color = zreq.Color,
                        IsNavigable = zreq.IsNavigable,
                        SpeedLimit = zreq.SpeedLimit
                    };

                    var created = await zoneRepo.CreateZoneAsync(zoneEntity);
                    zoneEntities[key] = created;
                    createdZones.Add(new ZoneDto(created.Id, created.Name, created.Type.ToString(), created.X, created.Y, created.Width, created.Height, created.Color, created.IsNavigable, created.SpeedLimit, created.Tables.Count, created.Checkpoints.Count));
                }
            }

            // find dining zone id (first with type Dining)
            int? diningZoneId = createdZones.FirstOrDefault(z => z.Type.Equals(ZoneType.Dining.ToString(), StringComparison.OrdinalIgnoreCase))?.Id;

            // create tables
            var createdTables = new List<TableDto>();
            if (config.Tables != null)
            {
                foreach (var t in config.Tables)
                {
                    Enum.TryParse<TableShape>(t.Shape, true, out var shape);
                    var tableEntity = new TableEntity
                    {
                        Label = t.Label,
                        Shape = shape,
                        Center = t.Center?.ToEntity() ?? new Position(),
                        Width = t.Width,
                        Height = t.Height,
                        Rotation = t.Rotation,
                        Capacity = t.Capacity,
                        ZoneId = diningZoneId
                    };

                    var created = await tableRepo.CreateAsync(tableEntity);
                    createdTables.Add(new TableDto(created.Id, created.Label, created.Shape.ToString(), created.Status.ToString(), PositionDto.FromEntity(created.Center), created.Width, created.Height, created.Rotation, created.Capacity, created.ZoneId, created.Zone?.Name, created.Guests.Count));
                }
            }

            // build MapPayload to store for planner
            var payload = new MapPayload();
            if (config.DiningArea != null)
            {
                payload.DiningArea = new Rect(config.DiningArea.X, config.DiningArea.Y, config.DiningArea.Width, config.DiningArea.Height);
            }
            payload.Zones = new Dictionary<string, Rect>();
            foreach (var kv in zoneEntities)
            {
                var key = kv.Key;
                var ent = kv.Value;
                payload.Zones[key] = new Rect(ent.X, ent.Y, ent.Width, ent.Height);
            }

            payload.Tables = new List<Table>();
            foreach (var ct in createdTables)
            {
                var tbl = new Table
                {
                    Id = ct.Id,
                    Type = "Dining",
                    Shape = ct.Shape,
                    Center = new Point2D(ct.Center.ScreenX, ct.Center.ScreenY),
                    Bounds = new Rect(ct.Center.ScreenX - ct.Width / 2, ct.Center.ScreenY - ct.Height / 2, ct.Width, ct.Height)
                };
                payload.Tables.Add(tbl);
            }

            store.SetMap(payload);
            

            var result = new MapEntitiesDto(createdZones, createdTables, config);
            return Results.Ok(new { message = "Map stored", mapId = store.CurrentMapId, entities = result });
        })
        .WithName("PostMap")
        .WithDescription("Upload map config, create zones and tables, and store map snapshot");

        // POST /api/plan - route planner
        group.MapPost("/plan", (PlanRequest request, MapStore store, PathPlanner planner) =>
        {
            var map = store.GetMap();
            if (map is null)
            {
                return Results.BadRequest(new { error = "No map uploaded. Call POST /api/map first." });
            }
            var result = planner.Plan(map, request);
            if (!result.Success)
                return Results.BadRequest(new { error = result.Error ?? "Planning failed" });
            return Results.Ok(result);
        })
        .WithName("PlanRoute")
        .WithDescription("Run path planner on stored map");
    }
}
