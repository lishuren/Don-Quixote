using MapPlannerApi.Entities;
using MapPlannerApi.Services;

namespace MapPlannerApi.Endpoints;

/// <summary>
/// Kitchen/Food-related endpoints for triggering delivery tasks.
/// </summary>
public static class KitchenEndpoints
{
    public static RouteGroupBuilder MapKitchenEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/kitchen/order-ready - Food/order ready for delivery
        group.MapPost("/order-ready", async (
            OrderReadyRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                TriggerEventType.OrderReady,
                TableId: request.TableId,
                OrderId: request.OrderId,
                Notes: request.Notes,
                Priority: request.IsRush ? TaskPriority.Urgent : TaskPriority.High
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("OrderReady")
        .WithSummary("Notify that an order is ready for delivery")
        .WithDescription("Creates a high-priority delivery task and auto-dispatches to available robot")
        .Produces(200)
        .Produces(400);

        // POST /api/kitchen/food-ready - Food item ready
        group.MapPost("/food-ready", async (
            FoodReadyRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                TriggerEventType.FoodReady,
                TableId: request.TableId,
                OrderId: request.OrderId,
                Notes: $"Items: {string.Join(", ", request.Items ?? [])}",
                Priority: TaskPriority.High
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("FoodReady")
        .WithSummary("Notify that food is ready for delivery")
        .Produces(200)
        .Produces(400);

        // POST /api/kitchen/drinks-ready - Drinks ready
        group.MapPost("/drinks-ready", async (
            DrinksReadyRequest request,
            IEventTriggerService eventTrigger) =>
        {
            var result = await eventTrigger.TriggerEvent(new TriggerEventRequest(
                TriggerEventType.DrinkReady,
                TableId: request.TableId,
                OrderId: request.OrderId,
                Notes: $"Drinks: {string.Join(", ", request.Items ?? [])}"
            ));

            return result.Success
                ? Results.Ok(new { result.Message, result.TaskId })
                : Results.BadRequest(new { result.Message });
        })
        .WithName("DrinksReady")
        .WithSummary("Notify that drinks are ready for delivery")
        .Produces(200)
        .Produces(400);

        return group;
    }
}

// DTOs for Kitchen endpoints
public record OrderReadyRequest(
    int TableId,
    string OrderId,
    string? Notes = null,
    bool IsRush = false
);

public record FoodReadyRequest(
    int TableId,
    string? OrderId = null,
    List<string>? Items = null
);

public record DrinksReadyRequest(
    int TableId,
    string? OrderId = null,
    List<string>? Items = null
);
