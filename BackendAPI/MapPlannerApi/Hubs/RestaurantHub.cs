using Microsoft.AspNetCore.SignalR;
using MapPlannerApi.Dtos;

namespace MapPlannerApi.Hubs;

/// <summary>
/// SignalR hub for real-time restaurant events.
/// Clients can subscribe to specific zones, robots, or tables.
/// </summary>
public class RestaurantHub : Hub
{
    private readonly ILogger<RestaurantHub> _logger;

    public RestaurantHub(ILogger<RestaurantHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Add to general group by default
        await Groups.AddToGroupAsync(Context.ConnectionId, "All");
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to events for a specific robot
    /// </summary>
    public async Task SubscribeToRobot(int robotId)
    {
        var groupName = $"Robot_{robotId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} subscribed to {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribe from robot events
    /// </summary>
    public async Task UnsubscribeFromRobot(int robotId)
    {
        var groupName = $"Robot_{robotId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Subscribe to events for a specific zone
    /// </summary>
    public async Task SubscribeToZone(int zoneId)
    {
        var groupName = $"Zone_{zoneId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} subscribed to {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribe from zone events
    /// </summary>
    public async Task UnsubscribeFromZone(int zoneId)
    {
        var groupName = $"Zone_{zoneId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Subscribe to events for a specific table
    /// </summary>
    public async Task SubscribeToTable(int tableId)
    {
        var groupName = $"Table_{tableId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} subscribed to {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribe from table events
    /// </summary>
    public async Task UnsubscribeFromTable(int tableId)
    {
        var groupName = $"Table_{tableId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Subscribe to all alerts
    /// </summary>
    public async Task SubscribeToAlerts()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Alerts");
    }

    /// <summary>
    /// Unsubscribe from alerts
    /// </summary>
    public async Task UnsubscribeFromAlerts()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Alerts");
    }

    /// <summary>
    /// Subscribe to task updates
    /// </summary>
    public async Task SubscribeToTasks()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Tasks");
    }

    /// <summary>
    /// Subscribe to simulation progress updates
    /// </summary>
    public async Task SubscribeToSimulation()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Simulation");
        _logger.LogDebug("Client {ConnectionId} subscribed to Simulation updates", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from simulation updates
    /// </summary>
    public async Task UnsubscribeFromSimulation()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Simulation");
    }

    /// <summary>
    /// Ping to check connection
    /// </summary>
    public async Task<string> Ping()
    {
        return await Task.FromResult("pong");
    }
}
