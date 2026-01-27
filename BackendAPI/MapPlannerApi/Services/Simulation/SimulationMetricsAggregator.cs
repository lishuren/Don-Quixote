using System.Collections.Concurrent;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Services.Simulation;

/// <summary>
/// Metrics for a single time bucket (typically 1 hour).
/// </summary>
public record HourlyMetrics
{
    public DateTime HourStart { get; init; }
    public int GuestsArrived { get; init; }
    public int GuestsSeated { get; init; }
    public int GuestsDeparted { get; init; }
    public int TasksCreated { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
    public int DeliveriesCompleted { get; init; }
    public double AverageTaskDurationSeconds { get; init; }
    public double AverageWaitTimeSeconds { get; init; }
    public int RobotIdleMinutes { get; init; }
    public int RobotBusyMinutes { get; init; }
    public int AlertsGenerated { get; init; }
    public int PeakConcurrentGuests { get; init; }
    public double AverageRobotBattery { get; init; }
}

/// <summary>
/// Daily aggregated metrics.
/// </summary>
public record DailyMetrics
{
    public DateTime Date { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public int TotalGuests { get; init; }
    public int TotalTasks { get; init; }
    public int TotalDeliveries { get; init; }
    public int TotalFailures { get; init; }
    public double SuccessRate { get; init; }
    public double PeakHourGuests { get; init; }
    public int PeakHour { get; init; }
    public double AverageTableTurnover { get; init; }
    public double AverageRobotUtilization { get; init; }
    public List<HourlyMetrics> HourlyBreakdown { get; init; } = new();
}

/// <summary>
/// Complete simulation report.
/// </summary>
public record SimulationReport
{
    public string SimulationId { get; init; } = string.Empty;
    public DateTime SimulatedStartTime { get; init; }
    public DateTime SimulatedEndTime { get; init; }
    public TimeSpan SimulatedDuration { get; init; }
    public TimeSpan RealDuration { get; init; }
    public double AccelerationFactor { get; init; }
    
    // Summary metrics
    public int TotalGuests { get; init; }
    public int TotalTasks { get; init; }
    public int TotalDeliveries { get; init; }
    public int TotalFailures { get; init; }
    public double OverallSuccessRate { get; init; }
    public double AverageTaskDurationSeconds { get; init; }
    public double AverageGuestWaitSeconds { get; init; }
    public double AverageRobotUtilization { get; init; }
    
    // Peak analysis
    public DateTime PeakGuestTime { get; init; }
    public int PeakGuestCount { get; init; }
    public DateTime PeakTaskTime { get; init; }
    public int PeakTaskCount { get; init; }
    
    // Robot performance
    public Dictionary<int, RobotPerformanceMetrics> RobotMetrics { get; init; } = new();
    
    // Table metrics
    public Dictionary<int, TableMetrics> TableMetrics { get; init; } = new();
    
    // Daily breakdown
    public List<DailyMetrics> DailyBreakdown { get; init; } = new();
    
    // Alerts summary
    public int TotalAlerts { get; init; }
    public Dictionary<string, int> AlertsByType { get; init; } = new();
}

/// <summary>
/// Performance metrics for a single robot.
/// </summary>
public record RobotPerformanceMetrics
{
    public int RobotId { get; init; }
    public string RobotName { get; init; } = string.Empty;
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
    public double SuccessRate { get; init; }
    public double TotalDistanceTraveled { get; init; }
    public double AverageTaskDuration { get; init; }
    public double UtilizationPercent { get; init; }
    public int ChargeCycles { get; init; }
    public double AverageBattery { get; init; }
}

/// <summary>
/// Metrics for a single table.
/// </summary>
public record TableMetrics
{
    public int TableId { get; init; }
    public string? TableLabel { get; init; }
    public int GuestsServed { get; init; }
    public int Turnovers { get; init; }
    public double AverageOccupancyMinutes { get; init; }
    public double UtilizationPercent { get; init; }
}

/// <summary>
/// Interface for simulation metrics aggregation.
/// </summary>
public interface ISimulationMetricsAggregator
{
    /// <summary>
    /// Starts tracking for a new simulation.
    /// </summary>
    void StartSimulation(string simulationId, DateTime simulatedStartTime);
    
