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
    public void Can_Exit_InStyleRecalc_To_StyleDirty()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator());
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);

        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, sm.CurrentPhase);

        // Should be able to exit InStyleRecalc (ExitInStyleRecalc -> StyleDirty)
        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.StyleDirty, sm.CurrentPhase);
    }

    // [Fact]
    // public void Firing_Trigger_With_Parameters_Without_SetTriggerParameters_Throws()
    // {
    //     var stateMachine = new StateMachine<int, string>(0);
    //
    //     // No SetTriggerParameters called
    //     Assert.Throws<ArgumentException>(() => stateMachine.Fire("paramTrigger", 42));
    // }

    // [Fact]
    // public void Firing_Trigger_With_Parameters_After_SetTriggerParameters_Works()
    // {
    //     var stateMachine = new StateMachine<int, string>(0);
    //     stateMachine.Configure(0)
    //         .Permit("go", 1);
    //     stateMachine.SetTriggerParameters<int>("go");
    //
    //     stateMachine.Fire("go", 1);
    //
    //     Assert.Equal(1, stateMachine.State);
    // }

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
}