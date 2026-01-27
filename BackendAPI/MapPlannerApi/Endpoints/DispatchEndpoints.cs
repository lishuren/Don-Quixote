using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Services;

namespace MapPlannerApi.Endpoints;

public static class DispatchEndpoints
{
    public static void MapDispatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dispatch").WithTags("Dispatch");

        // GET /api/dispatch/queue - Get pending dispatch queue
        group.MapGet("/queue", async (IDispatchEngine dispatch) =>
        {
            var queue = await dispatch.GetDispatchQueue();
            return Results.Ok(queue);
        })
        .WithName("GetDispatchQueue")
        .WithDescription("Get pending tasks in dispatch queue with suggested robot assignments");

        // POST /api/dispatch/auto-assign - Trigger automatic task assignment
        group.MapPost("/auto-assign", async (IDispatchEngine dispatch) =>
        {
            var result = await dispatch.AutoAssignPendingTasks();
            return Results.Ok(result);
        })
        .WithName("AutoAssignTasks")
        .WithDescription("Automatically assign pending tasks to available robots");

        // GET /api/dispatch/config - Get dispatch configuration
        group.MapGet("/config", async (IDispatchEngine dispatch) =>
        {
            var config = await dispatch.GetConfig();
            return Results.Ok(config);
        })
        .WithName("GetDispatchConfig")
        .WithDescription("Get dispatch engine configuration");

        // PATCH /api/dispatch/config - Update dispatch configuration
        group.MapPatch("/config", async (DispatchConfig config, IDispatchEngine dispatch) =>
        {
            var validAlgorithms = new[] { "nearest", "round_robin", "load_balanced", "priority" };
            if (!validAlgorithms.Contains(config.Algorithm.ToLower()))
                return Results.BadRequest(new { error = $"Invalid algorithm: {config.Algorithm}. Valid: {string.Join(", ", validAlgorithms)}" });

            if (config.MaxTasksPerRobot < 1 || config.MaxTasksPerRobot > 10)
                return Results.BadRequest(new { error = "MaxTasksPerRobot must be between 1 and 10" });

            if (config.MinBatteryForAssignment < 0 || config.MinBatteryForAssignment > 100)
                return Results.BadRequest(new { error = "MinBatteryForAssignment must be between 0 and 100" });

            await dispatch.UpdateConfig(config);
            return Results.Ok(new { message = "Dispatch config updated", config });
        })
        .WithName("UpdateDispatchConfig")
        .WithDescription("Update dispatch engine configuration");

        // POST /api/dispatch/suggest/{taskId} - Get suggested robot for specific task
        group.MapPost("/suggest/{taskId:int}", async (int taskId, TaskRepository taskRepo, IDispatchEngine dispatch) =>
        {
            var task = await taskRepo.GetByIdAsync(taskId);
            if (task is null)
                return Results.NotFound(new { error = $"Task {taskId} not found" });

            if (task.Status != Entities.TaskStatus.Pending)
                return Results.BadRequest(new { error = "Task is not pending" });

            var robotId = await dispatch.FindBestRobotForTask(task);
            
            if (!robotId.HasValue)
                return Results.Ok(new { taskId, suggestedRobotId = (int?)null, reason = "No available robots" });

            return Results.Ok(new { taskId, suggestedRobotId = robotId.Value, reason = "Best match based on current algorithm" });
        })
        .WithName("SuggestRobotForTask")
        .WithDescription("Get suggested robot for a specific task");
    }
}
