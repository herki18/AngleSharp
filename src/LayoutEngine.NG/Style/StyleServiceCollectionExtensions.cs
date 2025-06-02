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
            // Note: The older property names might not exist in the current version
            // Using the available properties based on AngleSharp.Css documentation
            var options = new CssParserOptions
            {
                IsIncludingUnknownDeclarations = true,
                IsIncludingUnknownRules = true,
                // These properties might not exist in the current version
                // Commented out to fix build errors
                // IsToleratingInvalidValues = true,
                // IsToleratingInvalidConstraints = true
            };

            // Alternative: If the properties don't exist, we can use default options
            // or create the parser without specific options
            return new CssParser(options);
        });

        // Register CSS style declaration factory
        services.AddSingleton<ICssStyleDeclarationFactory>(provider =>
        {
            var context = provider.GetRequiredService<IBrowsingContext>();
            // CssStyleDeclarationFactory only takes IBrowsingContext, not ICssParser
            return new CssStyleDeclarationFactory(context);
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