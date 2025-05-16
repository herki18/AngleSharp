using AngleSharp.Dom;
using LayoutEngine.Core.Events;
using Microsoft.Extensions.DependencyInjection;

namespace LayoutEngine.Core.Tests;

using Xunit.Abstractions;

public class DocumentLifecycleIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DocumentLifecycleIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Full_Lifecycle_Advances_Through_Style_Layout_Render()
    {
        // Arrange: set up DI container
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine(); // Your extension method that registers all real services

        // If you want to intercept or replace Unity parts, do it here:
        // services.AddSingleton<IUnityRenderer, TestUnityRenderer>();

        var provider = services.BuildServiceProvider();

        // Resolve the real coordinator and event aggregator
        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();
        var eventAggregator = provider.GetRequiredService<Infrastructure.EventAggregator.API.Aggregation.IEventAggregator>();

        // Act: drive the lifecycle with real events
        Assert.Equal(DocumentLifecyclePhase.Inactive, coordinator.CurrentPhase);

        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, coordinator.CurrentPhase);

        eventAggregator.Publish(new StyleInvalidatedEvent(new List<IElement>()));
        // Add these diagnostic lines right before the failing assertion
        _testOutputHelper.WriteLine($"Current phase after StyleInvalidatedEvent: {coordinator.CurrentPhase}");
        _testOutputHelper.WriteLine($"Is transition valid: {coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc)}");
        _testOutputHelper.WriteLine($"Is style operation allowed: {coordinator.IsOperationAllowed(DocumentOperation.StyleModification)}");

        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, coordinator.CurrentPhase);

        eventAggregator.Publish(new StyleComputedEvent(new List<IElement>(), new Dictionary<IElement, LayoutEngine.Core.Style.IComputedStyle>()));
        Assert.Equal(DocumentLifecyclePhase.StyleClean, coordinator.CurrentPhase);

        coordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, coordinator.CurrentPhase);

        eventAggregator.Publish(new LayoutInvalidatedEvent(new List<IElement>()));
        Assert.Equal(DocumentLifecyclePhase.InLayout, coordinator.CurrentPhase);

        eventAggregator.Publish(new FragmentTreeUpdatedEvent(new object()));
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, coordinator.CurrentPhase);

        coordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, coordinator.CurrentPhase);

        eventAggregator.Publish(new RenderInvalidatedEvent(new List<IElement>()));
        Assert.Equal(DocumentLifecyclePhase.InRender, coordinator.CurrentPhase);

        eventAggregator.Publish(new RenderCompletedEvent());
        Assert.Equal(DocumentLifecyclePhase.RenderReady, coordinator.CurrentPhase);
    }

    [Fact]
    public void StyleInvalidatedEvent_Triggers_EnterPhase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();
        var eventAggregator = provider.GetRequiredService<Infrastructure.EventAggregator.API.Aggregation.IEventAggregator>();

        // Manually transition to StyleClean first
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Create a test event with high priority explicitly
        var testEvent = new StyleInvalidatedEvent(new List<IElement>());
        eventAggregator.Publish(testEvent, Infrastructure.EventAggregator.API.Events.EventPriority.High);

        // Add these diagnostic lines right before the failing assertion
        _testOutputHelper.WriteLine($"Current phase after StyleInvalidatedEvent: {coordinator.CurrentPhase}");
        _testOutputHelper.WriteLine($"Is transition valid: {coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc)}");
        _testOutputHelper.WriteLine($"Is style operation allowed: {coordinator.IsOperationAllowed(DocumentOperation.StyleModification)}");


        // Verify the transition occurred
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, coordinator.CurrentPhase);
    }

    [Fact]
    public void DirectCallToOnStyleInvalidated_Triggers_EnterPhase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();

        // Manually transition to StyleClean first
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Directly call the event handler method using reflection
        var coordinatorType = coordinator.GetType();
        var methodInfo = coordinatorType.GetMethod("OnStyleInvalidated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var eventArg = new StyleInvalidatedEvent(new List<IElement>());
        methodInfo!.Invoke(coordinator, new object[] { eventArg });

        // Verify the transition occurred
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, coordinator.CurrentPhase);
    }
}