namespace LayoutEngine.Contracts.Platform.Dom.Abstractions;

using AngleSharp.Dom;

/// <summary>
/// Interface for resize observer to make testing easier
/// </summary>
public interface IResizeObserver
{
    /// <summary>
    /// Observes resize events on a target element
    /// </summary>
    /// <param name="target">Element to observe</param>
    void Observe(IElement target);

    /// <summary>
    /// Stops observing resize events
    /// </summary>
    void Disconnect();
}