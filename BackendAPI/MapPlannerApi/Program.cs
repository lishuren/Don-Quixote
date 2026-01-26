using MapPlannerApi.Services;
using MapPlannerApi.Models;
using System.Text.Json.Serialization;  

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// API metadata and documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow local development frontends to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173"
            );
    });
});

builder.Services.AddSingleton<MapStore>();
builder.Services.AddSingleton<PathPlanner>();


// JSON serialization preferences for minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic health probe for monitoring pipelines
app.MapGet("/api/health", () => new { status = "ok" });

// Retrieve the currently cached map and version
app.MapGet("/api/map", (MapStore store) =>
{
    var map = store.GetMap();
    
    if (map is null)
    {
        return Results.NotFound(new { error = "No map uploaded. Call POST /api/map first." });
    }
    return Results.Ok(new { mapId = store.CurrentMapId, map });
})
.WithName("GetMap");

// Store a new map snapshot and refresh the version
app.MapPost("/api/map", (MapPayload payload, MapStore store) =>
{
    store.SetMap(payload);
    store.Refresh();
    return Results.Ok(new { message = "Map received", mapId = store.CurrentMapId });
})
.WithName("PostMap");

// Run A* path planning for the given request
app.MapPost("/api/plan", (PlanRequest request, MapStore store, PathPlanner planner) =>
{
    var map = store.GetMap();
    if (map is null)
    {
        return Results.BadRequest(new { error = "No map uploaded. Call /api/map first." });
    }

    var result = planner.Plan(map, request);

    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.Error ?? "Planning failed" });
    }
    return Results.Ok(result);
})
.WithName("PlanRoute");

app.Run();
