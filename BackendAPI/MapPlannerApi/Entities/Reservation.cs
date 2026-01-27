using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Seated,
    Completed,
    Cancelled,
    NoShow
}

/// <summary>
/// Represents a table reservation.
/// </summary>
public class Reservation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Number of guests</summary>
    public int PartySize { get; set; } = 2;

    /// <summary>Reserved table</summary>
    public int? TableId { get; set; }
    public TableEntity? Table { get; set; }

    /// <summary>Reservation date and time</summary>
    public DateTime ReservationTime { get; set; }

    /// <summary>Expected duration in minutes</summary>
    public int DurationMinutes { get; set; } = 90;

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [MaxLength(500)]
    public string? SpecialRequests { get; set; }

    /// <summary>Associated guest record when seated</summary>
    public int? GuestId { get; set; }
    public Guest? Guest { get; set; }

    /// <summary>Confirmation code sent to guest</summary>
    [MaxLength(20)]
    public string? ConfirmationCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
