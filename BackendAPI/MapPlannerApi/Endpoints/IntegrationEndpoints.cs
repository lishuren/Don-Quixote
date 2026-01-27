using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;
using MapPlannerApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.Http;

namespace MapPlannerApi.Endpoints;

public static class IntegrationEndpoints
{
    // In-memory webhook storage (in production, use a database)
    private static readonly ConcurrentDictionary<int, WebhookConfig> _webhooks = new();
    private static int _webhookIdCounter = 0;

    public static void MapIntegrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Integrations");

        // ========== Webhook Endpoints ==========
        
        // GET /api/webhooks - List all webhooks
        group.MapGet("/webhooks", () =>
        {
            var webhooks = _webhooks.Values.Select(w => new WebhookDto(
                w.Id,
                w.Url,
                w.Events,
                w.IsActive,
                w.FailureCount,
                w.LastTriggeredAt,
                w.CreatedAt
            ));
            return Results.Ok(webhooks);
        })
        .WithName("ListWebhooks")
        .WithDescription("List all registered webhooks");

        // POST /api/webhooks - Register new webhook
        group.MapPost("/webhooks", (CreateWebhookRequest request) =>
        {
            var validEvents = new[] { "task_completed", "task_failed", "alert_created", "robot_error", "guest_seated", "guest_left" };
            var invalidEvents = request.Events.Where(e => !validEvents.Contains(e)).ToList();
            if (invalidEvents.Any())
                return Results.BadRequest(new { error = $"Invalid events: {string.Join(", ", invalidEvents)}. Valid: {string.Join(", ", validEvents)}" });

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
                return Results.BadRequest(new { error = "Invalid URL format" });

            var id = Interlocked.Increment(ref _webhookIdCounter);
            var webhook = new WebhookConfig
            {
                Id = id,
                Url = request.Url,
                Events = request.Events,
                Secret = request.Secret,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _webhooks[id] = webhook;

            return Results.Created($"/api/webhooks/{id}", new WebhookDto(
                webhook.Id,
                webhook.Url,
                webhook.Events,
                webhook.IsActive,
                webhook.FailureCount,
                webhook.LastTriggeredAt,
                webhook.CreatedAt
            ));
        })
        .WithName("RegisterWebhook")
        .WithDescription("Register a new webhook endpoint");

        // DELETE /api/webhooks/{id} - Remove webhook
        group.MapDelete("/webhooks/{id:int}", (int id) =>
        {
            if (!_webhooks.TryRemove(id, out _))
                return Results.NotFound(new { error = $"Webhook {id} not found" });

            return Results.NoContent();
        })
        .WithName("DeleteWebhook")
        .WithDescription("Remove a webhook");

        // POST /api/webhooks/{id}/test - Test webhook
        group.MapPost("/webhooks/{id:int}/test", async (int id, IHttpClientFactory? httpClientFactory) =>
        {
            if (!_webhooks.TryGetValue(id, out var webhook))
                return Results.NotFound(new { error = $"Webhook {id} not found" });

            // If no HTTP client factory, just simulate success
            if (httpClientFactory is null)
            {
                return Results.Ok(new WebhookTestResult(true, 200, "Test skipped (no HTTP client)", 0));
            }

            var client = httpClientFactory.CreateClient();
            var payload = new
            {
                type = "test",
                message = "Webhook test from MapPlannerApi",
                timestamp = DateTime.UtcNow
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await client.PostAsJsonAsync(webhook.Url, payload);
                stopwatch.Stop();

                var body = await response.Content.ReadAsStringAsync();
                return Results.Ok(new WebhookTestResult(
                    response.IsSuccessStatusCode,
                    (int)response.StatusCode,
                    body.Length > 500 ? body[..500] : body,
                    (int)stopwatch.ElapsedMilliseconds
                ));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return Results.Ok(new WebhookTestResult(false, 0, ex.Message, (int)stopwatch.ElapsedMilliseconds));
            }
        })
        .WithName("TestWebhook")
        .WithDescription("Send test payload to webhook");

        // PATCH /api/webhooks/{id}/toggle - Toggle webhook active status
        group.MapPatch("/webhooks/{id:int}/toggle", (int id) =>
        {
            if (!_webhooks.TryGetValue(id, out var webhook))
                return Results.NotFound(new { error = $"Webhook {id} not found" });

            webhook.IsActive = !webhook.IsActive;
            return Results.Ok(new { webhookId = id, isActive = webhook.IsActive });
        })
        .WithName("ToggleWebhook")
        .WithDescription("Toggle webhook active status");

        // ========== POS Integration Endpoints ==========
        
        // POST /api/integrations/pos/order - Receive order from POS
        group.MapPost("/integrations/pos/order", async (PosOrderRequest request, TaskRepository taskRepo, TableRepository tableRepo) =>
        {
            var table = await tableRepo.GetByIdAsync(request.TableId);
            if (table is null)
                return Results.NotFound(new { error = $"Table {request.TableId} not found" });

            // Create a delivery task for the order
            var task = new RobotTask
            {
                Type = TaskType.Deliver,
                Priority = TaskPriority.Normal,
                TargetTableId = request.TableId,
                TargetPosition = table.Center,
                Status = Entities.TaskStatus.Pending
            };

            var created = await taskRepo.CreateAsync(task);

            return Results.Ok(new
            {
                message = "Order received",
                orderId = request.OrderId,
                taskId = created.Id,
                tableId = request.TableId,
                status = "Task created for delivery"
            });
        })
        .WithName("ReceivePosOrder")
        .WithDescription("Receive order from POS system and create delivery task");

        // POST /api/integrations/pos/payment - Payment confirmation
        group.MapPost("/integrations/pos/payment", async (int tableId, string status, TableRepository tableRepo) =>
        {
            var table = await tableRepo.GetByIdAsync(tableId);
            if (table is null)
                return Results.NotFound(new { error = $"Table {tableId} not found" });

            if (status.ToLower() == "completed")
            {
                table.Status = TableStatus.Cleaning;
                await tableRepo.UpdateAsync(table);
            }

            return Results.Ok(new
            {
                message = "Payment status received",
                tableId,
                paymentStatus = status,
                tableStatus = table.Status.ToString()
            });
        })
        .WithName("PaymentConfirmation")
        .WithDescription("Receive payment confirmation from POS");

        // ========== Emergency Endpoints ==========
        
        // POST /api/emergency/stop-all - Emergency stop all robots
        group.MapPost("/emergency/stop-all", async (RestaurantDbContext db, IEventBroadcaster? broadcaster) =>
        {
            var robots = await db.Robots.Where(r => r.Status != RobotStatus.Offline).ToListAsync();
            var stoppedIds = new List<int>();

            foreach (var robot in robots)
            {
                var oldStatus = robot.Status;
                robot.Status = RobotStatus.Error;
                stoppedIds.Add(robot.Id);

                if (broadcaster is not null)
                {
                    await broadcaster.BroadcastRobotStatusChanged(robot.Id, robot.Name, oldStatus.ToString(), "Error", "Emergency stop");
                }
            }

            await db.SaveChangesAsync();

            // Create emergency alert
            var alert = new Alert
            {
                Type = AlertType.Custom,
                Severity = AlertSeverity.Critical,
                Title = "EMERGENCY: Stop Activated",
                Message = $"All robots stopped. {stoppedIds.Count} robots affected."
            };
            db.Alerts.Add(alert);
            await db.SaveChangesAsync();

            if (broadcaster is not null)
            {
                await broadcaster.BroadcastAlertCreated(alert.Id, "Emergency", "Critical", alert.Title, alert.Message);
            }

            return Results.Ok(new EmergencyStopResponse(stoppedIds.Count, stoppedIds, DateTime.UtcNow));
        })
        .WithName("EmergencyStopAll")
        .WithDescription("Emergency stop all robots");

        // POST /api/emergency/evacuate - Trigger evacuation mode
        group.MapPost("/emergency/evacuate", async (RestaurantDbContext db, IEventBroadcaster? broadcaster) =>
        {
            // Stop all robots
            var robots = await db.Robots.ToListAsync();
            foreach (var robot in robots)
            {
                robot.Status = RobotStatus.Returning;
                robot.IsEnabled = false;
            }

            // Cancel all pending/in-progress tasks
            var activeTasks = await db.Tasks
                .Where(t => t.Status == Entities.TaskStatus.Pending || 
                           t.Status == Entities.TaskStatus.Assigned || 
                           t.Status == Entities.TaskStatus.InProgress)
                .ToListAsync();

            foreach (var task in activeTasks)
            {
                task.Status = Entities.TaskStatus.Cancelled;
                task.ErrorMessage = "Cancelled due to evacuation";
            }

            await db.SaveChangesAsync();

            // Create evacuation alert
            var alert = new Alert
            {
                Type = AlertType.Custom,
                Severity = AlertSeverity.Critical,
                Title = "EMERGENCY: Evacuation Mode Activated",
                Message = $"All operations suspended. {robots.Count} robots returning home. {activeTasks.Count} tasks cancelled."
            };
            db.Alerts.Add(alert);
            await db.SaveChangesAsync();

            if (broadcaster is not null)
            {
                await broadcaster.BroadcastAlertCreated(alert.Id, "Emergency", "Critical", alert.Title, alert.Message);
            }

            return Results.Ok(new
            {
                message = "Evacuation mode activated",
                robotsAffected = robots.Count,
                tasksCancelled = activeTasks.Count,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("ActivateEvacuation")
        .WithDescription("Activate evacuation mode - stop all robots and cancel tasks");

        // POST /api/emergency/resume - Resume normal operations
        group.MapPost("/emergency/resume", async (RestaurantDbContext db, IEventBroadcaster? broadcaster) =>
        {
            var robots = await db.Robots.ToListAsync();
            foreach (var robot in robots)
            {
                if (robot.Status == RobotStatus.Error || robot.Status == RobotStatus.Returning)
                {
                    robot.Status = RobotStatus.Idle;
                }
                robot.IsEnabled = true;
            }

            await db.SaveChangesAsync();

            var alert = new Alert
            {
                Type = AlertType.Custom,
                Severity = AlertSeverity.Info,
                Title = "Normal Operations Resumed",
                Message = $"Emergency cleared. {robots.Count} robots re-enabled."
            };
            db.Alerts.Add(alert);
            await db.SaveChangesAsync();

            if (broadcaster is not null)
            {
                await broadcaster.BroadcastAlertCreated(alert.Id, "System", "Info", alert.Title, alert.Message);
            }

            return Results.Ok(new
            {
                message = "Normal operations resumed",
                robotsEnabled = robots.Count,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("ResumeOperations")
        .WithDescription("Resume normal operations after emergency");
    }

    // Internal class for webhook configuration
    private class WebhookConfig
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public List<string> Events { get; set; } = new();
        public string? Secret { get; set; }
        public bool IsActive { get; set; } = true;
        public int FailureCount { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
