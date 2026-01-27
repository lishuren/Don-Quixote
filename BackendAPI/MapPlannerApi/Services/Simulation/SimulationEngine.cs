using System.Collections.Concurrent;
using MapPlannerApi.Data;
using MapPlannerApi.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Services.Simulation;

/// <summary>
/// State of a long-running simulation.
/// </summary>
public enum SimulationState
{
    NotStarted,
    Initializing,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Configuration for a simulation run.
/// </summary>
public record SimulationConfig
{
    /// <summary>Unique identifier for this simulation run.</summary>
    public string SimulationId { get; init; } = Guid.NewGuid().ToString("N")[..8];
    
    /// <summary>Simulated start date/time.</summary>
    public DateTime SimulatedStartTime { get; init; } = DateTime.Today.AddHours(7); // 7 AM
    
    /// <summary>Simulated end date/time.</summary>
    public DateTime SimulatedEndTime { get; init; } = DateTime.Today.AddDays(30).AddHours(23); // 30 days
    
    /// <summary>Time acceleration factor.</summary>
    public double AccelerationFactor { get; init; } = 720; // 1 month in ~1 hour
    
    /// <summary>Number of robots to simulate.</summary>
    public int RobotCount { get; init; } = 5;
    
    /// <summary>Number of tables in the restaurant.</summary>
    public int TableCount { get; init; } = 20;
    
    /// <summary>Event pattern configuration.</summary>
    public EventPatternConfig? EventPatterns { get; init; }
    
    /// <summary>Random seed for reproducible simulations.</summary>
    public int? RandomSeed { get; init; }
    
    /// <summary>Interval for broadcasting progress updates.</summary>
    public TimeSpan ProgressBroadcastInterval { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>Whether to persist results to database.</summary>
    public bool PersistResults { get; init; } = false;
}

/// <summary>
/// Progress information for an active simulation.
/// </summary>
public record SimulationProgress
{
    public string SimulationId { get; init; } = string.Empty;
    public SimulationState State { get; init; }
    public DateTime SimulatedStartTime { get; init; }
    public DateTime SimulatedEndTime { get; init; }
    public DateTime CurrentSimulatedTime { get; init; }
    public double ProgressPercent { get; init; }
    public TimeSpan RealElapsedTime { get; init; }
    public TimeSpan EstimatedTimeRemaining { get; init; }
    public double AccelerationFactor { get; init; }
    
    // Live stats
    public int EventsProcessed { get; init; }
    public int TotalEventsScheduled { get; init; }
    public int GuestsProcessed { get; init; }
    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
    public double CurrentSuccessRate { get; init; }
}

/// <summary>
/// Interface for the simulation engine.
/// </summary>
public interface ISimulationEngine
{
    /// <summary>
    /// Gets the current simulation state.
    /// </summary>
    SimulationState State { get; }
    
    /// <summary>
    /// Gets the current simulation config (if running).
    /// </summary>
    SimulationConfig? CurrentConfig { get; }
    
    /// <summary>
    /// Starts a new long-running simulation.
    /// </summary>
    Task<string> StartSimulation(SimulationConfig config);
    
    /// <summary>
    /// Stops the current simulation.
    /// </summary>
    Task StopSimulation();
    
    /// <summary>
    /// Pauses the current simulation.
    /// </summary>
    Task PauseSimulation();
    
    /// <summary>
    /// Resumes a paused simulation.
    /// </summary>
    Task ResumeSimulation();
    
    /// <summary>
    /// Gets the current progress.
    /// </summary>
    SimulationProgress GetProgress();
    
    /// <summary>
    /// Gets the final report (if simulation completed).
    /// </summary>
    SimulationReport? GetReport();
    
    /// <summary>
    /// Event raised when progress is updated.
    /// </summary>
    event EventHandler<SimulationProgress>? ProgressUpdated;
    
