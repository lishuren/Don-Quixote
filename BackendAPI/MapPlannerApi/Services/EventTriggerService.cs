using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Services;

/// <summary>
/// Event types that can trigger automatic task creation.
/// </summary>
public enum TriggerEventType
{
    // Guest events
    GuestArrived,
    GuestSeated,
    GuestNeedsHelp,
    GuestRequestedCheck,
    GuestLeft,
    
    // Kitchen/Food events
    FoodReady,
    DrinkReady,
    OrderReady,
    
    // Table events
    TableNeedsService,
    TableNeedsCleaning,
    TableNeedsBussing,
    TableNeedsSetup,
    
    // Robot events
    RobotLowBattery,
    RobotBlocked,
    RobotError
}

/// <summary>
/// Request to trigger an event that may create tasks.
/// </summary>
public record TriggerEventRequest(
    TriggerEventType EventType,
    int? TableId = null,
    int? GuestId = null,
    int? RobotId = null,
    string? OrderId = null,
    string? Notes = null,
    TaskPriority Priority = TaskPriority.Normal
);

/// <summary>
/// Result of triggering an event.
/// </summary>
public record TriggerEventResult(
    bool Success,
    string Message,
    int? TaskId = null,
    int? AlertId = null
);

/// <summary>
/// Interface for the event trigger service.
/// </summary>
public interface IEventTriggerService
{
    Task<TriggerEventResult> TriggerEvent(TriggerEventRequest request);
    Task<List<RobotTask>> GetTasksForEvent(TriggerEventType eventType);
}

/// <summary>
/// Service that converts restaurant events into robot tasks.
/// Handles automatic task generation for guest, food, and table events.
/// </summary>
public class EventTriggerService : IEventTriggerService
{
    private readonly RestaurantDbContext _db;
    private readonly IDispatchEngine _dispatch;
    private readonly IEventBroadcaster? _broadcaster;
    private readonly ILogger<EventTriggerService> _logger;

    public EventTriggerService(
        RestaurantDbContext db,
        IDispatchEngine dispatch,
        ILogger<EventTriggerService> logger,
        IEventBroadcaster? broadcaster = null)
    {
        _db = db;
        _dispatch = dispatch;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public async Task<TriggerEventResult> TriggerEvent(TriggerEventRequest request)
    {
        _logger.LogInformation("Event triggered: {EventType}, TableId={TableId}, GuestId={GuestId}", 
            request.EventType, request.TableId, request.GuestId);

        return request.EventType switch
        {
            // Guest events
            TriggerEventType.GuestArrived => await HandleGuestArrived(request),
            TriggerEventType.GuestSeated => await HandleGuestSeated(request),
            TriggerEventType.GuestNeedsHelp => await HandleGuestNeedsHelp(request),
            TriggerEventType.GuestRequestedCheck => await HandleGuestRequestedCheck(request),
            TriggerEventType.GuestLeft => await HandleGuestLeft(request),
            
            // Kitchen/Food events
            TriggerEventType.FoodReady => await HandleFoodReady(request),
            TriggerEventType.DrinkReady => await HandleDrinkReady(request),
            TriggerEventType.OrderReady => await HandleOrderReady(request),
            
            // Table events
            TriggerEventType.TableNeedsService => await HandleTableNeedsService(request),
            TriggerEventType.TableNeedsCleaning => await HandleTableNeedsCleaning(request),
            TriggerEventType.TableNeedsBussing => await HandleTableNeedsBussing(request),
            TriggerEventType.TableNeedsSetup => await HandleTableNeedsSetup(request),
            
            // Robot events
            TriggerEventType.RobotLowBattery => await HandleRobotLowBattery(request),
            TriggerEventType.RobotBlocked => await HandleRobotBlocked(request),
            TriggerEventType.RobotError => await HandleRobotError(request),
            
            _ => new TriggerEventResult(false, $"Unknown event type: {request.EventType}")
        };
    }

    public async Task<List<RobotTask>> GetTasksForEvent(TriggerEventType eventType)
    {
        var taskType = MapEventToTaskType(eventType);
        return await _db.Tasks
            .Where(t => t.Type == taskType && t.Status == TaskStatus.Pending)
            .ToListAsync();
    }

    // ========== Guest Event Handlers ==========

    private async Task<TriggerEventResult> HandleGuestArrived(TriggerEventRequest request)
    {
        // Create a greeting/escort task when guest arrives
        var task = await CreateTask(
            TaskType.Escort,
            request.Priority,
            request.TableId,
            $"Guest arrived - escort to table. {request.Notes}"
        );

        // Also create an alert for host notification
        await CreateAlert(
            AlertType.GuestWaiting,
            AlertSeverity.Info,
            "Guest Arrived",
            $"New guest arrived. Party ID: {request.GuestId}"
        );

        return new TriggerEventResult(true, "Guest arrival task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleGuestSeated(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for GuestSeated event");

        // Create initial service task (bring menus, water)
        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.Normal,
            request.TableId,
            "Initial table service - menus and water"
        );

        return new TriggerEventResult(true, "Initial service task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleGuestNeedsHelp(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for GuestNeedsHelp event");

        // High priority task for guest assistance
        var task = await CreateTask(
            TaskType.Service,
            TaskPriority.High,
            request.TableId,
            $"Guest needs assistance. {request.Notes}"
        );

        // Create alert for immediate attention
        var alert = await CreateAlert(
            AlertType.TableService,
            AlertSeverity.Warning,
            $"Guest Needs Help - Table {request.TableId}",
            request.Notes
        );

        // Update table status
        await UpdateTableStatus(request.TableId.Value, TableStatus.NeedsService);

        return new TriggerEventResult(true, "Help request task created", task.Id, alert.Id);
    }

