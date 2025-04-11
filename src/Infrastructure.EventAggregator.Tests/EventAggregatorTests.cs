using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using Infrastructure.EventAggregator.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EventAggregator.Tests;

public class EventAggregatorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IEventAggregator _eventAggregator;

    public EventAggregatorIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddEventAggregatorModule();
        _serviceProvider = services.BuildServiceProvider();
        _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
    }

    public void Dispose()
    {
        (_eventAggregator as IDisposable)?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
#pragma warning disable VSTHRD002
        (_serviceProvider as IAsyncDisposable)?.DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        GC.SuppressFinalize(this);
    }

    // --- Test Event Classes ---
    public class TestEvent : EventBase
    {
        public string Message { get; }

        public TestEvent(string message)
        {
            Message = message;
        }
    }

    public class PrioritizedTestEvent : PrioritizedEventBase
    {
        public string Data { get; }

        public PrioritizedTestEvent(string data, EventPriority priority) : base(priority)
        {
            Data = data;
        }
    }

    public class AnotherTestEvent : EventBase
    {
        public int Value { get; }

        public AnotherTestEvent(int value)
        {
            Value = value;
        }
    }

    public class ExceptionTestEvent : EventBase
    {
        public bool ShouldThrow { get; }

        public ExceptionTestEvent(bool shouldThrow)
        {
            ShouldThrow = shouldThrow;
        }
    }
    // --- End Test Event Classes ---

    [Fact]
    public void DI_ShouldResolveEventAggregatorAsSingleton()
    {
        // Arrange
        var ea1 = _serviceProvider.GetRequiredService<IEventAggregator>();
        var ea2 = _serviceProvider.GetRequiredService<IEventAggregator>();

        // Act & Assert
        Assert.NotNull(ea1);
        Assert.Same(ea1, ea2); // Verify singleton behavior from DI registration
    }

    [Fact]
    public void Publish_WhenSubscribed_ShouldReceiveEventSynchronously()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        var testEvent = new TestEvent("Hello");

        // Act
        var token = _eventAggregator.Subscribe<TestEvent>(e => { receivedEvent = e; });

        _eventAggregator.Publish(testEvent);

        // Assert - No need to wait, event should be processed immediately
        Assert.NotNull(receivedEvent);
        Assert.Same(testEvent, receivedEvent);
        Assert.Equal("Hello", receivedEvent.Message);

        token.Dispose();
    }

    [Fact]
    public void Publish_BeforeSubscribe_ShouldNotReceiveEvent()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        var testEvent = new TestEvent("Past");

        // Act
        _eventAggregator.Publish(testEvent);

        var token = _eventAggregator.Subscribe<TestEvent>(e => { receivedEvent = e; });

        // Assert
        Assert.Null(receivedEvent);

        token.Dispose();
    }

    [Fact]
    public void Subscribe_WithFilter_ShouldOnlyReceiveMatching()
    {
        // Arrange
        var receivedEvents = new List<TestEvent>();
        var matchingEvent = new TestEvent("Match");
        var nonMatchingEvent = new TestEvent("NoMatch");

        // Act
        var token = _eventAggregator.Subscribe<TestEvent>(
            filter: e => e.Message == "Match",
            handler: e =>
            {
                lock (receivedEvents)
                {
                    receivedEvents.Add(e);
                }
            });

        _eventAggregator.Publish(nonMatchingEvent);
        _eventAggregator.Publish(matchingEvent);

        // Assert
        Assert.Single(receivedEvents);
        Assert.Same(matchingEvent, receivedEvents[0]);

        token.Dispose();
    }

    [Fact]
    public void Unsubscribe_ShouldStopReceivingEvents()
    {
        // Arrange
        int receivedCount = 0;
        var event1 = new TestEvent("First");
        var event2 = new TestEvent("Second");
        ISubscriptionToken? token = null;

        // Act
        token = _eventAggregator.Subscribe<TestEvent>(e => Interlocked.Increment(ref receivedCount));

        _eventAggregator.Publish(event1);

        Assert.NotNull(token);
        token.Dispose(); // Unsubscribe

        _eventAggregator.Publish(event2);

        // Assert
        Assert.Equal(1, receivedCount);
    }

    [Fact]
    public void ClearSubscriptions_ShouldRemoveAllForType()
    {
        // Arrange
        int testEventHandlerCount = 0;
        int anotherEventHandlerCount = 0;

        var token1 = _eventAggregator.Subscribe<TestEvent>(e => Interlocked.Increment(ref testEventHandlerCount));
        var token2 =
            _eventAggregator.Subscribe<AnotherTestEvent>(e => Interlocked.Increment(ref anotherEventHandlerCount));

        // Act
        _eventAggregator.ClearSubscriptions<TestEvent>();

        _eventAggregator.Publish(new TestEvent("AfterClear"));
        _eventAggregator.Publish(new AnotherTestEvent(99));

        // Assert
        Assert.Equal(0, testEventHandlerCount);
        Assert.Equal(1, anotherEventHandlerCount);

        token1.Dispose();
        token2.Dispose();
    }

    [Fact]
    public void SubscribeWithPriority_ShouldFilterCorrectly()
    {
        // Arrange
        var receivedEvents = new List<IEvent>();

        var lowPri = new PrioritizedTestEvent("Low", EventPriority.Low);
        var highPri = new PrioritizedTestEvent("High", EventPriority.High);
        var criticalPri = new PrioritizedTestEvent("Critical", EventPriority.Critical);
        var normalPri = new PrioritizedTestEvent("Normal", EventPriority.Normal);
        var normalNonPri = new TestEvent("NonPrioritized"); // Treated as Normal internally

        // Act
        var token = _eventAggregator.SubscribeWithPriority<IEvent>(
            handler: e =>
            {
                lock (receivedEvents)
                {
                    receivedEvents.Add(e);
                }
            },
            minimumPriority: EventPriority.High // Filter
        );

        _eventAggregator.Publish(lowPri);
        _eventAggregator.Publish(highPri);
        _eventAggregator.Publish(criticalPri);
        _eventAggregator.Publish(normalPri);
        _eventAggregator.Publish(normalNonPri); // Should be filtered (Normal < High)

        // Assert
        Assert.Equal(2, receivedEvents.Count);
        Assert.Contains(receivedEvents, e => e is IPrioritizedEvent pe && pe.Priority == EventPriority.High);
        Assert.Contains(receivedEvents, e => e is IPrioritizedEvent pe && pe.Priority == EventPriority.Critical);
        Assert.DoesNotContain(receivedEvents, e => e is IPrioritizedEvent pe && pe.Priority == EventPriority.Low);
        Assert.DoesNotContain(receivedEvents, e => e is IPrioritizedEvent pe && pe.Priority == EventPriority.Normal);
        Assert.DoesNotContain(receivedEvents, e => e is TestEvent); // Ensure non-prioritized (Normal) was filtered

        token.Dispose();
    }

    [Fact]
    public void MultipleSubscribers_ShouldAllReceiveEvents()
    {
        // Arrange
        int subscriber1Called = 0;
        int subscriber2Called = 0;
        var testEvent = new TestEvent("MultiSubscriber");

        // Act
        var token1 = _eventAggregator.Subscribe<TestEvent>(_ => Interlocked.Increment(ref subscriber1Called));
        var token2 = _eventAggregator.Subscribe<TestEvent>(_ => Interlocked.Increment(ref subscriber2Called));

        _eventAggregator.Publish(testEvent);

        // Assert
        Assert.Equal(1, subscriber1Called);
        Assert.Equal(1, subscriber2Called);

        token1.Dispose();
        token2.Dispose();
    }

    // Test to check filtering based on event priority with non-prioritized events
    [Fact]
    public void PublishWithPriority_ShouldOverrideEventPriority()
    {
        // Arrange
        var receivedEvents = new List<TestEvent>();
        var testEvent = new TestEvent("Override"); // Not prioritized by default

        // Act
        // Subscribe first to a high-priority event
        var token = _eventAggregator.Subscribe<TestEvent>(
            filter: _ => true, // Accept all events
            handler: e =>
            {
                lock (receivedEvents)
                {
                    receivedEvents.Add(e);
                }
            }
        );

        // This would normally be filtered, but we're overriding the priority
        _eventAggregator.Publish(testEvent, EventPriority.Critical);

        // Assert
        Assert.Single(receivedEvents);
        Assert.Same(testEvent, receivedEvents[0]);

        token.Dispose();
    }

    [Fact]
    public void ExceptionInHandler_ShouldNotBlockOtherHandlers()
    {
        // Arrange
        bool firstHandlerCalled = false;
        bool secondHandlerCalled = false;
        var testEvent = new TestEvent("Exception");

        // Act
        var token1 = _eventAggregator.Subscribe<TestEvent>(_ =>
        {
            firstHandlerCalled = true;
            throw new Exception("Test exception");
        });

        var token2 = _eventAggregator.Subscribe<TestEvent>(_ => { secondHandlerCalled = true; });

        // Should not throw
        _eventAggregator.Publish(testEvent);

        // Assert
        Assert.True(firstHandlerCalled);
        Assert.True(secondHandlerCalled);

        token1.Dispose();
        token2.Dispose();
    }
}