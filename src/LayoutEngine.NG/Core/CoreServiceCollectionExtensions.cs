namespace LayoutEngine.NG.Core;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering core engine services with DI.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core engine services with the DI container.
    /// </summary>
    public static IServiceCollection AddLayoutEngineCore(this IServiceCollection services)
    {
        // Register the main engine
        services.AddSingleton<Engine>();

        // Register the main thread scheduler
        services.AddSingleton<MainThreadScheduler>();

        // Register frame-related services
        // Note: Frames are typically created per document, so they're not registered as singletons
        // In BlinkNG, frames are managed by the Page/FrameTree, not DI

        // Register lifecycle coordinator as transient since each frame needs its own
        services.AddTransient<DocumentLifecycleCoordinator>();
        services.AddTransient<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();

        // Note: In real BlinkNG, there would be additional services like:
        // - FrameLoader
        // - FrameTree
        // - Page
        // - Settings
        // But these are outside the scope of this skeleton

        return services;
    }
}