    /// <summary>
    /// Records a guest arrival.
    /// </summary>
    void RecordGuestArrival(DateTime simulatedTime, int guestId, int partySize);
    
    /// <summary>
    /// Records a guest being seated.
    /// </summary>
    void RecordGuestSeated(DateTime simulatedTime, int guestId, int tableId);
    
    /// <summary>
    /// Records a guest departure.
    /// </summary>
    void RecordGuestDeparture(DateTime simulatedTime, int guestId, int tableId);
    
    /// <summary>
    /// Records a task creation.
    /// </summary>
    void RecordTaskCreated(DateTime simulatedTime, int taskId, TaskType type);
    
    /// <summary>
    /// Records a task completion.
    /// </summary>
    void RecordTaskCompleted(DateTime simulatedTime, int taskId, int robotId, double durationSeconds);
    
    /// <summary>
    /// Records a task failure.
    /// </summary>
    void RecordTaskFailed(DateTime simulatedTime, int taskId, int? robotId, string reason);
    
    /// <summary>
    /// Records robot position/status update.
    /// </summary>
    void RecordRobotUpdate(DateTime simulatedTime, int robotId, double battery, bool isBusy);
    
    /// <summary>
    /// Records an alert.
    /// </summary>
    void RecordAlert(DateTime simulatedTime, string alertType);
    
    /// <summary>
    /// Gets current metrics snapshot.
    /// </summary>
    SimulationProgressSnapshot GetProgressSnapshot();
    
    /// <summary>
    /// Finalizes and generates the complete report.
    /// </summary>
    SimulationReport GenerateReport(DateTime simulatedEndTime, TimeSpan realDuration, double accelerationFactor);
    
    /// <summary>
    /// Resets all metrics.
    /// </summary>
    void Reset();
}

/// <summary>
/// Current progress snapshot during simulation.
/// </summary>
public record SimulationProgressSnapshot
{
    public string SimulationId { get; init; } = string.Empty;
    public DateTime CurrentSimulatedTime { get; init; }
    public int TotalGuestsToDate { get; init; }
    public int TotalTasksToDate { get; init; }
    public int TasksCompletedToDate { get; init; }
    public int TasksFailedToDate { get; init; }
    public int CurrentActiveGuests { get; init; }
    public int CurrentPendingTasks { get; init; }
    public double CurrentSuccessRate { get; init; }
}

/// <summary>
/// Collects and aggregates metrics during simulation runs.
/// </summary>
public class SimulationMetricsAggregator : ISimulationMetricsAggregator
{
    private string _simulationId = string.Empty;
    private DateTime _simulatedStartTime;
    
    // Raw event counters
    private int _totalGuests;
    private int _totalTasks;
    private int _tasksCompleted;
    private int _tasksFailed;
    private int _totalDeliveries;
    private int _totalAlerts;
    
    // Current state
    private int _activeGuests;
    private int _pendingTasks;
    
    // Hourly buckets
    private readonly ConcurrentDictionary<DateTime, HourlyMetricsBucket> _hourlyBuckets = new();
    
    // Robot tracking
    private readonly ConcurrentDictionary<int, RobotTracker> _robotTrackers = new();
    
    // Table tracking
    private readonly ConcurrentDictionary<int, TableTracker> _tableTrackers = new();
    
    // Alert tracking
    private readonly ConcurrentDictionary<string, int> _alertCounts = new();
    
    // Task duration tracking
    private readonly ConcurrentBag<double> _taskDurations = new();
    private readonly ConcurrentBag<double> _waitTimes = new();
    
    // Peak tracking
    private int _peakGuests;
    private DateTime _peakGuestTime;
    private int _peakTasks;
    private DateTime _peakTaskTime;
    
    private readonly object _lock = new();
    
    public void StartSimulation(string simulationId, DateTime simulatedStartTime)
    {
        Reset();
        _simulationId = simulationId;
        _simulatedStartTime = simulatedStartTime;
    }
    
    public void RecordGuestArrival(DateTime simulatedTime, int guestId, int partySize)
    {
        lock (_lock)
        {
            _totalGuests++;
            _activeGuests += partySize;
            
            if (_activeGuests > _peakGuests)
            {
                _peakGuests = _activeGuests;
                _peakGuestTime = simulatedTime;
            }
            
            var bucket = GetOrCreateBucket(simulatedTime);
            bucket.GuestsArrived++;
        }
    }
    
