using AngleSharp.Dom;
using LayoutEngine.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LayoutEngine.Core.Tests;

using Core;
using Style.Public;

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
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();
        var eventAggregator = provider.GetRequiredService<Infrastructure.EventAggregator.API.Aggregation.IEventAggregator>();

        Assert.Equal(DocumentLifecyclePhase.Inactive, coordinator.CurrentPhase);

        // Start lifecycle
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, coordinator.CurrentPhase);

        // Test style phase transition
        eventAggregator.Publish(new StyleInvalidatedEvent(new List<IElement>()));
        _testOutputHelper.WriteLine($"Current phase after StyleInvalidatedEvent: {coordinator.CurrentPhase}");
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, coordinator.CurrentPhase);

        // Complete style calculation
        eventAggregator.Publish(new StyleComputedEvent(new List<IElement>(), new Dictionary<IElement, IComputedStyle>()));
        Assert.Equal(DocumentLifecyclePhase.StyleClean, coordinator.CurrentPhase);

        // Move to layout phase
        coordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, coordinator.CurrentPhase);

        // Test layout phase transition
        eventAggregator.Publish(new LayoutInvalidatedEvent(new List<IElement>()));
        Assert.Equal(DocumentLifecyclePhase.InLayout, coordinator.CurrentPhase);

        // Complete layout calculation
        eventAggregator.Publish(new FragmentTreeUpdatedEvent(new object()));
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, coordinator.CurrentPhase);

        // Move to render phase
        coordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, coordinator.CurrentPhase);

        // Test render phase transition
        eventAggregator.Publish(new RenderInvalidatedEvent(new List<IElement>()));
        Assert.Equal(DocumentLifecyclePhase.InRender, coordinator.CurrentPhase);

        // Complete rendering
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

        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        var testEvent = new StyleInvalidatedEvent(new List<IElement>());
        eventAggregator.Publish(testEvent, Infrastructure.EventAggregator.API.Events.EventPriority.High);

        _testOutputHelper.WriteLine($"Current phase after StyleInvalidatedEvent: {coordinator.CurrentPhase}");
        _testOutputHelper.WriteLine($"Is transition valid: {coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc)}");
        _testOutputHelper.WriteLine($"Is style operation allowed: {coordinator.IsOperationAllowed(DocumentOperation.StyleModification)}");

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
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        var coordinatorType = coordinator.GetType();
        var methodInfo = coordinatorType.GetMethod("OnStyleInvalidated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var eventArg = new StyleInvalidatedEvent(new List<IElement>());
        methodInfo!.Invoke(coordinator, new object[] { eventArg });

        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, coordinator.CurrentPhase);
    }

    [Fact]
    public void LayoutInvalidatedEvent_Triggers_EnterPhase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();
        var eventAggregator = provider.GetRequiredService<Infrastructure.EventAggregator.API.Aggregation.IEventAggregator>();

        // Set up for layout phase
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        coordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);

        var testEvent = new LayoutInvalidatedEvent(new List<IElement>());
        eventAggregator.Publish(testEvent);

        Assert.Equal(DocumentLifecyclePhase.InLayout, coordinator.CurrentPhase);
    }

    [Fact]
    public void RenderInvalidatedEvent_Triggers_EnterPhase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();
        var eventAggregator = provider.GetRequiredService<Infrastructure.EventAggregator.API.Aggregation.IEventAggregator>();

        // Set up for render phase
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        coordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
        coordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);

        var testEvent = new RenderInvalidatedEvent(new List<IElement>());
        eventAggregator.Publish(testEvent);

        Assert.Equal(DocumentLifecyclePhase.InRender, coordinator.CurrentPhase);
    }

    [Fact]
    public void Lifecycle_Coordinator_Handles_Phase_Transitions_Correctly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();

        // Test valid transitions
        Assert.True(coordinator.IsValidTransition(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.StyleClean));

        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        Assert.True(coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc));
        Assert.True(coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.LayoutClean));

        // Test invalid transitions
        Assert.False(coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InRender));
        Assert.False(coordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.RenderReady));
    }

    [Fact]
    public void Operation_Permissions_Work_Correctly_For_Each_Phase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetRequiredService<IDocumentLifecycleCoordinator>();

        // Test Inactive phase permissions
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.DomModification));
        Assert.False(coordinator.IsOperationAllowed(DocumentOperation.StyleReading));

        // Test StyleClean phase permissions
        coordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.StyleReading));
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.StyleModification));
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.DomModification));

        // Test InStyleRecalc phase permissions
        coordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        Assert.True(coordinator.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.False(coordinator.IsOperationAllowed(DocumentOperation.StyleModification));
        Assert.False(coordinator.IsOperationAllowed(DocumentOperation.DomModification));
    }
}