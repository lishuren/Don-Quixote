using MapPlannerApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapPlannerApi.Services
{
    /// <summary>
    /// Provides grid-based A* path planning with inflated obstacles and docking refinements.
    /// </summary>
    public class PathPlanner
    {
        /// <summary>
        /// Plans a path for the given map and request, returning world-space waypoints.
        /// </summary>
        public PlanResponse Plan(MapPayload map, PlanRequest request)
        {
            var start = request.Start ?? new Point2D(
                map.DiningArea.X + map.DiningArea.Width  - 2,
                map.DiningArea.Y + map.DiningArea.Height - 2
            );

            Point2D? endPoint = request.End;

            if (!string.IsNullOrWhiteSpace(request.TableId) && request.DockSide is ApproachSide side)
            {
                var tRect = ResolveTableRect(map, request.TableId);
                if (tRect.Width > 0 && tRect.Height > 0)
                {
                    double offset = request.DockOffset ?? 30;
                    double along  =
                        request.DockAlong
                        ?? (side is ApproachSide.Top or ApproachSide.Bottom
                            ? tRect.X + tRect.Width / 2.0
                            : tRect.Y + tRect.Height / 2.0);

                    double ex, ey;
                    switch (side)
                    {
                        case ApproachSide.Bottom:
                            ex = Math.Clamp(along, tRect.X, tRect.X + tRect.Width);
                            ey = tRect.Y + tRect.Height + offset;
                            break;
                        case ApproachSide.Top:
                            ex = Math.Clamp(along, tRect.X, tRect.X + tRect.Width);
                            ey = tRect.Y - offset;
                            break;
                        case ApproachSide.Left:
                            ex = tRect.X - offset;
                            ey = Math.Clamp(along, tRect.Y, tRect.Y + tRect.Height);
                            break;
                        case ApproachSide.Right:
                            ex = tRect.X + tRect.Width + offset;
                            ey = Math.Clamp(along, tRect.Y, tRect.Y + tRect.Height);
                            break;
                        default:
                            ex = Math.Clamp(along, tRect.X, tRect.X + tRect.Width);
                            ey = tRect.Y + tRect.Height + offset;
                            break;
                    }

                    ex = Math.Clamp(ex, map.DiningArea.X, map.DiningArea.X + map.DiningArea.Width);
                    ey = Math.Clamp(ey, map.DiningArea.Y, map.DiningArea.Y + map.DiningArea.Height);

                    endPoint = new Point2D(ex, ey);
                }
            }

            Rect targetRect = request.TargetRect ?? ResolveTableRect(map, request.TableId);
            if (endPoint is null && (targetRect.Width <= 0 || targetRect.Height <= 0))
            {
                return new PlanResponse { Success = false, Error = "Invalid target. Provide end or tableId or targetRect." };
            }

            var inflatedObstacles = BuildObstacles(map, request.RobotRadius);
            var grid = new Grid(map, inflatedObstacles);

            var startCell = grid.WorldToCell(start.X, start.Y);

            Cell goalCell;
            bool endInBounds = false;
            Cell endCell = default;

            if (endPoint is Point2D end)
            {
                endInBounds =
                    end.X >= map.DiningArea.X &&
                    end.X <= map.DiningArea.X + map.DiningArea.Width &&
                    end.Y >= map.DiningArea.Y &&
                    end.Y <= map.DiningArea.Y + map.DiningArea.Height;

                endCell = grid.WorldToCell(end.X, end.Y);

                if (endInBounds && !grid.IsBlocked(endCell.X, endCell.Y))
                {
                    goalCell = endCell;
                }
                else
                {
                    goalCell = grid.FindNearestFreeCellNear(end.X, end.Y);
                }
            }
            else
            {
                goalCell = grid.FindApproachCell(targetRect);
            }

            var pathCells = AStar(grid, startCell, goalCell);
            if (pathCells == null || pathCells.Count == 0)
            {
                return new PlanResponse { Success = false, Error = "No path found." };
            }

            var points = new List<Point2D>(pathCells.Count);
            double length = 0;
            bool hasPrev = false;
            Cell prev = default;

            foreach (var c in pathCells)
            {
                var p = grid.CellCenterToWorld(c);
                points.Add(new Point2D(p.X, p.Y));
                if (hasPrev)
                {
                    length += Math.Sqrt(Math.Pow(c.X - prev.X, 2) + Math.Pow(c.Y - prev.Y, 2))
                              * grid.CellSize;
                }
                prev = c;
                hasPrev = true;
            }

            if (endPoint is Point2D end2)
            {
                if (endInBounds && goalCell.Equals(endCell) && !grid.IsBlocked(endCell.X, endCell.Y))
                {
                    points[^1] = end2;
                }
                else
                {
                    points[^1] = ProjectToEndWithDocking(points[^1], end2, grid);
                }
            }

            return new PlanResponse
            {
                Success = true,
                Path = points,
                Length = length
            };
        }

        /// <summary>
        /// Looks up the bounding rectangle for a table identifier.
        /// </summary>
        private Rect ResolveTableRect(MapPayload map, string? tableId)
        {
            if (string.IsNullOrWhiteSpace(tableId)) return new Rect(0, 0, 0, 0);
            int id;
            if (tableId.StartsWith("table", StringComparison.OrdinalIgnoreCase))
                int.TryParse(tableId.AsSpan(5), out id);
            else
                int.TryParse(tableId, out id);

            var t = map.Tables.FirstOrDefault(x => x.Id == id);
            return t?.Bounds ?? new Rect(0, 0, 0, 0);
        }

        /// <summary>
        /// Creates inflated obstacle rectangles for tables and functional zones.
        /// </summary>
        private List<Rect> BuildObstacles(MapPayload map, double inflate)
        {
            var list = new List<Rect>();

            foreach (var t in map.Tables)
                list.Add(Inflate(t.Bounds, inflate));

            if (map.Zones is not null)
                foreach (var rect in map.Zones.Values)
                    list.Add(Inflate(rect, inflate));

            return list;
        }

        /// <summary>
        /// Expands a rectangle by the provided distance on all sides.
        /// </summary>
        private Rect Inflate(Rect r, double d)
        {
            return new Rect(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
        }

        /// <summary>
        /// Occupancy grid derived from map geometry and inflated obstacles.
        /// </summary>
        private class Grid
        {
            public readonly double CellSize;
            public readonly int Width;
            public readonly int Height;
            private readonly bool[,] occ;
            private readonly MapPayload map;

            private readonly List<Rect> obstacles;
            private readonly List<Rect> rawObstacles;

            public Grid(MapPayload map, List<Rect> obstacles)
            {
                this.map = map;
                this.obstacles = obstacles ?? new List<Rect>();

                this.rawObstacles = new List<Rect>();
                foreach (var t in map.Tables) this.rawObstacles.Add(t.Bounds);
                if (map.Zones is not null) foreach (var r in map.Zones.Values) this.rawObstacles.Add(r);

                CellSize = Math.Max(1, map.GridSize);
                Width  = Math.Max(1, (int)Math.Ceiling(map.DiningArea.Width  / CellSize));
                Height = Math.Max(1, (int)Math.Ceiling(map.DiningArea.Height / CellSize));
                occ = new bool[Width, Height];

                foreach (var o in this.obstacles)
                    FillObstacle(o);
            }

            public Cell WorldToCell(double x, double y)
            {
                int cx = (int)Math.Floor((x - map.DiningArea.X) / CellSize);
                int cy = (int)Math.Floor((y - map.DiningArea.Y) / CellSize);
                cx = Math.Clamp(cx, 0, Width - 1);
                cy = Math.Clamp(cy, 0, Height - 1);
                return new Cell(cx, cy);
            }

            public Point2D CellCenterToWorld(Cell c)
            {
                double wx = map.DiningArea.X + (c.X + 0.5) * CellSize;
                double wy = map.DiningArea.Y + (c.Y + 0.5) * CellSize;
                return new Point2D(wx, wy);
            }

            public bool IsBlocked(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return true;
                return occ[x, y];
            }

            /// <summary>
            /// Rasterizes an obstacle into the occupancy grid using center-point inclusion.
            /// </summary>
            private void FillObstacle(Rect r)
            {

                int minX = Math.Max(0, (int)Math.Floor((r.X - map.DiningArea.X) / CellSize) - 1);
                int maxX = Math.Min(Width - 1,  (int)Math.Floor(((r.X + r.Width)  - map.DiningArea.X) / CellSize) + 1);
                int minY = Math.Max(0, (int)Math.Floor((r.Y - map.DiningArea.Y) / CellSize) - 1);
                int maxY = Math.Min(Height - 1, (int)Math.Floor(((r.Y + r.Height) - map.DiningArea.Y) / CellSize) + 1);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var c = new Cell(x, y);
                        var center = CellCenterToWorld(c);

                        if (center.X >= r.X && center.X <= r.X + r.Width &&
                            center.Y >= r.Y && center.Y <= r.Y + r.Height)
                        {
                            occ[x, y] = true;
                        }
                    }
                }
            }

            /// <summary>
            /// Finds a reachable grid cell adjacent to the target rectangle.
            /// </summary>
            public Cell FindApproachCell(Rect target)
            {
                double worldX = target.X + target.Width / 2.0;
                double worldY = target.Y + target.Height;
                var bcCell = WorldToCell(worldX, worldY);

                int col = bcCell.X;
                int rowBelow = Math.Min(Height - 1, bcCell.Y + 1);
                for (int y = rowBelow; y < Height; y++)
                    if (!IsBlocked(col, y)) return new Cell(col, y);

                for (int y = Math.Max(0, bcCell.Y - 1); y >= 0; y--)
                    if (!IsBlocked(col, y)) return new Cell(col, y);

                int sx = Math.Max(0, WorldToCell(target.X, target.Y).X - 1);
                int sy = Math.Max(0, WorldToCell(target.X, target.Y).Y - 1);
                int ex = Math.Min(Width - 1, WorldToCell(target.X + target.Width,  target.Y + target.Height).X + 1);
                int ey = Math.Min(Height - 1, WorldToCell(target.X + target.Width,  target.Y + target.Height).Y + 1);

                int radius = 1;
                while (radius < Math.Max(Width, Height))
                {
                    for (int x = sx - radius; x <= ex + radius; x++)
                    {
                        if (!IsBlocked(x, sy - radius)) return new Cell(x, sy - radius);
                        if (!IsBlocked(x, ey + radius)) return new Cell(x, ey + radius);
                    }
                    for (int y = sy - radius; y <= ey + radius; y++)
                    {
                        if (!IsBlocked(sx - radius, y)) return new Cell(sx - radius, y);
                        if (!IsBlocked(ex + radius, y)) return new Cell(ex + radius, y);
                    }
                    radius++;
                }
                return bcCell;
            }

            /// <summary>
            /// Runs BFS around the given world position to locate the nearest free cell.
            /// </summary>
            public Cell FindNearestFreeCellNear(double wx, double wy)
            {
                var start = WorldToCell(wx, wy);
                if (!IsBlocked(start.X, start.Y)) return start;

                var visited = new bool[Width, Height];
                var q = new Queue<Cell>();
                visited[start.X, start.Y] = true;
                q.Enqueue(start);

                var dirs = new (int dx, int dy)[] {
                    (0,-1), (-1,-1), (1,-1),
                    (-1,0), (1,0),
                    (0,1),  (-1,1),  (1,1)
                };

                while (q.Count > 0)
                {
                    var u = q.Dequeue();

                    foreach (var (dx, dy) in dirs)
                    {
                        int nx = u.X + dx, ny = u.Y + dy;
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        if (visited[nx, ny]) continue;
                        visited[nx, ny] = true;

                        if (!IsBlocked(nx, ny))
                            return new Cell(nx, ny);

                        q.Enqueue(new Cell(nx, ny));
                    }
                }
                return start;
            }

            public bool IsWorldInsideInflated(double x, double y)
            {
                foreach (var r in obstacles)
                    if (x >= r.X && x <= r.X + r.Width &&
                        y >= r.Y && y <= r.Y + r.Height)
                        return true;
                return false;
            }

            public bool IsWorldInsideRaw(double x, double y)
            {
                foreach (var r in rawObstacles)
                    if (x > r.X && x < r.X + r.Width &&
                        y > r.Y && y < r.Y + r.Height)
                        return true;
                return false;
            }
        }

        private readonly record struct Cell(int X, int Y);
    /// <summary>
    /// Executes A* search over the grid between the provided start and goal cells.
    /// </summary>
    private List<Cell>? AStar(Grid grid, Cell start, Cell goal)
        {
            var open = new PriorityQueue<Cell, double>();
            var cameFrom = new Dictionary<Cell, Cell>();
            var gScore = new Dictionary<Cell, double>();

            gScore[start] = 0;
            open.Enqueue(start, Heuristic(start, goal));

            var dirs = new (int dx, int dy)[] {
                (1,0),(-1,0),(0,1),(0,-1),
                (1,1),(1,-1),(-1,1),(-1,-1)
            };

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                if (current.Equals(goal))
                    return Reconstruct(cameFrom, current);

                foreach (var (dx, dy) in dirs)
                {
                    int nx = current.X + dx, ny = current.Y + dy;
                    if (grid.IsBlocked(nx, ny)) continue;

                    if (dx != 0 && dy != 0)
                    {
                        if (grid.IsBlocked(current.X + dx, current.Y) ||
                            grid.IsBlocked(current.X, current.Y + dy))
                            continue;
                    }

                    var neighbor = new Cell(nx, ny);
                    var tentative = gScore[current] + ((dx == 0 || dy == 0) ? 1.0 : Math.Sqrt(2.0));

                    if (!gScore.TryGetValue(neighbor, out var g) || tentative < g)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;
                        var f = tentative + Heuristic(neighbor, goal);
                        open.Enqueue(neighbor, f);
                    }
                }
            }
            return null;
        }

        private double Heuristic(Cell a, Cell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        /// <summary>
        /// Reconstructs the path from the goal cell back to the start using predecessor links.
        /// </summary>
        private List<Cell> Reconstruct(Dictionary<Cell, Cell> cameFrom, Cell current)
        {
            var path = new List<Cell> { current };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Projects the final waypoint toward the docking target while respecting inflated and raw obstacles.
        /// </summary>
        private Point2D ProjectToEndWithDocking(Point2D from, Point2D end, Grid grid)
        {
            double dx = end.X - from.X;
            double dy = end.Y - from.Y;

            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 40; i++)
            {
                double mid = 0.5 * (lo + hi);
                double px = from.X + dx * mid;
                double py = from.Y + dy * mid;

                bool unsafeInflated = grid.IsWorldInsideInflated(px, py);
                if (unsafeInflated) hi = mid; else lo = mid;
            }
            double t1 = lo;
            if (1.0 - t1 < 1e-6) return end;

            lo = t1; hi = 1.0;
            for (int i = 0; i < 40; i++)
            {
                double mid = 0.5 * (lo + hi);
                double px = from.X + dx * mid;
                double py = from.Y + dy * mid;

                bool insideRaw = grid.IsWorldInsideRaw(px, py);
                if (insideRaw) hi = mid; else lo = mid;
            }
            double t2 = lo;

            return new Point2D(from.X + dx * t2, from.Y + dy * t2);
        }
    }
}
