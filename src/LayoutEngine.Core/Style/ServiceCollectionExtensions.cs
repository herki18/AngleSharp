namespace LayoutEngine.Core.Style;

using Internal;
using Microsoft.Extensions.DependencyInjection;
using Public;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLayoutModule(this IServiceCollection services)
    {
        // Core systems
        services.AddSingleton<IStyleSystem, StyleSystem>();

        return services;
    }
}