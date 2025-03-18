using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.Internal.Caches;

namespace Infrastructure.CacheManager.Internal.Core
{
    /// <summary>
    /// Factory class for creating specialized MemoryCache instances optimized for different use cases.
    /// Provides a unified creation point for all cache types needed in the rendering pipeline.
    /// </summary>
    internal static class MemoryCacheFactory
    {
        /// <summary>
        /// Creates a basic MemoryCache-based cache.
        /// </summary>
        public static ICache<TKey, TValue> CreateCache<TKey, TValue>(
            string name,
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null,
            MemoryCacheOptions? options = null)
        {
            return new MemoryCacheBase<TKey, TValue>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a MemoryCache with priority-based eviction support.
        /// </summary>
        public static IPrioritizedCache<TKey, TValue> CreatePrioritizedCache<TKey, TValue>(
            string name,
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null,
            MemoryCacheOptions? options = null)
        {
            return new PrioritizedMemoryCache<TKey, TValue>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a MemoryCache with dependency tracking support.
        /// </summary>
        public static IDependencyTrackingCache<TKey, TValue> CreateDependencyTrackingCache<TKey, TValue>(
            string name,
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null,
            MemoryCacheOptions? options = null)
        {
            return new DependencyTrackingMemoryCache<TKey, TValue>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a MemoryCache with both priority-based eviction and dependency tracking.
        /// Optimized for rendering pipeline use cases.
        /// </summary>
        public static PrioritizedDependencyTrackingMemoryCache<TKey, TValue> CreateAdvancedCache<TKey, TValue>(
            string name,
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null,
            MemoryCacheOptions? options = null)
        {
            return new PrioritizedDependencyTrackingMemoryCache<TKey, TValue>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a specialized StyleCache optimized for style computations.
        /// </summary>
        public static PrioritizedDependencyTrackingMemoryCache<string, object> CreateStyleCache(
            string name = "StyleCache",
            CachePriority priority = CachePriority.High,
            TimeSpan? cleanupInterval = null)
        {
            // Style cache typically needs more aggressive memory limits since styles change frequently
            var options = new MemoryCacheOptions
            {
                SizeLimit = 10_000, // Limit entries to prevent memory issues
                ExpirationScanFrequency = TimeSpan.FromMinutes(1)
            };

            return CreateAdvancedCache<string, object>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a specialized LayoutCache optimized for layout computations.
        /// </summary>
        public static PrioritizedDependencyTrackingMemoryCache<string, object> CreateLayoutCache(
            string name = "LayoutCache",
            CachePriority priority = CachePriority.High,
            TimeSpan? cleanupInterval = null)
        {
            // Layout cache is performance-critical, so optimize for speed
            var options = new MemoryCacheOptions
            {
                SizeLimit = 5_000, // Layout objects can be large, so limit entries
                ExpirationScanFrequency = TimeSpan.FromMinutes(5)
            };

            return CreateAdvancedCache<string, object>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a specialized RenderCache optimized for rendered output.
        /// </summary>
        public static PrioritizedDependencyTrackingMemoryCache<string, object> CreateRenderCache(
            string name = "RenderCache",
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null)
        {
            // Render cache contains the largest objects, so be more aggressive with expiration
            var options = new MemoryCacheOptions
            {
                SizeLimit = 1_000, // Rendered output can be very large, so strictly limit entries
                ExpirationScanFrequency = TimeSpan.FromSeconds(30)
            };

            return CreateAdvancedCache<string, object>(name, priority, cleanupInterval, options);
        }

        /// <summary>
        /// Creates a specialized ResourceCache optimized for external resources.
        /// </summary>
        public static PrioritizedDependencyTrackingMemoryCache<string, object> CreateResourceCache(
            string name = "ResourceCache",
            CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null)
        {
            var options = new MemoryCacheOptions
            {
                SizeLimit = 2_000, // Resources can vary in size dramatically
                ExpirationScanFrequency = TimeSpan.FromMinutes(10)
            };

            return CreateAdvancedCache<string, object>(name, priority, cleanupInterval, options);
        }
    }
}