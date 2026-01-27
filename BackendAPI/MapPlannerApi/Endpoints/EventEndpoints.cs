using MapPlannerApi.Entities;
using MapPlannerApi.Services;

namespace MapPlannerApi.Endpoints;

/// <summary>
/// Event trigger endpoints for manual or automated event triggering.
/// </summary>
public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/events/trigger - Generic event trigger
        group.MapPost("/trigger", async (
            EventTriggerDto request,
            IEventTriggerService eventTrigger) =>
        {
            if (!Enum.TryParse<TriggerEventType>(request.EventType, true, out var eventType))
            {
                return Results.BadRequest(new { Message = $"Invalid event type: {request.EventType}" });
            }

            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                eventType,
                TableId: request.TableId,
                GuestId: request.GuestId,
                RobotId: request.RobotId,
                OrderId: request.OrderId,
                Notes: request.Notes,
                Priority: Enum.TryParse<TaskPriority>(request.Priority, true, out var priority) 
                    ? priority 
                    : TaskPriority.Normal
            ));

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("TriggerEvent")
        .WithSummary("Trigger a restaurant event")
        .WithDescription("Generic endpoint to trigger any event type that may create tasks or alerts")
        .Produces<TriggerEventResult>(200)
        .Produces(400);

        // GET /api/events/types - List available event types
        group.MapGet("/types", () =>
        {
            var eventTypes = Enum.GetValues<TriggerEventType>()
                .Select(e => new EventTypeInfo(
                    e.ToString(),
                    GetEventDescription(e),
                    GetRequiredFields(e)
                ));

            return Results.Ok(eventTypes);
        })
        .WithName("GetEventTypes")
        .WithSummary("Get available event types")
        .Produces<IEnumerable<EventTypeInfo>>(200);

        // POST /api/events/guest-help - Guest needs help (convenience endpoint)
        group.MapPost("/guest-help", async (
            GuestHelpRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                TriggerEventType.GuestNeedsHelp,
                TableId: request.TableId,
                Notes: request.Reason,
                Priority: request.IsUrgent ? TaskPriority.Urgent : TaskPriority.High
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId, result.AlertId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("GuestHelp")
        .WithSummary("Guest requests assistance")
        .WithDescription("Creates a high-priority help task and alert for the specified table")
        .Produces(200)
        .Produces(400);

        // POST /api/events/guest-arrived - Guest arrival (convenience endpoint)
        group.MapPost("/guest-arrived", async (
            GuestArrivedRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                TriggerEventType.GuestArrived,
                GuestId: request.GuestId,
                TableId: request.ReservedTableId,
                Notes: $"Party size: {request.PartySize}. {request.Notes}"
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("GuestArrived")
        .WithSummary("Guest has arrived at restaurant")
        .Produces(200)
        .Produces(400);

        // POST /api/events/table-status - Table status changed
        group.MapPost("/table-status", async (
            TableStatusChangeRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var eventType = request.NewStatus.ToLower() switch
            {
                "needsservice" => TriggerEventType.TableNeedsService,
                "cleaning" => TriggerEventType.TableNeedsCleaning,
                "bussing" => TriggerEventType.TableNeedsBussing,
                "setup" => TriggerEventType.TableNeedsSetup,
                _ => (TriggerEventType?)null
            };

            if (!eventType.HasValue)
            {
                return Results.BadRequest(new { Message = $"No event trigger for status: {request.NewStatus}" });
            }

            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                eventType.Value,
                TableId: request.TableId,
                Notes: request.Notes
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("TableStatusChanged")
        .WithSummary("Notify table status change")
        .Produces(200)
        .Produces(400);

        return group;
    }

    private static string GetEventDescription(TriggerEventType eventType) => eventType switch
    {
        TriggerEventType.GuestArrived => "Guest has arrived at the restaurant",
        TriggerEventType.GuestSeated => "Guest has been seated at a table",
        TriggerEventType.GuestNeedsHelp => "Guest requires assistance",
        TriggerEventType.GuestRequestedCheck => "Guest has requested their check",
        TriggerEventType.GuestLeft => "Guest has left their table",
        TriggerEventType.FoodReady => "Food is ready for delivery",
        TriggerEventType.DrinkReady => "Drinks are ready for delivery",
        TriggerEventType.OrderReady => "Complete order is ready for delivery",
        TriggerEventType.TableNeedsService => "Table requires staff attention",
        TriggerEventType.TableNeedsCleaning => "Table needs to be cleaned",
        TriggerEventType.TableNeedsBussing => "Table has dirty dishes to clear",
        TriggerEventType.TableNeedsSetup => "Table needs to be set for next guests",
        TriggerEventType.RobotLowBattery => "Robot battery is low",
        TriggerEventType.RobotBlocked => "Robot is blocked and cannot proceed",
        TriggerEventType.RobotError => "Robot has encountered an error",
        _ => "Unknown event"
    };

    private static string[] GetRequiredFields(TriggerEventType eventType) => eventType switch
    {
        TriggerEventType.GuestArrived => [],
        TriggerEventType.GuestSeated => ["tableId"],
        TriggerEventType.GuestNeedsHelp => ["tableId"],
        TriggerEventType.GuestRequestedCheck => ["tableId"],
        TriggerEventType.GuestLeft => ["tableId"],
        TriggerEventType.FoodReady => ["tableId"],
        TriggerEventType.DrinkReady => ["tableId"],
        TriggerEventType.OrderReady => ["tableId"],
        TriggerEventType.TableNeedsService => ["tableId"],
        TriggerEventType.TableNeedsCleaning => ["tableId"],
        TriggerEventType.TableNeedsBussing => ["tableId"],
        TriggerEventType.TableNeedsSetup => ["tableId"],
        TriggerEventType.RobotLowBattery => ["robotId"],
        TriggerEventType.RobotBlocked => ["robotId"],
        TriggerEventType.RobotError => ["robotId"],
        _ => []
    };
}

// DTOs for Event endpoints
public record EventTriggerDto(
    string EventType,
    int? TableId = null,
    int? GuestId = null,
    int? RobotId = null,
    string? OrderId = null,
    string? Notes = null,
    string? Priority = null
);

public record EventTypeInfo(
    string Name,
    string Description,
    string[] RequiredFields
);

public record GuestHelpRequest(
    int TableId,
    string? Reason = null,
    bool IsUrgent = false
);

public record GuestArrivedRequest(
    int? GuestId = null,
    int? ReservedTableId = null,
    int PartySize = 1,
    string? Notes = null
);

public record TableStatusChangeRequest(
    int TableId,
    string NewStatus,
    string? Notes = null
);
