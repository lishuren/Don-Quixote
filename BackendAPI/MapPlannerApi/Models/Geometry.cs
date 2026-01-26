namespace MapPlannerApi.Models;

public record Point2D(double X, double Y);

public record Rect(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y)
        => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
