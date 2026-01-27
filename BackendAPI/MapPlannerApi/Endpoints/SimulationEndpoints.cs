using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Services;
using MapPlannerApi.Services.Simulation;
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

        // ========== Long-Run Simulation Endpoints ==========
        // These endpoints support time-accelerated simulations (e.g., 1 hour real time = 1 month simulated)

        // POST /api/simulation/long-run - Start a long-running accelerated simulation
        group.MapPost("/long-run", async (StartSimulationRequest request, ISimulationEngine engine) =>
        {
            if (engine.State == SimulationState.Running)
            {
                return Results.Conflict(new { error = "A simulation is already running. Stop it first." });
            }

            var startTime = request.SimulatedStartTime ?? DateTime.Today.AddHours(7); // Default: 7 AM today
            var endTime = request.SimulatedEndTime ?? startTime.AddDays(30); // Default: 30 days

            // Validate time range
            if (endTime <= startTime)
            {
                return Results.BadRequest(new { error = "SimulatedEndTime must be after SimulatedStartTime" });
            }

            // Build event pattern config
            EventPatternConfig? eventPatterns = null;
            if (request.EventPatterns != null)
            {
                eventPatterns = new EventPatternConfig
                {
                    HourlyArrivalRates = request.EventPatterns.HourlyArrivalRates ?? new EventPatternConfig().HourlyArrivalRates,
                    DayOfWeekMultipliers = request.EventPatterns.DayOfWeekMultipliers ?? new EventPatternConfig().DayOfWeekMultipliers,
                    AverageMealDurationMinutes = request.EventPatterns.AverageMealDurationMinutes,
                    GuestNeedsHelpProbability = request.EventPatterns.GuestNeedsHelpProbability,
                    AveragePartySize = request.EventPatterns.AveragePartySize
                };
            }

            var config = new SimulationConfig
            {
                SimulatedStartTime = startTime,
                SimulatedEndTime = endTime,
                AccelerationFactor = request.AccelerationFactor,
                RobotCount = request.RobotCount,
                TableCount = request.TableCount,
                RandomSeed = request.RandomSeed,
                EventPatterns = eventPatterns
            };

            var simulationId = await engine.StartSimulation(config);

            // Estimate real duration
            var simulatedDuration = endTime - startTime;
            var estimatedRealDuration = TimeSpan.FromTicks((long)(simulatedDuration.Ticks / config.AccelerationFactor));

            // Get initial progress to get event count
            var progress = engine.GetProgress();

            return Results.Ok(new StartSimulationResponse(
                SimulationId: simulationId,
                Status: "Running",
                SimulatedStartTime: startTime,
                SimulatedEndTime: endTime,
                AccelerationFactor: config.AccelerationFactor,
                EstimatedEventsCount: progress.TotalEventsScheduled,
                EstimatedRealDuration: estimatedRealDuration
            ));
        })
        .WithName("StartLongRunSimulation")
        .WithDescription("Start a long-running time-accelerated simulation (e.g., 720x = 1 month in ~1 hour)");

        // GET /api/simulation/long-run/progress - Get progress of active long-run simulation
        group.MapGet("/long-run/progress", (ISimulationEngine engine) =>
        {
            if (engine.State == SimulationState.NotStarted)
            {
                return Results.NotFound(new { error = "No simulation is running" });
            }

            var progress = engine.GetProgress();

            return Results.Ok(new SimulationProgressDto(
                SimulationId: progress.SimulationId,
                State: progress.State.ToString(),
                SimulatedStartTime: progress.SimulatedStartTime,
                SimulatedEndTime: progress.SimulatedEndTime,
                CurrentSimulatedTime: progress.CurrentSimulatedTime,
                ProgressPercent: Math.Round(progress.ProgressPercent, 2),
                RealElapsedTime: FormatDuration(progress.RealElapsedTime),
                EstimatedTimeRemaining: FormatDuration(progress.EstimatedTimeRemaining),
                AccelerationFactor: progress.AccelerationFactor,
                EventsProcessed: progress.EventsProcessed,
                TotalEventsScheduled: progress.TotalEventsScheduled,
                GuestsProcessed: progress.GuestsProcessed,
                TasksCreated: progress.TasksCreated,
                TasksCompleted: progress.TasksCompleted,
                CurrentSuccessRate: Math.Round(progress.CurrentSuccessRate, 2)
            ));
        })
        .WithName("GetLongRunProgress")
        .WithDescription("Get progress of the active long-run simulation");

        // POST /api/simulation/long-run/pause - Pause the long-run simulation
        group.MapPost("/long-run/pause", async (ISimulationEngine engine) =>
        {
            if (engine.State != SimulationState.Running)
            {
                return Results.BadRequest(new { error = "No running simulation to pause" });
            }

            await engine.PauseSimulation();
            return Results.Ok(new { message = "Simulation paused", state = "Paused" });
        })
        .WithName("PauseLongRunSimulation")
        .WithDescription("Pause the active long-run simulation");

        // POST /api/simulation/long-run/resume - Resume a paused simulation
        group.MapPost("/long-run/resume", async (ISimulationEngine engine) =>
        {
            if (engine.State != SimulationState.Paused)
            {
                return Results.BadRequest(new { error = "No paused simulation to resume" });
            }

            await engine.ResumeSimulation();
            return Results.Ok(new { message = "Simulation resumed", state = "Running" });
        })
        .WithName("ResumeLongRunSimulation")
        .WithDescription("Resume a paused long-run simulation");

        // POST /api/simulation/long-run/stop - Stop the long-run simulation
        group.MapPost("/long-run/stop", async (ISimulationEngine engine) =>
        {
            if (engine.State == SimulationState.NotStarted)
            {
                return Results.BadRequest(new { error = "No simulation is running" });
            }

            await engine.StopSimulation();
            return Results.Ok(new { message = "Simulation stopped", state = "Cancelled" });
        })
        .WithName("StopLongRunSimulation")
        .WithDescription("Stop the active long-run simulation");

        // GET /api/simulation/long-run/report - Get the report from a completed simulation
        group.MapGet("/long-run/report", (ISimulationEngine engine) =>
        {
            var report = engine.GetReport();
            if (report == null)
            {
                if (engine.State == SimulationState.Running)
                {
                    return Results.BadRequest(new { error = "Simulation is still running. Wait for completion." });
                }
                return Results.NotFound(new { error = "No simulation report available" });
            }

            var summary = new SimulationReportSummaryDto(
                SimulationId: report.SimulationId,
                State: "Completed",
                SimulatedStartTime: report.SimulatedStartTime,
                SimulatedEndTime: report.SimulatedEndTime,
                SimulatedDuration: FormatDuration(report.SimulatedDuration),
                RealDuration: FormatDuration(report.RealDuration),
                AccelerationFactor: report.AccelerationFactor,
                TotalGuests: report.TotalGuests,
                TotalTasks: report.TotalTasks,
                TotalDeliveries: report.TotalDeliveries,
                TotalFailures: report.TotalFailures,
                OverallSuccessRate: Math.Round(report.OverallSuccessRate, 2),
                AverageTaskDurationSeconds: Math.Round(report.AverageTaskDurationSeconds, 2),
                AverageRobotUtilization: Math.Round(report.AverageRobotUtilization, 2),
                PeakGuestCount: report.PeakGuestCount,
                PeakGuestTime: report.PeakGuestTime,
                TotalAlerts: report.TotalAlerts,
                AlertsByType: report.AlertsByType
            );

            var dailyBreakdown = report.DailyBreakdown.Select(d => new SimulationDailyMetricsDto(
                Date: d.Date,
                DayOfWeek: d.DayOfWeek.ToString(),
                TotalGuests: d.TotalGuests,
                TotalTasks: d.TotalTasks,
                TotalDeliveries: d.TotalDeliveries,
                TotalFailures: d.TotalFailures,
                SuccessRate: Math.Round(d.SuccessRate, 2),
                PeakHour: d.PeakHour,
                PeakHourGuests: d.PeakHourGuests,
                AverageRobotUtilization: Math.Round(d.AverageRobotUtilization, 2)
            )).ToList();

            var robotMetrics = report.RobotMetrics.Values.Select(r => new SimulationRobotMetricsDto(
                RobotId: r.RobotId,
                RobotName: r.RobotName,
                TasksCompleted: r.TasksCompleted,
                TasksFailed: r.TasksFailed,
                SuccessRate: Math.Round(r.SuccessRate, 2),
                AverageTaskDuration: Math.Round(r.AverageTaskDuration, 2),
                UtilizationPercent: Math.Round(r.UtilizationPercent, 2),
                AverageBattery: Math.Round(r.AverageBattery, 2)
            )).ToList();

            return Results.Ok(new SimulationReportDto(
                Summary: summary,
                DailyBreakdown: dailyBreakdown,
                RobotMetrics: robotMetrics
            ));
        })
        .WithName("GetLongRunReport")
        .WithDescription("Get the detailed report from a completed long-run simulation");

        // GET /api/simulation/long-run/acceleration-presets - Get acceleration presets
        group.MapGet("/long-run/acceleration-presets", () =>
        {
            var presets = new[]
            {
                new { name = "RealTime", factor = 1.0, description = "1x speed - Real time" },
                new { name = "Fast", factor = 10.0, description = "10x speed - 1 hour = 6 minutes" },
                new { name = "VeryFast", factor = 60.0, description = "60x speed - 1 hour = 1 minute" },
                new { name = "Daily", factor = 144.0, description = "144x speed - 1 day = 10 minutes" },
                new { name = "Weekly", factor = 1008.0, description = "1008x speed - 1 week = 10 minutes" },
                new { name = "Monthly", factor = 720.0, description = "720x speed - 1 month ≈ 1 hour" },
                new { name = "Yearly", factor = 8640.0, description = "8640x speed - 1 year ≈ 1 hour" }
            };
            return Results.Ok(presets);
        })
        .WithName("GetAccelerationPresets")
        .WithDescription("Get predefined acceleration factor presets for long-run simulations");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
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
