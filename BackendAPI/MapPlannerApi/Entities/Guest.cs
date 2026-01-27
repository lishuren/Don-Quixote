using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum GuestStatus
{
    Waiting,
    Seated,
    Ordering,
    Dining,
    RequestingBill,
    Departed
}

/// <summary>
/// Represents a guest or party in the restaurant.
/// </summary>
public class Guest
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Number of people in the party</summary>
    public int PartySize { get; set; } = 1;

    public GuestStatus Status { get; set; } = GuestStatus.Waiting;

    /// <summary>Table assigned to this guest</summary>
    public int? TableId { get; set; }
    public TableEntity? Table { get; set; }

    /// <summary>Queue position for waiting guests</summary>
    public int? QueuePosition { get; set; }

    /// <summary>Time guest arrived</summary>
    public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;

    /// <summary>Time guest was seated</summary>
    public DateTime? SeatedTime { get; set; }

    /// <summary>Time guest departed</summary>
    public DateTime? DepartedTime { get; set; }

    /// <summary>Estimated wait time in minutes</summary>
    public int? EstimatedWaitMinutes { get; set; }

    /// <summary>Special requests or notes</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Phone number for waitlist notification</summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Associated reservation if any</summary>
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
