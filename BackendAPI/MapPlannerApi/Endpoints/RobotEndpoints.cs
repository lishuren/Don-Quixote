using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Services;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace MapPlannerApi.Endpoints;

public static class RobotEndpoints
{
    public static void MapRobotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/robots").WithTags("Robots");

        // GET /api/robots - List all robots with optional filters
        group.MapGet("/", async (string? status, int? zoneId, bool? isAvailable, string? sortBy, RobotRepository repo, RestaurantDbContext db) =>
        {
            var query = db.Robots.Include(r => r.CurrentTask).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RobotStatus>(status, true, out var robotStatus))
                query = query.Where(r => r.Status == robotStatus);
            
            if (isAvailable == true)
                query = query.Where(r => r.Status == RobotStatus.Idle && r.IsEnabled);

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "name" => query.OrderBy(r => r.Name),
                "battery" or "battery_level" => query.OrderByDescending(r => r.BatteryLevel),
                "last_seen" or "lastseen" => query.OrderByDescending(r => r.LastUpdated),
                _ => query.OrderBy(r => r.Id)
            };

            var robots = await query.ToListAsync();
            return Results.Ok(robots.Select(ToDto));
        })
        .WithName("GetRobots")
        .WithDescription("Get all robots with optional filters");

        // GET /api/robots/{id} - Get robot by ID
        group.MapGet("/{id:int}", async (int id, RobotRepository repo) =>
        {
            var robot = await repo.GetByIdAsync(id);
            return robot is null 
                ? Results.NotFound(new { error = $"Robot {id} not found" })
                : Results.Ok(ToDto(robot));
        })
        .WithName("GetRobot")
        .WithDescription("Get robot by ID");

        // POST /api/robots - Create one or more robots
        group.MapPost("/", async (HttpRequest http, RobotRepository repo) =>
        {
            string body;
            try
            {
                using var reader = new StreamReader(http.Body);
                body = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Failed to read request body.", details = ex.Message });
            }

            List<CreateRobotRequest>? requests = null;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                requests = JsonSerializer.Deserialize<List<CreateRobotRequest>>(body, opts);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Invalid JSON for CreateRobotRequest[]", details = ex.Message, raw = body });
            }

            if (requests == null || requests.Count == 0)
                return Results.BadRequest(new { error = "Request must be a non-empty array of CreateRobotRequest objects.", raw = body });

            var createdList = new List<Robot>();
            foreach (var request in requests)
            {
                var robot = new Robot
                {
                    Name = string.IsNullOrWhiteSpace(request.Name) ? $"Robot-{Guid.NewGuid().ToString()[..8]}" : request.Name,
                    Model = request.Model,
                    IsEnabled = request.IsEnabled,
                    Position = request.Position?.ToEntity() ?? new Position()
                };

                var created = await repo.CreateAsync(robot);
                createdList.Add(created);
            }

            // Return created robots
            var dtos = createdList.Select(ToDto).ToList();
            return Results.Ok(dtos);
        })
        .WithName("CreateRobots")
        .WithDescription("Create one or more robots");

        // PUT /api/robots/{id} - Update robot
        group.MapPut("/{id:int}", async (int id, UpdateRobotRequest request, RobotRepository repo) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            if (request.Name is not null) robot.Name = request.Name;
            if (request.Model is not null) robot.Model = request.Model;
            if (request.IsEnabled.HasValue) robot.IsEnabled = request.IsEnabled.Value;
            if (request.Status is not null && Enum.TryParse<RobotStatus>(request.Status, true, out var status))
                robot.Status = status;

            var updated = await repo.UpdateAsync(robot);
            return Results.Ok(ToDto(updated));
        })
        .WithName("UpdateRobot")
        .WithDescription("Update a robot");

        // DELETE /api/robots/{id} - Delete robot
        group.MapDelete("/{id:int}", async (int id, RobotRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted 
                ? Results.NoContent() 
                : Results.NotFound(new { error = $"Robot {id} not found" });
        })
        .WithName("DeleteRobot")
        .WithDescription("Delete a robot");

        // GET /api/robots/status/{status} - Get robots by status
        group.MapGet("/status/{status}", async (string status, RobotRepository repo) =>
        {
            if (!Enum.TryParse<RobotStatus>(status, true, out var robotStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var robots = await repo.GetByStatusAsync(robotStatus);
            return Results.Ok(robots.Select(ToDto));
        })
        .WithName("GetRobotsByStatus")
        .WithDescription("Get robots filtered by status");

        // GET /api/robots/idle - Get idle robots available for tasks
        group.MapGet("/idle", async (RobotRepository repo) =>
        {
            var robots = await repo.GetIdleRobotsAsync();
            return Results.Ok(robots.Select(ToDto));
        })
        .WithName("GetIdleRobots")
        .WithDescription("Get idle robots available for task assignment");

        // PUT /api/robots/{id}/position - Update robot position
        group.MapPut("/{id:int}/position", async (int id, RobotPositionUpdate update, RobotRepository repo, ConfigRepository config) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var pixelsPerMeter = await config.GetPixelsPerMeterAsync();
            
            var position = new Position(
                update.ScreenX,
                update.ScreenY,
                update.PhysicalX ?? update.ScreenX / pixelsPerMeter,
                update.PhysicalY ?? update.ScreenY / pixelsPerMeter
            );

            await repo.UpdatePositionAsync(id, position, update.Heading, update.Velocity);
            return Results.Ok(new { message = "Position updated" });
        })
        .WithName("UpdateRobotPosition")
        .WithDescription("Update robot position and heading");

        // PUT /api/robots/{id}/battery - Update robot battery level
        group.MapPut("/{id:int}/battery", async (int id, double level, RobotRepository repo) =>
        {
            if (level < 0 || level > 100)
                return Results.BadRequest(new { error = "Battery level must be between 0 and 100" });

            await repo.UpdateBatteryAsync(id, level);
            return Results.Ok(new { message = "Battery level updated" });
        })
        .WithName("UpdateRobotBattery")
        .WithDescription("Update robot battery level");

        // PUT /api/robots/{id}/status - Update robot status
        group.MapPut("/{id:int}/status", async (int id, string status, RobotRepository repo) =>
        {
            if (!Enum.TryParse<RobotStatus>(status, true, out var robotStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            await repo.UpdateStatusAsync(id, robotStatus);
            return Results.Ok(new { message = "Status updated" });
        })
        .WithName("UpdateRobotStatus")
        .WithDescription("Update robot status");

        // PATCH /api/robots/{id}/command - Send command to robot
        group.MapPatch("/{id:int}/command", async (int id, RobotCommandRequest request, RobotRepository repo, IEventBroadcaster? broadcaster) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var validCommands = new[] { "go_to", "return_home", "pause", "resume", "emergency_stop", "clear_error" };
            if (!validCommands.Contains(request.Command.ToLower()))
                return Results.BadRequest(new { error = $"Invalid command: {request.Command}. Valid: {string.Join(", ", validCommands)}" });

            int? estimatedSeconds = null;
            string? message = null;
            var oldStatus = robot.Status;

            switch (request.Command.ToLower())
            {
                case "go_to":
                    if (request.TargetPosition is null && request.TargetTableId is null)
                        return Results.BadRequest(new { error = "go_to requires TargetPosition or TargetTableId" });
                    robot.Status = RobotStatus.Navigating;
                    estimatedSeconds = 30; // Placeholder estimation
                    message = "Navigation started";
                    break;

                case "return_home":
                    robot.Status = RobotStatus.Returning;
                    estimatedSeconds = 45;
                    message = "Returning to home position";
                    break;

                case "pause":
                    if (robot.Status == RobotStatus.Idle)
                        return Results.BadRequest(new { error = "Robot is already idle" });
                    robot.Status = RobotStatus.Idle;
                    message = "Robot paused";
                    break;

                case "resume":
                    if (robot.CurrentTaskId.HasValue)
                    {
                        robot.Status = RobotStatus.Navigating;
                        message = "Robot resumed";
                    }
                    else
                    {
                        message = "No task to resume, robot remains idle";
                    }
                    break;

                case "emergency_stop":
                    robot.Status = RobotStatus.Error;
                    message = "Emergency stop executed";
                    break;

                case "clear_error":
                    if (robot.Status != RobotStatus.Error)
                        return Results.BadRequest(new { error = "Robot is not in error state" });
                    robot.Status = RobotStatus.Idle;
                    message = "Error cleared";
                    break;
            }

            await repo.UpdateAsync(robot);

            // Broadcast status change if broadcaster available
            if (broadcaster is not null && oldStatus != robot.Status)
            {
                await broadcaster.BroadcastRobotStatusChanged(robot.Id, robot.Name, oldStatus.ToString(), robot.Status.ToString(), request.Reason);
            }

            return Results.Ok(new CommandAcknowledgment(
                robot.Id,
                request.Command,
                true,
                message,
                estimatedSeconds,
                DateTime.UtcNow
            ));
        })
        .WithName("SendRobotCommand")
        .WithDescription("Send command to robot (go_to, return_home, pause, resume, emergency_stop, clear_error)");

        // POST /api/robots/{id}/recovery - Initiate robot recovery
        group.MapPost("/{id:int}/recovery", async (int id, RobotRecoveryRequest request, RobotRepository repo, TaskRepository taskRepo) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var validActions = new[] { "retry_task", "return_home", "manual_intervention" };
            if (!validActions.Contains(request.Action.ToLower()))
                return Results.BadRequest(new { error = $"Invalid action: {request.Action}. Valid: {string.Join(", ", validActions)}" });

            switch (request.Action.ToLower())
            {
                case "retry_task":
                    if (robot.CurrentTaskId.HasValue)
                    {
                        await taskRepo.StartTaskAsync(robot.CurrentTaskId.Value);
                        robot.Status = RobotStatus.Navigating;
                    }
                    else
                    {
                        return Results.BadRequest(new { error = "No current task to retry" });
                    }
                    break;

                case "return_home":
                    robot.Status = RobotStatus.Returning;
                    robot.CurrentTaskId = null;
                    break;

                case "manual_intervention":
                    robot.Status = RobotStatus.Offline;
                    robot.IsEnabled = false;
                    break;
            }

            await repo.UpdateAsync(robot);
            return Results.Ok(new { message = $"Recovery action '{request.Action}' initiated", robotId = id, notes = request.Notes });
        })
        .WithName("InitiateRobotRecovery")
        .WithDescription("Initiate recovery for blocked/error state robot");

        // POST /api/robots/{id}/heartbeat - Robot heartbeat
        group.MapPost("/{id:int}/heartbeat", async (int id, RobotHeartbeatRequest request, RobotRepository repo, ConfigRepository config) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var pixelsPerMeter = await config.GetPixelsPerMeterAsync();
            var position = request.Position.ToEntity();

            robot.BatteryLevel = request.BatteryLevel;
            robot.Position = position;
            robot.Heading = request.Heading;
            robot.Velocity = request.Velocity;
            robot.LastUpdated = DateTime.UtcNow;
            robot.LastHeartbeat = DateTime.UtcNow;

            // Check for error conditions
            if (!string.IsNullOrEmpty(request.ErrorMessage))
            {
                robot.Status = RobotStatus.Error;
            }

            await repo.UpdateAsync(robot);

            // Determine if there's a pending command or task
            string? pendingCommand = null;
            int? assignedTaskId = robot.CurrentTaskId;

            return Results.Ok(new RobotHeartbeatResponse(
                true,
                pendingCommand,
                assignedTaskId,
                DateTime.UtcNow
            ));
        })
        .WithName("RobotHeartbeat")
        .WithDescription("Robot heartbeat with position, battery, and diagnostics");

        // GET /api/robots/{id}/history - Get robot task history
        group.MapGet("/{id:int}/history", async (int id, DateTime? from, DateTime? to, int? limit, RobotRepository repo, RestaurantDbContext db) =>
        {
            var robot = await repo.GetByIdAsync(id);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {id} not found" });

            var query = db.Tasks
                .Include(t => t.TargetTable)
                .Where(t => t.RobotId == id)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            query = query.OrderByDescending(t => t.CreatedAt);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            var tasks = await query.ToListAsync();

            var history = tasks.Select(t => new RobotHistoryEntry(
                t.Id,
                t.Type.ToString(),
                t.Status.ToString(),
                t.StartedAt,
                t.CompletedAt,
                t.ActualDurationSeconds,
                t.TargetTable?.Label
            )).ToList();

            var completedTasks = tasks.Count(t => t.Status == Entities.TaskStatus.Completed);
            var failedTasks = tasks.Count(t => t.Status == Entities.TaskStatus.Failed);
            var avgDuration = tasks.Where(t => t.ActualDurationSeconds.HasValue)
                                   .Select(t => t.ActualDurationSeconds!.Value)
                                   .DefaultIfEmpty(0)
                                   .Average();

            return Results.Ok(new RobotHistoryResponse(
                robot.Id,
                robot.Name,
                history,
                tasks.Count,
                completedTasks,
                failedTasks,
                avgDuration
            ));
        })
        .WithName("GetRobotHistory")
        .WithDescription("Get robot task history with optional date range filter");
    }

    private static RobotDto ToDto(Robot r) => new(
        r.Id,
        r.Name,
        r.Model,
        r.Status.ToString(),
        PositionDto.FromEntity(r.Position),
        r.Heading,
        r.BatteryLevel,
        r.Velocity,
        r.CurrentTaskId,
        r.IsEnabled,
        r.LastUpdated
    );
}
