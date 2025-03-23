using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Platform;
using LayoutEngine.Platform.DOM;
using LayoutEngine.Platform.DOM.Abstractions;
using LayoutEngine.Platform.Lifecycle;
using LayoutEngine.Platform.Resource;
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;
using LayoutEngine.Platform.Abstractions;
using LayoutEngine.Platform.Resource.Abstractions;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Resource.Abstractions;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;

namespace LayoutEngine.Platform.Tests.Integration;

using Contracts.Platform.Resource;
using Infrastructure.EventAggregator.API.Aggregation;
using Unit.Helpers;
using IResourceErrorHandler = Contracts.Resource.IResourceErrorHandler;
using ThreadPool = Threading.ThreadPool;

public class PlatformServiceExtensionsTests
{
    [Fact]
    public void AddPlatformServices_WithNoOptions_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformServices();
        var provider = services.BuildServiceProvider();

        // Assert - Check core services
        AssertServiceIsRegistered<IMutationObserverFactory>(provider, typeof(MutationObserverFactory));
        AssertServiceIsRegistered<IResizeObserverFactory>(provider, typeof(ResizeObserverFactory));
        AssertServiceIsRegistered<IWindowProvider>(provider, typeof(DefaultWindowProvider));
        AssertServiceIsRegistered<ITimeProvider>(provider, typeof(SystemTimeProvider));
        AssertServiceIsRegistered<IDocumentLifecycleCoordinator>(provider, typeof(DocumentLifecycleCoordinator));

        // Assert - Check DOM services
        AssertServiceIsRegistered<IElementAdapter>(provider, typeof(ElementAdapter));
        AssertServiceIsRegistered<IDomMutationTracker>(provider, typeof(DomMutationTracker));
        AssertServiceIsRegistered<IViewportDetector>(provider, typeof(ViewportDetector));

        // Assert - Check Threading services
        AssertServiceIsRegistered<IThreadPool>(provider, typeof(ThreadPool));
        AssertServiceIsRegistered<IThreadingCoordinator>(provider, typeof(ThreadingCoordinator));

        // Assert - Check Update services
        AssertServiceIsRegistered<IUpdateScheduler>(provider, typeof(UpdateScheduler));
        AssertServiceIsRegistered<IFrameScheduler>(provider, typeof(FrameScheduler));
        AssertServiceIsRegistered<IIdleTaskScheduler>(provider, typeof(IdleTaskScheduler));

        // Assert - Check Resource services
        AssertServiceIsRegistered<IResourceLoadingStrategy>(provider, typeof(DefaultResourceLoadingStrategy));
        AssertServiceIsRegistered<IResourceTypeResolver>(provider, typeof(DefaultResourceTypeResolver));
        AssertServiceIsRegistered<IResourceLoader>(provider, typeof(ResourceLoader));
        AssertServiceIsRegistered<IResourceErrorHandler>(provider, typeof(ResourceErrorHandler));
    }

    [Fact]
    public void AddPlatformServices_WithOptions_ShouldRegisterServicesWithConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformServices(options => {
            options.DefaultResourceLoadTimeoutMs = 5000;
            options.MaxConcurrentResourceLoads = 10;
            options.TargetFramesPerSecond = 30;
            options.ThreadPool.MaxThreads = 4;
        });
        var provider = services.BuildServiceProvider();

        // Assert - Check services are registered
        AssertServiceIsRegistered<IMutationObserverFactory>(provider, typeof(MutationObserverFactory));
        AssertServiceIsRegistered<IThreadPool>(provider, typeof(ThreadPool));

        // We can't directly assert that the options were applied without exposing internal state,
        // but we can verify that the service initialization succeeded
    }

    [Fact]
    public void AddPlatformServices_WithNullOptions_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddPlatformServices(null!));
    }

    [Fact]
    public void AddPlatformServices_WithNullServices_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddPlatformServices());
    }

    [Fact]
    public void AddTestPlatformServices_ShouldRegisterTestServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTestPlatformServices();
        var provider = services.BuildServiceProvider();

        // Assert - Check key test services
        AssertServiceIsRegistered<ITimeProvider>(provider, typeof(TestTimeProvider));
        AssertServiceIsRegistered<IDocumentLifecycleCoordinator>(provider, typeof(DocumentLifecycleCoordinator));
        AssertServiceIsRegistered<IThreadPool>(provider, typeof(ThreadPool));
    }

    [Fact]
    public void AddTestPlatformServices_WithNullServices_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddTestPlatformServices());
    }

    [Fact]
    public void ServiceProvider_ShouldResolveComplexDependencyChain()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEventAggregator, TestEventAggregator>();

        // Act
        services.AddPlatformServices();
        var provider = services.BuildServiceProvider();

        // Resolve a service with complex dependencies
        var updateScheduler = provider.GetRequiredService<IUpdateScheduler>();
        var idleTaskScheduler = provider.GetRequiredService<IIdleTaskScheduler>();
        var resourceReferenceManager = provider.GetRequiredService<IResourceReferenceManager>();

        // Assert
        Assert.NotNull(updateScheduler);
        Assert.NotNull(idleTaskScheduler);
        Assert.NotNull(resourceReferenceManager);
    }

    private void AssertServiceIsRegistered<T>(IServiceProvider provider, Type expectedImplementationType)
    {
        var service = provider.GetService<T>();
        Assert.NotNull(service);
        Assert.IsType(expectedImplementationType, service);
    }
}