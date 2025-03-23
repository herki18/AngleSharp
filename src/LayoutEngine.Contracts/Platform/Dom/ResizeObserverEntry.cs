namespace LayoutEngine.Contracts.Platform.Dom;

using AngleSharp.Dom;

/// <summary>
/// Represents a resize observer entry.
/// </summary>
public class ResizeObserverEntry
{
    /// <summary>
    /// Gets the target of the resize.
    /// </summary>
    public IElement Target { get; }

    /// <summary>
    /// Gets the content rectangle of the target.
    /// </summary>
    public Rectangle ContentRect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeObserverEntry"/> class.
    /// </summary>
    /// <param name="target">The target of the resize.</param>
    /// <param name="contentRect">The content rectangle of the target.</param>
    public ResizeObserverEntry(IElement target, Rectangle contentRect)
    {
        Target = target;
        ContentRect = contentRect;
    }
}