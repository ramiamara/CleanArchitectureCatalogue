namespace Catalog.Infrastructure.Cache;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// IMemoryCache-backed implementation of ICacheService with prefix-based invalidation.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _keys = new();
    private readonly object _lock = new();

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("[Cache HIT] {Key}", key);
            return value;
        }
        _logger.LogDebug("[Cache MISS] {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null)
    {
        var expiry = ttl ?? DefaultTtl;
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry,
            Priority = CacheItemPriority.Normal
        };
        _cache.Set(key, value, options);
        lock (_lock) { _keys.Add(key); }
        _logger.LogDebug("[Cache SET] {Key} TTL={Ttl}", key, expiry);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
        _logger.LogDebug("[Cache REMOVE] {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> toRemove;
        lock (_lock)
        {
            toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
        }
        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
        }
        _logger.LogDebug("[Cache INVALIDATE] prefix={Prefix} ({Count} keys)", prefix, toRemove.Count);
    }
}
