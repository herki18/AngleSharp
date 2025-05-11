namespace LayoutEngine.Core.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when rendering is invalidated.
/// </summary>
public class RenderInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the fragment tree to render.
    /// </summary>
    public object? FragmentTree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="fragmentTree">The fragment tree to render.</param>
    public RenderInvalidatedEvent(object? fragmentTree)
    {
        FragmentTree = fragmentTree;
    }
}