namespace LayoutEngine.NG.Style;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering style system services with DI.
/// </summary>
public static class StyleServiceCollectionExtensions
{
    /// <summary>
    /// Registers all style system services with the DI container.
    /// </summary>
    public static IServiceCollection AddStyleSystem(this IServiceCollection services)
    {
        // Register the main interfaces and implementations
        services.AddSingleton<IStyleResolver, StyleResolver>();

        services.AddSingleton<ICascadeResolver, CascadeResolver>();

        // Register factories if needed
        // In BlinkNG, various factories create style-related objects

        // Note: The following registrations assume these implementations exist
        // They would need to be created or come from AngleSharp
        // services.AddSingleton<ICssParser>(); // From AngleSharp
        // services.AddSingleton<ICssStyleDeclarationFactory>(); // From AngleSharp or custom

        return services;
    }
}