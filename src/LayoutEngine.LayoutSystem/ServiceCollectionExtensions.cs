namespace LayoutEngine.LayoutSystem;

using Contracts.LayoutSystem;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLayoutSystem(this IServiceCollection services)
    {
        services.AddSingleton<ILayoutEngine, LayoutEngine>();
        return services;
    }
}