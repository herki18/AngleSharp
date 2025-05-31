namespace LayoutEngine.Core.LayoutStyle;

using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Core.LayoutStyle.Public;
using LayoutEngine.Core.LayoutStyle.Internal;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the LayoutNG-aligned style system services.
    /// </summary>
    public static IServiceCollection AddLayoutStyleSystem(this IServiceCollection services)
    {
        // Core style system
        services.AddSingleton<ILayoutStyleSystem, LayoutStyleSystem>();

        // Style resolution components
        services.AddScoped<ILayoutStyleResolver, LayoutStyleResolver>();
        services.AddScoped<IAnonymousStyleSynthesizer, AnonymousStyleSynthesizer>();

        // Note: The following would need to be provided or adapted from existing services:
        // - IStyleSheetManager (manages stylesheets)
        // - ICssStyleDeclarationFactory (creates CSS declarations)
        // - ICssParser (parses CSS)

        // These could be adapted from the existing style system or AngleSharp

        return services;
    }
}