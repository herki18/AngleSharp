namespace LayoutEngine.Platform.Tests.Unit.Lifecycle;

using System;
using AutoFixture;
using Helpers;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Platform.Lifecycle;
using Xunit;

public class DocumentLifecycleTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestEventAggregator _eventAggregator;
    private readonly ILifecycleStateValidator _stateValidator;
    private readonly DocumentLifecycleCoordinator _lifecycleCoordinator;

    public DocumentLifecycleTests()
    {
        _fixture = new Fixture();
        _eventAggregator = new TestEventAggregator();
        _stateValidator = new LifecycleStateValidator();
        _lifecycleCoordinator = new DocumentLifecycleCoordinator(_eventAggregator, _stateValidator);
    }

    [Fact]
    public void CurrentPhase_InitialValue_ShouldBeInactive()
    {
        // Assert
        Assert.Equal(DocumentLifecyclePhase.Inactive, _lifecycleCoordinator.CurrentPhase);
    }

    [Fact]
    public void EnterPhase_WithValidTransition_ShouldUpdatePhase()
    {
        // Arrange
        var targetPhase = DocumentLifecyclePhase.StyleClean;

        // Act
        _lifecycleCoordinator.EnterPhase(targetPhase);

        // Assert
        Assert.Equal(targetPhase, _lifecycleCoordinator.CurrentPhase);

        var phaseChangedEvents = _eventAggregator.GetPublishedEvents<PhaseChangedEvent>();
        Assert.Single(phaseChangedEvents);
        Assert.Equal(targetPhase, phaseChangedEvents[0].Phase);
        Assert.Equal(PhaseChangeType.Enter, phaseChangedEvents[0].ChangeType);
    }

    [Fact]
    public void EnterPhase_WithInvalidTransition_ShouldThrow()
    {
        // Arrange
        var invalidPhase = DocumentLifecyclePhase.RenderDirty;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _lifecycleCoordinator.EnterPhase(invalidPhase));
    }

    [Fact]
    public void ExitPhase_CurrentPhase_ShouldPublishEventAndTransition()
    {
        // Arrange
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        _eventAggregator.ClearPublishedEvents();

        // Act
        _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.StyleClean);

        // Assert
        var phaseChangedEvents = _eventAggregator.GetPublishedEvents<PhaseChangedEvent>();
        Assert.Equal(2, phaseChangedEvents.Count);

        // First event should be exit of StyleClean
        Assert.Equal(DocumentLifecyclePhase.StyleClean, phaseChangedEvents[0].Phase);
        Assert.Equal(PhaseChangeType.Exit, phaseChangedEvents[0].ChangeType);

        // Next phase should be InStyleRecalc or LayoutClean (depending on the state validator's implementation)
        Assert.True(
            phaseChangedEvents[1].Phase == DocumentLifecyclePhase.InStyleRecalc ||
            phaseChangedEvents[1].Phase == DocumentLifecyclePhase.LayoutClean
        );
        Assert.Equal(PhaseChangeType.Enter, phaseChangedEvents[1].ChangeType);
    }

    [Fact]
    public void ExitPhase_NotCurrentPhase_ShouldThrow()
    {
        // Arrange
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.InStyleRecalc));
    }

    [Fact]
    public void IsValidTransition_WithValidTransition_ShouldReturnTrue()
    {
        // Arrange & Act
        bool isValid = _lifecycleCoordinator.IsValidTransition(
            DocumentLifecyclePhase.Inactive,
            DocumentLifecyclePhase.StyleClean);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidTransition_WithInvalidTransition_ShouldReturnFalse()
    {
        // Arrange & Act
        bool isValid = _lifecycleCoordinator.IsValidTransition(
            DocumentLifecyclePhase.Inactive,
            DocumentLifecyclePhase.RenderDirty);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsOperationAllowed_WithAllowedOperation_ShouldReturnTrue()
    {
        // Arrange
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Act
        bool isAllowed = _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.StyleReading);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void IsOperationAllowed_WithDisallowedOperation_ShouldReturnFalse()
    {
        // Arrange
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);

        // Act
        bool isAllowed = _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.StyleModification);

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void OnStyleComputed_InStyleRecalcPhase_ShouldExitPhase()
    {
        // Arrange
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        _eventAggregator.ClearPublishedEvents();

        var element = new TestDocument().CreateElement("div");
        var computedStyle = new { Width = 100, Height = 200 };
        var styleComputedEvent = new StyleComputedEvent(element, computedStyle);

        // Act
        _eventAggregator.Publish(styleComputedEvent);

        // Assert
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, _lifecycleCoordinator.CurrentPhase);

        var phaseChangedEvents = _eventAggregator.GetPublishedEvents<PhaseChangedEvent>();
        Assert.Equal(2, phaseChangedEvents.Count);
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, phaseChangedEvents[0].Phase);
        Assert.Equal(PhaseChangeType.Exit, phaseChangedEvents[0].ChangeType);
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, phaseChangedEvents[1].Phase);
        Assert.Equal(PhaseChangeType.Enter, phaseChangedEvents[1].ChangeType);
    }

    [Fact]
    public void StyleLifecycleCycle_ShouldTransitionCorrectly()
    {
        // This test verifies the style computation lifecycle transitions

        // Start in Inactive
        Assert.Equal(DocumentLifecyclePhase.Inactive, _lifecycleCoordinator.CurrentPhase);

        // 1. Enter StyleClean
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, _lifecycleCoordinator.CurrentPhase);

        // 2. Publish StyleInvalidatedEvent
        var element = new TestDocument().CreateElement("div");
        _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { element }));

        // Should transition to InStyleRecalc
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, _lifecycleCoordinator.CurrentPhase);

        // 3. Publish StyleComputedEvent
        _eventAggregator.Publish(new StyleComputedEvent(element, new { Width = 100 }));

        // Should transition to StyleDirty
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, _lifecycleCoordinator.CurrentPhase);

        // 4. Exit StyleDirty
        _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.StyleDirty);

        // Should transition back to StyleClean
        Assert.Equal(DocumentLifecyclePhase.StyleClean, _lifecycleCoordinator.CurrentPhase);
    }

    [Fact]
    public void LayoutLifecycleCycle_ShouldTransitionCorrectly()
    {
        // This test verifies the layout computation lifecycle transitions

        // Setup initial state
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, _lifecycleCoordinator.CurrentPhase);

        // 1. Publish LayoutInvalidatedEvent
        var element = new TestDocument().CreateElement("div");
        _eventAggregator.Publish(new LayoutInvalidatedEvent(new[] { element }));

        // Should transition to InLayout
        Assert.Equal(DocumentLifecyclePhase.InLayout, _lifecycleCoordinator.CurrentPhase);

        // 2. Publish FragmentTreeUpdatedEvent
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(new object()));

        // Should transition to LayoutDirty
        Assert.Equal(DocumentLifecyclePhase.LayoutDirty, _lifecycleCoordinator.CurrentPhase);

        // 3. Exit LayoutDirty
        _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.LayoutDirty);

        // Should transition back to LayoutClean
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, _lifecycleCoordinator.CurrentPhase);
    }

    [Fact]
    public void RenderLifecycleCycle_ShouldTransitionCorrectly()
    {
        // This test verifies the render lifecycle transitions

        // Setup initial state
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, _lifecycleCoordinator.CurrentPhase);

        // 1. Publish RenderInvalidatedEvent
        _eventAggregator.Publish(new RenderInvalidatedEvent(null));

        // Should transition to InRender
        Assert.Equal(DocumentLifecyclePhase.InRender, _lifecycleCoordinator.CurrentPhase);

        // 2. Publish RenderCompletedEvent
        _eventAggregator.Publish(new RenderCompletedEvent());

        // Should transition to RenderDirty
        Assert.Equal(DocumentLifecyclePhase.RenderDirty, _lifecycleCoordinator.CurrentPhase);

        // 3. Exit RenderDirty
        _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.RenderDirty);

        // Should transition back to RenderReady
        Assert.Equal(DocumentLifecyclePhase.RenderReady, _lifecycleCoordinator.CurrentPhase);
    }

    public void Dispose()
    {
        _lifecycleCoordinator.Dispose();
    }
}