using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for Alert CRUD operations.
/// </summary>
public class AlertRepository
{
    private readonly RestaurantDbContext _db;

    public AlertRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<Alert>> GetAllAsync(int? limit = null, bool includeResolved = false)
    {
        var query = _db.Alerts
            .Include(a => a.Robot)
            .Include(a => a.Table)
            .Include(a => a.Task)
            .AsQueryable();

        if (!includeResolved)
            query = query.Where(a => !a.IsResolved);

        query = query.OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<Alert?> GetByIdAsync(int id)
    {
        return await _db.Alerts
            .Include(a => a.Robot)
            .Include(a => a.Table)
            .Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Alert>> GetBySeverityAsync(AlertSeverity severity)
    {
        return await _db.Alerts
            .Include(a => a.Robot)
            .Where(a => a.Severity == severity && !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetByRobotAsync(int robotId)
    {
        return await _db.Alerts
            .Where(a => a.RobotId == robotId && !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetUnacknowledgedAsync()
    {
        return await _db.Alerts
            .Include(a => a.Robot)
            .Include(a => a.Table)
            .Where(a => !a.IsAcknowledged && !a.IsResolved)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Alert> CreateAsync(Alert alert)
    {
        alert.CreatedAt = DateTime.UtcNow;
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();
        return alert;
    }

    public async Task<Alert> UpdateAsync(Alert alert)
    {
        _db.Alerts.Update(alert);
        await _db.SaveChangesAsync();
        return alert;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var alert = await _db.Alerts.FindAsync(id);
        if (alert == null) return false;

        _db.Alerts.Remove(alert);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AcknowledgeAsync(int id, string? acknowledgedBy = null)
    {
        var alert = await _db.Alerts.FindAsync(id);
        if (alert != null)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = acknowledgedBy;
            alert.AcknowledgedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ResolveAsync(int id)
    {
        var alert = await _db.Alerts.FindAsync(id);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _db.Alerts.CountAsync(a => !a.IsResolved);
    }

    public async Task<int> GetCriticalCountAsync()
    {
        return await _db.Alerts.CountAsync(a => 
            !a.IsResolved && 
            (a.Severity == AlertSeverity.Critical || a.Severity == AlertSeverity.Error));
    }

    public async Task CleanupOldAlertsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var oldAlerts = await _db.Alerts
            .Where(a => a.IsResolved && a.ResolvedAt < cutoff)
            .ToListAsync();

        _db.Alerts.RemoveRange(oldAlerts);
        await _db.SaveChangesAsync();
    }
}
