using Microsoft.EntityFrameworkCore;

namespace MapPlannerApi.Entities;

/// <summary>
/// Represents dual coordinate system - screen pixels and physical meters.
/// Used as an owned entity type for EF Core.
/// </summary>
[Owned]
public class Position
{
    /// <summary>Screen X coordinate in pixels</summary>
    public double ScreenX { get; set; }
    
    /// <summary>Screen Y coordinate in pixels</summary>
    public double ScreenY { get; set; }
    
    /// <summary>Physical X coordinate in meters</summary>
    public double PhysicalX { get; set; }
    
    /// <summary>Physical Y coordinate in meters</summary>
    public double PhysicalY { get; set; }

    public Position() { }

    public Position(double screenX, double screenY, double physicalX, double physicalY)
    {
        ScreenX = screenX;
        ScreenY = screenY;
        PhysicalX = physicalX;
        PhysicalY = physicalY;
    }

    /// <summary>
    /// Create position from screen coordinates with auto-conversion.
    /// Uses default scale: 100 pixels = 1 meter.
    /// </summary>
    public static Position FromScreen(double screenX, double screenY, double pixelsPerMeter = 100)
    {
        return new Position(
            screenX, 
            screenY, 
            screenX / pixelsPerMeter, 
            screenY / pixelsPerMeter
        );
    }

    /// <summary>
    /// Create position from physical coordinates with auto-conversion.
    /// </summary>
    public static Position FromPhysical(double physicalX, double physicalY, double pixelsPerMeter = 100)
    {
        return new Position(
            physicalX * pixelsPerMeter,
            physicalY * pixelsPerMeter,
            physicalX,
            physicalY
        );
    }
}
