using System;
using Infrastructure.CacheManager.API.Invalidation;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Monitoring;
using Infrastructure.CacheManager.Internal.Core;
using Infrastructure.CacheManager.Internal.Invalidation;
using Infrastructure.CacheManager.Internal.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.CacheManager.DI
{
    /// <summary>
    /// Extension methods for registering cache manager services in the DI container.
    /// </summary>
    public static class CacheManagerServiceExtensions
    {
        /// <summary>
        /// Adds cache manager services to the service collection.
        /// </summary>
        public static IServiceCollection AddCacheManagerModule(
            this IServiceCollection services,
            Action<CacheManagerOptions>? configureOptions = null)
        {
            var options = new CacheManagerOptions();
            configureOptions?.Invoke(options);

            // Register memory pressure monitor
            services.TryAddSingleton<IMemoryPressureMonitor>(provider =>
                new MemoryPressureMonitor(
                    options.LowMemoryThresholdPercentage,
                    options.MediumMemoryThresholdPercentage,
                    options.HighMemoryThresholdPercentage,
                    options.CriticalMemoryThresholdPercentage,
                    options.MemoryCheckInterval));

            // Register cache manager
            services.TryAddSingleton<ICacheManager>(provider =>
                new MemoryCacheManager(
                    provider.GetRequiredService<IMemoryPressureMonitor>(),
                    options.StatisticsCollectionInterval));

            // Register cache invalidator
            services.TryAddSingleton<ICacheInvalidator>(provider =>
                new CacheInvalidator(provider.GetRequiredService<ICacheManager>()));

            // Register specialized caches for the rendering pipeline
            services.TryAddSingleton(provider =>
            {
                var cacheManager = provider.GetRequiredService<ICacheManager>();

                // Create and register standard caches using the factory
                var styleCache = MemoryCacheFactory.CreateStyleCache();
                var layoutCache = MemoryCacheFactory.CreateLayoutCache();
                var renderCache = MemoryCacheFactory.CreateRenderCache();
                var resourceCache = MemoryCacheFactory.CreateResourceCache();

                // Register with the cache manager
                cacheManager.RegisterCache(styleCache.Name, styleCache);
                cacheManager.RegisterCache(layoutCache.Name, layoutCache);
                cacheManager.RegisterCache(renderCache.Name, renderCache);
                cacheManager.RegisterCache(resourceCache.Name, resourceCache);

                // Create registration object to hold references
                return new CacheRegistrationHolder(styleCache, layoutCache, renderCache, resourceCache);
            });

            // Start memory monitoring service if requested
            if (options.AutoStartMemoryMonitoring)
            {
                services.TryAddSingleton<MemoryMonitoringService>();
            }

            return services;
        }


        /// <summary>
        /// Helper class to hold references to the specialized caches to prevent garbage collection.
        /// </summary>
        private class CacheRegistrationHolder : IDisposable
        {
            private readonly object _styleCache;
            private readonly object _layoutCache;
            private readonly object _renderCache;
            private readonly object _resourceCache;
            private bool _disposed = false;

            public CacheRegistrationHolder(
                object styleCache,
                object layoutCache,
                object renderCache,
                object resourceCache)
            {
                _styleCache = styleCache;
                _layoutCache = layoutCache;
                _renderCache = renderCache;
                _resourceCache = resourceCache;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_styleCache is IDisposable styleCacheDisposable)
                    styleCacheDisposable.Dispose();

                if (_layoutCache is IDisposable layoutCacheDisposable)
                    layoutCacheDisposable.Dispose();

                if (_renderCache is IDisposable renderCacheDisposable)
                    renderCacheDisposable.Dispose();

                if (_resourceCache is IDisposable resourceCacheDisposable)
                    resourceCacheDisposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Configuration options for the cache manager.
    /// </summary>
    public class CacheManagerOptions
    {
        /// <summary>
        /// Percentage of system memory at which low memory pressure is triggered.
        /// </summary>
        public double LowMemoryThresholdPercentage { get; set; } = 60;

        /// <summary>
        /// Percentage of system memory at which medium memory pressure is triggered.
        /// </summary>
        public double MediumMemoryThresholdPercentage { get; set; } = 70;

        /// <summary>
        /// Percentage of system memory at which high memory pressure is triggered.
        /// </summary>
        public double HighMemoryThresholdPercentage { get; set; } = 80;

        /// <summary>
        /// Percentage of system memory at which critical memory pressure is triggered.
        /// </summary>
        public double CriticalMemoryThresholdPercentage { get; set; } = 90;

        /// <summary>
        /// Interval at which memory usage is checked.
        /// </summary>
        public TimeSpan MemoryCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Interval at which cache statistics are collected.
        /// </summary>
        public TimeSpan StatisticsCollectionInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to automatically start memory monitoring when the services are registered.
        /// </summary>
        public bool AutoStartMemoryMonitoring { get; set; } = true;

        /// <summary>
        /// Whether to enable automatic cleanup of expired cache entries.
        /// </summary>
        public bool EnableAutomaticCleanup { get; set; } = true;

        /// <summary>
        /// Interval at which expired cache entries are cleaned up.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);
    }
}