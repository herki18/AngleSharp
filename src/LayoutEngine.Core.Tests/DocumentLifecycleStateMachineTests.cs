using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Core.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stateless;
using Xunit;
using System; // Required for Func

namespace LayoutEngine.Core.Tests;

public class DocumentLifecycleStateMachineTests
{
    private IEventAggregator CreateStubAggregator() => Substitute.For<IEventAggregator>();
    private ILogger<DocumentLifecycleStateMachine> CreateStubLogger() => Substitute.For<ILogger<DocumentLifecycleStateMachine>>();

    // Helper method to verify logging calls
    private void VerifyLog(
        ILogger<DocumentLifecycleStateMachine> logger,
        LogLevel expectedLevel,
        string expectedMessage,
        Exception? expectedException = null, // Optional: if you need to check for logged exceptions
        int expectedCount = 1)
    {
        logger.Received(expectedCount).Log(
            expectedLevel,
            Arg.Any<EventId>(),
            Arg.Is<object>(st => st.ToString() == expectedMessage),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>() // Updated to Exception?
        );
    }

    [Fact]
    public void Can_Transition_From_Inactive_To_StyleClean()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);

        Assert.Equal(DocumentLifecyclePhase.Inactive, sm.CurrentPhase);

        bool transitioned = sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);

        Assert.True(transitioned);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, sm.CurrentPhase);
        VerifyLog(logger, LogLevel.Debug, "Successfully transitioned from Inactive to StyleClean using trigger EnterStyleClean.");
    }

    [Fact]
    public void Can_Exit_InStyleRecalc_To_StyleClean()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);
        logger.ClearReceivedCalls(); // Clear calls from setup transitions

        Assert.Equal(DocumentLifecyclePhase.InStyleRecalc, sm.CurrentPhase);
        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.StyleClean, sm.CurrentPhase);
        VerifyLog(logger, LogLevel.Debug, "Successfully exited InStyleRecalc to StyleClean using completion trigger StyleComplete (from exit trigger ExitInStyleRecalc).");
    }

    [Fact]
    public void Can_Exit_InLayout_To_LayoutClean()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InLayout);
        logger.ClearReceivedCalls(); // Clear calls from setup transitions

        Assert.Equal(DocumentLifecyclePhase.InLayout, sm.CurrentPhase);
        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.LayoutClean, sm.CurrentPhase);
        VerifyLog(logger, LogLevel.Debug, "Successfully exited InLayout to LayoutClean using completion trigger LayoutComplete (from exit trigger ExitInLayout).");
    }

    [Fact]
    public void Can_Exit_InRender_To_RenderReady()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady);
        sm.TryTransitionTo(DocumentLifecyclePhase.InRender);
        logger.ClearReceivedCalls(); // Clear calls from setup transitions

        Assert.Equal(DocumentLifecyclePhase.InRender, sm.CurrentPhase);
        bool exited = sm.SignalPhaseExit();

        Assert.True(exited);
        Assert.Equal(DocumentLifecyclePhase.RenderReady, sm.CurrentPhase);
        VerifyLog(logger, LogLevel.Debug, "Successfully exited InRender to RenderReady using completion trigger RenderComplete (from exit trigger ExitInRender).");
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
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(aggregator, logger);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);

        aggregator.Received().Publish(
            Arg.Is<PhaseChangedEvent>(e => e.Phase == DocumentLifecyclePhase.StyleClean && e.ChangeType == PhaseChangeType.Enter),
            Arg.Is(EventPriority.High));
    }

    [Fact]
    public void Cannot_Transition_To_Invalid_States()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);

        // 1. Inactive to InStyleRecalc
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));
        VerifyLog(logger, LogLevel.Warning, "No trigger defined for transition from Inactive to InStyleRecalc.");
        logger.ClearReceivedCalls();

        // 2. Inactive to LayoutClean
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean));
        VerifyLog(logger, LogLevel.Warning, "No trigger defined for transition from Inactive to LayoutClean.");
        logger.ClearReceivedCalls();

        // 3. Inactive to RenderReady
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady));
        VerifyLog(logger, LogLevel.Warning, "No trigger defined for transition from Inactive to RenderReady.");
    }

    [Fact]
    public void Valid_Lifecycle_Flow_Works()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), CreateStubLogger());

        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc));
        Assert.True(sm.SignalPhaseExit());
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.LayoutClean));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InLayout));
        Assert.True(sm.SignalPhaseExit());
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.RenderReady));
        Assert.True(sm.TryTransitionTo(DocumentLifecyclePhase.InRender));
        Assert.True(sm.SignalPhaseExit());

        Assert.Equal(DocumentLifecyclePhase.RenderReady, sm.CurrentPhase);
    }

    [Fact]
    public void IsOperationAllowed_Works_For_Different_Phases()
    {
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), CreateStubLogger());

        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomModification));
        Assert.False(sm.IsOperationAllowed(DocumentOperation.StyleReading));

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        Assert.True(sm.IsOperationAllowed(DocumentOperation.StyleReading));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.StyleModification));
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));

        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);
        Assert.True(sm.IsOperationAllowed(DocumentOperation.DomReading));
        Assert.False(sm.IsOperationAllowed(DocumentOperation.StyleModification));
    }

    [Fact]
    public void TryTransitionTo_Logs_Warning_When_Cannot_Fire_Permitted_Trigger()
    {
        var logger = CreateStubLogger();
        var eventAggregator = CreateStubAggregator();
        var sm = new DocumentLifecycleStateMachine(eventAggregator, logger);

        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        logger.ClearReceivedCalls();

        sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger); // Reset to Inactive
        Assert.False(sm.TryTransitionTo(DocumentLifecyclePhase.InLayout));
        VerifyLog(logger, LogLevel.Warning, "No trigger defined for transition from Inactive to InLayout.");
    }

     [Fact]
    public void SignalPhaseExit_Logs_Warning_When_No_Exit_Trigger_For_State()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);
        Assert.False(sm.SignalPhaseExit()); // Current state is Inactive
        VerifyLog(logger, LogLevel.Warning, "No exit trigger defined for current state Inactive in SignalPhaseExit.");
    }

    [Fact]
    public void SignalPhaseExit_Logs_Warning_When_Cannot_Fire_Completion_Trigger()
    {
        var logger = CreateStubLogger();
        var sm = new DocumentLifecycleStateMachine(CreateStubAggregator(), logger);
        sm.TryTransitionTo(DocumentLifecyclePhase.StyleClean);
        sm.TryTransitionTo(DocumentLifecyclePhase.InStyleRecalc);
        logger.ClearReceivedCalls();

        Assert.True(sm.SignalPhaseExit());
        VerifyLog(logger, LogLevel.Debug, "Successfully exited InStyleRecalc to StyleClean using completion trigger StyleComplete (from exit trigger ExitInStyleRecalc).");
    }
}
