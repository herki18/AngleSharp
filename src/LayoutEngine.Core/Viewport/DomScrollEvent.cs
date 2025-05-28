namespace LayoutEngine.Core.Viewport;

using Infrastructure.EventAggregator.API.Events;
using Layout;
using Layout.Internal;

public class DomScrollEvent : EventBase
{
    public string ViewportId { get; }
    public Point NewScrollOffset { get; }
    public Point OldScrollOffset { get; }

    public DomScrollEvent(string viewportId, Point newScrollOffset, Point oldScrollOffset)
    {
        ViewportId = viewportId;
        NewScrollOffset = newScrollOffset;
        OldScrollOffset = oldScrollOffset;
    }
}

public class UnityScrollEvent : EventBase
{
    public string ViewportId { get; }
    public Point NewScrollOffset { get; }

    public UnityScrollEvent(string viewportId, Point newScrollOffset)
    {
        ViewportId = viewportId;
        NewScrollOffset = newScrollOffset;
    }
}