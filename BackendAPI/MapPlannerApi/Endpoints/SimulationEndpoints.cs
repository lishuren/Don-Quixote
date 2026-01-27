using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MapPlannerApi.Endpoints;

public static class SimulationEndpoints
{
    // In-memory simulation sessions (in production, use a proper store)
    private static readonly ConcurrentDictionary<string, SimulationSession> _sessions = new();

    public static void MapSimulationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/simulation").WithTags("Simulation");

        // GET /api/simulation/sessions - List all simulation sessions
        group.MapGet("/sessions", () =>
        {
            var sessions = _sessions.Values.Select(s => new SimulationSessionDto(
                s.SessionId,
                s.Status,
                s.TimeScale,
                s.StartedAt,
                s.SpawnedGuests,
                s.CompletedTasks
            ));
            return Results.Ok(sessions);
        })
        .WithName("ListSimulationSessions")
        .WithDescription("List all simulation sessions");

        // POST /api/simulation/sessions - Create new simulation session
        group.MapPost("/sessions", (CreateSimulationRequest request) =>
        {
            var sessionId = Guid.NewGuid().ToString("N")[..8];
            var session = new SimulationSession
            {
                SessionId = sessionId,
                Status = "Running",
                TimeScale = request.TimeScale,
                StartedAt = DateTime.UtcNow,
                AutoSpawnGuests = request.AutoSpawnGuests,
                SpawnIntervalSeconds = request.SpawnIntervalSeconds
            };

            _sessions[sessionId] = session;

            return Results.Created($"/api/simulation/sessions/{sessionId}", new SimulationSessionDto(
                session.SessionId,
                session.Status,
                session.TimeScale,
                session.StartedAt,
                session.SpawnedGuests,
                session.CompletedTasks
            ));
        })
        .WithName("CreateSimulationSession")
        .WithDescription("Create a new simulation session");

