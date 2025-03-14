using System;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Css;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace AngleSharp.StyleSystem.DependencyInjection;

/// <summary>
/// Extension methods for registering AngleSharp services with a ServiceCollection.
/// </summary>
public static class AngleSharpServiceCollectionExtensions
{
    /// <summary>
    /// Registers AngleSharp core services with the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="context">The AngleSharp browsing context instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAngleSharpServices(
        this IServiceCollection services,
        IBrowsingContext context)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // Register the browsing context
        services.AddSingleton<IBrowsingContext>(context);

        // Register essential AngleSharp services
        RegisterServiceIfAvailable<ICssParser>(services, context);
        RegisterServiceIfAvailable<ICssSelectorParser>(services, context);
        RegisterServiceIfAvailable<IDeclarationFactory>(services, context);
        RegisterServiceIfAvailable<IRenderDevice>(services, context);
        RegisterServiceIfAvailable<IHtmlParser>(services, context);
        RegisterServiceIfAvailable<IDocumentFactory>(services, context);
        RegisterServiceIfAvailable<ICssDefaultStyleSheetProvider>(services, context);
        RegisterServiceIfAvailable<IMarkupFormatter>(services, context);
        RegisterServiceIfAvailable<IStyleFormatter>(services, context);

        return services;
    }

    private static void RegisterServiceIfAvailable<T>(
        IServiceCollection services,
        IBrowsingContext context) where T : class
    {
        var service = context.GetService<T>();
        if (service != null)
        {
            services.AddSingleton<T>(service);
        }
    }
}