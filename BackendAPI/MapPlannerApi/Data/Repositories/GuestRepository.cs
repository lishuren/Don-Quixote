using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for Guest CRUD operations.
/// </summary>
public class GuestRepository
{
    private readonly RestaurantDbContext _db;

    public GuestRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<Guest>> GetAllAsync(int? limit = null)
    {
        var query = _db.Guests
            .Include(g => g.Table)
            .Include(g => g.Reservation)
            .OrderByDescending(g => g.ArrivalTime)
            .AsQueryable();

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<Guest?> GetByIdAsync(int id)
    {
        return await _db.Guests
            .Include(g => g.Table)
            .Include(g => g.Reservation)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Guest>> GetByStatusAsync(GuestStatus status)
    {
        return await _db.Guests
            .Include(g => g.Table)
            .Where(g => g.Status == status)
            .OrderBy(g => g.QueuePosition ?? int.MaxValue)
            .ThenBy(g => g.ArrivalTime)
            .ToListAsync();
    }

    public async Task<List<Guest>> GetWaitlistAsync()
    {
        return await _db.Guests
            .Where(g => g.Status == GuestStatus.Waiting && g.QueuePosition.HasValue)
            .OrderBy(g => g.QueuePosition)
            .ToListAsync();
    }

    public async Task<List<Guest>> GetByTableAsync(int tableId)
    {
        return await _db.Guests
            .Where(g => g.TableId == tableId)
            .ToListAsync();
    }

    public async Task<Guest> CreateAsync(Guest guest)
    {
        guest.CreatedAt = DateTime.UtcNow;
        guest.ArrivalTime = DateTime.UtcNow;
        
        // Auto-assign queue position for waiting guests
        if (guest.Status == GuestStatus.Waiting)
        {
            var maxPosition = await _db.Guests
                .Where(g => g.Status == GuestStatus.Waiting && g.QueuePosition.HasValue)
                .MaxAsync(g => (int?)g.QueuePosition) ?? 0;
            guest.QueuePosition = maxPosition + 1;
        }

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();
        return guest;
    }

    public async Task<Guest> UpdateAsync(Guest guest)
    {
        _db.Guests.Update(guest);
        await _db.SaveChangesAsync();
        return guest;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return false;

        _db.Guests.Remove(guest);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SeatGuestAsync(int guestId, int tableId)
    {
        var guest = await _db.Guests.FindAsync(guestId);
        var table = await _db.Tables.FindAsync(tableId);
        
        if (guest != null && table != null)
        {
            guest.TableId = tableId;
            guest.Status = GuestStatus.Seated;
            guest.SeatedTime = DateTime.UtcNow;
            guest.QueuePosition = null;

            table.Status = TableStatus.Occupied;
            table.UpdatedAt = DateTime.UtcNow;

            // Reorder queue positions
            await ReorderQueueAsync();
            await _db.SaveChangesAsync();
        }
    }

    public async Task CheckoutGuestAsync(int guestId)
    {
        var guest = await _db.Guests
            .Include(g => g.Table)
            .FirstOrDefaultAsync(g => g.Id == guestId);
        
        if (guest != null)
        {
            guest.Status = GuestStatus.Departed;
            guest.DepartedTime = DateTime.UtcNow;

            if (guest.Table != null)
            {
                guest.Table.Status = TableStatus.Cleaning;
                guest.Table.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetWaitlistCountAsync()
    {
        return await _db.Guests.CountAsync(g => g.Status == GuestStatus.Waiting);
    }

    public async Task<int> GetActiveGuestCountAsync()
    {
        return await _db.Guests.CountAsync(g => 
            g.Status != GuestStatus.Departed && 
            g.Status != GuestStatus.Waiting);
    }

    private async Task ReorderQueueAsync()
    {
        var waitingGuests = await _db.Guests
            .Where(g => g.Status == GuestStatus.Waiting && g.QueuePosition.HasValue)
            .OrderBy(g => g.QueuePosition)
            .ToListAsync();

        for (int i = 0; i < waitingGuests.Count; i++)
        {
            waitingGuests[i].QueuePosition = i + 1;
        }
    }
}
