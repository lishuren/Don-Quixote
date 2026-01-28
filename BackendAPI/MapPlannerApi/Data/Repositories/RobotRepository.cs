using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for Robot CRUD operations.
/// </summary>
public class RobotRepository
{
    private readonly RestaurantDbContext _db;

    public RobotRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<Robot>> GetAllAsync()
    {
        return await _db.Robots
            .Include(r => r.CurrentTask)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<Robot?> GetByIdAsync(int id)
    {
        return await _db.Robots
            .Include(r => r.CurrentTask)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Robot?> GetByNameAsync(string name)
    {
        return await _db.Robots
            .Include(r => r.CurrentTask)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<List<Robot>> GetByStatusAsync(RobotStatus status)
    {
        return await _db.Robots
            .Include(r => r.CurrentTask)
            .Where(r => r.Status == status)
            .ToListAsync();
    }

    public async Task<List<Robot>> GetIdleRobotsAsync()
    {
        return await _db.Robots
            .Where(r => r.Status == RobotStatus.Idle && r.IsEnabled)
            .OrderByDescending(r => r.BatteryLevel)
            .ToListAsync();
    }

    public async Task<Robot> CreateAsync(Robot robot)
    {
        robot.CreatedAt = DateTime.UtcNow;
        robot.LastUpdated = DateTime.UtcNow;

        // Ensure robot name is unique (SQLite has UNIQUE constraint on Name)
        var baseName = string.IsNullOrWhiteSpace(robot.Name) ? $"Robot-{Guid.NewGuid().ToString()[..8]}" : robot.Name;
        var candidate = baseName;
        int suffix = 1;
        while (await _db.Robots.AnyAsync(r => r.Name == candidate))
        {
            candidate = baseName + "-" + suffix.ToString();
            suffix++;
        }
        robot.Name = candidate;

        _db.Robots.Add(robot);
        await _db.SaveChangesAsync();
        return robot;
    }

    public async Task<Robot> UpdateAsync(Robot robot)
    {
        robot.LastUpdated = DateTime.UtcNow;
        _db.Robots.Update(robot);
        await _db.SaveChangesAsync();
        return robot;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var robot = await _db.Robots.FindAsync(id);
        if (robot == null) return false;

        _db.Robots.Remove(robot);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdatePositionAsync(int id, Position position, double heading, double velocity)
    {
        var robot = await _db.Robots.FindAsync(id);
        if (robot != null)
        {
            robot.Position = position;
            robot.Heading = heading;
            robot.Velocity = velocity;
            robot.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(int id, RobotStatus status)
    {
        var robot = await _db.Robots.FindAsync(id);
        if (robot != null)
        {
            robot.Status = status;
            robot.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateBatteryAsync(int id, double batteryLevel)
    {
        var robot = await _db.Robots.FindAsync(id);
        if (robot != null)
        {
            robot.BatteryLevel = batteryLevel;
            robot.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
