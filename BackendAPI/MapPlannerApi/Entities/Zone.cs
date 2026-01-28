using System.ComponentModel.DataAnnotations;

namespace MapPlannerApi.Entities;

public enum ZoneType
{
    Dining,
    Kitchen,
    Entrance,
    Bar,
    Restroom,
    Storage,
    Charging,
    Corridor,
    Restricted,
}
    // removed Table per new design

/// <summary>
/// Represents a zone/area in the restaurant floor plan.
/// </summary>
public class Zone
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ZoneType Type { get; set; } = ZoneType.Dining;

    /// <summary>Top-left X in screen coordinates</summary>
    public double X { get; set; }

    /// <summary>Top-left Y in screen coordinates</summary>
    public double Y { get; set; }

    /// <summary>Width in screen coordinates</summary>
    public double Width { get; set; }

    /// <summary>Height in screen coordinates</summary>
    public double Height { get; set; }

    /// <summary>Display color (hex format)</summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>Whether robots can navigate through this zone</summary>
    public bool IsNavigable { get; set; } = true;

    /// <summary>Speed limit in m/s for this zone (null = default)</summary>
    public double? SpeedLimit { get; set; }

    /// <summary>Tables in this zone</summary>
    public ICollection<TableEntity> Tables { get; set; } = new List<TableEntity>();

    /// <summary>Checkpoints in this zone</summary>
    public ICollection<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
