namespace MapPlannerApi.Models;

public enum ApproachSide
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Describes the payload sent by the client when requesting a path.
/// </summary>
public class PlanRequest
{
    public Point2D? Start { get; set; }
    public string? TableId { get; set; }
    public Rect? TargetRect { get; set; }
    public Point2D? End { get; set; }
    public double RobotRadius { get; set; } = 16;

    /// <summary>
    /// Optional docking side used to align the robot with a table edge.
    /// </summary>
    public ApproachSide? DockSide { get; set; }

    /// <summary>
    /// Optional outward offset, in map units, applied when docking.
    /// </summary>
    public double? DockOffset { get; set; }

    /// <summary>
    /// Along-edge coordinate used when docking to a table side.
    /// </summary>
    public double? DockAlong { get; set; }

    /// <summary>
    /// Keeps the final waypoint on a corridor row when enabled.
    /// </summary>
    public bool? SnapFinalToCorridor { get; set; }

    /// <summary>
    /// Explicit corridor Y coordinate used when snapping the final waypoint.
    /// </summary>
    public double? CorridorY { get; set; }
}

/// <summary>
/// Represents the response returned after path planning.
/// </summary>
public class PlanResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<Point2D> Path { get; set; } = new();
    public double Length { get; set; }
}
