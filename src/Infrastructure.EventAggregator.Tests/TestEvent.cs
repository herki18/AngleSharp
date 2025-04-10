// --- Test Event Classes (Place in your test project or a shared location) ---
using Infrastructure.EventAggregator.API.Events;
using System;

// Basic event
public class TestEvent : EventBase
{
    public string Message { get; }
    public TestEvent(string message) { Message = message; }
}

// Prioritized event
public class PrioritizedTestEvent : PrioritizedEventBase
{
    public string Data { get; }
    public PrioritizedTestEvent(string data, EventPriority priority) : base(priority)
    {
        Data = data;
    }
}

// Another event type for distinct testing
public class AnotherTestEvent : EventBase
{
    public int Value { get; }
    public AnotherTestEvent(int value) { Value = value; }
}
// --- End Test Event Classes ---