namespace Catalog.Infrastructure.Cache;

/// <summary>
/// Abstraction for in-memory caching used by application services.
/// </summary>
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? ttl = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
}
