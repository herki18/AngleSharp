using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using NSubstitute;

namespace LayoutEngine.Platform.Tests.Helpers;

/// <summary>
/// Test implementation of SynchronizationContext for controlling execution in tests
/// </summary>
public class TestSynchronizationContext : SynchronizationContext
{
    private readonly List<(SendOrPostCallback Callback, object? State)> _postedCallbacks = new();

    public IReadOnlyList<(SendOrPostCallback Callback, object? State)> PostedCallbacks => _postedCallbacks;

    public override void Post(SendOrPostCallback d, object? state)
    {
        _postedCallbacks.Add((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }

    public void ExecutePostedCallbacks()
    {
        var callbacks = new List<(SendOrPostCallback Callback, object? State)>(_postedCallbacks);
        _postedCallbacks.Clear();

        foreach (var (callback, state) in callbacks)
        {
            callback(state);
        }
    }
}

/// <summary>
/// Test implementation of IEventAggregator that records published events
/// </summary>
public class TestEventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<object>> _subscriptions = new();
    private readonly List<object> _publishedEvents = new();
    private readonly Dictionary<ISubscriptionToken, object> _tokenHandlers = new();
    private int _tokenCounter = 0;

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    public void Publish<TEvent>(TEvent eventToPublish, EventPriority priority = EventPriority.Normal) where TEvent : IEvent
    {
        _publishedEvents.Add(eventToPublish!);

        if (_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Action<TEvent> typedHandler)
                {
                    typedHandler(eventToPublish);
                }
            }
        }
    }

    public ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = new List<object>();
            _subscriptions[typeof(TEvent)] = handlers;
        }

        handlers.Add(handler);

        var token = new SubscriptionToken(_tokenCounter++);
        _tokenHandlers[token] = handler;

        return token;
    }

    public void Unsubscribe(ISubscriptionToken token)
    {
        if (_tokenHandlers.TryGetValue(token, out var handler))
        {
            foreach (var subscriptionList in _subscriptions.Values)
            {
                subscriptionList.Remove(handler);
            }

            _tokenHandlers.Remove(token);
        }
    }

    public void Clear()
    {
        _publishedEvents.Clear();
        _subscriptions.Clear();
        _tokenHandlers.Clear();
    }

    private class SubscriptionToken : ISubscriptionToken
    {
        public int Id { get; }

        public SubscriptionToken(int id)
        {
            Id = id;
        }
    }
}

/// <summary>
/// Mock DOM implementations for testing
/// </summary>
public static class MockDomImplementations
{
    public static IElement CreateElement(string id = "test-id", string tagName = "div", IElement? parent = null)
    {
        var element = Substitute.For<IElement>();
        element.Id.Returns(id);
        element.TagName.Returns(tagName);
        element.NodeType.Returns(NodeType.Element);
        element.NodeName.Returns(tagName.ToUpperInvariant());
        element.ParentElement.Returns(parent);
        element.ChildNodes.Returns(Array.Empty<IDomNode>());

        return element;
    }

    public static IText CreateTextNode(string data = "test-text", IDomNode? parent = null)
    {
        var text = Substitute.For<IText>();
        text.Data.Returns(data);
        text.NodeType.Returns(NodeType.Text);
        text.NodeName.Returns("#text");
        text.ParentNode.Returns(parent);

        return text;
    }

    public static IDocument CreateDocument(IElement documentElement)
    {
        var document = Substitute.For<IDocument>();
        document.DocumentElement.Returns(documentElement);
        document.NodeType.Returns(NodeType.Document);
        document.NodeName.Returns("#document");

        document.GetElementById(Arg.Any<string>()).Returns(callInfo => {
            var id = callInfo.Arg<string>();
            return documentElement.Id == id ? documentElement : null;
        });

        return document;
    }
}

/// <summary>
/// Helper class to create a visual update for testing
/// </summary>
public class TestVisualUpdate : IVisualUpdate
{
    public Guid Id { get; }
    public UpdateType Type { get; }
    public IElement Element { get; }
    public IReadOnlyList<string> ChangedProperties { get; }
    public DateTime Timestamp { get; }

    public TestVisualUpdate(UpdateType type, IElement element, IReadOnlyList<string>? changedProperties = null)
    {
        Id = Guid.NewGuid();
        Type = type;
        Element = element;
        ChangedProperties = changedProperties ?? new List<string>();
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Helper class to create a test resource
/// </summary>
public class TestResource : IResource
{
    public string Url { get; }
    public string ContentType { get; }
    public byte[] Data { get; }
    public bool IsLoaded { get; }
    public ResourceType ResourceType { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public TestResource(string url, ResourceType resourceType, bool isLoaded = true)
    {
        Url = url;
        ResourceType = resourceType;
        IsLoaded = isLoaded;

        ContentType = resourceType switch
        {
            ResourceType.Image => "image/png",
            ResourceType.Font => "font/woff2",
            ResourceType.StyleSheet => "text/css",
            ResourceType.Script => "application/javascript",
            _ => "application/octet-stream"
        };

        Data = Array.Empty<byte>();
        Metadata = new Dictionary<string, string>();
    }
}