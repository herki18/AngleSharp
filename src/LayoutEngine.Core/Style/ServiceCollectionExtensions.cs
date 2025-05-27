namespace LayoutEngine.Core.Style;

using Internal;
using Microsoft.Extensions.DependencyInjection;
using Public;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStyleSystem(this IServiceCollection services)
    {
        // Core style system
        services.AddSingleton<IStyleSystem, StyleSystem>();
        services.AddSingleton<IStyleResolver, StyleResolver>();
        services.AddSingleton<IStyleSheetManager, StyleSheetManager>();

        // Style building components
        services.AddScoped<IElementRuleCollector, ElementRuleCollector>();
        services.AddScoped<IStyleBuilder, StyleBuilder>();
        services.AddScoped<ICascadeResolver, CascadeResolver>();
        services.AddScoped<IInheritanceResolver, InheritanceResolver>();

        // Factories
        services.AddScoped<ICssStyleDeclarationFactory, CssStyleDeclarationFactory>();

        return services;
    }
}