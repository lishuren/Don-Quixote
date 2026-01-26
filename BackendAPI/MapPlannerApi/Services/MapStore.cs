using MapPlannerApi.Models;

namespace MapPlannerApi.Services;

/// <summary>
/// Maintains the latest map snapshot and an in-memory version counter.
/// </summary>
public class MapStore
{
    private MapPayload? _map;
    private long _version;

    public string CurrentMapId => _version.ToString();

    /// <summary>
    /// Stores the latest map data and bumps the version atomically.
    /// </summary>
    public void SetMap(MapPayload map)
    {
        _map = map;
        Interlocked.Increment(ref _version);
    }

    public MapPayload? GetMap() => _map;

    /// <summary>
    /// Creates a new version id without changing the stored map.
    /// </summary>
    public void Refresh()
    {
        Interlocked.Increment(ref _version);
    }
}