    /// <summary>
    /// Event raised when simulation completes.
    /// </summary>
    event EventHandler<SimulationReport>? SimulationCompleted;
}

/// <summary>
/// Background service that runs time-accelerated restaurant simulations.
/// </summary>
public class SimulationEngine : BackgroundService, ISimulationEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimulationClock _clock;
    private readonly IRestaurantEventGenerator _eventGenerator;
    private readonly ISimulationMetricsAggregator _metrics;
    private readonly IEventBroadcaster? _broadcaster;
    private readonly ILogger<SimulationEngine> _logger;
    
    private SimulationState _state = SimulationState.NotStarted;
    private SimulationConfig? _config;
    private SimulationReport? _lastReport;
    private CancellationTokenSource? _simulationCts;
    
    // Event queue
    private readonly ConcurrentQueue<ScheduledEvent> _eventQueue = new();
    private List<ScheduledEvent> _scheduledEvents = new();
    private int _eventsProcessed;
    
    // In-memory simulation state (for speed)
    private readonly ConcurrentDictionary<int, SimulatedRobot> _robots = new();
    private readonly ConcurrentDictionary<int, SimulatedTable> _tables = new();
    private readonly ConcurrentDictionary<int, SimulatedGuest> _guests = new();
    private readonly ConcurrentDictionary<int, SimulatedTask> _tasks = new();
    private int _nextTaskId = 1;
    private int _nextGuestId = 1;
    
    private DateTime _lastProgressBroadcast = DateTime.MinValue;
    
    public SimulationState State => _state;
    public SimulationConfig? CurrentConfig => _config;
    
    public event EventHandler<SimulationProgress>? ProgressUpdated;
    public event EventHandler<SimulationReport>? SimulationCompleted;
    
    public SimulationEngine(
        IServiceScopeFactory scopeFactory,
        ISimulationClock clock,
        IRestaurantEventGenerator eventGenerator,
        ISimulationMetricsAggregator metrics,
        ILogger<SimulationEngine> logger,
        IEventBroadcaster? broadcaster = null)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _eventGenerator = eventGenerator;
        _metrics = metrics;
        _logger = logger;
        _broadcaster = broadcaster;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulationEngine started, waiting for simulation requests");
        
        // This background service runs continuously but only processes when a simulation is active
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_state == SimulationState.Running && _simulationCts != null)
            {
                try
                {
                    await RunSimulationLoop(_simulationCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Simulation cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulation failed");
                    _state = SimulationState.Failed;
                }
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }
    }
    
    public async Task<string> StartSimulation(SimulationConfig config)
    {
        if (_state == SimulationState.Running)
            throw new InvalidOperationException("A simulation is already running");
        
        _config = config;
        _simulationCts = new CancellationTokenSource();
        _eventsProcessed = 0;
        _nextTaskId = 1;
        _nextGuestId = 1;
        
        _logger.LogInformation(
            "Starting simulation {Id}: {Start} to {End} at {Speed}x speed",
            config.SimulationId,
            config.SimulatedStartTime,
            config.SimulatedEndTime,
            config.AccelerationFactor);
        
        // Initialize state
        _state = SimulationState.Initializing;
        await InitializeSimulationState(config);
        
        // Generate all events
        _logger.LogInformation("Generating events for simulation period...");
        _scheduledEvents = _eventGenerator.GenerateEventsForPeriod(
            config.SimulatedStartTime,
            config.SimulatedEndTime,
            config.EventPatterns);
        
        _logger.LogInformation("Generated {Count} events", _scheduledEvents.Count);
        
        // Start metrics tracking
        _metrics.StartSimulation(config.SimulationId, config.SimulatedStartTime);
        
        // Start the clock
        _clock.Start(config.SimulatedStartTime, config.SimulatedEndTime, config.AccelerationFactor);
        
        _state = SimulationState.Running;
        
        return config.SimulationId;
    }
    
