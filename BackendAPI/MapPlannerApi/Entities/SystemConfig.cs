using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

/// <summary>
/// Represents a system configuration key-value pair.
/// </summary>
public class SystemConfig
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Value { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Data type hint for the value</summary>
    [MaxLength(20)]
    public string ValueType { get; set; } = "string";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Commonly used configuration keys as constants.
/// </summary>
public static class ConfigKeys
{
    public const string PixelsPerMeter = "map.pixelsPerMeter";
    public const string DefaultRobotSpeed = "robot.defaultSpeed";
    public const string LowBatteryThreshold = "robot.lowBatteryThreshold";
    public const string MaxConcurrentTasks = "task.maxConcurrent";
    public const string TaskRetryLimit = "task.retryLimit";
    public const string AlertRetentionDays = "alert.retentionDays";
    public const string ReservationLeadTimeMinutes = "reservation.leadTimeMinutes";
    public const string WaitlistEnabled = "guest.waitlistEnabled";
}
