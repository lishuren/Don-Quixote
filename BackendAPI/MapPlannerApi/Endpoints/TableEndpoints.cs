using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class TableEndpoints
{
    public static void MapTableEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tables").WithTags("Tables");

        // GET /api/tables - List all tables
        group.MapGet("/", async (TableRepository repo) =>
        {
            var tables = await repo.GetAllAsync();
            return Results.Ok(tables.Select(ToDto));
        })
        .WithName("GetTables")
        .WithDescription("Get all tables");

        // GET /api/tables/{id} - Get table by ID
        group.MapGet("/{id:int}", async (int id, TableRepository repo) =>
        {
            var table = await repo.GetByIdAsync(id);
            return table is null
                ? Results.NotFound(new { error = $"Table {id} not found" })
                : Results.Ok(ToDto(table));
        })
        .WithName("GetTable")
        .WithDescription("Get table by ID");

        // POST /api/tables - Create one or more new tables (only creates tables now)
        group.MapPost("/", async (List<CreateTableRequest> tableList, TableRepository repo, ConfigRepository configRepo) =>
        {
            var createdTableDtos = new List<TableDto>();
            var pixelsPerMeter = await configRepo.GetPixelsPerMeterAsync();

            foreach (var request in tableList)
            {
                Enum.TryParse<TableShape>(request.Shape, true, out var shape);

                var table = new TableEntity
                {
                    Label = request.Label,
                    Shape = shape,
                    Center = request.Center?.ToEntity() ?? new Position(),
                    Width = request.Width,
                    Height = request.Height,
                    Rotation = request.Rotation,
                    Capacity = request.Capacity,
                    ZoneId = request.ZoneId
                };

                var created = await repo.CreateAsync(table);
                createdTableDtos.Add(ToDto(created));
            }

            return Results.Ok(new { tables = createdTableDtos });
        })
        .WithName("CreateTables")
        .WithDescription("Create one or more tables");

        // PUT /api/tables/{id} - Update table
        group.MapPut("/{id:int}", async (int id, UpdateTableRequest request, TableRepository repo) =>
        {
            var table = await repo.GetByIdAsync(id);
            if (table is null)
                return Results.NotFound(new { error = $"Table {id} not found" });

            if (request.Label is not null) table.Label = request.Label;
            if (request.Shape is not null && Enum.TryParse<TableShape>(request.Shape, true, out var shape))
                table.Shape = shape;
            if (request.Status is not null && Enum.TryParse<TableStatus>(request.Status, true, out var status))
                table.Status = status;
            if (request.Center is not null) table.Center = request.Center.ToEntity();
            if (request.Width.HasValue) table.Width = request.Width.Value;
            if (request.Height.HasValue) table.Height = request.Height.Value;
            if (request.Rotation.HasValue) table.Rotation = request.Rotation.Value;
            if (request.Capacity.HasValue) table.Capacity = request.Capacity.Value;
            if (request.ZoneId.HasValue) table.ZoneId = request.ZoneId;

            var updated = await repo.UpdateAsync(table);
            return Results.Ok(ToDto(updated));
        })
        .WithName("UpdateTable")
        .WithDescription("Update a table");

        // DELETE /api/tables/{id} - Delete table
        group.MapDelete("/{id:int}", async (int id, TableRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Table {id} not found" });
        })
        .WithName("DeleteTable")
        .WithDescription("Delete a table");

        // GET /api/tables/status/{status} - Get tables by status
        group.MapGet("/status/{status}", async (string status, TableRepository repo) =>
        {
            if (!Enum.TryParse<TableStatus>(status, true, out var tableStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var tables = await repo.GetByStatusAsync(tableStatus);
            return Results.Ok(tables.Select(ToDto));
        })
        .WithName("GetTablesByStatus")
        .WithDescription("Get tables filtered by status");

        // GET /api/tables/available - Get available tables
        group.MapGet("/available", async (int? minCapacity, TableRepository repo) =>
        {
            var tables = await repo.GetAvailableTablesAsync(minCapacity ?? 1);
            return Results.Ok(tables.Select(ToDto));
        })
        .WithName("GetAvailableTables")
        .WithDescription("Get available tables with minimum capacity");

        // PUT /api/tables/{id}/status - Update table status
        group.MapPut("/{id:int}/status", async (int id, string status, TableRepository repo) =>
        {
            if (!Enum.TryParse<TableStatus>(status, true, out var tableStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            await repo.UpdateStatusAsync(id, tableStatus);
            return Results.Ok(new { message = "Status updated" });
        })
        .WithName("UpdateTableStatus")
        .WithDescription("Update table status");

        // GET /api/tables/summary - Get table status summary
        group.MapGet("/summary", async (TableRepository repo) =>
        {
            var statusCounts = await repo.GetStatusSummaryAsync();
            var occupancy = await repo.GetOccupancyAsync();
            var total = statusCounts.Values.Sum();

            return Results.Ok(new TableStatusSummary(
                total,
                statusCounts.GetValueOrDefault(TableStatus.Available),
                statusCounts.GetValueOrDefault(TableStatus.Occupied),
                statusCounts.GetValueOrDefault(TableStatus.Reserved),
                statusCounts.GetValueOrDefault(TableStatus.NeedsService),
                statusCounts.GetValueOrDefault(TableStatus.Cleaning),
                occupancy
            ));
        })
        .WithName("GetTableSummary")
        .WithDescription("Get table status summary");

        // GET /api/tables/zone/{zoneId} - Get tables in a zone
        group.MapGet("/zone/{zoneId:int}", async (int zoneId, TableRepository repo) =>
        {
            var tables = await repo.GetByZoneAsync(zoneId);
            return Results.Ok(tables.Select(ToDto));
        })
        .WithName("GetTablesByZone")
        .WithDescription("Get tables in a specific zone");
    }

    private static TableDto ToDto(TableEntity t) => new(
        t.Id,
        t.Label,
        t.Shape.ToString(),
        t.Status.ToString(),
        PositionDto.FromEntity(t.Center),
        t.Width,
        t.Height,
        t.Rotation,
        t.Capacity,
        t.ZoneId,
        t.Zone?.Name,
        t.Guests.Count
    );
}
