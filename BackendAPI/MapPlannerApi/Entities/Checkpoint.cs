using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum CheckpointType
{
    Waypoint,
    ChargingStation,
    PickupPoint,
    DropoffPoint,
    WaitingArea,
    Intersection
}

/// <summary>
/// Represents a checkpoint/waypoint for robot navigation.
/// </summary>
public class Checkpoint
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public CheckpointType Type { get; set; } = CheckpointType.Waypoint;

    /// <summary>Position with dual coordinates</summary>
    public Position Position { get; set; } = new();

    /// <summary>Required heading angle when reaching this checkpoint (null = any)</summary>
    public double? RequiredHeading { get; set; }

    /// <summary>Zone this checkpoint belongs to</summary>
    public int? ZoneId { get; set; }
    public Zone? Zone { get; set; }

    /// <summary>Whether this is a mandatory stop point</summary>
    public bool IsMandatoryStop { get; set; }

    /// <summary>Order in a predefined route (null if not part of a route)</summary>
    public int? RouteOrder { get; set; }

    /// <summary>Maximum robots allowed at this checkpoint simultaneously</summary>
    public int Capacity { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
