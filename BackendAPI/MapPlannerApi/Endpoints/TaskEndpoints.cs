using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        // GET /api/tasks - List all tasks
        group.MapGet("/", async (int? limit, TaskRepository repo) =>
        {
            var tasks = await repo.GetAllAsync(limit);
            return Results.Ok(tasks.Select(ToDto));
        })
        .WithName("GetTasks")
        .WithDescription("Get all tasks");

        // GET /api/tasks/{id} - Get task by ID
        group.MapGet("/{id:int}", async (int id, TaskRepository repo) =>
        {
            var task = await repo.GetByIdAsync(id);
            return task is null
                ? Results.NotFound(new { error = $"Task {id} not found" })
                : Results.Ok(ToDto(task));
        })
        .WithName("GetTask")
        .WithDescription("Get task by ID");

        // POST /api/tasks - Create a new task
        group.MapPost("/", async (CreateTaskRequest request, TaskRepository repo) =>
        {
            if (!Enum.TryParse<TaskType>(request.Type, true, out var taskType))
                return Results.BadRequest(new { error = $"Invalid task type: {request.Type}" });

            Enum.TryParse<TaskPriority>(request.Priority, true, out var priority);

            var task = new RobotTask
            {
                Type = taskType,
                Priority = priority,
                RobotId = request.RobotId,
                TargetTableId = request.TargetTableId,
                StartPosition = request.StartPosition?.ToEntity() ?? new Position(),
                TargetPosition = request.TargetPosition?.ToEntity() ?? new Position(),
                Status = request.RobotId.HasValue ? TaskStatus.Assigned : TaskStatus.Pending
            };

            var created = await repo.CreateAsync(task);
            return Results.Created($"/api/tasks/{created.Id}", ToDto(created));
        })
        .WithName("CreateTask")
        .WithDescription("Create a new task");

        // DELETE /api/tasks/{id} - Delete/Cancel task
        group.MapDelete("/{id:int}", async (int id, TaskRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Task {id} not found" });
        })
        .WithName("DeleteTask")
        .WithDescription("Delete a task");

        // GET /api/tasks/status/{status} - Get tasks by status
        group.MapGet("/status/{status}", async (string status, TaskRepository repo) =>
        {
            if (!Enum.TryParse<TaskStatus>(status, true, out var taskStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var tasks = await repo.GetByStatusAsync(taskStatus);
            return Results.Ok(tasks.Select(ToDto));
        })
        .WithName("GetTasksByStatus")
        .WithDescription("Get tasks filtered by status");

        // GET /api/tasks/pending - Get pending tasks queue
        group.MapGet("/pending", async (TaskRepository repo) =>
        {
            var tasks = await repo.GetPendingTasksAsync();
            return Results.Ok(tasks.Select(ToDto));
        })
        .WithName("GetPendingTasks")
        .WithDescription("Get pending tasks queue");

        // GET /api/tasks/robot/{robotId} - Get tasks for a robot
        group.MapGet("/robot/{robotId:int}", async (int robotId, TaskRepository repo) =>
        {
            var tasks = await repo.GetByRobotAsync(robotId);
            return Results.Ok(tasks.Select(ToDto));
        })
        .WithName("GetTasksByRobot")
        .WithDescription("Get tasks assigned to a robot");

        // POST /api/tasks/{id}/assign - Assign task to robot
        group.MapPost("/{id:int}/assign", async (int id, int robotId, TaskRepository taskRepo, RobotRepository robotRepo) =>
        {
            var task = await taskRepo.GetByIdAsync(id);
            if (task is null)
                return Results.NotFound(new { error = $"Task {id} not found" });

            var robot = await robotRepo.GetByIdAsync(robotId);
            if (robot is null)
                return Results.NotFound(new { error = $"Robot {robotId} not found" });

            await taskRepo.AssignToRobotAsync(id, robotId);
            return Results.Ok(new { message = $"Task {id} assigned to robot {robotId}" });
        })
        .WithName("AssignTask")
        .WithDescription("Assign a task to a robot");

        // POST /api/tasks/{id}/start - Start task execution
        group.MapPost("/{id:int}/start", async (int id, TaskRepository repo) =>
        {
            var task = await repo.GetByIdAsync(id);
            if (task is null)
                return Results.NotFound(new { error = $"Task {id} not found" });

            if (task.Status != TaskStatus.Assigned)
                return Results.BadRequest(new { error = "Task must be assigned before starting" });

            await repo.StartTaskAsync(id);
            return Results.Ok(new { message = $"Task {id} started" });
        })
        .WithName("StartTask")
        .WithDescription("Start task execution");

        // POST /api/tasks/{id}/complete - Complete task
        group.MapPost("/{id:int}/complete", async (int id, TaskRepository repo) =>
        {
            var task = await repo.GetByIdAsync(id);
            if (task is null)
                return Results.NotFound(new { error = $"Task {id} not found" });

            await repo.CompleteTaskAsync(id);
            return Results.Ok(new { message = $"Task {id} completed" });
        })
        .WithName("CompleteTask")
        .WithDescription("Mark task as completed");

        // POST /api/tasks/{id}/fail - Mark task as failed
        group.MapPost("/{id:int}/fail", async (int id, string? error, TaskRepository repo) =>
        {
            var task = await repo.GetByIdAsync(id);
            if (task is null)
                return Results.NotFound(new { error = $"Task {id} not found" });

            await repo.FailTaskAsync(id, error);
            return Results.Ok(new { message = $"Task {id} marked as failed" });
        })
        .WithName("FailTask")
        .WithDescription("Mark task as failed");

        // POST /api/tasks/{id}/unassign - Unassign task from robot and return to queue
        group.MapPost("/{id:int}/unassign", async (int id, TaskRepository repo, RobotRepository robotRepo, RestaurantDbContext db) =>
        {
            var task = await repo.GetByIdAsync(id);
            if (task is null)
                return Results.NotFound(new { error = $"Task {id} not found" });

            if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Cancelled)
                return Results.BadRequest(new { error = $"Cannot unassign task with status {task.Status}" });

            var previousRobotId = task.RobotId;
            
            // Reset task to pending
            task.RobotId = null;
            task.Status = TaskStatus.Pending;
            task.AssignedAt = null;
            
            // Clear robot's current task reference if it was assigned
            if (previousRobotId.HasValue)
            {
                var robot = await robotRepo.GetByIdAsync(previousRobotId.Value);
                if (robot != null && robot.CurrentTaskId == id)
                {
                    robot.CurrentTaskId = null;
                    robot.Status = RobotStatus.Idle;
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { 
                message = $"Task {id} unassigned and returned to queue",
                previousRobotId
            });
        })
        .WithName("UnassignTask")
        .WithDescription("Unassign task from robot and return to pending queue");

        // GET /api/tasks/queue/summary - Get queue summary
        group.MapGet("/queue/summary", async (TaskRepository repo, RestaurantDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var pending = await db.Tasks.CountAsync(t => t.Status == TaskStatus.Pending);
            var assigned = await db.Tasks.CountAsync(t => t.Status == TaskStatus.Assigned);
            var inProgress = await db.Tasks.CountAsync(t => t.Status == TaskStatus.InProgress);
            var completedToday = await db.Tasks.CountAsync(t => 
                t.Status == TaskStatus.Completed && t.CompletedAt >= today);
            var failedToday = await db.Tasks.CountAsync(t => 
                t.Status == TaskStatus.Failed && t.CompletedAt >= today);

            return Results.Ok(new TaskQueueSummary(pending, assigned, inProgress, completedToday, failedToday));
        })
        .WithName("GetTaskQueueSummary")
        .WithDescription("Get task queue summary");
    }

    private static TaskDto ToDto(RobotTask t) => new(
        t.Id,
        t.Type.ToString(),
        t.Status.ToString(),
        t.Priority.ToString(),
        t.RobotId,
        t.Robot?.Name,
        t.TargetTableId,
        t.TargetTable?.Label,
        PositionDto.FromEntity(t.StartPosition),
        PositionDto.FromEntity(t.TargetPosition),
        t.EstimatedDurationSeconds,
        t.ActualDurationSeconds,
        t.ErrorMessage,
        t.RetryCount,
        t.CreatedAt,
        t.StartedAt,
        t.CompletedAt
    );
}