    private async Task<TriggerEventResult> HandleGuestRequestedCheck(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for GuestRequestedCheck event");

        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.Normal,
            request.TableId,
            "Deliver check to table"
        );

        return new TriggerEventResult(true, "Check delivery task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleGuestLeft(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for GuestLeft event");

        // Create cleaning task
        var task = await CreateTask(
            TaskType.Custom,
            TaskPriority.Normal,
            request.TableId,
            "Bus and clean table"
        );

        // Update table status
        await UpdateTableStatus(request.TableId.Value, TableStatus.Cleaning);

        return new TriggerEventResult(true, "Table cleaning task created", task.Id);
    }

    // ========== Kitchen/Food Event Handlers ==========

    private async Task<TriggerEventResult> HandleFoodReady(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for FoodReady event");

        // High priority delivery task for hot food
        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.High,
            request.TableId,
            $"Food ready for delivery. Order: {request.OrderId}"
        );

        // Trigger auto-assign to get food delivered quickly
        await _dispatch.AutoAssignPendingTasks();

        return new TriggerEventResult(true, "Food delivery task created and dispatched", task.Id);
    }

    private async Task<TriggerEventResult> HandleDrinkReady(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for DrinkReady event");

        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.Normal,
            request.TableId,
            $"Drinks ready for delivery. Order: {request.OrderId}"
        );

        await _dispatch.AutoAssignPendingTasks();

        return new TriggerEventResult(true, "Drink delivery task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleOrderReady(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required for OrderReady event");

        // Urgent priority for complete orders
        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.Urgent,
            request.TableId,
            $"Complete order ready. Order: {request.OrderId}. {request.Notes}"
        );

        await _dispatch.AutoAssignPendingTasks();

        return new TriggerEventResult(true, "Order delivery task created", task.Id);
    }

    // ========== Table Event Handlers ==========

    private async Task<TriggerEventResult> HandleTableNeedsService(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required");

        var task = await CreateTask(
            TaskType.Service,
            request.Priority,
            request.TableId,
            $"Table service needed. {request.Notes}"
        );

        await UpdateTableStatus(request.TableId.Value, TableStatus.NeedsService);

        return new TriggerEventResult(true, "Service task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleTableNeedsCleaning(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required");

        var task = await CreateTask(
            TaskType.Cleaning,
            TaskPriority.Normal,
            request.TableId,
            "Table needs cleaning"
        );

        await UpdateTableStatus(request.TableId.Value, TableStatus.Cleaning);

        return new TriggerEventResult(true, "Cleaning task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleTableNeedsBussing(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required");

        var task = await CreateTask(
            TaskType.Return,
            TaskPriority.Normal,
            request.TableId,
            "Bus dirty dishes from table"
        );

        return new TriggerEventResult(true, "Bussing task created", task.Id);
    }

    private async Task<TriggerEventResult> HandleTableNeedsSetup(TriggerEventRequest request)
    {
        if (!request.TableId.HasValue)
            return new TriggerEventResult(false, "TableId required");

        var task = await CreateTask(
            TaskType.Deliver,
            TaskPriority.Normal,
            request.TableId,
            "Set up table for next guests"
        );

        return new TriggerEventResult(true, "Setup task created", task.Id);
    }

    // ========== Robot Event Handlers ==========

    private async Task<TriggerEventResult> HandleRobotLowBattery(TriggerEventRequest request)
    {
        if (!request.RobotId.HasValue)
            return new TriggerEventResult(false, "RobotId required");

        var robot = await _db.Robots.FindAsync(request.RobotId.Value);
        if (robot == null)
            return new TriggerEventResult(false, "Robot not found");

        // Create charging task
        var task = new RobotTask
        {
            Type = TaskType.Charge,
            Priority = TaskPriority.High,
            RobotId = request.RobotId,
            Status = TaskStatus.Assigned
        };
        _db.Tasks.Add(task);

        // Create alert
        var alert = await CreateAlert(
            AlertType.LowBattery,
            AlertSeverity.Warning,
            $"Low Battery - {robot.Name}",
            $"Battery at {robot.BatteryLevel}%. Charging task created."
        );

        await _db.SaveChangesAsync();

        return new TriggerEventResult(true, "Charging task created", task.Id, alert.Id);
    }

    private async Task<TriggerEventResult> HandleRobotBlocked(TriggerEventRequest request)
    {
        if (!request.RobotId.HasValue)
            return new TriggerEventResult(false, "RobotId required");

        var robot = await _db.Robots
            .Include(r => r.CurrentTask)
            .FirstOrDefaultAsync(r => r.Id == request.RobotId.Value);
            
        if (robot == null)
            return new TriggerEventResult(false, "Robot not found");

        // Create alert
        var alert = await CreateAlert(
            AlertType.NavigationBlocked,
            AlertSeverity.Error,
            $"Robot Blocked - {robot.Name}",
            $"Robot has been blocked. {request.Notes}"
        );

        // If robot has an active task, try to reassign it
        if (robot.CurrentTaskId.HasValue)
        {
            var task = robot.CurrentTask;
            if (task != null && task.Status == TaskStatus.InProgress)
            {
                // Reset task to pending for reassignment
                task.RobotId = null;
                task.Status = TaskStatus.Pending;
                task.RetryCount++;
                
                // Set robot to error state
                robot.Status = RobotStatus.Error;
                
                await _db.SaveChangesAsync();

                // Try to reassign to another robot
                await _dispatch.AutoAssignPendingTasks();

                _logger.LogWarning("Task {TaskId} unassigned from blocked robot {RobotId} for reassignment", 
                    task.Id, robot.Id);

                return new TriggerEventResult(true, $"Robot blocked. Task {task.Id} queued for reassignment", task.Id, alert.Id);
            }
        }

        robot.Status = RobotStatus.Error;
        await _db.SaveChangesAsync();

        return new TriggerEventResult(true, "Robot blocked alert created", null, alert.Id);
    }

    private async Task<TriggerEventResult> HandleRobotError(TriggerEventRequest request)
    {
        if (!request.RobotId.HasValue)
            return new TriggerEventResult(false, "RobotId required");

        var robot = await _db.Robots.FindAsync(request.RobotId.Value);
        if (robot == null)
            return new TriggerEventResult(false, "Robot not found");

        robot.Status = RobotStatus.Error;

        var alert = await CreateAlert(
            AlertType.RobotError,
            AlertSeverity.Error,
            $"Robot Error - {robot.Name}",
            request.Notes ?? "Unknown error"
        );

        await _db.SaveChangesAsync();

        return new TriggerEventResult(true, "Robot error alert created", null, alert.Id);
    }

    // ========== Helper Methods ==========

    private async Task<RobotTask> CreateTask(TaskType type, TaskPriority priority, int? tableId, string? notes)
    {
        Position targetPosition = new();
        
        if (tableId.HasValue)
        {
            var table = await _db.Tables.FindAsync(tableId.Value);
            if (table != null)
            {
                targetPosition = table.Center;
            }
        }

        var task = new RobotTask
        {
            Type = type,
            Priority = priority,
            TargetTableId = tableId,
            TargetPosition = targetPosition,
            Status = TaskStatus.Pending
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created task {TaskId}: Type={Type}, Priority={Priority}, TableId={TableId}", 
            task.Id, type, priority, tableId);

        // Broadcast task created event
        if (_broadcaster != null)
        {
            await _broadcaster.BroadcastTaskStatusChanged(task.Id, type.ToString(), "None", "Pending");
        }

        return task;
    }

    private async Task<Alert> CreateAlert(AlertType type, AlertSeverity severity, string title, string? message)
    {
        var alert = new Alert
        {
            Type = type,
            Severity = severity,
            Title = title,
            Message = message
        };

        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        if (_broadcaster != null)
        {
            await _broadcaster.BroadcastAlertCreated(alert.Id, type.ToString(), severity.ToString(), title, message);
        }

        return alert;
    }

    private async Task UpdateTableStatus(int tableId, TableStatus status)
    {
        var table = await _db.Tables.FindAsync(tableId);
        if (table != null)
        {
            var oldStatus = table.Status;
            table.Status = status;
            await _db.SaveChangesAsync();

            if (_broadcaster != null)
            {
                await _broadcaster.BroadcastTableStatusChanged(tableId, table.Label, oldStatus.ToString(), status.ToString());
            }
        }
    }

    private static TaskType MapEventToTaskType(TriggerEventType eventType) => eventType switch
    {
        TriggerEventType.FoodReady or TriggerEventType.DrinkReady or TriggerEventType.OrderReady => TaskType.Deliver,
        TriggerEventType.TableNeedsBussing or TriggerEventType.GuestLeft => TaskType.Return,
        TriggerEventType.RobotLowBattery => TaskType.Charge,
        _ => TaskType.Custom
    };
}
