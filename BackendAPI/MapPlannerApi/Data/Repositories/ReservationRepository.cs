using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for Reservation CRUD operations.
/// </summary>
public class ReservationRepository
{
    private readonly RestaurantDbContext _db;

    public ReservationRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<Reservation>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _db.Reservations
            .Include(r => r.Table)
            .Include(r => r.Guest)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(r => r.ReservationTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ReservationTime <= toDate.Value);

        return await query
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _db.Reservations
            .Include(r => r.Table)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Reservation?> GetByConfirmationCodeAsync(string code)
    {
        return await _db.Reservations
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.ConfirmationCode == code);
    }

    public async Task<List<Reservation>> GetByStatusAsync(ReservationStatus status)
    {
        return await _db.Reservations
            .Include(r => r.Table)
            .Where(r => r.Status == status)
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetTodaysReservationsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _db.Reservations
            .Include(r => r.Table)
            .Where(r => r.ReservationTime >= today && r.ReservationTime < tomorrow)
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetUpcomingAsync(int minutes = 30)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(minutes);

        return await _db.Reservations
            .Include(r => r.Table)
            .Where(r => r.ReservationTime >= now && 
                   r.ReservationTime <= cutoff &&
                   r.Status == ReservationStatus.Confirmed)
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetByTableAsync(int tableId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _db.Reservations
            .Where(r => r.TableId == tableId && 
                   r.ReservationTime >= startOfDay && 
                   r.ReservationTime < endOfDay)
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        reservation.CreatedAt = DateTime.UtcNow;
        reservation.ConfirmationCode = GenerateConfirmationCode();
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation)
    {
        reservation.UpdatedAt = DateTime.UtcNow;
        _db.Reservations.Update(reservation);
        await _db.SaveChangesAsync();
        return reservation;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation == null) return false;

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ConfirmAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation != null)
        {
            reservation.Status = ReservationStatus.Confirmed;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task CancelAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation != null)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkSeatedAsync(int id, int guestId)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation != null)
        {
            reservation.Status = ReservationStatus.Seated;
            reservation.GuestId = guestId;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsTableAvailableAsync(int tableId, DateTime time, int durationMinutes)
    {
        var endTime = time.AddMinutes(durationMinutes);

        var conflicting = await _db.Reservations
            .AnyAsync(r => r.TableId == tableId &&
                      r.Status != ReservationStatus.Cancelled &&
                      r.Status != ReservationStatus.NoShow &&
                      r.ReservationTime < endTime &&
                      r.ReservationTime.AddMinutes(r.DurationMinutes) > time);

        return !conflicting;
    }

    private static string GenerateConfirmationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
