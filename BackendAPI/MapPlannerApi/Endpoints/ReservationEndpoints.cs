using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reservations").WithTags("Reservations");

        // GET /api/reservations - List reservations
        group.MapGet("/", async (DateTime? from, DateTime? to, ReservationRepository repo) =>
        {
            var reservations = await repo.GetAllAsync(from, to);
            return Results.Ok(reservations.Select(ToDto));
        })
        .WithName("GetReservations")
        .WithDescription("Get all reservations");

        // GET /api/reservations/{id} - Get reservation by ID
        group.MapGet("/{id:int}", async (int id, ReservationRepository repo) =>
        {
            var reservation = await repo.GetByIdAsync(id);
            return reservation is null
                ? Results.NotFound(new { error = $"Reservation {id} not found" })
                : Results.Ok(ToDto(reservation));
        })
        .WithName("GetReservation")
        .WithDescription("Get reservation by ID");

        // GET /api/reservations/code/{code} - Get by confirmation code
        group.MapGet("/code/{code}", async (string code, ReservationRepository repo) =>
        {
            var reservation = await repo.GetByConfirmationCodeAsync(code);
            return reservation is null
                ? Results.NotFound(new { error = $"Reservation with code {code} not found" })
                : Results.Ok(ToDto(reservation));
        })
        .WithName("GetReservationByCode")
        .WithDescription("Get reservation by confirmation code");

        // POST /api/reservations - Create reservation
        group.MapPost("/", async (CreateReservationRequest request, ReservationRepository repo) =>
        {
            // Check table availability if specified
            if (request.TableId.HasValue)
            {
                var available = await repo.IsTableAvailableAsync(
                    request.TableId.Value, 
                    request.ReservationTime, 
                    request.DurationMinutes);

                if (!available)
                    return Results.Conflict(new { error = "Table is not available at that time" });
            }

            var reservation = new Reservation
            {
                GuestName = request.GuestName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                PartySize = request.PartySize,
                TableId = request.TableId,
                ReservationTime = request.ReservationTime,
                DurationMinutes = request.DurationMinutes,
                SpecialRequests = request.SpecialRequests,
                Status = ReservationStatus.Pending
            };

            var created = await repo.CreateAsync(reservation);
            return Results.Created($"/api/reservations/{created.Id}", ToDto(created));
        })
        .WithName("CreateReservation")
        .WithDescription("Create a new reservation");

        // PUT /api/reservations/{id} - Update reservation
        group.MapPut("/{id:int}", async (int id, UpdateReservationRequest request, ReservationRepository repo) =>
        {
            var reservation = await repo.GetByIdAsync(id);
            if (reservation is null)
                return Results.NotFound(new { error = $"Reservation {id} not found" });

            if (request.GuestName is not null) reservation.GuestName = request.GuestName;
            if (request.PhoneNumber is not null) reservation.PhoneNumber = request.PhoneNumber;
            if (request.Email is not null) reservation.Email = request.Email;
            if (request.PartySize.HasValue) reservation.PartySize = request.PartySize.Value;
            if (request.TableId.HasValue) reservation.TableId = request.TableId;
            if (request.ReservationTime.HasValue) reservation.ReservationTime = request.ReservationTime.Value;
            if (request.DurationMinutes.HasValue) reservation.DurationMinutes = request.DurationMinutes.Value;
            if (request.SpecialRequests is not null) reservation.SpecialRequests = request.SpecialRequests;

            var updated = await repo.UpdateAsync(reservation);
            return Results.Ok(ToDto(updated));
        })
        .WithName("UpdateReservation")
        .WithDescription("Update a reservation");

        // DELETE /api/reservations/{id} - Delete reservation
        group.MapDelete("/{id:int}", async (int id, ReservationRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Reservation {id} not found" });
        })
        .WithName("DeleteReservation")
        .WithDescription("Delete a reservation");

        // GET /api/reservations/status/{status} - Get by status
        group.MapGet("/status/{status}", async (string status, ReservationRepository repo) =>
        {
            if (!Enum.TryParse<ReservationStatus>(status, true, out var reservationStatus))
                return Results.BadRequest(new { error = $"Invalid status: {status}" });

            var reservations = await repo.GetByStatusAsync(reservationStatus);
            return Results.Ok(reservations.Select(ToDto));
        })
        .WithName("GetReservationsByStatus")
        .WithDescription("Get reservations by status");

        // GET /api/reservations/today - Get today's reservations
        group.MapGet("/today", async (ReservationRepository repo) =>
        {
            var reservations = await repo.GetTodaysReservationsAsync();
            return Results.Ok(reservations.Select(ToDto));
        })
        .WithName("GetTodaysReservations")
        .WithDescription("Get today's reservations");

        // GET /api/reservations/upcoming - Get upcoming reservations
        group.MapGet("/upcoming", async (int? minutes, ReservationRepository repo) =>
        {
            var reservations = await repo.GetUpcomingAsync(minutes ?? 30);
            return Results.Ok(reservations.Select(ToDto));
        })
        .WithName("GetUpcomingReservations")
        .WithDescription("Get reservations arriving soon");

        // POST /api/reservations/{id}/confirm - Confirm reservation
        group.MapPost("/{id:int}/confirm", async (int id, ReservationRepository repo) =>
        {
            var reservation = await repo.GetByIdAsync(id);
            if (reservation is null)
                return Results.NotFound(new { error = $"Reservation {id} not found" });

            await repo.ConfirmAsync(id);
            return Results.Ok(new { message = $"Reservation {id} confirmed" });
        })
        .WithName("ConfirmReservation")
        .WithDescription("Confirm a reservation");

        // POST /api/reservations/{id}/cancel - Cancel reservation
        group.MapPost("/{id:int}/cancel", async (int id, ReservationRepository repo) =>
        {
            var reservation = await repo.GetByIdAsync(id);
            if (reservation is null)
                return Results.NotFound(new { error = $"Reservation {id} not found" });

            await repo.CancelAsync(id);
            return Results.Ok(new { message = $"Reservation {id} cancelled" });
        })
        .WithName("CancelReservation")
        .WithDescription("Cancel a reservation");

        // GET /api/reservations/table/{tableId}/check - Check table availability
        group.MapGet("/table/{tableId:int}/check", async (
            int tableId, DateTime time, int duration, ReservationRepository repo) =>
        {
            var available = await repo.IsTableAvailableAsync(tableId, time, duration);
            return Results.Ok(new { tableId, time, durationMinutes = duration, available });
        })
        .WithName("CheckTableAvailability")
        .WithDescription("Check if a table is available at a specific time");
    }

    private static ReservationDto ToDto(Reservation r) => new(
        r.Id,
        r.GuestName,
        r.PhoneNumber,
        r.Email,
        r.PartySize,
        r.TableId,
        r.Table?.Label,
        r.ReservationTime,
        r.DurationMinutes,
        r.Status.ToString(),
        r.SpecialRequests,
        r.ConfirmationCode,
        r.CreatedAt
    );
}
