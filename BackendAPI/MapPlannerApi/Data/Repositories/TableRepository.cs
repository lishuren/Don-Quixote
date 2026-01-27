using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for TableEntity CRUD operations.
/// </summary>
public class TableRepository
{
    private readonly RestaurantDbContext _db;

    public TableRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<TableEntity>> GetAllAsync()
    {
        return await _db.Tables
            .Include(t => t.Zone)
            .Include(t => t.Guests)
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<TableEntity?> GetByIdAsync(int id)
    {
        return await _db.Tables
            .Include(t => t.Zone)
            .Include(t => t.Guests)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<TableEntity>> GetByStatusAsync(TableStatus status)
    {
        return await _db.Tables
            .Include(t => t.Zone)
            .Include(t => t.Guests)
            .Where(t => t.Status == status)
            .ToListAsync();
    }

    public async Task<List<TableEntity>> GetAvailableTablesAsync(int minCapacity = 1)
    {
        return await _db.Tables
            .Include(t => t.Zone)
            .Where(t => t.Status == TableStatus.Available && t.Capacity >= minCapacity)
            .OrderBy(t => t.Capacity)
            .ToListAsync();
    }

    public async Task<List<TableEntity>> GetByZoneAsync(int zoneId)
    {
        return await _db.Tables
            .Include(t => t.Guests)
            .Where(t => t.ZoneId == zoneId)
            .ToListAsync();
    }

    public async Task<TableEntity> CreateAsync(TableEntity table)
    {
        table.CreatedAt = DateTime.UtcNow;
        _db.Tables.Add(table);
        await _db.SaveChangesAsync();
        return table;
    }

    public async Task<TableEntity> UpdateAsync(TableEntity table)
    {
        table.UpdatedAt = DateTime.UtcNow;
        _db.Tables.Update(table);
        await _db.SaveChangesAsync();
        return table;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return false;

        _db.Tables.Remove(table);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateStatusAsync(int id, TableStatus status)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table != null)
        {
            table.Status = status;
            table.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<TableStatus, int>> GetStatusSummaryAsync()
    {
        return await _db.Tables
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<int> GetOccupancyAsync()
    {
        var total = await _db.Tables.CountAsync();
        var occupied = await _db.Tables.CountAsync(t => t.Status == TableStatus.Occupied);
        return total > 0 ? (occupied * 100 / total) : 0;
    }
}
