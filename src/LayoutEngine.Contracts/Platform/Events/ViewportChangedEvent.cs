namespace LayoutEngine.Contracts.Platform.Events;

using Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when the viewport changes.
/// </summary>
public class ViewportChangedEvent : EventBase
{
    /// <summary>
    /// Gets the old viewport size.
    /// </summary>
    public Size OldSize { get; }

    /// <summary>
    /// Gets the new viewport size.
    /// </summary>
    public Size NewSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewportChangedEvent"/> class.
    /// </summary>
    /// <param name="oldSize">The old viewport size.</param>
    /// <param name="newSize">The new viewport size.</param>
    public ViewportChangedEvent(Size oldSize, Size newSize)
    {
        OldSize = oldSize;
        NewSize = newSize;
    }
}