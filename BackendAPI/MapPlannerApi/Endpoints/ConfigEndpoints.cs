using MapPlannerApi.Data;
using MapPlannerApi.Dtos;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/config").WithTags("Configuration");

        // GET /api/config - List all config
        group.MapGet("/", async (ConfigRepository repo) =>
        {
            var configs = await repo.GetAllAsync();
            return Results.Ok(configs.Select(ToDto));
        })
        .WithName("GetAllConfig")
        .WithDescription("Get all configuration values");

        // GET /api/config/{key} - Get config value
        group.MapGet("/{key}", async (string key, ConfigRepository repo) =>
        {
            var config = await repo.GetAsync(key);
            return config is null
                ? Results.NotFound(new { error = $"Config key '{key}' not found" })
                : Results.Ok(ToDto(config));
        })
        .WithName("GetConfig")
        .WithDescription("Get configuration value by key");

        // PUT /api/config/{key} - Set config value
        group.MapPut("/{key}", async (string key, SetConfigRequest request, ConfigRepository repo) =>
        {
            await repo.SetAsync(key, request.Value, request.Description);
            return Results.Ok(new { message = $"Config '{key}' updated" });
        })
        .WithName("SetConfig")
        .WithDescription("Set configuration value");

        // DELETE /api/config/{key} - Delete config
        group.MapDelete("/{key}", async (string key, ConfigRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(key);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Config key '{key}' not found" });
        })
        .WithName("DeleteConfig")
        .WithDescription("Delete configuration value");
    }

    private static ConfigDto ToDto(SystemConfig c) => new(
        c.Key,
        c.Value,
        c.Description,
        c.ValueType,
        c.UpdatedAt
    );
}
