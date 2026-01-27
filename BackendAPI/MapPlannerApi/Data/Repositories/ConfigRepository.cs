using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for SystemConfig operations.
/// </summary>
public class ConfigRepository
{
    private readonly RestaurantDbContext _db;

    public ConfigRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<SystemConfig>> GetAllAsync()
    {
        return await _db.SystemConfigs.OrderBy(c => c.Key).ToListAsync();
    }

    public async Task<SystemConfig?> GetAsync(string key)
    {
        return await _db.SystemConfigs.FindAsync(key);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var config = await _db.SystemConfigs.FindAsync(key);
        return config?.Value;
    }

    public async Task<T?> GetValueAsync<T>(string key) where T : struct
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return null;
        }
    }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue) where T : struct
    {
        return await GetValueAsync<T>(key) ?? defaultValue;
    }

    public async Task SetAsync(string key, string value, string? description = null, string valueType = "string")
    {
        var config = await _db.SystemConfigs.FindAsync(key);
        if (config == null)
        {
            config = new SystemConfig
            {
                Key = key,
                Value = value,
                Description = description,
                ValueType = valueType,
                UpdatedAt = DateTime.UtcNow
            };
            _db.SystemConfigs.Add(config);
        }
        else
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
            if (description != null) config.Description = description;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var config = await _db.SystemConfigs.FindAsync(key);
        if (config == null) return false;

        _db.SystemConfigs.Remove(config);
        await _db.SaveChangesAsync();
        return true;
    }

    // Convenience methods for common config values
    public async Task<double> GetPixelsPerMeterAsync()
    {
        return await GetValueAsync(ConfigKeys.PixelsPerMeter, 100.0);
    }

    public async Task<double> GetDefaultRobotSpeedAsync()
    {
        return await GetValueAsync(ConfigKeys.DefaultRobotSpeed, 0.5);
    }

    public async Task<int> GetLowBatteryThresholdAsync()
    {
        return await GetValueAsync(ConfigKeys.LowBatteryThreshold, 20);
    }

    public async Task<int> GetMaxConcurrentTasksAsync()
    {
        return await GetValueAsync(ConfigKeys.MaxConcurrentTasks, 3);
    }

    public async Task<int> GetTaskRetryLimitAsync()
    {
        return await GetValueAsync(ConfigKeys.TaskRetryLimit, 3);
    }
}
