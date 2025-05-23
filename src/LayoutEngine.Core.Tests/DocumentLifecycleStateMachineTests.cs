using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Core.Events;
using NSubstitute;
using Stateless;

namespace LayoutEngine.Core.Tests;

public class DocumentLifecycleStateMachineTests
{
    private IEventAggregator CreateStubAggregator() => Substitute.For<IEventAggregator>();

    [Fact]
    public void Can_Transition_From_Inactive_To_StyleClean()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        Assert.Equal(DocumentLifecyclePhase.Inactive, sm.CurrentPhase);

        bool transitioned = sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);

        Assert.True(transitioned);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, sm.CurrentPhase);
    }

    [Fact]
    public void Can_Exit_InStyleRecalc_To_StyleClean()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // Navigate to InStyleRecalc state
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);

        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, sm.CurrentPhase);

        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, sm.CurrentPhase);
    }

    [Fact]
    public void Can_Exit_InLayout_To_LayoutClean()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // Navigate to InLayout state
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InLayout);

        Assert.Equal(DocumentLifecyclePhase.InLayout, sm.CurrentPhase);

        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, sm.CurrentPhase);
    }

    [Fact]
    public void Can_Exit_InRender_To_RenderReady()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // Navigate to InRender state
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady);
        sm.TryTransitionTo(DocumentLifecyclePhase.InRender);

        Assert.Equal(DocumentLifecyclePhase.InRender, sm.CurrentPhase);

        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, sm.CurrentPhase);
    }

    [Fact]
    public void Firing_Trigger_Without_Parameters_Does_Not_Require_Configuration()
    {
        var stateMachine = new StateMachine<int, string>(0);
        stateMachine.Configure(0)
            .Permit("go", 1);

        stateMachine.Fire("go");

        Assert.Equal(1, stateMachine.State);
    }

    [Fact]
    public void Publishes_PhaseChangedEvent_On_Transition()
    {
        var aggregator = CreateStubAggregator();
        var sm = new DocumentLifecycleStateMachine(aggregator);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);

        aggregator.Received().Publish(
            Arg.Is<PhaseChangedEvent>(e => e.Phase == DocumentLifecyclePhase.StyleClean),
            Arg.Any<EventPriority>());
    }

    [Fact]
    public void Cannot_Transition_To_Invalid_States()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // Cannot go directly from Inactive to InStyleRecalc
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));

        // Cannot go directly from Inactive to LayoutClean
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean));

        // Cannot go directly from Inactive to RenderReady
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady));
    }

    [Fact]
    public void Valid_Lifecycle_Flow_Works()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // Normal flow: Inactive -> StyleClean -> InStyleRecalc -> StyleClean -> LayoutClean -> InLayout -> LayoutClean -> RenderReady -> InRender -> RenderReady
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));
        Assert.True(sm.SignalPhaseExit()); // Back to StyleClean
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InLayout));
        Assert.True(sm.SignalPhaseExit()); // Back to LayoutClean
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InRender));
        Assert.True(sm.SignalPhaseExit()); // Back to RenderReady

        Assert.Equal(DocumentLifecyclePhase.RenderReady, sm.CurrentPhase);
    }

    [Fact]
    public void IsOperationAllowed_Works_For_Different_Phases()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());

        // In Inactive phase
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomModification));
        Assert.False(sm.IsOperationAllowed(DocumentOperation.StyleReading));

        // Move to StyleClean
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        Assert.True(sm.IsOperationAllowed(DocumentOperation.StyleReading));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.StyleModification));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));

        // Move to InStyleRecalc
        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.False(sm.IsOperationAllowed(DocumentOperation.StyleModification));
    }
}