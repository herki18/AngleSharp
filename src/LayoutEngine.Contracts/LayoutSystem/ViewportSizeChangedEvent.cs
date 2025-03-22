namespace LayoutEngine.Contracts.LayoutSystem;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when the viewport size changes.
/// </summary>
public class ViewportSizeChangedEvent : EventBase
{
    /// <summary>
    /// Gets the old viewport size.
    /// </summary>
    public Rect OldViewport { get; }
        
    /// <summary>
    /// Gets the new viewport size.
    /// </summary>
    public Rect NewViewport { get; }
        
    /// <summary>
    /// Initializes a new instance of the ViewportSizeChangedEvent class.
    /// </summary>
    /// <param name="oldViewport">The old viewport size.</param>
    /// <param name="newViewport">The new viewport size.</param>
    public ViewportSizeChangedEvent(Rect oldViewport, Rect newViewport)
    {
        OldViewport = oldViewport;
        NewViewport = newViewport;
    }
}