using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class ZoneEndpoints
{
    public static void MapZoneEndpoints(this WebApplication app)
    {
        var zoneGroup = app.MapGroup("/api/zones").WithTags("Zones");
        var checkpointGroup = app.MapGroup("/api/checkpoints").WithTags("Checkpoints");

        // ===== Zone endpoints =====

        // GET /api/zones - List all zones
        zoneGroup.MapGet("/", async (ZoneRepository repo) =>
        {
            var zones = await repo.GetAllZonesAsync();
            return Results.Ok(zones.Select(ToZoneDto));
        })
        .WithName("GetZones")
        .WithDescription("Get all zones");

        // GET /api/zones/{id} - Get zone by ID
        zoneGroup.MapGet("/{id:int}", async (int id, ZoneRepository repo) =>
        {
            var zone = await repo.GetZoneByIdAsync(id);
            return zone is null
                ? Results.NotFound(new { error = $"Zone {id} not found" })
                : Results.Ok(ToZoneDto(zone));
        })
        .WithName("GetZone")
        .WithDescription("Get zone by ID");

        // POST /api/zones - Create zone
        zoneGroup.MapPost("/", async (CreateZoneRequest request, ZoneRepository repo) =>
        {
            Enum.TryParse<ZoneType>(request.Type, true, out var zoneType);

            var zone = new Zone
            {
                Name = request.Name,
                Type = zoneType,
                X = request.X,
                Y = request.Y,
                Width = request.Width,
                Height = request.Height,
                Color = request.Color,
                IsNavigable = request.IsNavigable,
                SpeedLimit = request.SpeedLimit
            };

            var created = await repo.CreateZoneAsync(zone);
            return Results.Created($"/api/zones/{created.Id}", ToZoneDto(created));
        })
        .WithName("CreateZone")
        .WithDescription("Create a new zone");

        // DELETE /api/zones/{id} - Delete zone
        zoneGroup.MapDelete("/{id:int}", async (int id, ZoneRepository repo) =>
        {
            var deleted = await repo.DeleteZoneAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Zone {id} not found" });
        })
        .WithName("DeleteZone")
        .WithDescription("Delete a zone");

        // GET /api/zones/type/{type} - Get zones by type
        zoneGroup.MapGet("/type/{type}", async (string type, ZoneRepository repo) =>
        {
            if (!Enum.TryParse<ZoneType>(type, true, out var zoneType))
                return Results.BadRequest(new { error = $"Invalid zone type: {type}" });

            var zones = await repo.GetZonesByTypeAsync(zoneType);
            return Results.Ok(zones.Select(ToZoneDto));
        })
        .WithName("GetZonesByType")
        .WithDescription("Get zones filtered by type");

        // ===== Checkpoint endpoints =====

        // GET /api/checkpoints - List all checkpoints
        checkpointGroup.MapGet("/", async (ZoneRepository repo) =>
        {
            var checkpoints = await repo.GetAllCheckpointsAsync();
            return Results.Ok(checkpoints.Select(ToCheckpointDto));
        })
        .WithName("GetCheckpoints")
        .WithDescription("Get all checkpoints");

        // GET /api/checkpoints/{id} - Get checkpoint by ID
        checkpointGroup.MapGet("/{id:int}", async (int id, ZoneRepository repo) =>
        {
            var checkpoint = await repo.GetCheckpointByIdAsync(id);
            return checkpoint is null
                ? Results.NotFound(new { error = $"Checkpoint {id} not found" })
                : Results.Ok(ToCheckpointDto(checkpoint));
        })
        .WithName("GetCheckpoint")
        .WithDescription("Get checkpoint by ID");

        // POST /api/checkpoints - Create checkpoint
        checkpointGroup.MapPost("/", async (CreateCheckpointRequest request, ZoneRepository repo) =>
        {
            Enum.TryParse<CheckpointType>(request.Type, true, out var checkpointType);

            var checkpoint = new Checkpoint
            {
                Name = request.Name,
                Type = checkpointType,
                Position = request.Position?.ToEntity() ?? new Position(),
                RequiredHeading = request.RequiredHeading,
                ZoneId = request.ZoneId,
                IsMandatoryStop = request.IsMandatoryStop,
                RouteOrder = request.RouteOrder,
                Capacity = request.Capacity
            };

            var created = await repo.CreateCheckpointAsync(checkpoint);
            return Results.Created($"/api/checkpoints/{created.Id}", ToCheckpointDto(created));
        })
        .WithName("CreateCheckpoint")
        .WithDescription("Create a new checkpoint");

        // DELETE /api/checkpoints/{id} - Delete checkpoint
        checkpointGroup.MapDelete("/{id:int}", async (int id, ZoneRepository repo) =>
        {
            var deleted = await repo.DeleteCheckpointAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Checkpoint {id} not found" });
        })
        .WithName("DeleteCheckpoint")
        .WithDescription("Delete a checkpoint");

        // GET /api/checkpoints/type/{type} - Get checkpoints by type
        checkpointGroup.MapGet("/type/{type}", async (string type, ZoneRepository repo) =>
        {
            if (!Enum.TryParse<CheckpointType>(type, true, out var checkpointType))
                return Results.BadRequest(new { error = $"Invalid checkpoint type: {type}" });

            var checkpoints = await repo.GetCheckpointsByTypeAsync(checkpointType);
            return Results.Ok(checkpoints.Select(ToCheckpointDto));
        })
        .WithName("GetCheckpointsByType")
        .WithDescription("Get checkpoints filtered by type");

        // GET /api/checkpoints/charging - Get charging stations
        checkpointGroup.MapGet("/charging", async (ZoneRepository repo) =>
        {
            var stations = await repo.GetChargingStationsAsync();
            return Results.Ok(stations.Select(ToCheckpointDto));
        })
        .WithName("GetChargingStations")
        .WithDescription("Get charging station checkpoints");

        // GET /api/checkpoints/zone/{zoneId} - Get checkpoints in zone
        checkpointGroup.MapGet("/zone/{zoneId:int}", async (int zoneId, ZoneRepository repo) =>
        {
            var checkpoints = await repo.GetCheckpointsByZoneAsync(zoneId);
            return Results.Ok(checkpoints.Select(ToCheckpointDto));
        })
        .WithName("GetCheckpointsByZone")
        .WithDescription("Get checkpoints in a zone");
    }

    private static ZoneDto ToZoneDto(Zone z) => new(
        z.Id,
        z.Name,
        z.Type.ToString(),
        z.X,
        z.Y,
        z.Width,
        z.Height,
        z.Color,
        z.IsNavigable,
        z.SpeedLimit,
        z.Tables.Count,
        z.Checkpoints.Count
    );

    private static CheckpointDto ToCheckpointDto(Checkpoint c) => new(
        c.Id,
        c.Name,
        c.Type.ToString(),
        PositionDto.FromEntity(c.Position),
        c.RequiredHeading,
        c.ZoneId,
        c.Zone?.Name,
        c.IsMandatoryStop,
        c.RouteOrder,
        c.Capacity
    );
}
