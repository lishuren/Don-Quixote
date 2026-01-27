using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class GuestEndpoints
{
    public static void MapGuestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/guests").WithTags("Guests");

        // GET /api/guests - List all guests
        group.MapGet("/", async (int? limit, GuestRepository repo) =>
        {
            var guests = await repo.GetAllAsync(limit);
            return Results.Ok(guests.Select(ToDto));
        })
        .WithName("GetGuests")
        .WithDescription("Get all guests");

        // GET /api/guests/{id} - Get guest by ID
        group.MapGet("/{id:int}", async (int id, GuestRepository repo) =>
        {
            var guest = await repo.GetByIdAsync(id);
            return guest is null
                ? Results.NotFound(new { error = $"Guest {id} not found" })
                : Results.Ok(ToDto(guest));
        })
        .WithName("GetGuest")
        .WithDescription("Get guest by ID");

        // POST /api/guests - Add a new guest (to waitlist)
        group.MapPost("/", async (CreateGuestRequest request, GuestRepository repo) =>
        {
            var guest = new Guest
            {
                Name = request.Name,
                PartySize = request.PartySize,
                Notes = request.Notes,
                PhoneNumber = request.PhoneNumber,
                ReservationId = request.ReservationId,
                Status = GuestStatus.Waiting
            };

            var created = await repo.CreateAsync(guest);
            return Results.Created($"/api/guests/{created.Id}", ToDto(created));
        })
        .WithName("CreateGuest")
        .WithDescription("Add a new guest to the waitlist");

        // DELETE /api/guests/{id} - Remove guest
        group.MapDelete("/{id:int}", async (int id, GuestRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Guest {id} not found" });
        })
        .WithName("DeleteGuest")
        .WithDescription("Remove a guest");

        // GET /api/guests/status/{status} - Get guests by status
        group.MapGet("/status/{status}", async (string status, GuestRepository repo) =>
        {
            if (!Enum.TryParse<GuestStatus>(status, true, out var guestStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var guests = await repo.GetByStatusAsync(guestStatus);
            return Results.Ok(guests.Select(ToDto));
        })
        .WithName("GetGuestsByStatus")
        .WithDescription("Get guests filtered by status");

        // GET /api/guests/waitlist - Get waitlist
        group.MapGet("/waitlist", async (GuestRepository repo) =>
        {
            var guests = await repo.GetWaitlistAsync();
            var avgWait = guests.Any() 
                ? (int)guests.Average(g => (DateTime.UtcNow - g.ArrivalTime).TotalMinutes)
                : 0;

            return Results.Ok(new WaitlistSummary(
                guests.Count,
                avgWait,
                guests.Select(ToDto).ToList()
            ));
        })
        .WithName("GetWaitlist")
        .WithDescription("Get waitlist with summary");

        // POST /api/guests/{id}/seat - Seat a guest
        group.MapPost("/{id:int}/seat", async (int id, SeatGuestRequest request, GuestRepository repo) =>
        {
            var guest = await repo.GetByIdAsync(id);
            if (guest is null)
                return Results.NotFound(new { error = $"Guest {id} not found" });

            await repo.SeatGuestAsync(id, request.TableId);
            return Results.Ok(new { message = $"Guest {id} seated at table {request.TableId}" });
        })
        .WithName("SeatGuest")
        .WithDescription("Seat a guest at a table");

        // POST /api/guests/{id}/checkout - Check out a guest
        group.MapPost("/{id:int}/checkout", async (int id, GuestRepository repo) =>
        {
            var guest = await repo.GetByIdAsync(id);
            if (guest is null)
                return Results.NotFound(new { error = $"Guest {id} not found" });

            await repo.CheckoutGuestAsync(id);
            return Results.Ok(new { message = $"Guest {id} checked out" });
        })
        .WithName("CheckoutGuest")
        .WithDescription("Check out a guest");

        // PUT /api/guests/{id}/status - Update guest status
        group.MapPut("/{id:int}/status", async (int id, string status, GuestRepository repo) =>
        {
            if (!Enum.TryParse<GuestStatus>(status, true, out var guestStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var guest = await repo.GetByIdAsync(id);
            if (guest is null)
                return Results.NotFound(new { error = $"Guest {id} not found" });

            guest.Status = guestStatus;
            await repo.UpdateAsync(guest);
            return Results.Ok(new { message = "Status updated" });
        })
        .WithName("UpdateGuestStatus")
        .WithDescription("Update guest status");

        // GET /api/guests/table/{tableId} - Get guests at a table
        group.MapGet("/table/{tableId:int}", async (int tableId, GuestRepository repo) =>
        {
            var guests = await repo.GetByTableAsync(tableId);
            return Results.Ok(guests.Select(ToDto));
        })
        .WithName("GetGuestsByTable")
        .WithDescription("Get guests seated at a table");
    }

    private static GuestDto ToDto(Guest g) => new(
        g.Id,
        g.Name,
        g.PartySize,
        g.Status.ToString(),
        g.TableId,
        g.Table?.Label,
        g.QueuePosition,
        g.ArrivalTime,
        g.SeatedTime,
        g.DepartedTime,
        g.EstimatedWaitMinutes,
        g.Notes,
        g.PhoneNumber,
        g.ReservationId
    );
}
