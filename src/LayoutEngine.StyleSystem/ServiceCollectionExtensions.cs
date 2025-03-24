namespace LayoutEngine.StyleSystem;

using Contracts.StyleSystem;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStyleSystem(this IServiceCollection services)
    {
        services.AddSingleton<IStyleEngine, StyleEngine>();
        return services;
    }
}