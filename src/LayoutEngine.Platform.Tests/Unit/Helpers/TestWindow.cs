namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Test window for testing
/// </summary>
public class TestWindow : IWindow
{
    private readonly Dictionary<string, List<Action<Event>>> _eventListeners = new Dictionary<string, List<Action<Event>>>();
    private readonly TestDocument _document;

    public TestWindow()
    {
        _document = new TestDocument();
        InnerWidth = 1024;
        InnerHeight = 768;
        DevicePixelRatio = 1.0;
    }

    public IDocument Document => _document;
    public int InnerWidth { get; set; }
    public int InnerHeight { get; set; }
    public double DevicePixelRatio { get; set; }

    public void AddEventListener(string eventType, Action<Event> listener)
    {
        if (!_eventListeners.TryGetValue(eventType, out var listeners))
        {
            listeners = new List<Action<Event>>();
            _eventListeners[eventType] = listeners;
        }

        listeners.Add(listener);
    }

    public void RemoveEventListener(string eventType, Action<Event> listener)
    {
        if (_eventListeners.TryGetValue(eventType, out var listeners))
        {
            listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Simulates an event for testing
    /// </summary>
    public void SimulateEvent(string eventType, IDomNode target)
    {
        var e = new Event(eventType, target);

        if (_eventListeners.TryGetValue(eventType, out var listeners))
        {
            foreach (var listener in listeners.ToArray())
            {
                listener(e);
            }
        }
    }

    /// <summary>
    /// Simulates a resize for testing
    /// </summary>
    public void SimulateResize(int width, int height)
    {
        InnerWidth = width;
        InnerHeight = height;
        SimulateEvent("resize", _document.DocumentElement);
    }

    /// <summary>
    /// Gets the document
    /// </summary>
    public TestDocument TestDocument => _document;
}