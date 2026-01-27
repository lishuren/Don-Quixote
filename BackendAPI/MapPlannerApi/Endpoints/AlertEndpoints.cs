using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/alerts").WithTags("Alerts");

        // GET /api/alerts - List alerts
        group.MapGet("/", async (int? limit, bool? includeResolved, AlertRepository repo) =>
        {
            var alerts = await repo.GetAllAsync(limit, includeResolved ?? false);
            return Results.Ok(alerts.Select(ToDto));
        })
        .WithName("GetAlerts")
        .WithDescription("Get all alerts");

        // GET /api/alerts/{id} - Get alert by ID
        group.MapGet("/{id:int}", async (int id, AlertRepository repo) =>
        {
            var alert = await repo.GetByIdAsync(id);
            return alert is null
                ? Results.NotFound(new { error = $"Alert {id} not found" })
                : Results.Ok(ToDto(alert));
        })
        .WithName("GetAlert")
        .WithDescription("Get alert by ID");

        // POST /api/alerts - Create alert
        group.MapPost("/", async (CreateAlertRequest request, AlertRepository repo) =>
        {
            if (!Enum.TryParse<AlertType>(request.Type, true, out var alertType))
                return Results.BadRequest(new { error = $"Invalid alert type: {request.Type}" });

            if (!Enum.TryParse<AlertSeverity>(request.Severity, true, out var severity))
                return Results.BadRequest(new { error = $"Invalid severity: {request.Severity}" });

            var alert = new Alert
            {
                Type = alertType,
                Severity = severity,
                Title = request.Title,
                Message = request.Message,
                RobotId = request.RobotId,
                TableId = request.TableId,
                TaskId = request.TaskId
            };

            var created = await repo.CreateAsync(alert);
            return Results.Created($"/api/alerts/{created.Id}", ToDto(created));
        })
        .WithName("CreateAlert")
        .WithDescription("Create a new alert");

        // DELETE /api/alerts/{id} - Delete alert
        group.MapDelete("/{id:int}", async (int id, AlertRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Alert {id} not found" });
        })
        .WithName("DeleteAlert")
        .WithDescription("Delete an alert");

        // GET /api/alerts/severity/{severity} - Get alerts by severity
        group.MapGet("/severity/{severity}", async (string severity, AlertRepository repo) =>
        {
            if (!Enum.TryParse<AlertSeverity>(severity, true, out var alertSeverity))
                return Results.BadRequest(new { error = $"Invalid severity: {severity}" });

            var alerts = await repo.GetBySeverityAsync(alertSeverity);
            return Results.Ok(alerts.Select(ToDto));
        })
        .WithName("GetAlertsBySeverity")
        .WithDescription("Get alerts filtered by severity");

        // GET /api/alerts/unacknowledged - Get unacknowledged alerts
        group.MapGet("/unacknowledged", async (AlertRepository repo) =>
        {
            var alerts = await repo.GetUnacknowledgedAsync();
            return Results.Ok(alerts.Select(ToDto));
        })
        .WithName("GetUnacknowledgedAlerts")
        .WithDescription("Get unacknowledged alerts");

        // POST /api/alerts/{id}/acknowledge - Acknowledge alert
        group.MapPost("/{id:int}/acknowledge", async (int id, string? user, AlertRepository repo) =>
        {
            var alert = await repo.GetByIdAsync(id);
            if (alert is null)
                return Results.NotFound(new { error = $"Alert {id} not found" });

            await repo.AcknowledgeAsync(id, user);
            return Results.Ok(new { message = $"Alert {id} acknowledged" });
        })
        .WithName("AcknowledgeAlert")
        .WithDescription("Acknowledge an alert");

        // POST /api/alerts/{id}/resolve - Resolve alert
        group.MapPost("/{id:int}/resolve", async (int id, AlertRepository repo) =>
        {
            var alert = await repo.GetByIdAsync(id);
            if (alert is null)
                return Results.NotFound(new { error = $"Alert {id} not found" });

            await repo.ResolveAsync(id);
            return Results.Ok(new { message = $"Alert {id} resolved" });
        })
        .WithName("ResolveAlert")
        .WithDescription("Resolve an alert");

        // GET /api/alerts/robot/{robotId} - Get alerts for a robot
        group.MapGet("/robot/{robotId:int}", async (int robotId, AlertRepository repo) =>
        {
            var alerts = await repo.GetByRobotAsync(robotId);
            return Results.Ok(alerts.Select(ToDto));
        })
        .WithName("GetAlertsByRobot")
        .WithDescription("Get alerts for a specific robot");

        // GET /api/alerts/summary - Get alert summary
        group.MapGet("/summary", async (AlertRepository repo, RestaurantDbContext db) =>
        {
            var total = await repo.GetActiveCountAsync();
            var critical = await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Critical);
            var errors = await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Error);
            var warnings = await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Warning);
            var unack = await db.Alerts.CountAsync(a => !a.IsResolved && !a.IsAcknowledged);

            return Results.Ok(new AlertSummary(total, critical, errors, warnings, unack));
        })
        .WithName("GetAlertSummary")
        .WithDescription("Get alert summary");
    }

    private static AlertDto ToDto(Alert a) => new(
        a.Id,
        a.Type.ToString(),
        a.Severity.ToString(),
        a.Title,
        a.Message,
        a.RobotId,
        a.Robot?.Name,
        a.TableId,
        a.TaskId,
        a.IsAcknowledged,
        a.AcknowledgedBy,
        a.AcknowledgedAt,
        a.IsResolved,
        a.ResolvedAt,
        a.CreatedAt
    );
}
