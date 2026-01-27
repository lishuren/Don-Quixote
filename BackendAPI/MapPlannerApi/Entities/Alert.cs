using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum AlertType
{
    RobotError,
    LowBattery,
    NavigationBlocked,
    TaskFailed,
    SystemWarning,
    TableService,
    GuestWaiting,
    Custom
}

/// <summary>
/// Represents an alert/notification in the system.
/// </summary>
public class Alert
{
    [Key]
    public int Id { get; set; }

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>Related robot if applicable</summary>
    public int? RobotId { get; set; }
    public Robot? Robot { get; set; }

    /// <summary>Related table if applicable</summary>
    public int? TableId { get; set; }
    public TableEntity? Table { get; set; }

    /// <summary>Related task if applicable</summary>
    public int? TaskId { get; set; }
    public RobotTask? Task { get; set; }

    /// <summary>Whether the alert has been acknowledged</summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>User who acknowledged the alert</summary>
    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Whether the alert has been resolved</summary>
    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
