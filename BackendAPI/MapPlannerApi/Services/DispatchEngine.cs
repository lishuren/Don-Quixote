using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Services;

/// <summary>
/// Interface for the dispatch engine that handles automatic task assignment.
/// </summary>
public interface IDispatchEngine
{
    Task<AutoAssignResult> AutoAssignPendingTasks();
    Task<List<DispatchQueueItem>> GetDispatchQueue();
    Task<DispatchConfig> GetConfig();
    Task UpdateConfig(DispatchConfig config);
    Task<int?> FindBestRobotForTask(RobotTask task);
}

/// <summary>
/// Dispatch engine for automatic task assignment to robots.
/// Supports multiple assignment algorithms and failover logic.
/// </summary>
public class DispatchEngine : IDispatchEngine
{
    private readonly RestaurantDbContext _db;
    private readonly IEventBroadcaster? _broadcaster;
    private readonly ILogger<DispatchEngine> _logger;

    // Configuration (in production, this would be persisted)
    private DispatchConfig _config = new(
        Algorithm: "nearest",
        MaxTasksPerRobot: 3,
        MinBatteryForAssignment: 20,
        AutoAssignEnabled: true,
        AssignmentIntervalSeconds: 10
    );

    public DispatchEngine(RestaurantDbContext db, ILogger<DispatchEngine> logger, IEventBroadcaster? broadcaster = null)
    {
        _db = db;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public Task<DispatchConfig> GetConfig() => Task.FromResult(_config);

    public Task UpdateConfig(DispatchConfig config)
    {
        _config = config;
        _logger.LogInformation("Dispatch config updated: Algorithm={Algorithm}, MaxTasks={MaxTasks}", 
            config.Algorithm, config.MaxTasksPerRobot);
        return Task.CompletedTask;
    }

    public async Task<List<DispatchQueueItem>> GetDispatchQueue()
    {
        var pendingTasks = await _db.Tasks
            .Include(t => t.TargetTable)
            .Where(t => t.Status == TaskStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        var queueItems = new List<DispatchQueueItem>();

        foreach (var task in pendingTasks)
        {
            var suggestedRobotId = await FindBestRobotForTask(task);
            Robot? suggestedRobot = null;
            
            if (suggestedRobotId.HasValue)
            {
                suggestedRobot = await _db.Robots.FindAsync(suggestedRobotId.Value);
            }

            queueItems.Add(new DispatchQueueItem(
                task.Id,
                task.Type.ToString(),
                task.Priority.ToString(),
                task.CreatedAt,
                suggestedRobotId,
                suggestedRobot?.Name,
                suggestedRobot != null ? CalculateDistance(suggestedRobot.Position, task.TargetPosition) : null
            ));
        }

        return queueItems;
    }

    public async Task<AutoAssignResult> AutoAssignPendingTasks()
    {
        if (!_config.AutoAssignEnabled)
        {
            _logger.LogDebug("Auto-assign is disabled");
            return new AutoAssignResult(0, 0, new List<TaskAssignment>());
        }

        var pendingTasks = await _db.Tasks
            .Where(t => t.Status == TaskStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        var assignments = new List<TaskAssignment>();
        int skipped = 0;

        foreach (var task in pendingTasks)
        {
            var robotId = await FindBestRobotForTask(task);
            
            if (robotId.HasValue)
            {
                var robot = await _db.Robots.FindAsync(robotId.Value);
                if (robot != null)
                {
                    // Assign task to robot
                    task.RobotId = robotId.Value;
                    task.Status = TaskStatus.Assigned;
                    
                    assignments.Add(new TaskAssignment(
                        task.Id,
                        robot.Id,
                        robot.Name,
                        GetAssignmentReason(robot, task)
                    ));

                    _logger.LogInformation("Auto-assigned task {TaskId} to robot {RobotId} ({RobotName})", 
                        task.Id, robot.Id, robot.Name);

                    // Broadcast task status change
                    if (_broadcaster != null)
                    {
                        await _broadcaster.BroadcastTaskStatusChanged(
                            task.Id, task.Type.ToString(), "Pending", "Assigned", robot.Id, robot.Name);
                    }
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                skipped++;
                _logger.LogDebug("No available robot for task {TaskId}", task.Id);
            }
        }

        if (assignments.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return new AutoAssignResult(assignments.Count, skipped, assignments);
    }

    public async Task<int?> FindBestRobotForTask(RobotTask task)
    {
        var availableRobots = await _db.Robots
            .Where(r => r.IsEnabled && 
                        r.Status == RobotStatus.Idle && 
                        r.BatteryLevel >= _config.MinBatteryForAssignment)
            .ToListAsync();

        if (!availableRobots.Any())
            return null;

        // Count current tasks per robot
        var taskCounts = await _db.Tasks
            .Where(t => t.RobotId.HasValue && 
                       (t.Status == TaskStatus.Assigned || t.Status == TaskStatus.InProgress))
            .GroupBy(t => t.RobotId)
            .Select(g => new { RobotId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RobotId!.Value, x => x.Count);

        // Filter robots that haven't exceeded max tasks
        availableRobots = availableRobots
            .Where(r => !taskCounts.ContainsKey(r.Id) || taskCounts[r.Id] < _config.MaxTasksPerRobot)
            .ToList();

        if (!availableRobots.Any())
            return null;

        return _config.Algorithm.ToLower() switch
        {
            "nearest" => FindNearestRobot(availableRobots, task.TargetPosition),
            "round_robin" => FindRoundRobinRobot(availableRobots, taskCounts),
            "load_balanced" => FindLoadBalancedRobot(availableRobots, taskCounts),
            "priority" => FindPriorityRobot(availableRobots, task),
            _ => FindNearestRobot(availableRobots, task.TargetPosition)
        };
    }

    private int? FindNearestRobot(List<Robot> robots, Position targetPosition)
    {
        return robots
            .OrderBy(r => CalculateDistance(r.Position, targetPosition))
            .FirstOrDefault()?.Id;
    }

    private int? FindRoundRobinRobot(List<Robot> robots, Dictionary<int, int> taskCounts)
    {
        // Find robot with lowest recent task count
        return robots
            .OrderBy(r => taskCounts.GetValueOrDefault(r.Id, 0))
            .ThenBy(r => r.Id)
            .FirstOrDefault()?.Id;
    }

    private int? FindLoadBalancedRobot(List<Robot> robots, Dictionary<int, int> taskCounts)
    {
        // Balance by task count and battery level
        return robots
            .OrderBy(r => taskCounts.GetValueOrDefault(r.Id, 0))
            .ThenByDescending(r => r.BatteryLevel)
            .FirstOrDefault()?.Id;
    }

    private int? FindPriorityRobot(List<Robot> robots, RobotTask task)
    {
        // For high priority tasks, prefer robots with higher battery
        if (task.Priority >= TaskPriority.High)
        {
            return robots
                .OrderByDescending(r => r.BatteryLevel)
                .FirstOrDefault()?.Id;
        }

        return FindNearestRobot(robots, task.TargetPosition);
    }

    private double CalculateDistance(Position from, Position to)
    {
        // Use physical coordinates for distance calculation
        var dx = to.PhysicalX - from.PhysicalX;
        var dy = to.PhysicalY - from.PhysicalY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private string GetAssignmentReason(Robot robot, RobotTask task)
    {
        return _config.Algorithm.ToLower() switch
        {
            "nearest" => $"Nearest available robot ({CalculateDistance(robot.Position, task.TargetPosition):F1}m)",
            "round_robin" => "Round-robin selection",
            "load_balanced" => $"Load balanced (battery: {robot.BatteryLevel}%)",
            "priority" => "Priority-based selection",
            _ => "Auto-assigned"
        };
    }
}
