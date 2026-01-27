using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        // GET /api/dashboard - Get full dashboard summary
        group.MapGet("/", async (RestaurantDbContext db) =>
        {
            // Robot summary
            var robots = await db.Robots.ToListAsync();
            var robotSummary = new RobotSummary(
                robots.Count,
                robots.Count(r => r.Status == RobotStatus.Idle),
                robots.Count(r => r.Status == RobotStatus.Navigating),
                robots.Count(r => r.Status == RobotStatus.Delivering),
                robots.Count(r => r.Status == RobotStatus.Charging),
                robots.Count(r => r.Status == RobotStatus.Error),
                robots.Count(r => r.Status == RobotStatus.Offline),
                robots.Any() ? robots.Average(r => r.BatteryLevel) : 0
            );

            // Table summary
            var tables = await db.Tables.ToListAsync();
            var totalTables = tables.Count;
            var tableSummary = new TableStatusSummary(
                totalTables,
                tables.Count(t => t.Status == TableStatus.Available),
                tables.Count(t => t.Status == TableStatus.Occupied),
                tables.Count(t => t.Status == TableStatus.Reserved),
                tables.Count(t => t.Status == TableStatus.NeedsService),
                tables.Count(t => t.Status == TableStatus.Cleaning),
                totalTables > 0 ? tables.Count(t => t.Status == TableStatus.Occupied) * 100 / totalTables : 0
            );

            // Task summary
            var today = DateTime.UtcNow.Date;
            var taskSummary = new TaskQueueSummary(
                await db.Tasks.CountAsync(t => t.Status == Entities.TaskStatus.Pending),
                await db.Tasks.CountAsync(t => t.Status == Entities.TaskStatus.Assigned),
                await db.Tasks.CountAsync(t => t.Status == Entities.TaskStatus.InProgress),
                await db.Tasks.CountAsync(t => t.Status == Entities.TaskStatus.Completed && t.CompletedAt >= today),
                await db.Tasks.CountAsync(t => t.Status == Entities.TaskStatus.Failed && t.CompletedAt >= today)
            );

            // Alert summary
            var alertSummary = new AlertSummary(
                await db.Alerts.CountAsync(a => !a.IsResolved),
                await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Critical),
                await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Error),
                await db.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Warning),
                await db.Alerts.CountAsync(a => !a.IsResolved && !a.IsAcknowledged)
            );

            // Guest counts
            var activeGuests = await db.Guests.CountAsync(g =>
                g.Status != GuestStatus.Departed && g.Status != GuestStatus.Waiting);
            var waitlistCount = await db.Guests.CountAsync(g => g.Status == GuestStatus.Waiting);

            return Results.Ok(new DashboardSummary(
                robotSummary,
                tableSummary,
                taskSummary,
                alertSummary,
                activeGuests,
                waitlistCount
            ));
        })
        .WithName("GetDashboard")
        .WithDescription("Get full dashboard summary");

        // GET /api/dashboard/robots - Robot status overview
        group.MapGet("/robots", async (RestaurantDbContext db) =>
        {
            var robots = await db.Robots.ToListAsync();
            return Results.Ok(new RobotSummary(
                robots.Count,
                robots.Count(r => r.Status == RobotStatus.Idle),
                robots.Count(r => r.Status == RobotStatus.Navigating),
                robots.Count(r => r.Status == RobotStatus.Delivering),
                robots.Count(r => r.Status == RobotStatus.Charging),
                robots.Count(r => r.Status == RobotStatus.Error),
                robots.Count(r => r.Status == RobotStatus.Offline),
                robots.Any() ? robots.Average(r => r.BatteryLevel) : 0
            ));
        })
        .WithName("GetRobotDashboard")
        .WithDescription("Get robot status summary");

        // GET /api/dashboard/tables - Table status overview
        group.MapGet("/tables", async (RestaurantDbContext db) =>
        {
            var tables = await db.Tables.ToListAsync();
            var total = tables.Count;
            return Results.Ok(new TableStatusSummary(
                total,
                tables.Count(t => t.Status == TableStatus.Available),
                tables.Count(t => t.Status == TableStatus.Occupied),
                tables.Count(t => t.Status == TableStatus.Reserved),
                tables.Count(t => t.Status == TableStatus.NeedsService),
                tables.Count(t => t.Status == TableStatus.Cleaning),
                total > 0 ? tables.Count(t => t.Status == TableStatus.Occupied) * 100 / total : 0
            ));
        })
        .WithName("GetTableDashboard")
        .WithDescription("Get table status summary");
    }
}