    public Task StopSimulation()
    {
        _simulationCts?.Cancel();
        _clock.Stop();
        _state = SimulationState.Cancelled;
        return Task.CompletedTask;
    }
    
    public Task PauseSimulation()
    {
        if (_state != SimulationState.Running) return Task.CompletedTask;
        
        _clock.Pause();
        _state = SimulationState.Paused;
        return Task.CompletedTask;
    }
    
    public Task ResumeSimulation()
    {
        if (_state != SimulationState.Paused) return Task.CompletedTask;
        
        _clock.Resume();
        _state = SimulationState.Running;
        return Task.CompletedTask;
    }
    
    public SimulationProgress GetProgress()
    {
        var config = _config ?? new SimulationConfig();
        var currentTime = _clock.Now;
        var progress = _clock.ProgressPercent;
        var realElapsed = _clock.RealElapsedTime;
        
        // Estimate remaining time
        var estimatedRemaining = TimeSpan.Zero;
        if (progress > 0 && progress < 100)
        {
            var totalEstimated = TimeSpan.FromTicks((long)(realElapsed.Ticks * 100 / progress));
            estimatedRemaining = totalEstimated - realElapsed;
        }
        
        var snapshot = _metrics.GetProgressSnapshot();
        
        return new SimulationProgress
        {
            SimulationId = config.SimulationId,
            State = _state,
            SimulatedStartTime = config.SimulatedStartTime,
            SimulatedEndTime = config.SimulatedEndTime,
            CurrentSimulatedTime = currentTime,
            ProgressPercent = progress,
            RealElapsedTime = realElapsed,
            EstimatedTimeRemaining = estimatedRemaining,
            AccelerationFactor = config.AccelerationFactor,
            EventsProcessed = _eventsProcessed,
            TotalEventsScheduled = _scheduledEvents.Count,
            GuestsProcessed = snapshot.TotalGuestsToDate,
            TasksCreated = snapshot.TotalTasksToDate,
            TasksCompleted = snapshot.TasksCompletedToDate,
            CurrentSuccessRate = snapshot.CurrentSuccessRate
        };
    }
    
    public SimulationReport? GetReport() => _lastReport;
    
    private async Task InitializeSimulationState(SimulationConfig config)
    {
        // Clear previous state
        _robots.Clear();
        _tables.Clear();
        _guests.Clear();
        _tasks.Clear();
        _eventQueue.Clear();
        
        // Create simulated robots
        for (int i = 1; i <= config.RobotCount; i++)
        {
            _robots[i] = new SimulatedRobot
            {
                Id = i,
                Name = $"Robot-{i}",
                Status = RobotStatus.Idle,
                Battery = 100,
                Position = new Position(100 + i * 50, 100, 1 + i * 0.5, 1),
                CurrentTaskId = null
            };
        }
        
        // Create simulated tables
        for (int i = 1; i <= config.TableCount; i++)
        {
            _tables[i] = new SimulatedTable
            {
                Id = i,
                Label = $"Table-{i}",
                Status = TableStatus.Available,
                Capacity = i <= 10 ? 4 : (i <= 15 ? 6 : 8),
                CurrentGuestId = null
            };
        }
        
        await Task.CompletedTask;
    }
    
    private async Task RunSimulationLoop(CancellationToken ct)
    {
        var eventIndex = 0;
        var config = _config!;
        
        _logger.LogInformation("Simulation loop started");
        
        while (!ct.IsCancellationRequested && _clock.IsRunning && !_clock.IsPaused)
        {
            var currentTime = _clock.Now;
            
            // Check if simulation is complete
            if (currentTime >= config.SimulatedEndTime)
            {
                await CompleteSimulation();
                break;
            }
            
            // Process all events up to current time
            while (eventIndex < _scheduledEvents.Count && 
                   _scheduledEvents[eventIndex].ScheduledTime <= currentTime)
            {
                var evt = _scheduledEvents[eventIndex];
                await ProcessEvent(evt, currentTime);
                eventIndex++;
                _eventsProcessed++;
            }
            
            // Update robot states (tasks, battery, etc.)
            UpdateRobotStates(currentTime);
            
            // Broadcast progress periodically
            if (DateTime.UtcNow - _lastProgressBroadcast >= config.ProgressBroadcastInterval)
            {
                var progress = GetProgress();
                ProgressUpdated?.Invoke(this, progress);
                await BroadcastProgress(progress);
                _lastProgressBroadcast = DateTime.UtcNow;
            }
            
            // Small delay to prevent CPU spinning (but not too long - we're accelerated!)
            await Task.Delay(1, ct);
        }
    }
    
