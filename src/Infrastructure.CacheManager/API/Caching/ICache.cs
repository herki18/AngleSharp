namespace Infrastructure.CacheManager.API.Caching;

using System;
using Management;
using Models;

/// <summary>
/// Base interface for all cache implementations.
/// Provides key-value storage with common cache operations.
/// </summary>
/// <typeparam name="TKey">The type of the cache key</typeparam>
/// <typeparam name="TValue">The type of the cached value</typeparam>
public interface ICache<TKey, TValue> : ITrimableCache
{
    /// <summary>
    /// Attempts to get a value from the cache.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The retrieved value if found, default otherwise.</param>
    /// <returns>True if the key was found, false otherwise.</returns>
    bool TryGetValue(TKey key, out TValue? value);

    /// <summary>
    /// Gets a value from the cache if it exists, otherwise creates it using the factory.
    /// This is an atomic operation to prevent duplicate computation.
    /// </summary>
    /// <param name="key">The key to look up or create.</param>
    /// <param name="valueFactory">The factory function to create the value if not found.</param>
    /// <returns>The cached or created value.</returns>
    TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional cache entry options.</param>
    void Set(TKey key, TValue value, CacheEntryOptions? options = null);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    bool Remove(TKey key);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool Contains(TKey key);
}