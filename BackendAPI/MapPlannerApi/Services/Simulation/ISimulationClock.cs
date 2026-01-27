using System.Collections.Concurrent;

namespace MapPlannerApi.Services.Simulation;

/// <summary>
/// Represents time acceleration modes for simulation.
/// </summary>
public enum TimeAcceleration
{
    /// <summary>Real-time (1x speed)</summary>
    RealTime = 1,
    /// <summary>10x speed - 1 hour simulates ~6 min real</summary>
    Fast = 10,
    /// <summary>60x speed - 1 hour simulates 1 min real</summary>
    VeryFast = 60,
    /// <summary>720x speed - 1 month simulates ~1 hour real</summary>
    Monthly = 720,
    /// <summary>8640x speed - 1 year simulates ~1 hour real</summary>
    Yearly = 8640
}

/// <summary>
/// Interface for the simulation clock service.
/// Provides virtual time that can be accelerated for long-duration simulations.
/// </summary>
public interface ISimulationClock
{
    /// <summary>
    /// Current virtual time in the simulation.
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// Start time of the current simulation.
    /// </summary>
    DateTime SimulationStartTime { get; }
    
    /// <summary>
    /// End time target for the simulation (if set).
    /// </summary>
    DateTime? SimulationEndTime { get; }
    
    /// <summary>
    /// Whether a simulation is currently running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Whether the simulation is paused.
    /// </summary>
    bool IsPaused { get; }
    
    /// <summary>
    /// Current time acceleration factor.
    /// </summary>
    double AccelerationFactor { get; }
    
    /// <summary>
    /// Real elapsed time since simulation started.
    /// </summary>
    TimeSpan RealElapsedTime { get; }
    
    /// <summary>
    /// Simulated elapsed time since simulation started.
    /// </summary>
    TimeSpan SimulatedElapsedTime { get; }
    
    /// <summary>
    /// Progress percentage (0-100) if end time is set.
    /// </summary>
    double ProgressPercent { get; }
    
    /// <summary>
    /// Starts a new simulation with the given parameters.
    /// </summary>
    void Start(DateTime startTime, DateTime? endTime = null, double accelerationFactor = 1.0);
    
    /// <summary>
    /// Stops the current simulation.
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Pauses the simulation clock.
    /// </summary>
    void Pause();
    
    /// <summary>
    /// Resumes a paused simulation.
    /// </summary>
    void Resume();
    
    /// <summary>
    /// Sets the acceleration factor.
    /// </summary>
    void SetAcceleration(double factor);
    
    /// <summary>
    /// Advances the simulation clock by a specified amount.
    /// Used for tick-based simulation loops.
    /// </summary>
    void AdvanceBy(TimeSpan amount);
    
    /// <summary>
    /// Event raised when simulation time changes (throttled).
    /// </summary>
    event EventHandler<SimulationTimeEventArgs>? TimeChanged;
}

/// <summary>
/// Event arguments for simulation time changes.
/// </summary>
public class SimulationTimeEventArgs : EventArgs
{
    public DateTime SimulatedTime { get; init; }
    public TimeSpan RealElapsed { get; init; }
    public TimeSpan SimulatedElapsed { get; init; }
    public double ProgressPercent { get; init; }
}

/// <summary>
/// Implementation of the simulation clock with time acceleration support.
/// </summary>
public class SimulationClock : ISimulationClock
{
    private DateTime _simulationStartTime;
    private DateTime? _simulationEndTime;
    private DateTime _currentTime;
    private DateTime _realStartTime;
    private DateTime? _pausedAt;
    private TimeSpan _totalPausedTime;
    private double _accelerationFactor = 1.0;
    private bool _isRunning;
    private bool _isPaused;
    
    private readonly object _lock = new();
    private DateTime _lastEventTime = DateTime.MinValue;
    private readonly TimeSpan _eventThrottle = TimeSpan.FromMilliseconds(100); // Max 10 events/sec
    
