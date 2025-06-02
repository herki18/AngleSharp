namespace LayoutEngine.NG.Style;

using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Parser;
using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.NG.Layout.Dom;

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
        // Register layout data manager (needed by StyleResolver)
        services.AddSingleton<LayoutDataManager>();

        // Register AngleSharp configuration with CSS support
        services.AddSingleton<IConfiguration>(provider =>
        {
            return Configuration.Default
                .WithCss()  // Enable CSS support
                .WithDefaultLoader();  // Enable resource loading
        });

        // Register browsing context
        services.AddSingleton<IBrowsingContext>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return BrowsingContext.New(config);
        });

        // Register CSS parser
        services.AddSingleton<ICssParser>(provider =>
        {
            var options = new CssParserOptions
            {
                IsIncludingUnknownDeclarations = true,
                IsToleratingInvalidValues = true,
                IsToleratingInvalidConstraints = true
            };
            return new CssParser(options);
        });

        // Register CSS style declaration factory
        services.AddSingleton<ICssStyleDeclarationFactory>(provider =>
        {
            var cssParser = provider.GetRequiredService<ICssParser>();
            var context = provider.GetRequiredService<IBrowsingContext>();
            return new CssStyleDeclarationFactory(cssParser, context);
        });

        // Register cascade resolver
        services.AddSingleton<ICascadeResolver>(provider =>
        {
            var factory = provider.GetRequiredService<ICssStyleDeclarationFactory>();
            var cssParser = provider.GetRequiredService<ICssParser>();
            return new CascadeResolver(factory, cssParser);
        });

        // Register style resolver
        services.AddSingleton<IStyleResolver>(provider =>
        {
            var cascadeResolver = provider.GetRequiredService<ICascadeResolver>();
            var layoutDataManager = provider.GetRequiredService<LayoutDataManager>();
            return new StyleResolver(cascadeResolver, layoutDataManager);
        });

        return services;
    }
}