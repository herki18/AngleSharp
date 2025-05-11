namespace LayoutEngine.Core.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when rendering is completed.
/// </summary>
public class RenderCompletedEvent : EventBase
{
    /// <summary>
    /// Gets information about the completed render.
    /// </summary>
    public object? RenderInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCompletedEvent"/> class.
    /// </summary>
    /// <param name="renderInfo">Information about the completed render.</param>
    public RenderCompletedEvent(object? renderInfo = null)
    {
        RenderInfo = renderInfo;
    }
}