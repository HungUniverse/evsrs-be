using System.Collections.Concurrent;
using EVSRS.Services.Interface;

namespace EVSRS.API.Services;

/// <summary>
/// Simple in-memory cache for storing planning constraints
/// In production, use Redis or distributed cache
/// </summary>
public interface IConstraintsCache
{
    void Set(string key, PlanningConstraints constraints, TimeSpan? expiration = null);
    PlanningConstraints? Get(string key);
    bool TryGet(string key, out PlanningConstraints? constraints);
    void Remove(string key);
}

public class InMemoryConstraintsCache : IConstraintsCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private class CacheEntry
    {
        public PlanningConstraints Constraints { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public void Set(string key, PlanningConstraints constraints, TimeSpan? expiration = null)
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1));
        _cache[key] = new CacheEntry
        {
            Constraints = constraints,
            ExpiresAt = expiresAt
        };
    }

    public PlanningConstraints? Get(string key)
    {
        TryGet(key, out var constraints);
        return constraints;
    }

    public bool TryGet(string key, out PlanningConstraints? constraints)
    {
        constraints = null;
        
        if (!_cache.TryGetValue(key, out var entry))
            return false;

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        constraints = entry.Constraints;
        return true;
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }
}
