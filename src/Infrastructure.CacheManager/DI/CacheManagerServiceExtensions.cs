using System;
using Infrastructure.CacheManager.API.Invalidation;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.Internal.Core;
using Infrastructure.CacheManager.Internal.Invalidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.CacheManager.DI;

using API.Factory;
using Internal.Factory;

public static class CacheManagerServiceExtensions
{
    /// <summary>
    /// Adds the CacheManager infrastructure to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for CacheManager options</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddCacheManagerModule(
        this IServiceCollection services,
        Action<CacheManagerOptions>? configureOptions = null)
    {
        var options = new CacheManagerOptions();
        configureOptions?.Invoke(options);

        // Register ICacheManager without memory pressure monitor
        services.TryAddSingleton<ICacheManager>(provider =>
            new MemoryCacheManager(options.StatisticsCollectionInterval));

        // Register cache factory
        services.TryAddSingleton<ICacheFactory, MemoryCacheFactory>();

        // Register cache invalidator
        services.TryAddSingleton<ICacheInvalidator>(provider =>
            new CacheInvalidator(provider.GetRequiredService<ICacheManager>()));

        return services;
    }
}