    public DateTime Now
    {
        get
        {
            lock (_lock)
            {
                if (!_isRunning || _isPaused)
                    return _currentTime;
                
                // Calculate current virtual time based on real elapsed time and acceleration
                var realElapsed = DateTime.UtcNow - _realStartTime - _totalPausedTime;
                var simulatedElapsed = TimeSpan.FromTicks((long)(realElapsed.Ticks * _accelerationFactor));
                var calculatedTime = _simulationStartTime + simulatedElapsed;
                
                // Don't exceed end time
                if (_simulationEndTime.HasValue && calculatedTime > _simulationEndTime.Value)
                    return _simulationEndTime.Value;
                    
                return calculatedTime;
            }
        }
    }
    
    public DateTime SimulationStartTime => _simulationStartTime;
    public DateTime? SimulationEndTime => _simulationEndTime;
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public double AccelerationFactor => _accelerationFactor;
    
    public TimeSpan RealElapsedTime
    {
        get
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return TimeSpan.Zero;
                if (_isPaused && _pausedAt.HasValue)
                    return _pausedAt.Value - _realStartTime - _totalPausedTime;
                return DateTime.UtcNow - _realStartTime - _totalPausedTime;
            }
        }
    }
    
    public TimeSpan SimulatedElapsedTime
    {
        get
        {
            lock (_lock)
            {
                return Now - _simulationStartTime;
            }
        }
    }
    
    public double ProgressPercent
    {
        get
        {
            lock (_lock)
            {
                if (!_simulationEndTime.HasValue || !_isRunning)
                    return 0;
                
                var total = (_simulationEndTime.Value - _simulationStartTime).TotalSeconds;
                var elapsed = SimulatedElapsedTime.TotalSeconds;
                
                if (total <= 0) return 100;
                return Math.Min(100, (elapsed / total) * 100);
            }
        }
    }
    
    public event EventHandler<SimulationTimeEventArgs>? TimeChanged;
    
    public void Start(DateTime startTime, DateTime? endTime = null, double accelerationFactor = 1.0)
    {
        lock (_lock)
        {
            _simulationStartTime = startTime;
            _simulationEndTime = endTime;
            _currentTime = startTime;
            _realStartTime = DateTime.UtcNow;
            _pausedAt = null;
            _totalPausedTime = TimeSpan.Zero;
            _accelerationFactor = Math.Max(0.1, accelerationFactor);
            _isRunning = true;
            _isPaused = false;
            
            RaiseTimeChanged();
        }
    }
    
    public void Stop()
    {
        lock (_lock)
        {
            _currentTime = Now; // Capture final time
            _isRunning = false;
            _isPaused = false;
            
            RaiseTimeChanged();
        }
    }
    
    public void Pause()
    {
        lock (_lock)
        {
            if (!_isRunning || _isPaused) return;
            
            _currentTime = Now; // Capture current time
            _pausedAt = DateTime.UtcNow;
            _isPaused = true;
            
            RaiseTimeChanged();
        }
    }
    
    public void Resume()
    {
        lock (_lock)
        {
            if (!_isRunning || !_isPaused || !_pausedAt.HasValue) return;
            
            _totalPausedTime += DateTime.UtcNow - _pausedAt.Value;
            _pausedAt = null;
            _isPaused = false;
            
            RaiseTimeChanged();
        }
    }
    
    public void SetAcceleration(double factor)
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            
            // Capture current simulated time before changing acceleration
            _currentTime = Now;
            _realStartTime = DateTime.UtcNow;
            _totalPausedTime = TimeSpan.Zero;
            _simulationStartTime = _currentTime;
            _accelerationFactor = Math.Max(0.1, factor);
            
            RaiseTimeChanged();
        }
    }
    
    public void AdvanceBy(TimeSpan amount)
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            
            _currentTime = _currentTime.Add(amount);
            
            // Check end time
            if (_simulationEndTime.HasValue && _currentTime >= _simulationEndTime.Value)
            {
                _currentTime = _simulationEndTime.Value;
                _isRunning = false;
            }
            
            RaiseTimeChanged();
        }
    }
    
    private void RaiseTimeChanged()
    {
        var now = DateTime.UtcNow;
        if (now - _lastEventTime < _eventThrottle)
            return;
        
        _lastEventTime = now;
        
        TimeChanged?.Invoke(this, new SimulationTimeEventArgs
        {
            SimulatedTime = Now,
            RealElapsed = RealElapsedTime,
            SimulatedElapsed = SimulatedElapsedTime,
            ProgressPercent = ProgressPercent
        });
    }
}
