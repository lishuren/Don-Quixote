# GitHub Copilot Instructions for MapPlannerApi (Backend)

> **Project**: MapPlannerApi  
> **Type**: Backend REST API  
> **Location**: `BackendAPI/MapPlannerApi/`

## Project Overview

This is a **Restaurant Robot Management API** built with .NET 10.0 using ASP.NET Core Minimal APIs. The system manages autonomous delivery robots in a restaurant environment, handling task dispatch, real-time tracking, guest management, and simulation capabilities.

## Technology Stack

- **Framework**: .NET 10.0 (Preview)
- **API Pattern**: ASP.NET Core Minimal APIs
- **Database**: SQLite via Entity Framework Core 10.0
- **Real-time**: SignalR for WebSocket events
- **Architecture**: Repository pattern with scoped services

## Project Structure

```
MapPlannerApi/
├── Data/
│   ├── RestaurantDbContext.cs      # EF Core DbContext
│   └── Repositories/               # Repository classes for each entity
├── Endpoints/                      # Minimal API endpoint definitions
│   ├── RobotEndpoints.cs          # Robot CRUD + commands
│   ├── TaskEndpoints.cs           # Task lifecycle management
│   ├── TableEndpoints.cs          # Table management
│   ├── GuestEndpoints.cs          # Guest/waitlist management
│   ├── AlertEndpoints.cs          # Alert management
│   ├── ZoneEndpoints.cs           # Zone/checkpoint management
│   ├── DispatchEndpoints.cs       # Auto task dispatch
│   ├── SimulationEndpoints.cs     # Simulation controls
│   └── IntegrationEndpoints.cs    # Webhooks, POS, Emergency
├── Entities/                       # EF Core entity models
├── Dtos/
│   └── ApiDtos.cs                 # All request/response DTOs
├── Hubs/
│   └── RestaurantHub.cs           # SignalR hub
├── Services/
│   ├── EventBroadcaster.cs        # SignalR event broadcasting
│   ├── DispatchEngine.cs          # Task assignment algorithms
│   ├── MapStore.cs                # Legacy map storage
│   └── PathPlanner.cs             # Legacy path planning
├── Models/                         # Legacy models
└── Program.cs                      # Application bootstrap
```

## Coding Conventions

### Endpoint Pattern
Use the Minimal API pattern with route groups:

```csharp
public static class ExampleEndpoints
{
    public static void MapExampleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/examples").WithTags("Examples");

        group.MapGet("/", async (ExampleRepository repo) =>
        {
            var items = await repo.GetAllAsync();
            return Results.Ok(items.Select(ToDto));
        })
        .WithName("GetExamples")
        .WithDescription("Get all examples");
    }

    private static ExampleDto ToDto(Example e) => new(...);
}
```

### DTO Naming
- Request DTOs: `Create{Entity}Request`, `Update{Entity}Request`
- Response DTOs: `{Entity}Dto`
- Summary DTOs: `{Entity}Summary`
- Event DTOs: `{Entity}Event`

### Repository Pattern
Each entity has a dedicated repository in `Data/Repositories/`:

```csharp
public class ExampleRepository
{
    private readonly RestaurantDbContext _db;

    public ExampleRepository(RestaurantDbContext db) => _db = db;

    public async Task<List<Example>> GetAllAsync() =>
        await _db.Examples.ToListAsync();

    public async Task<Example?> GetByIdAsync(int id) =>
        await _db.Examples.FindAsync(id);
}
```

### Entity Enums
Define enums in the same file as the entity:

```csharp
public enum ExampleStatus { Active, Inactive, Pending }

public class Example
{
    public int Id { get; set; }
    public ExampleStatus Status { get; set; }
}
```

## Key Entities

| Entity | Purpose |
|--------|---------|
| `Robot` | Delivery robot with position, battery, status |
| `RobotTask` | Task assigned to robot (Deliver, Return, Charge, Patrol) |
| `TableEntity` | Restaurant table with capacity and status |
| `Guest` | Guest/party with waitlist position |
| `Alert` | System alerts and notifications |
| `Zone` | Restaurant zones (Dining, Kitchen, Charging) |
| `Checkpoint` | Navigation waypoints |
| `Reservation` | Table reservations |

## Robot Status Flow

```
Idle → Navigating → Delivering → Returning → Idle
  ↓                                    ↓
Charging ← ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘
  ↓
Error → (recovery) → Idle
```

## Task Status Flow

```
Pending → Assigned → InProgress → Completed
    ↓         ↓          ↓
    └─────────┴──────────┴──→ Failed/Cancelled
```

## SignalR Events

The hub is at `/hubs/restaurant`. Events broadcast:

- `RobotPositionUpdated` - Real-time robot position
- `RobotStatusChanged` - Robot state transitions
- `TaskStatusChanged` - Task lifecycle events
- `AlertCreated` - New alerts
- `TableStatusChanged` - Table occupancy changes
- `GuestEvent` - Guest arrived/seated/left

Inject `IEventBroadcaster` to broadcast events:

```csharp
if (broadcaster is not null)
{
    await broadcaster.BroadcastRobotStatusChanged(
        robot.Id, robot.Name, oldStatus, newStatus, reason);
}
```

## Dispatch Engine

The `IDispatchEngine` handles automatic task assignment with algorithms:
- `nearest` - Assign to nearest available robot
- `round_robin` - Distribute tasks evenly
- `load_balanced` - Balance by task count and battery
- `priority` - Prioritize by task urgency

## Dual Coordinate System

Positions use both screen (pixels) and physical (meters) coordinates:

```csharp
public record Position(
    double ScreenX,   // UI pixels
    double ScreenY,
    double PhysicalX, // Real-world meters
    double PhysicalY
);
```

Convert using `ConfigRepository.GetPixelsPerMeterAsync()`.

## Common Patterns

### Error Responses
```csharp
return Results.NotFound(new { error = $"Robot {id} not found" });
return Results.BadRequest(new { error = "Invalid status" });
```

### Validation
```csharp
if (!Enum.TryParse<RobotStatus>(status, true, out var robotStatus))
    return Results.BadRequest(new { error = $"Invalid status: {status}" });
```

### Include Related Data
```csharp
var robot = await _db.Robots
    .Include(r => r.CurrentTask)
    .FirstOrDefaultAsync(r => r.Id == id);
```

## Testing Endpoints

Run the server:
```bash
cd BackendAPI/MapPlannerApi
dotnet run --urls "http://localhost:5199"
```

Test with curl:
```bash
curl http://localhost:5199/api/health
curl http://localhost:5199/api/robots
curl -X POST http://localhost:5199/api/robots \
  -H "Content-Type: application/json" \
  -d '{"name":"Bot1","model":"V2"}'
```

Swagger UI available at: `http://localhost:5199/swagger`

## Do's and Don'ts

### Do
- Use async/await for all database operations
- Return appropriate HTTP status codes (Created, NoContent, NotFound, BadRequest)
- Include `.WithName()` and `.WithDescription()` on endpoints
- Use the repository pattern for data access
- Broadcast events via `IEventBroadcaster` for state changes

### Don't
- Don't use `Task.Result` or `.Wait()` (use await)
- Don't expose entity models directly (use DTOs)
- Don't hardcode connection strings
- Don't forget to register new services in `Program.cs`
- Don't use `AlertType.Emergency` or `AlertType.System` (use `AlertType.Custom`)
- Don't use `TaskType.Delivery` (use `TaskType.Deliver`)