    private async Task ProcessEvent(ScheduledEvent evt, DateTime currentTime)
    {
        switch (evt.EventType)
        {
            case TriggerEventType.GuestArrived:
                await HandleGuestArrived(evt, currentTime);
                break;
            
            case TriggerEventType.GuestSeated:
                await HandleGuestSeated(evt, currentTime);
                break;
            
            case TriggerEventType.FoodReady:
            case TriggerEventType.DrinkReady:
                await HandleFoodReady(evt, currentTime);
                break;
            
            case TriggerEventType.GuestNeedsHelp:
                await HandleGuestNeedsHelp(evt, currentTime);
                break;
            
            case TriggerEventType.GuestRequestedCheck:
                await HandleGuestRequestedCheck(evt, currentTime);
                break;
            
            case TriggerEventType.GuestLeft:
                await HandleGuestLeft(evt, currentTime);
                break;
            
            case TriggerEventType.TableNeedsCleaning:
                await HandleTableNeedsCleaning(evt, currentTime);
                break;
        }
    }
    
    private Task HandleGuestArrived(ScheduledEvent evt, DateTime currentTime)
    {
        var guestId = evt.GuestId ?? _nextGuestId++;
        var partySize = int.TryParse(evt.Notes?.Split(' ').LastOrDefault(), out var size) ? size : 2;
        
        _guests[guestId] = new SimulatedGuest
        {
            Id = guestId,
            PartySize = partySize,
            Status = GuestStatus.Waiting,
            ArrivalTime = currentTime
        };
        
        _metrics.RecordGuestArrival(currentTime, guestId, partySize);
        
        // Create greeting task
        CreateTask(currentTime, TaskType.Greeting, null, TaskPriority.Normal);
        
        return Task.CompletedTask;
    }
    
    private Task HandleGuestSeated(ScheduledEvent evt, DateTime currentTime)
    {
        if (evt.GuestId.HasValue && _guests.TryGetValue(evt.GuestId.Value, out var guest))
        {
            guest.Status = GuestStatus.Seated;
            guest.TableId = evt.TableId;
        }
        
        if (evt.TableId.HasValue && _tables.TryGetValue(evt.TableId.Value, out var table))
        {
            table.Status = TableStatus.Occupied;
            table.CurrentGuestId = evt.GuestId;
        }
        
        _metrics.RecordGuestSeated(currentTime, evt.GuestId ?? 0, evt.TableId ?? 0);
        
        return Task.CompletedTask;
    }
    
    private Task HandleFoodReady(ScheduledEvent evt, DateTime currentTime)
    {
        var taskType = evt.EventType == TriggerEventType.DrinkReady ? TaskType.Deliver : TaskType.Deliver;
        var priority = evt.EventType == TriggerEventType.FoodReady ? TaskPriority.High : TaskPriority.Normal;
        
        CreateTask(currentTime, taskType, evt.TableId, priority);
        
        return Task.CompletedTask;
    }
    
    private Task HandleGuestNeedsHelp(ScheduledEvent evt, DateTime currentTime)
    {
        CreateTask(currentTime, TaskType.Service, evt.TableId, TaskPriority.High);
        _metrics.RecordAlert(currentTime, "GuestNeedsHelp");
        return Task.CompletedTask;
    }
    
