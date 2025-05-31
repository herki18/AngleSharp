namespace LayoutEngine.NG;

using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.NG.Core;
using LayoutEngine.NG.Style;

/// <summary>
/// Extension methods for registering the complete LayoutEngine.NG with DI.
/// </summary>
public static class LayoutEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers all LayoutEngine.NG services with the DI container.
    /// This is the main entry point for configuring the layout engine.
    /// </summary>
    public static IServiceCollection AddLayoutEngineNG(this IServiceCollection services)
    {
        // Register core engine services
        services.AddLayoutEngineCore();

        // Register style system services
        services.AddStyleSystem();

        // Register paint system services
        // Note: IPaintSystem implementation is not provided in this skeleton
        // In real implementation, you would call services.AddPaintSystem();

        // Note: Layout services are not registered separately as in LayoutNG
        // Layout is performed directly by LayoutObject/LayoutView instances,
        // not through a separate service abstraction

        return services;
    }
}