using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using System.IO;
using System.Text.Json;

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

        // POST /api/guests - Add one or more guests to the waitlist
        group.MapPost("/", async (HttpRequest http, GuestRepository repo) =>
        {
            string body;
            try
            {
                using var reader = new StreamReader(http.Body);
                body = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Failed to read request body.", details = ex.Message });
            }

            List<CreateGuestRequest>? requests = null;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                requests = JsonSerializer.Deserialize<List<CreateGuestRequest>>(body, opts);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Invalid JSON for CreateGuestRequest[]", details = ex.Message, raw = body });
            }

            if (requests == null || requests.Count == 0)
                return Results.BadRequest(new { error = "Request must be a non-empty array of CreateGuestRequest objects.", raw = body });

            var createdList = new List<Guest>();
            foreach (var request in requests)
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
                createdList.Add(created);
            }

            var dtos = createdList.Select(ToDto).ToList();
            return Results.Ok(dtos);
        })
        .WithName("CreateGuests")
        .WithDescription("Add one or more guests to the waitlist");

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
