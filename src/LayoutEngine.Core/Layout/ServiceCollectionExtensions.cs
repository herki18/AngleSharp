namespace LayoutEngine.Core.Layout;

using Internal;
using Microsoft.Extensions.DependencyInjection;
using Public;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLayoutSystem(this IServiceCollection services)
    {
        // Core style system
        services.AddSingleton<ILayoutSystem, LayoutSystem>();

        return services;
    }
}