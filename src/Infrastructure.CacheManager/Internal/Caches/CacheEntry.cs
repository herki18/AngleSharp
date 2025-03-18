namespace Infrastructure.CacheManager.Internal.Caches;

using System;
using API.Caching;
using API.Models;
using Utilities;

/// <summary>
/// Implementation of ICacheEntry that stores a value and its metadata.
/// </summary>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
internal class CacheEntry<TValue> : ICacheEntry<TValue>, IDisposable
{
    private readonly object _syncLock = new object();
    private TValue _value;
    private DateTime? _absoluteExpiration;
    private TimeSpan? _slidingExpiration;
    private bool _isDisposed;
    private readonly CacheEntryOptions _options;

    /// <summary>
    /// Gets the key associated with this cache entry.
    /// </summary>
    public object Key { get; }

    /// <summary>
    /// Gets or sets the value stored in this cache entry.
    /// </summary>
    public TValue Value
    {
        get => _value;
        set
        {
            lock (_syncLock)
            {
                _value = value;
                RecalculateSize();
            }
        }
    }

    /// <summary>
    /// Gets the options associated with this cache entry.
    /// </summary>
    public CacheEntryOptions Options => _options;

    /// <summary>
    /// Gets the UTC time when this entry was created.
    /// </summary>
    public DateTime CreationTime { get; }

    /// <summary>
    /// Gets the UTC time when this entry was last accessed.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Gets the UTC time when this entry will expire, if applicable.
    /// </summary>
    public DateTime? ExpirationTime
    {
        get
        {
            lock (_syncLock)
            {
                return _absoluteExpiration;
            }
        }
    }

    /// <summary>
    /// Gets or sets the priority of this individual cache entry.
    /// </summary>
    public CacheEntryPriority Priority { get; set; }

    /// <summary>
    /// Gets the estimated size of this entry in bytes.
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry{TValue}"/> class.
    /// </summary>
    /// <param name="key">The key associated with this entry.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="priority">The priority of this entry.</param>
    public CacheEntry(object key, TValue value, CacheEntryOptions? options = null, CacheEntryPriority priority = CacheEntryPriority.Normal)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _value = value;
        _options = options?.Clone() ?? new CacheEntryOptions();
        Priority = priority;
        CreationTime = DateTime.UtcNow;
        LastAccessTime = CreationTime;

        // Calculate size
        RecalculateSize();

        // Set expiration times
        if (_options.AbsoluteExpiration.HasValue)
        {
            _absoluteExpiration = CreationTime.Add(_options.AbsoluteExpiration.Value);
        }

        _slidingExpiration = _options.SlidingExpiration;

        // Set initial expiration time
        RefreshExpiration();
    }

    /// <summary>
    /// Refreshes the expiration time of this entry based on its options.
    /// </summary>
    public void RefreshExpiration()
    {
        lock (_syncLock)
        {
            if (_isDisposed)
                return;

            LastAccessTime = DateTime.UtcNow;

            if (_slidingExpiration.HasValue)
            {
                // Update absolute expiration based on sliding window
                DateTime newExpiration = LastAccessTime.Add(_slidingExpiration.Value);

                // If there's already an absolute expiration, take the earlier one
                if (_absoluteExpiration.HasValue)
                {
                    _absoluteExpiration = newExpiration < _absoluteExpiration.Value ?
                        newExpiration : _absoluteExpiration.Value;
                }
                else
                {
                    _absoluteExpiration = newExpiration;
                }
            }
        }
    }

    /// <summary>
    /// Checks whether this entry has expired.
    /// </summary>
    /// <returns>True if the entry has expired, false otherwise.</returns>
    public bool HasExpired()
    {
        lock (_syncLock)
        {
            if (_isDisposed)
                return true;

            if (_absoluteExpiration.HasValue)
            {
                return DateTime.UtcNow >= _absoluteExpiration.Value;
            }

            return false;
        }
    }

    /// <summary>
    /// Recalculates the size of this entry.
    /// </summary>
    private void RecalculateSize()
    {
        // If size is explicitly specified in options, use that
        if (_options.Size.HasValue)
        {
            Size = _options.Size.Value;
            return;
        }

        // Otherwise estimate it
        Size = SizeEstimator.EstimateSize(_value);
    }

    /// <summary>
    /// Disposes resources used by the cache entry.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        lock (_syncLock)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_value != null)
            {
                _options.PostEvictionCallback?.Invoke(_value);
            }

            // If value implements IDisposable, dispose it
            if (_value is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _value = default!;
        }
    }
}