    private Task HandleGuestRequestedCheck(ScheduledEvent evt, DateTime currentTime)
    {
        CreateTask(currentTime, TaskType.Deliver, evt.TableId, TaskPriority.Normal);
        return Task.CompletedTask;
    }
    
    private Task HandleGuestLeft(ScheduledEvent evt, DateTime currentTime)
    {
        if (evt.GuestId.HasValue && _guests.TryGetValue(evt.GuestId.Value, out var guest))
        {
            guest.Status = GuestStatus.Departed;
        }
        
        if (evt.TableId.HasValue && _tables.TryGetValue(evt.TableId.Value, out var table))
        {
            table.Status = TableStatus.NeedsService;
        }
        
        _metrics.RecordGuestDeparture(currentTime, evt.GuestId ?? 0, evt.TableId ?? 0);
        
        return Task.CompletedTask;
    }
    
    private Task HandleTableNeedsCleaning(ScheduledEvent evt, DateTime currentTime)
    {
        if (evt.TableId.HasValue && _tables.TryGetValue(evt.TableId.Value, out var table))
        {
            table.Status = TableStatus.Cleaning;
        }
        
        CreateTask(currentTime, TaskType.Cleaning, evt.TableId, TaskPriority.Normal);
        
        return Task.CompletedTask;
    }
    
    private void CreateTask(DateTime currentTime, TaskType type, int? tableId, TaskPriority priority)
    {
        var taskId = _nextTaskId++;
        
        var task = new SimulatedTask
        {
            Id = taskId,
            Type = type,
            Status = TaskStatus.Pending,
            Priority = priority,
            TableId = tableId,
            CreatedAt = currentTime
        };
        
        _tasks[taskId] = task;
        _metrics.RecordTaskCreated(currentTime, taskId, type);
        
        // Try to auto-assign
        TryAssignTask(task, currentTime);
    }
    
    private void TryAssignTask(SimulatedTask task, DateTime currentTime)
    {
        // Find available robot with highest battery
        var availableRobot = _robots.Values
            .Where(r => r.Status == RobotStatus.Idle && r.Battery > 20)
            .OrderByDescending(r => r.Battery)
            .FirstOrDefault();
        
        if (availableRobot != null)
        {
            task.Status = TaskStatus.Assigned;
            task.RobotId = availableRobot.Id;
            task.StartedAt = currentTime;
            
            availableRobot.Status = RobotStatus.Navigating;
            availableRobot.CurrentTaskId = task.Id;
            
            // Calculate simple completion time (10-60 seconds simulated)
            var random = new Random();
            task.EstimatedCompletionTime = currentTime.AddSeconds(random.Next(10, 60));
        }
    }
    
    private void UpdateRobotStates(DateTime currentTime)
    {
        foreach (var robot in _robots.Values)
        {
            // Drain battery slowly
            if (robot.Status != RobotStatus.Idle)
            {
                robot.Battery = Math.Max(0, robot.Battery - 0.01);
            }
            
            // Check for task completion
            if (robot.CurrentTaskId.HasValue && _tasks.TryGetValue(robot.CurrentTaskId.Value, out var task))
            {
                if (task.EstimatedCompletionTime.HasValue && currentTime >= task.EstimatedCompletionTime.Value)
                {
                    // Complete the task
                    task.Status = TaskStatus.Completed;
                    task.CompletedAt = currentTime;
                    
                    var duration = (task.CompletedAt.Value - task.StartedAt!.Value).TotalSeconds;
                    _metrics.RecordTaskCompleted(currentTime, task.Id, robot.Id, duration);
                    
                    // Free the robot
                    robot.Status = RobotStatus.Idle;
                    robot.CurrentTaskId = null;
                    
                    // Try to assign pending tasks
                    var pendingTask = _tasks.Values.FirstOrDefault(t => t.Status == TaskStatus.Pending);
                    if (pendingTask != null)
                    {
                        TryAssignTask(pendingTask, currentTime);
                    }
                }
            }
            
            // Auto-charge if low battery and idle
            if (robot.Status == RobotStatus.Idle && robot.Battery < 20)
            {
                robot.Status = RobotStatus.Charging;
            }
            else if (robot.Status == RobotStatus.Charging)
            {
                robot.Battery = Math.Min(100, robot.Battery + 0.5);
                if (robot.Battery >= 100)
                {
                    robot.Status = RobotStatus.Idle;
                }
            }
            
            _metrics.RecordRobotUpdate(currentTime, robot.Id, robot.Battery, robot.Status != RobotStatus.Idle);
        }
    }
    
