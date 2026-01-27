using MapPlannerApi.Data;
using MapPlannerApi.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Services;

/// <summary>
/// Background service that monitors robot health and handles failover scenarios.
/// Automatically detects blocked/unresponsive robots and reassigns their tasks.
/// </summary>
public class RobotMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RobotMonitorService> _logger;
    
    // Configuration
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _blockedTimeout = TimeSpan.FromMinutes(2);
    private readonly int _lowBatteryThreshold = 15;

    public RobotMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<RobotMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Robot Monitor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckRobotHealth(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in robot monitor service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Robot Monitor Service stopped");
    }

    private async Task CheckRobotHealth(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var eventTrigger = scope.ServiceProvider.GetRequiredService<IEventTriggerService>();

        var robots = await db.Robots
            .Include(r => r.CurrentTask)
            .ToListAsync(ct);

        foreach (var robot in robots)
        {
            // Skip robots already in error/offline state
            if (robot.Status == RobotStatus.Error || robot.Status == RobotStatus.Offline)
                continue;

            // Check for heartbeat timeout (robot unresponsive)
            if (robot.LastHeartbeat.HasValue && 
                DateTime.UtcNow - robot.LastHeartbeat.Value > _heartbeatTimeout)
            {
                _logger.LogWarning("Robot {RobotId} ({Name}) missed heartbeat - marking as unresponsive", 
                    robot.Id, robot.Name);
                
                await HandleUnresponsiveRobot(db, robot, eventTrigger);
                continue;
            }

            // Check for blocked robot (navigating too long)
            if (robot.Status == RobotStatus.Navigating && 
                robot.CurrentTaskId.HasValue &&
                robot.CurrentTask != null &&
                robot.CurrentTask.AssignedAt.HasValue)
            {
                var taskDuration = DateTime.UtcNow - robot.CurrentTask.AssignedAt.Value;
                if (taskDuration > _blockedTimeout)
                {
                    _logger.LogWarning("Robot {RobotId} ({Name}) potentially blocked - task taking too long", 
                        robot.Id, robot.Name);
                    
                    await eventTrigger.TriggerEvent(new TriggerEventRequest(
                        TriggerEventType.RobotBlocked,
                        RobotId: robot.Id,
                        Notes: $"Task {robot.CurrentTaskId} running for {taskDuration.TotalMinutes:F1} minutes"
                    ));
                }
            }

            // Check for low battery
            if (robot.BatteryLevel <= _lowBatteryThreshold && 
                robot.Status != RobotStatus.Charging)
            {
                _logger.LogWarning("Robot {RobotId} ({Name}) has low battery: {Level}%", 
                    robot.Id, robot.Name, robot.BatteryLevel);
                
                await eventTrigger.TriggerEvent(new TriggerEventRequest(
                    TriggerEventType.RobotLowBattery,
                    RobotId: robot.Id
                ));
            }
        }

        // Check for stale pending tasks that need reassignment
        await CheckStaleTasks(db, ct);
    }

    private async Task HandleUnresponsiveRobot(
        RestaurantDbContext db, 
        Robot robot, 
        IEventTriggerService eventTrigger)
    {
        var previousStatus = robot.Status;
        robot.Status = RobotStatus.Error;

        // If robot had an active task, reassign it
        if (robot.CurrentTaskId.HasValue)
        {
            var task = await db.Tasks.FindAsync(robot.CurrentTaskId.Value);
            if (task != null && (task.Status == TaskStatus.InProgress || task.Status == TaskStatus.Assigned))
            {
                task.RetryCount++;
                
                // Check if task exceeded max retries
                if (task.MaxRetryCount > 0 && task.RetryCount >= task.MaxRetryCount)
                {
                    task.Status = TaskStatus.Failed;
                    task.ErrorMessage = $"Max retries ({task.MaxRetryCount}) exceeded after robot failover";
                    task.CompletedAt = DateTime.UtcNow;
                    
                    _logger.LogWarning("Task {TaskId} failed - max retries exceeded", task.Id);
                    
                    // Create alert for failed task
                    db.Alerts.Add(new Alert
                    {
                        Type = AlertType.TaskFailed,
                        Severity = AlertSeverity.Error,
                        Title = $"Task {task.Id} Failed",
                        Message = $"Task exceeded max retry limit ({task.MaxRetryCount}) and has been marked as failed",
                        TaskId = task.Id
                    });
                }
                else
                {
                    // Return to pending queue for reassignment
                    task.RobotId = null;
                    task.Status = TaskStatus.Pending;
                    
                    _logger.LogInformation("Task {TaskId} unassigned from unresponsive robot {RobotId} (retry {Retry}/{Max})", 
                        task.Id, robot.Id, task.RetryCount, task.MaxRetryCount);
                }
            }
        }

        robot.CurrentTaskId = null;
        await db.SaveChangesAsync();

        // Trigger error event for alerting
        await eventTrigger.TriggerEvent(new TriggerEventRequest(
            TriggerEventType.RobotError,
            RobotId: robot.Id,
            Notes: $"Robot unresponsive (heartbeat timeout). Previous status: {previousStatus}"
        ));
    }

    private async Task CheckStaleTasks(RestaurantDbContext db, CancellationToken ct)
    {
        // Find pending tasks older than 5 minutes without assignment
        var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
        var escalationThreshold = DateTime.UtcNow.AddMinutes(-3);
        
        var staleTasks = await db.Tasks
            .Where(t => t.Status == TaskStatus.Pending && 
                       !t.RobotId.HasValue &&
                       t.CreatedAt < staleThreshold)
            .ToListAsync(ct);

        // Escalate priority of tasks older than 3 minutes
        var tasksToEscalate = await db.Tasks
            .Where(t => t.Status == TaskStatus.Pending &&
                       !t.RobotId.HasValue &&
                       t.CreatedAt < escalationThreshold &&
                       t.Priority < TaskPriority.Urgent)
            .ToListAsync(ct);

        foreach (var task in tasksToEscalate)
        {
            var oldPriority = task.Priority;
            task.Priority = task.Priority switch
            {
                TaskPriority.Low => TaskPriority.Normal,
                TaskPriority.Normal => TaskPriority.High,
                TaskPriority.High => TaskPriority.Urgent,
                _ => task.Priority
            };

            if (task.Priority != oldPriority)
            {
                _logger.LogInformation("Escalated task {TaskId} priority from {Old} to {New} due to age", 
                    task.Id, oldPriority, task.Priority);
            }
        }

        if (staleTasks.Count > 0)
        {
            _logger.LogWarning("Found {Count} stale pending tasks without assignment", staleTasks.Count);
            
            // Create alerts for stale high-priority tasks
            foreach (var task in staleTasks.Where(t => t.Priority >= TaskPriority.High))
            {
                // Check if alert already exists for this task
                var existingAlert = await db.Alerts
                    .AnyAsync(a => a.TaskId == task.Id && a.Type == AlertType.TaskOverdue && !a.IsAcknowledged, ct);
                
                if (!existingAlert)
                {
                    var alert = new Alert
                    {
                        Type = AlertType.TaskOverdue,
                        Severity = AlertSeverity.Warning,
                        Title = $"High Priority Task Unassigned",
                        Message = $"Task {task.Id} ({task.Type}) pending for {(DateTime.UtcNow - task.CreatedAt).TotalMinutes:F0} minutes",
                        TaskId = task.Id
                    };
                    db.Alerts.Add(alert);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