    public void RecordGuestSeated(DateTime simulatedTime, int guestId, int tableId)
    {
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.GuestsSeated++;
        
        var tableTracker = _tableTrackers.GetOrAdd(tableId, _ => new TableTracker { TableId = tableId });
        tableTracker.RecordOccupied(simulatedTime);
    }
    
    public void RecordGuestDeparture(DateTime simulatedTime, int guestId, int tableId)
    {
        lock (_lock)
        {
            _activeGuests = Math.Max(0, _activeGuests - 1);
        }
        
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.GuestsDeparted++;
        
        if (_tableTrackers.TryGetValue(tableId, out var tableTracker))
        {
            tableTracker.RecordVacated(simulatedTime);
        }
    }
    
    public void RecordTaskCreated(DateTime simulatedTime, int taskId, TaskType type)
    {
        lock (_lock)
        {
            _totalTasks++;
            _pendingTasks++;
            
            if (_pendingTasks > _peakTasks)
            {
                _peakTasks = _pendingTasks;
                _peakTaskTime = simulatedTime;
            }
        }
        
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.TasksCreated++;
    }
    
    public void RecordTaskCompleted(DateTime simulatedTime, int taskId, int robotId, double durationSeconds)
    {
        lock (_lock)
        {
            _tasksCompleted++;
            _pendingTasks = Math.Max(0, _pendingTasks - 1);
            _totalDeliveries++;
        }
        
        _taskDurations.Add(durationSeconds);
        
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.TasksCompleted++;
        bucket.DeliveriesCompleted++;
        
        var robotTracker = _robotTrackers.GetOrAdd(robotId, _ => new RobotTracker { RobotId = robotId });
        robotTracker.TasksCompleted++;
        robotTracker.TotalTaskDuration += durationSeconds;
    }
    
    public void RecordTaskFailed(DateTime simulatedTime, int taskId, int? robotId, string reason)
    {
        lock (_lock)
        {
            _tasksFailed++;
            _pendingTasks = Math.Max(0, _pendingTasks - 1);
        }
        
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.TasksFailed++;
        
        if (robotId.HasValue)
        {
            var robotTracker = _robotTrackers.GetOrAdd(robotId.Value, _ => new RobotTracker { RobotId = robotId.Value });
            robotTracker.TasksFailed++;
        }
    }
    
    public void RecordRobotUpdate(DateTime simulatedTime, int robotId, double battery, bool isBusy)
    {
        var robotTracker = _robotTrackers.GetOrAdd(robotId, _ => new RobotTracker { RobotId = robotId });
        robotTracker.RecordUpdate(simulatedTime, battery, isBusy);
    }
    
    public void RecordAlert(DateTime simulatedTime, string alertType)
    {
        lock (_lock)
        {
            _totalAlerts++;
        }
        
        _alertCounts.AddOrUpdate(alertType, 1, (_, count) => count + 1);
        
        var bucket = GetOrCreateBucket(simulatedTime);
        bucket.AlertsGenerated++;
    }
    
    public SimulationProgressSnapshot GetProgressSnapshot()
    {
        lock (_lock)
        {
            var successRate = _tasksCompleted + _tasksFailed > 0
                ? (double)_tasksCompleted / (_tasksCompleted + _tasksFailed) * 100
                : 100;
            
            return new SimulationProgressSnapshot
            {
                SimulationId = _simulationId,
                CurrentSimulatedTime = DateTime.UtcNow, // Will be updated by caller
                TotalGuestsToDate = _totalGuests,
                TotalTasksToDate = _totalTasks,
                TasksCompletedToDate = _tasksCompleted,
                TasksFailedToDate = _tasksFailed,
                CurrentActiveGuests = _activeGuests,
                CurrentPendingTasks = _pendingTasks,
                CurrentSuccessRate = successRate
            };
        }
    }
    
