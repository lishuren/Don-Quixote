using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum RobotStatus
{
    Idle,
    Navigating,
    Delivering,
    Returning,
    Charging,
    Error,
    Offline
}

/// <summary>
/// Represents a delivery robot in the restaurant.
/// </summary>
public class Robot
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Model { get; set; }

    public RobotStatus Status { get; set; } = RobotStatus.Idle;

    /// <summary>Current position with dual coordinates</summary>
    public Position Position { get; set; } = new();

    /// <summary>Heading angle in degrees (0-360)</summary>
    public double Heading { get; set; }

    /// <summary>Battery level percentage (0-100)</summary>
    public double BatteryLevel { get; set; } = 100;

    /// <summary>Current velocity in m/s</summary>
    public double Velocity { get; set; }

    /// <summary>ID of current task, if any</summary>
    public int? CurrentTaskId { get; set; }

    /// <summary>Navigation reference for current task</summary>
    public RobotTask? CurrentTask { get; set; }

    /// <summary>Whether robot is enabled for task assignment</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Last time position was updated</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>Last heartbeat timestamp from robot</summary>
    public DateTime? LastHeartbeat { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>All tasks assigned to this robot</summary>
    public ICollection<RobotTask> Tasks { get; set; } = new List<RobotTask>();
}
