using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    NeedsService,
    Cleaning
}

public enum TableShape
{
    Rectangle,
    Circle,
    Square
}

/// <summary>
/// Represents a dining table in the restaurant.
/// Named TableEntity to avoid conflict with existing Table model.
/// </summary>
public class TableEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Label { get; set; }

    public TableShape Shape { get; set; } = TableShape.Rectangle;

    public TableStatus Status { get; set; } = TableStatus.Available;

    /// <summary>Center position with dual coordinates</summary>
    public Position Center { get; set; } = new();

    /// <summary>Width in pixels (screen coordinates)</summary>
    public double Width { get; set; }

    /// <summary>Height in pixels (screen coordinates)</summary>
    public double Height { get; set; }

    /// <summary>Rotation angle in degrees</summary>
    public double Rotation { get; set; }

    /// <summary>Seating capacity</summary>
    public int Capacity { get; set; } = 4;

    /// <summary>Zone this table belongs to</summary>
    public int? ZoneId { get; set; }
    public Zone? Zone { get; set; }

    /// <summary>Current guests seated at this table</summary>
    public ICollection<Guest> Guests { get; set; } = new List<Guest>();

    /// <summary>Reservations for this table</summary>
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