    public SimulationReport GenerateReport(DateTime simulatedEndTime, TimeSpan realDuration, double accelerationFactor)
    {
        var avgTaskDuration = _taskDurations.Count > 0 ? _taskDurations.Average() : 0;
        var avgWaitTime = _waitTimes.Count > 0 ? _waitTimes.Average() : 0;
        var successRate = _tasksCompleted + _tasksFailed > 0
            ? (double)_tasksCompleted / (_tasksCompleted + _tasksFailed) * 100
            : 100;
        
        // Calculate robot utilization
        var avgRobotUtilization = _robotTrackers.Count > 0
            ? _robotTrackers.Values.Average(r => r.GetUtilizationPercent())
            : 0;
        
        // Build hourly metrics
        var hourlyMetrics = _hourlyBuckets
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value.ToHourlyMetrics())
            .ToList();
        
        // Build daily breakdown
        var dailyMetrics = hourlyMetrics
            .GroupBy(h => h.HourStart.Date)
            .Select(g => new DailyMetrics
            {
                Date = g.Key,
                DayOfWeek = g.Key.DayOfWeek,
                TotalGuests = g.Sum(h => h.GuestsArrived),
                TotalTasks = g.Sum(h => h.TasksCreated),
                TotalDeliveries = g.Sum(h => h.DeliveriesCompleted),
                TotalFailures = g.Sum(h => h.TasksFailed),
                SuccessRate = g.Sum(h => h.TasksCompleted) + g.Sum(h => h.TasksFailed) > 0
                    ? (double)g.Sum(h => h.TasksCompleted) / (g.Sum(h => h.TasksCompleted) + g.Sum(h => h.TasksFailed)) * 100
                    : 100,
                PeakHourGuests = g.Max(h => h.GuestsArrived),
                PeakHour = g.OrderByDescending(h => h.GuestsArrived).First().HourStart.Hour,
                AverageTableTurnover = _tableTrackers.Count > 0 ? _tableTrackers.Values.Average(t => t.Turnovers) : 0,
                AverageRobotUtilization = avgRobotUtilization,
                HourlyBreakdown = g.ToList()
            })
            .ToList();
        
        // Build robot metrics
        var robotMetrics = _robotTrackers.ToDictionary(
            kv => kv.Key,
            kv => new RobotPerformanceMetrics
            {
                RobotId = kv.Key,
                RobotName = $"Robot-{kv.Key}",
                TasksCompleted = kv.Value.TasksCompleted,
                TasksFailed = kv.Value.TasksFailed,
                SuccessRate = kv.Value.TasksCompleted + kv.Value.TasksFailed > 0
                    ? (double)kv.Value.TasksCompleted / (kv.Value.TasksCompleted + kv.Value.TasksFailed) * 100
                    : 100,
                AverageTaskDuration = kv.Value.TasksCompleted > 0 ? kv.Value.TotalTaskDuration / kv.Value.TasksCompleted : 0,
                UtilizationPercent = kv.Value.GetUtilizationPercent(),
                AverageBattery = kv.Value.GetAverageBattery()
            }
        );
        
        // Build table metrics
        var tableMetrics = _tableTrackers.ToDictionary(
            kv => kv.Key,
            kv => new TableMetrics
            {
                TableId = kv.Key,
                TableLabel = $"Table-{kv.Key}",
                GuestsServed = kv.Value.GuestsServed,
                Turnovers = kv.Value.Turnovers,
                AverageOccupancyMinutes = kv.Value.GetAverageOccupancyMinutes(),
                UtilizationPercent = kv.Value.GetUtilizationPercent(simulatedEndTime - _simulatedStartTime)
            }
        );
        
        return new SimulationReport
        {
            SimulationId = _simulationId,
            SimulatedStartTime = _simulatedStartTime,
            SimulatedEndTime = simulatedEndTime,
            SimulatedDuration = simulatedEndTime - _simulatedStartTime,
            RealDuration = realDuration,
            AccelerationFactor = accelerationFactor,
            TotalGuests = _totalGuests,
            TotalTasks = _totalTasks,
            TotalDeliveries = _totalDeliveries,
            TotalFailures = _tasksFailed,
            OverallSuccessRate = successRate,
            AverageTaskDurationSeconds = avgTaskDuration,
            AverageGuestWaitSeconds = avgWaitTime,
            AverageRobotUtilization = avgRobotUtilization,
            PeakGuestTime = _peakGuestTime,
            PeakGuestCount = _peakGuests,
            PeakTaskTime = _peakTaskTime,
            PeakTaskCount = _peakTasks,
            RobotMetrics = robotMetrics,
            TableMetrics = tableMetrics,
            DailyBreakdown = dailyMetrics,
            TotalAlerts = _totalAlerts,
            AlertsByType = new Dictionary<string, int>(_alertCounts)
        };
    }
    
