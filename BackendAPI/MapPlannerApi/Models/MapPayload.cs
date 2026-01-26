namespace MapPlannerApi.Models;

/// <summary>
/// Represents the map description uploaded from the frontend.
/// </summary>
public class MapPayload
{
    public Rect DiningArea { get; set; } = new(0, 0, 800, 600);
    public Dictionary<string, Rect> Zones { get; set; } = new();
    public List<Table> Tables { get; set; } = new();
    public double GridSize { get; set; } = 10;
}

/// <summary>
/// Describes an individual table on the map.
/// </summary>
public class Table
{
    public int Id { get; set; }
    public string? Type { get; set; }
    public string? Shape { get; set; }
    public Point2D Center { get; set; } = new(0, 0);
    public Rect Bounds { get; set; } = new(0, 0, 0, 0);
}
