using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum TaskType
{
    Deliver,
    Return,
    Charge,
    Patrol,
    Escort,
    Greeting,
    Service,
    Cleaning,
    Custom
}

public enum TaskStatus
{
    Pending,
    Assigned,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Represents a task assigned to a robot.
/// </summary>
public class RobotTask
{
    [Key]
    public int Id { get; set; }

    public TaskType Type { get; set; } = TaskType.Deliver;

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>Robot assigned to this task</summary>
    public int? RobotId { get; set; }
    public Robot? Robot { get; set; }

    /// <summary>Target table for delivery tasks</summary>
    public int? TargetTableId { get; set; }
    public TableEntity? TargetTable { get; set; }

    /// <summary>Starting position for the task</summary>
    public Position StartPosition { get; set; } = new();

    /// <summary>Target position for the task</summary>
    public Position TargetPosition { get; set; } = new();

    /// <summary>Computed path as JSON array of waypoints</summary>
    public string? PathJson { get; set; }

    /// <summary>Estimated duration in seconds</summary>
    public int? EstimatedDurationSeconds { get; set; }

    /// <summary>Actual duration in seconds</summary>
    public int? ActualDurationSeconds { get; set; }

    /// <summary>Error message if task failed</summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>Number of retry attempts</summary>
    public int RetryCount { get; set; }

    /// <summary>Maximum retries allowed (0 = unlimited)</summary>
    public int MaxRetryCount { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