    private async Task CompleteSimulation()
    {
        _clock.Stop();
        _state = SimulationState.Completed;
        
        var config = _config!;
        var report = _metrics.GenerateReport(
            config.SimulatedEndTime,
            _clock.RealElapsedTime,
            config.AccelerationFactor);
        
        _lastReport = report;
        
        _logger.LogInformation(
            "Simulation {Id} completed: {Guests} guests, {Tasks} tasks, {Success:F1}% success rate",
            config.SimulationId,
            report.TotalGuests,
            report.TotalTasks,
            report.OverallSuccessRate);
        
        SimulationCompleted?.Invoke(this, report);
        
        await Task.CompletedTask;
    }
    
    private async Task BroadcastProgress(SimulationProgress progress)
    {
        try
        {
            if (_broadcaster != null)
            {
                var evt = new Dtos.SimulationProgressEvent(
                    SimulationId: progress.SimulationId,
                    State: progress.State.ToString(),
                    CurrentSimulatedTime: progress.CurrentSimulatedTime,
                    ProgressPercent: Math.Round(progress.ProgressPercent, 2),
                    RealElapsedTime: FormatDuration(progress.RealElapsedTime),
                    EstimatedTimeRemaining: FormatDuration(progress.EstimatedTimeRemaining),
                    EventsProcessed: progress.EventsProcessed,
                    TotalEventsScheduled: progress.TotalEventsScheduled,
                    GuestsProcessed: progress.GuestsProcessed,
                    TasksCreated: progress.TasksCreated,
                    TasksCompleted: progress.TasksCompleted,
                    TasksFailed: progress.TasksCreated - progress.TasksCompleted, // Approximate
                    CurrentSuccessRate: Math.Round(progress.CurrentSuccessRate, 2),
                    Timestamp: DateTime.UtcNow
                );
                
                await _broadcaster.BroadcastSimulationProgress(evt);
            }
            
            _logger.LogDebug(
                "Simulation progress: {Percent:F1}% - Events: {Processed}/{Total}",
                progress.ProgressPercent,
                progress.EventsProcessed,
                progress.TotalEventsScheduled);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast simulation progress");
        }
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
}

/// <summary>
/// In-memory robot for simulation.
/// </summary>
internal class SimulatedRobot
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public RobotStatus Status { get; set; }
    public double Battery { get; set; }
    public Position Position { get; set; } = new(0, 0, 0, 0);
    public int? CurrentTaskId { get; set; }
}

/// <summary>
/// In-memory table for simulation.
/// </summary>
internal class SimulatedTable
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public TableStatus Status { get; set; }
    public int Capacity { get; init; }
    public int? CurrentGuestId { get; set; }
}

/// <summary>
/// In-memory guest for simulation.
/// </summary>
internal class SimulatedGuest
{
    public int Id { get; init; }
    public int PartySize { get; set; }
    public GuestStatus Status { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int? TableId { get; set; }
}

/// <summary>
/// In-memory task for simulation.
/// </summary>
internal class SimulatedTask
{
    public int Id { get; init; }
    public TaskType Type { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public int? TableId { get; set; }
    public int? RobotId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }
}
