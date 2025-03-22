namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using System;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using LayoutEngine.Contracts.Platform.Resource.Abstractions;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform;
using LayoutEngine.Platform.Abstractions;
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;
using Microsoft.Extensions.DependencyInjection;
using Tests.Helpers;

/// <summary>
/// Helper class to set up a testable environment for LayoutEngine platform components
/// </summary>
public class PlatformTestEnvironment : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceCollection _services;
    private bool _isDisposed;

    /// <summary>
    /// Creates a new test environment with default services
    /// </summary>
    public PlatformTestEnvironment()
    {
        _services = new ServiceCollection();

        // Register test mock event aggregator
        _services.AddSingleton<IEventAggregator, TestEventAggregator>();

        // Register time provider for deterministic time
        _services.AddSingleton<ITimeProvider, TestTimeProvider>();

        // Register window provider for DOM testing
        _services.AddSingleton<IWindowProvider, TestWindowProvider>();

        // Register mutation observer factory for DOM testing
        _services.AddSingleton<IMutationObserverFactory, TestMutationObserverFactory>();

        // Register resize observer factory for DOM testing
        _services.AddSingleton<IResizeObserverFactory, TestResizeObserverFactory>();

        // Register resource loading strategy for resource testing
        _services.AddSingleton<IResourceLoadingStrategy, TestResourceLoadingStrategy>();

        // Add standard platform services
        _services.AddPlatformServices();

        // Build service provider
        _serviceProvider = _services.BuildServiceProvider();

        // Get threading coordinator and enable synchronous mode
        var threadingCoordinator = (ThreadingCoordinator)GetService<IThreadingCoordinator>();
        threadingCoordinator.EnableSynchronousMode(true);

        // Get frame scheduler and enable synchronous mode
        var frameScheduler = (FrameScheduler)GetService<IFrameScheduler>();
        frameScheduler.EnableSynchronousMode(true);
    }

    /// <summary>
    /// Gets a service from the service provider
    /// </summary>
    public T GetService<T>() where T : class
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PlatformTestEnvironment));

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets the threading coordinator
    /// </summary>
    public IThreadingCoordinator ThreadingCoordinator => GetService<IThreadingCoordinator>();

    /// <summary>
    /// Gets the frame scheduler
    /// </summary>
    public IFrameScheduler FrameScheduler => GetService<IFrameScheduler>();

    /// <summary>
    /// Gets the DOM mutation tracker
    /// </summary>
    public IDomMutationTracker DomMutationTracker => GetService<IDomMutationTracker>();

    /// <summary>
    /// Gets the viewport detector
    /// </summary>
    public IViewportDetector ViewportDetector => GetService<IViewportDetector>();

    /// <summary>
    /// Gets the resource loader
    /// </summary>
    public IResourceLoader ResourceLoader => GetService<IResourceLoader>();

    /// <summary>
    /// Gets the time provider
    /// </summary>
    public TestTimeProvider TimeProvider => (TestTimeProvider)GetService<ITimeProvider>();

    /// <summary>
    /// Gets the event aggregator
    /// </summary>
    public TestEventAggregator EventAggregator => (TestEventAggregator)GetService<IEventAggregator>();

    /// <summary>
    /// Executes all pending work synchronously
    /// </summary>
    public void ExecuteAllPendingWork()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PlatformTestEnvironment));

        ((ThreadingCoordinator)ThreadingCoordinator).ExecuteQueuedActionsSync();
    }

    /// <summary>
    /// Advances time and processes any frame callbacks
    /// </summary>
    public void AdvanceTimeAndRunFrame(double milliseconds)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PlatformTestEnvironment));

        // Advance time
        TimeProvider.AdvanceTime(milliseconds);

        // Run a frame
        ((FrameScheduler)FrameScheduler).RunFrameSynchronously();

        // Execute any pending work
        ExecuteAllPendingWork();
    }

    /// <summary>
    /// Disposes all services
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}