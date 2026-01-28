using MapPlannerApi.Services;
using MapPlannerApi.Services.Simulation;
using MapPlannerApi.Models;
using MapPlannerApi.Data;
using MapPlannerApi.Endpoints;
using MapPlannerApi.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;  

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Database configuration - SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=restaurant.db";
builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseSqlite(connectionString));

// API metadata and documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Restaurant Robot Direction API", 
        Version = "v1",
        Description = "Backend API for restaurant robot management and coordination"
    });
});

// SignalR for real-time communication
builder.Services.AddSignalR();

// Allow local development frontends to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173"
            );
    });
});

// Legacy services (kept for backward compatibility)
builder.Services.AddSingleton<MapStore>();
builder.Services.AddSingleton<PathPlanner>();

// Repository services
builder.Services.AddScoped<RobotRepository>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<TableRepository>();
builder.Services.AddScoped<GuestRepository>();
builder.Services.AddScoped<AlertRepository>();
builder.Services.AddScoped<ZoneRepository>();
builder.Services.AddScoped<ReservationRepository>();
builder.Services.AddScoped<ConfigRepository>();

// Application services
builder.Services.AddScoped<IDispatchEngine, DispatchEngine>();
builder.Services.AddScoped<IEventTriggerService, EventTriggerService>();

// Simulation services (singletons for long-running simulations)
builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();
builder.Services.AddSingleton<ISimulationClock, SimulationClock>();
builder.Services.AddSingleton<IRestaurantEventGenerator, RestaurantEventGenerator>();
builder.Services.AddSingleton<ISimulationMetricsAggregator, SimulationMetricsAggregator>();
builder.Services.AddSingleton<SimulationEngine>();
builder.Services.AddSingleton<ISimulationEngine>(sp => sp.GetRequiredService<SimulationEngine>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SimulationEngine>());

// Background services for monitoring
builder.Services.AddHostedService<RobotMonitorService>();
builder.Services.AddHostedService<DispatchBackgroundService>();

// HTTP client for webhook calls
builder.Services.AddHttpClient();

// JSON serialization preferences for minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic health probe for monitoring pipelines
app.MapGet("/api/health", () => new { status = "ok", version = "1.0.0" });

// Map endpoints for map upload/plan moved to MapEndpoints
app.MapMapEndpoints();

// ===== New API Endpoints =====
app.MapRobotEndpoints();
app.MapTaskEndpoints();
app.MapTableEndpoints();
app.MapGuestEndpoints();
app.MapAlertEndpoints();
app.MapZoneEndpoints();
app.MapReservationEndpoints();
app.MapConfigEndpoints();
app.MapDashboardEndpoints();
app.MapDispatchEndpoints();
app.MapSimulationEndpoints();
app.MapIntegrationEndpoints();
app.MapGroup("/api/kitchen").MapKitchenEndpoints();
app.MapGroup("/api/events").MapEventEndpoints();

// SignalR hub for real-time events
app.MapHub<RestaurantHub>("/hubs/restaurant");

app.Run();