    public void Reset()
    {
        _simulationId = string.Empty;
        _simulatedStartTime = DateTime.MinValue;
        _totalGuests = 0;
        _totalTasks = 0;
        _tasksCompleted = 0;
        _tasksFailed = 0;
        _totalDeliveries = 0;
        _totalAlerts = 0;
        _activeGuests = 0;
        _pendingTasks = 0;
        _peakGuests = 0;
        _peakGuestTime = DateTime.MinValue;
        _peakTasks = 0;
        _peakTaskTime = DateTime.MinValue;
        
        _hourlyBuckets.Clear();
        _robotTrackers.Clear();
        _tableTrackers.Clear();
        _alertCounts.Clear();
        
        while (_taskDurations.TryTake(out _)) { }
        while (_waitTimes.TryTake(out _)) { }
    }
    
    private HourlyMetricsBucket GetOrCreateBucket(DateTime time)
    {
        var hourStart = new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Kind);
        return _hourlyBuckets.GetOrAdd(hourStart, _ => new HourlyMetricsBucket { HourStart = hourStart });
    }
}

/// <summary>
/// Internal bucket for aggregating hourly metrics.
/// </summary>
internal class HourlyMetricsBucket
{
    public DateTime HourStart { get; init; }
    public int GuestsArrived;
    public int GuestsSeated;
    public int GuestsDeparted;
    public int TasksCreated;
    public int TasksCompleted;
    public int TasksFailed;
    public int DeliveriesCompleted;
    public int AlertsGenerated;
    public int PeakConcurrentGuests;
    
    public HourlyMetrics ToHourlyMetrics() => new()
    {
        HourStart = HourStart,
        GuestsArrived = GuestsArrived,
        GuestsSeated = GuestsSeated,
        GuestsDeparted = GuestsDeparted,
        TasksCreated = TasksCreated,
        TasksCompleted = TasksCompleted,
        TasksFailed = TasksFailed,
        DeliveriesCompleted = DeliveriesCompleted,
        AlertsGenerated = AlertsGenerated,
        PeakConcurrentGuests = PeakConcurrentGuests
    };
}

/// <summary>
/// Internal tracker for robot performance.
/// </summary>
internal class RobotTracker
{
    public int RobotId { get; init; }
    public int TasksCompleted;
    public int TasksFailed;
    public double TotalTaskDuration;
    
    private readonly List<double> _batteryReadings = new();
    private int _busyUpdates;
    private int _totalUpdates;
    
    public void RecordUpdate(DateTime time, double battery, bool isBusy)
    {
        _batteryReadings.Add(battery);
        _totalUpdates++;
        if (isBusy) _busyUpdates++;
    }
    
    public double GetUtilizationPercent() => _totalUpdates > 0 ? (double)_busyUpdates / _totalUpdates * 100 : 0;
    public double GetAverageBattery() => _batteryReadings.Count > 0 ? _batteryReadings.Average() : 100;
}

/// <summary>
/// Internal tracker for table usage.
/// </summary>
internal class TableTracker
{
    public int TableId { get; init; }
    public int GuestsServed;
    public int Turnovers;
    
    private DateTime? _currentOccupiedSince;
    private readonly List<double> _occupancyDurations = new();
    private TimeSpan _totalOccupiedTime = TimeSpan.Zero;
    
    public void RecordOccupied(DateTime time)
    {
        _currentOccupiedSince = time;
        GuestsServed++;
    }
    
    public void RecordVacated(DateTime time)
    {
        if (_currentOccupiedSince.HasValue)
        {
            var duration = time - _currentOccupiedSince.Value;
            _occupancyDurations.Add(duration.TotalMinutes);
            _totalOccupiedTime += duration;
            Turnovers++;
        }
        _currentOccupiedSince = null;
    }
    
    public double GetAverageOccupancyMinutes() => _occupancyDurations.Count > 0 ? _occupancyDurations.Average() : 0;
    public double GetUtilizationPercent(TimeSpan totalSimTime) => totalSimTime.TotalMinutes > 0 
        ? _totalOccupiedTime.TotalMinutes / totalSimTime.TotalMinutes * 100 
        : 0;
}
