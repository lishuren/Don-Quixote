using Microsoft.AspNetCore.SignalR;
using MapPlannerApi.Dtos;
using MapPlannerApi.Hubs;

namespace MapPlannerApi.Services;

/// <summary>
/// Interface for broadcasting real-time events via SignalR.
/// </summary>
public interface IEventBroadcaster
{
    Task BroadcastRobotPositionUpdated(int robotId, string robotName, PositionDto position, double heading, double velocity);
    Task BroadcastRobotStatusChanged(int robotId, string robotName, string oldStatus, string newStatus, string? reason = null);
    Task BroadcastTaskStatusChanged(int taskId, string taskType, string oldStatus, string newStatus, int? robotId = null, string? robotName = null);
    Task BroadcastAlertCreated(int alertId, string type, string severity, string title, string? message = null, int? robotId = null);
    Task BroadcastTableStatusChanged(int tableId, string? label, string oldStatus, string newStatus, int? guestId = null);
    Task BroadcastGuestEvent(int guestId, string? guestName, string eventType, int? tableId = null, string? tableLabel = null);
}

/// <summary>
/// Service to broadcast events to connected SignalR clients.
/// </summary>
public class EventBroadcaster : IEventBroadcaster
{
    private readonly IHubContext<RestaurantHub> _hubContext;
    private readonly ILogger<EventBroadcaster> _logger;

    public EventBroadcaster(IHubContext<RestaurantHub> hubContext, ILogger<EventBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastRobotPositionUpdated(int robotId, string robotName, PositionDto position, double heading, double velocity)
    {
        var evt = new RobotPositionEvent(robotId, robotName, position, heading, velocity, DateTime.UtcNow);
        
        // Send to all clients and specific robot subscribers
        await Task.WhenAll(
            _hubContext.Clients.Group("All").SendAsync("RobotPositionUpdated", evt),
            _hubContext.Clients.Group($"Robot_{robotId}").SendAsync("RobotPositionUpdated", evt)
        );

        _logger.LogDebug("Broadcast RobotPositionUpdated for robot {RobotId}", robotId);
    }

    public async Task BroadcastRobotStatusChanged(int robotId, string robotName, string oldStatus, string newStatus, string? reason = null)
    {
        var evt = new RobotStatusEvent(robotId, robotName, oldStatus, newStatus, reason, DateTime.UtcNow);

        await Task.WhenAll(
            _hubContext.Clients.Group("All").SendAsync("RobotStatusChanged", evt),
            _hubContext.Clients.Group($"Robot_{robotId}").SendAsync("RobotStatusChanged", evt)
        );

        _logger.LogInformation("Robot {RobotId} status changed: {OldStatus} -> {NewStatus}", robotId, oldStatus, newStatus);
    }

    public async Task BroadcastTaskStatusChanged(int taskId, string taskType, string oldStatus, string newStatus, int? robotId = null, string? robotName = null)
    {
        var evt = new TaskStatusEvent(taskId, taskType, oldStatus, newStatus, robotId, robotName, DateTime.UtcNow);

        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("All").SendAsync("TaskStatusChanged", evt),
            _hubContext.Clients.Group("Tasks").SendAsync("TaskStatusChanged", evt)
        };

        if (robotId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"Robot_{robotId}").SendAsync("TaskStatusChanged", evt));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("Task {TaskId} status changed: {OldStatus} -> {NewStatus}", taskId, oldStatus, newStatus);
    }

    public async Task BroadcastAlertCreated(int alertId, string type, string severity, string title, string? message = null, int? robotId = null)
    {
        var evt = new AlertEvent(alertId, type, severity, title, message, robotId, DateTime.UtcNow);

        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("All").SendAsync("AlertCreated", evt),
            _hubContext.Clients.Group("Alerts").SendAsync("AlertCreated", evt)
        };

        if (robotId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"Robot_{robotId}").SendAsync("AlertCreated", evt));
        }

        await Task.WhenAll(tasks);

        _logger.LogWarning("Alert created: [{Severity}] {Title}", severity, title);
    }

    public async Task BroadcastTableStatusChanged(int tableId, string? label, string oldStatus, string newStatus, int? guestId = null)
    {
        var evt = new TableStatusEvent(tableId, label, oldStatus, newStatus, guestId, DateTime.UtcNow);

        await Task.WhenAll(
            _hubContext.Clients.Group("All").SendAsync("TableStatusChanged", evt),
            _hubContext.Clients.Group($"Table_{tableId}").SendAsync("TableStatusChanged", evt)
        );

        _logger.LogInformation("Table {TableId} status changed: {OldStatus} -> {NewStatus}", tableId, oldStatus, newStatus);
    }

    public async Task BroadcastGuestEvent(int guestId, string? guestName, string eventType, int? tableId = null, string? tableLabel = null)
    {
        var evt = new GuestEvent(guestId, guestName, eventType, tableId, tableLabel, DateTime.UtcNow);

        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("All").SendAsync("GuestEvent", evt)
        };

        if (tableId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"Table_{tableId}").SendAsync("GuestEvent", evt));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("Guest event: {EventType} for guest {GuestId}", eventType, guestId);
    }
}
