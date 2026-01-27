using MapPlannerApi.Data;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Services;

/// <summary>
/// Background service that automatically assigns pending tasks to available robots
/// at configurable intervals.
/// </summary>
public class DispatchBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DispatchBackgroundService> _logger;
    
    // Default interval, will be overridden by config
    private TimeSpan _interval = TimeSpan.FromSeconds(10);
    private bool _enabled = true;

    public DispatchBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DispatchBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dispatch Background Service started");

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshConfig(stoppingToken);

                if (_enabled)
                {
                    await DispatchPendingTasks(stoppingToken);
                }

                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in dispatch background service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Dispatch Background Service stopped");
    }

    private async Task RefreshConfig(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<ConfigRepository>();

        var intervalSeconds = await configRepo.GetValueAsync(ConfigKeys.DispatchIntervalSeconds, 10);
        _interval = TimeSpan.FromSeconds(intervalSeconds);

        var enabledStr = await configRepo.GetValueAsync(ConfigKeys.DispatchAutoEnabled);
        _enabled = string.IsNullOrEmpty(enabledStr) || enabledStr.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task DispatchPendingTasks(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<IDispatchEngine>();
        var config = await dispatch.GetConfig();

        if (!config.AutoAssignEnabled)
        {
            return;
        }

        var result = await dispatch.AutoAssignPendingTasks();
        
        if (result.TasksAssigned > 0)
        {
            _logger.LogInformation("Auto-dispatch: assigned {Count} tasks to robots", result.TasksAssigned);
        }
    }
}
