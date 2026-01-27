using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for Zone and Checkpoint CRUD operations.
/// </summary>
public class ZoneRepository
{
    private readonly RestaurantDbContext _db;

    public ZoneRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    // Zone operations
    public async Task<List<Zone>> GetAllZonesAsync()
    {
        return await _db.Zones
            .Include(z => z.Tables)
            .Include(z => z.Checkpoints)
            .OrderBy(z => z.Name)
            .ToListAsync();
    }

    public async Task<Zone?> GetZoneByIdAsync(int id)
    {
        return await _db.Zones
            .Include(z => z.Tables)
            .Include(z => z.Checkpoints)
            .FirstOrDefaultAsync(z => z.Id == id);
    }

    public async Task<Zone?> GetZoneByNameAsync(string name)
    {
        return await _db.Zones
            .Include(z => z.Tables)
            .Include(z => z.Checkpoints)
            .FirstOrDefaultAsync(z => z.Name == name);
    }

    public async Task<List<Zone>> GetZonesByTypeAsync(ZoneType type)
    {
        return await _db.Zones
            .Include(z => z.Tables)
            .Where(z => z.Type == type)
            .ToListAsync();
    }

    public async Task<Zone> CreateZoneAsync(Zone zone)
    {
        zone.CreatedAt = DateTime.UtcNow;
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();
        return zone;
    }

    public async Task<Zone> UpdateZoneAsync(Zone zone)
    {
        _db.Zones.Update(zone);
        await _db.SaveChangesAsync();
        return zone;
    }

    public async Task<bool> DeleteZoneAsync(int id)
    {
        var zone = await _db.Zones.FindAsync(id);
        if (zone == null) return false;

        _db.Zones.Remove(zone);
        await _db.SaveChangesAsync();
        return true;
    }

    // Checkpoint operations
    public async Task<List<Checkpoint>> GetAllCheckpointsAsync()
    {
        return await _db.Checkpoints
            .Include(c => c.Zone)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Checkpoint?> GetCheckpointByIdAsync(int id)
    {
        return await _db.Checkpoints
            .Include(c => c.Zone)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Checkpoint>> GetCheckpointsByTypeAsync(CheckpointType type)
    {
        return await _db.Checkpoints
            .Include(c => c.Zone)
            .Where(c => c.Type == type)
            .ToListAsync();
    }

    public async Task<List<Checkpoint>> GetCheckpointsByZoneAsync(int zoneId)
    {
        return await _db.Checkpoints
            .Where(c => c.ZoneId == zoneId)
            .OrderBy(c => c.RouteOrder ?? int.MaxValue)
            .ToListAsync();
    }

    public async Task<List<Checkpoint>> GetChargingStationsAsync()
    {
        return await _db.Checkpoints
            .Include(c => c.Zone)
            .Where(c => c.Type == CheckpointType.ChargingStation)
            .ToListAsync();
    }

    public async Task<Checkpoint> CreateCheckpointAsync(Checkpoint checkpoint)
    {
        checkpoint.CreatedAt = DateTime.UtcNow;
        _db.Checkpoints.Add(checkpoint);
        await _db.SaveChangesAsync();
        return checkpoint;
    }

    public async Task<Checkpoint> UpdateCheckpointAsync(Checkpoint checkpoint)
    {
        _db.Checkpoints.Update(checkpoint);
        await _db.SaveChangesAsync();
        return checkpoint;
    }

    public async Task<bool> DeleteCheckpointAsync(int id)
    {
        var checkpoint = await _db.Checkpoints.FindAsync(id);
        if (checkpoint == null) return false;

        _db.Checkpoints.Remove(checkpoint);
        await _db.SaveChangesAsync();
        return true;
    }
}
