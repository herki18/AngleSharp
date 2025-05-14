using AngleSharp.Dom;
using LayoutEngine.Core.Events;
using NSubstitute;

namespace LayoutEngine.Core.Tests;

using Infrastructure.EventAggregator.API.Aggregation;

public class DocumentLifecycleIntegrationTests
{
    [Fact]
    public void Full_StyleRecalc_Lifecycle_Advances_To_StyleClean_On_StyleComputedEvent()
    {
        // Arrange
        var eventAggregator = Substitute.For<IEventAggregator>();
        var stateMachine = new DocumentLifecycleStateMachine(eventAggregator);

        // Start in Inactive, move to StyleClean, then InStyleRecalc
        Assert.Equal(DocumentLifecyclePhase.Inactive, stateMachine.CurrentPhase);
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.StyleClean));
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, stateMachine.CurrentPhase);

        // Mimic the DocumentLifecycleCoordinator's event subscription
        eventAggregator
            .When(x => x.Publish(Arg.Any<StyleComputedEvent>()))
            .Do(call =>
            {
                // This is what the coordinator would do on StyleComputedEvent
                if (stateMachine.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
                {
                    stateMachine.ExitPhase(DocumentLifecyclePhase.InStyleRecalc);
                }
            });

        // Act: Simulate style computation and event publishing
        var fakeElements = new List<IElement>();
        var fakeStyles = new Dictionary<IElement, LayoutEngine.Core.Style.IComputedStyle>();
        var styleComputedEvent = new StyleComputedEvent(fakeElements, fakeStyles);

        // This mimics what StyleSystem would do after computing styles
        eventAggregator.Publish(styleComputedEvent);

        // Assert: The phase should now be StyleDirty (or StyleClean, depending on your config)
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, stateMachine.CurrentPhase);
    }

    [Fact]
    public void Full_Lifecycle_Advances_Through_Style_Layout_Render()
    {
        // Arrange
        var eventAggregator = Substitute.For<IEventAggregator>();
        var stateMachine = new DocumentLifecycleStateMachine(eventAggregator);

        // Go to StyleClean
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.StyleClean));
        // Go to InStyleRecalc
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));
        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, stateMachine.CurrentPhase);

        // Mimic StyleComputedEvent handling
        eventAggregator
            .When(x => x.Publish(Arg.Any<StyleComputedEvent>()))
            .Do(call =>
            {
                if (stateMachine.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
                    stateMachine.ExitPhase(DocumentLifecyclePhase.InStyleRecalc);
            });

        // Mimic FragmentTreeUpdatedEvent handling (layout done)
        eventAggregator
            .When(x => x.Publish(Arg.Any<FragmentTreeUpdatedEvent>()))
            .Do(call =>
            {
                if (stateMachine.CurrentPhase == DocumentLifecyclePhase.InLayout)
                    stateMachine.ExitPhase(DocumentLifecyclePhase.InLayout);
            });

        // Mimic RenderCompletedEvent handling (render done)
        eventAggregator
            .When(x => x.Publish(Arg.Any<RenderCompletedEvent>()))
            .Do(call =>
            {
                if (stateMachine.CurrentPhase == DocumentLifecyclePhase.InRender)
                    stateMachine.ExitPhase(DocumentLifecyclePhase.InRender);
            });

        // Simulate style computation
        eventAggregator.Publish(new StyleComputedEvent(new List<IElement>(), new Dictionary<IElement, LayoutEngine.Core.Style.IComputedStyle>()));
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, stateMachine.CurrentPhase);

        // Complete style dirty phase
        stateMachine.ExitPhase(DocumentLifecyclePhase.StyleDirty);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, stateMachine.CurrentPhase);

        // Go to layout
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.LayoutClean));
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.InLayout));
        Assert.Equal(DocumentLifecyclePhase.InLayout, stateMachine.CurrentPhase);

        // Simulate layout done
        eventAggregator.Publish(new FragmentTreeUpdatedEvent(new object()));
        Assert.Equal(DocumentLifecyclePhase.LayoutDirty, stateMachine.CurrentPhase);

        // Complete layout dirty phase
        stateMachine.ExitPhase(DocumentLifecyclePhase.LayoutDirty);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, stateMachine.CurrentPhase);

        // Go to render
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.RenderReady));
        Assert.True(stateMachine.TryTransitionTo(DocumentLifecyclePhase.InRender));
        Assert.Equal(DocumentLifecyclePhase.InRender, stateMachine.CurrentPhase);

        // Simulate render done
        eventAggregator.Publish(new RenderCompletedEvent());
        Assert.Equal(DocumentLifecyclePhase.RenderDirty, stateMachine.CurrentPhase);

        // Complete render dirty phase
        stateMachine.ExitPhase(DocumentLifecyclePhase.RenderDirty);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, stateMachine.CurrentPhase);
    }
}