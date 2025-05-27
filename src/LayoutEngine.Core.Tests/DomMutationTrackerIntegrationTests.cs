using AngleSharp.Dom;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.EventAggregator.API.Aggregation;
using System.Collections.Concurrent;

namespace LayoutEngine.Core.Tests;

using Core;
using Events;

public class DomMutationTrackerIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IEngine _engine;
    private readonly TestEventSubscriber _subscriber;

    public DomMutationTrackerIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        _serviceProvider = services.BuildServiceProvider();
        _engine = _serviceProvider.GetRequiredService<IEngine>();
        var eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
        // Set up the test subscriber and subscribe to events
        _subscriber = new TestEventSubscriber();
        eventAggregator.Subscribe<DomAttributeChangedEvent>(_subscriber.OnDomAttributeChangedEvent);
        eventAggregator.Subscribe<DomNodeAddedEvent>(_subscriber.OnDomNodeAddedEvent);
        eventAggregator.Subscribe<DomTextChangedEvent>(_subscriber.OnDomTextChangedEvent);
    }

    [Fact]
    public async Task AttributeChange_PublishesDomAttributeChangedEvent()
    {
        var html = "<html><body><div id='test'></div></body></html>";
        var document = await _engine.OpenAsync(html);

        var div = document.QuerySelector("#test");
        Assert.NotNull(div);

        div.SetAttribute("data-foo", "bar");

        await Task.Delay(50);

        Assert.Contains(_subscriber.DomAttributeChangedEvents, evt =>
            evt.Element == div &&
            evt.AttributeName == "data-foo" &&
            evt.NewValue == "bar");
    }

    [Fact]
    public async Task NodeAddition_PublishesDomNodeAddedEvent()
    {
        var html = "<html><body></body></html>";
        var document = await _engine.OpenAsync(html);

        var body = document.Body;
        Assert.NotNull(body);

        var newDiv = document.CreateElement("div");
        body.AppendChild(newDiv);

        await Task.Delay(50);

        Assert.Contains(_subscriber.DomNodeAddedEvents, evt =>
            evt.Node == newDiv &&
            evt.Parent == body);
    }

    [Fact]
    public async Task TextChange_PublishesDomTextChangedEvent()
    {
        var html = "<html><body><span id='s'>hello</span></body></html>";
        var document = await _engine.OpenAsync(html);

        var span = document.QuerySelector("#s");
        Assert.NotNull(span);

        var textNode = span.FirstChild as IText;
        Assert.NotNull(textNode);

        textNode.TextContent = "world";

        await Task.Delay(50);

        Assert.Contains(_subscriber.DomTextChangedEvents, evt =>
            evt.TextNode == textNode &&
            evt.NewValue == "world");
    }

    // Test subscriber implementation
    public class TestEventSubscriber
    {
        public ConcurrentBag<DomAttributeChangedEvent> DomAttributeChangedEvents { get; } = new();
        public ConcurrentBag<DomNodeAddedEvent> DomNodeAddedEvents { get; } = new();
        public ConcurrentBag<DomTextChangedEvent> DomTextChangedEvents { get; } = new();

        public void OnDomAttributeChangedEvent(DomAttributeChangedEvent evt) => DomAttributeChangedEvents.Add(evt);
        public void OnDomNodeAddedEvent(DomNodeAddedEvent evt) => DomNodeAddedEvents.Add(evt);
        public void OnDomTextChangedEvent(DomTextChangedEvent evt) => DomTextChangedEvents.Add(evt);
    }
}