        // GET /api/simulation/sessions/{sessionId} - Get session details
        group.MapGet("/sessions/{sessionId}", (string sessionId) =>
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            return Results.Ok(new SimulationSessionDto(
                session.SessionId,
                session.Status,
                session.TimeScale,
                session.StartedAt,
                session.SpawnedGuests,
                session.CompletedTasks
            ));
        })
        .WithName("GetSimulationSession")
        .WithDescription("Get simulation session details");

        // DELETE /api/simulation/sessions/{sessionId} - Stop and delete session
        group.MapDelete("/sessions/{sessionId}", (string sessionId) =>
        {
            if (!_sessions.TryRemove(sessionId, out _))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            return Results.NoContent();
        })
        .WithName("DeleteSimulationSession")
        .WithDescription("Stop and delete simulation session");

        // PATCH /api/simulation/sessions/{sessionId}/pause - Pause simulation
        group.MapPatch("/sessions/{sessionId}/pause", (string sessionId) =>
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            session.Status = "Paused";
            return Results.Ok(new { message = "Simulation paused", sessionId });
        })
        .WithName("PauseSimulation")
        .WithDescription("Pause simulation session");

        // PATCH /api/simulation/sessions/{sessionId}/resume - Resume simulation
        group.MapPatch("/sessions/{sessionId}/resume", (string sessionId) =>
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            session.Status = "Running";
            return Results.Ok(new { message = "Simulation resumed", sessionId });
        })
        .WithName("ResumeSimulation")
        .WithDescription("Resume simulation session");

        // PATCH /api/simulation/time-scale - Update time scale
        group.MapPatch("/time-scale", (string sessionId, double timeScale) =>
        {
            if (timeScale < 0.1 || timeScale > 10)
                return Results.BadRequest(new { error = "Time scale must be between 0.1 and 10" });

            if (!_sessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            session.TimeScale = timeScale;
            return Results.Ok(new { message = $"Time scale set to {timeScale}x", sessionId });
        })
        .WithName("UpdateTimeScale")
        .WithDescription("Update simulation time scale (0.1x to 10x)");

        // POST /api/simulation/spawn-guest - Spawn simulated guest
        group.MapPost("/spawn-guest", async (SpawnGuestRequest request, string? sessionId, GuestRepository guestRepo) =>
        {
            var guest = new Guest
            {
                Name = request.Name ?? $"SimGuest_{DateTime.UtcNow.Ticks % 10000}",
                PartySize = request.PartySize,
                Status = GuestStatus.Waiting,
                Notes = request.Preferences != null ? string.Join(", ", request.Preferences) : null
            };

            var created = await guestRepo.CreateAsync(guest);

            // Update session stats if applicable
            if (!string.IsNullOrEmpty(sessionId) && _sessions.TryGetValue(sessionId, out var session))
            {
                session.SpawnedGuests++;
            }

            return Results.Created($"/api/guests/{created.Id}", new
            {
                guestId = created.Id,
                name = created.Name,
                partySize = created.PartySize,
                status = created.Status.ToString()
            });
        })
        .WithName("SpawnSimulatedGuest")
        .WithDescription("Spawn a simulated guest");

        // PATCH /api/simulation/robots/{id}/position - Move robot position
        group.MapPatch("/robots/{id:int}/position", async (int id, PositionDto position, RobotRepository repo, IEventBroadcaster? broadcaster) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            await repo.UpdatePositionAsync(id, position.ToEntity(), robot.Heading, robot.Velocity);

            if (broadcaster is not null)
            {
                await broadcaster.BroadcastRobotPositionUpdated(robot.Id, robot.Name, position, robot.Heading, robot.Velocity);
            }

            return Results.Ok(new { message = "Position updated", robotId = id, position });
        })
        .WithName("SimulateRobotPosition")
        .WithDescription("Simulate robot position change");

        // PATCH /api/simulation/robots/{id}/battery - Set robot battery
        group.MapPatch("/robots/{id:int}/battery", async (int id, double level, RobotRepository repo) =>
        {
            if (level < 0 || level > 100)
                return Results.BadRequest(new { error = "Battery level must be between 0 and 100" });

            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            await repo.UpdateBatteryAsync(id, level);

            return Results.Ok(new { message = "Battery updated", robotId = id, batteryLevel = level });
        })
        .WithName("SimulateRobotBattery")
        .WithDescription("Simulate robot battery level change");

        // POST /api/simulation/robots/{id}/trigger-error - Trigger robot error
        group.MapPost("/robots/{id:int}/trigger-error", async (int id, string? errorType, RobotRepository repo, AlertRepository alertRepo, IEventBroadcaster? broadcaster) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var oldStatus = robot.Status;
            robot.Status = RobotStatus.Error;
            await repo.UpdateAsync(robot);

            // Create an alert for the error
            var alert = new Alert
            {
                Type = AlertType.RobotError,
                Severity = AlertSeverity.Error,
                Title = $"Simulated error on {robot.Name}",
                Message = errorType ?? "Simulated error for testing",
                RobotId = robot.Id
            };
            await alertRepo.CreateAsync(alert);

            if (broadcaster is not null)
            {
                await broadcaster.BroadcastRobotStatusChanged(robot.Id, robot.Name, oldStatus.ToString(), "Error", errorType);
                await broadcaster.BroadcastAlertCreated(alert.Id, alert.Type.ToString(), alert.Severity.ToString(), alert.Title, alert.Message, robot.Id);
            }

            return Results.Ok(new { message = "Error triggered", robotId = id, errorType, alertId = alert.Id });
        })
        .WithName("TriggerRobotError")
        .WithDescription("Trigger a simulated error on robot");

        // GET /api/simulation/metrics - Get simulation metrics
        group.MapGet("/metrics", async (string sessionId, RestaurantDbContext db) =>
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = $"Session {sessionId} not found" });

            var elapsed = DateTime.UtcNow - session.StartedAt;

            // Calculate metrics from database
            var completedTasks = await db.Tasks
                .Where(t => t.Status == Entities.TaskStatus.Completed && t.CompletedAt >= session.StartedAt)
                .ToListAsync();

            var seatedGuests = await db.Guests
                .Where(g => g.SeatedTime >= session.StartedAt)
                .CountAsync();

            // Calculate average wait time in minutes
            var guestsWithWait = await db.Guests
                .Where(g => g.SeatedTime.HasValue && g.ArrivalTime >= session.StartedAt)
                .Select(g => new { g.ArrivalTime, g.SeatedTime })
                .ToListAsync();
            
            var avgWaitTime = guestsWithWait.Count > 0 
                ? guestsWithWait.Average(g => (g.SeatedTime!.Value - g.ArrivalTime).TotalMinutes)
                : 0;

            var avgTaskDuration = completedTasks
                .Where(t => t.ActualDurationSeconds.HasValue)
                .Select(t => t.ActualDurationSeconds!.Value)
                .DefaultIfEmpty(0)
                .Average();

            var totalRobots = await db.Robots.CountAsync(r => r.IsEnabled);
            var busyRobots = await db.Robots.CountAsync(r => r.Status != RobotStatus.Idle && r.Status != RobotStatus.Offline);
            var utilization = totalRobots > 0 ? (double)busyRobots / totalRobots * 100 : 0;

            var totalTables = await db.Tables.CountAsync();
            var occupiedTables = await db.Tables.CountAsync(t => t.Status == TableStatus.Occupied);
            var turnoverRate = totalTables > 0 ? (double)seatedGuests / totalTables : 0;

            session.CompletedTasks = completedTasks.Count;

            return Results.Ok(new SimulationMetrics(
                sessionId,
                elapsed,
                seatedGuests,
                completedTasks.Count,
                avgWaitTime,
                avgTaskDuration,
                utilization,
                turnoverRate
            ));
        })
        .WithName("GetSimulationMetrics")
        .WithDescription("Get metrics for simulation session");

        // POST /api/simulation/scenarios/{name} - Load predefined scenario
        group.MapPost("/scenarios/{name}", async (string name, RestaurantDbContext db, RobotRepository robotRepo, TableRepository tableRepo) =>
        {
            switch (name.ToLower())
            {
                case "busy_lunch":
                    // Create scenario: multiple guests, all robots active
                    var robots = await db.Robots.ToListAsync();
                    foreach (var robot in robots.Take(3))
                    {
                        robot.Status = RobotStatus.Navigating;
                    }
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = "Loaded 'busy_lunch' scenario", robotsActive = Math.Min(robots.Count, 3) });

                case "quiet_morning":
                    // Reset to idle state
                    var allRobots = await db.Robots.ToListAsync();
                    foreach (var robot in allRobots)
                    {
                        robot.Status = RobotStatus.Idle;
                        robot.BatteryLevel = 100;
                    }
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = "Loaded 'quiet_morning' scenario" });

                case "robot_failure":
                    // Simulate a robot failure
                    var firstRobot = await db.Robots.FirstOrDefaultAsync();
                    if (firstRobot != null)
                    {
                        firstRobot.Status = RobotStatus.Error;
                        firstRobot.BatteryLevel = 5;
                        await db.SaveChangesAsync();
                        return Results.Ok(new { message = "Loaded 'robot_failure' scenario", affectedRobot = firstRobot.Id });
                    }
                    return Results.BadRequest(new { error = "No robots available for scenario" });

                default:
                    return Results.NotFound(new { error = $"Scenario '{name}' not found. Available: busy_lunch, quiet_morning, robot_failure" });
            }
        })
        .WithName("LoadSimulationScenario")
        .WithDescription("Load a predefined simulation scenario");
    }

    // Internal class to track simulation sessions
    private class SimulationSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = "Running";
        public double TimeScale { get; set; } = 1.0;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public bool AutoSpawnGuests { get; set; }
        public int SpawnIntervalSeconds { get; set; } = 60;
        public int SpawnedGuests { get; set; }
        public int CompletedTasks { get; set; }
    }
}
