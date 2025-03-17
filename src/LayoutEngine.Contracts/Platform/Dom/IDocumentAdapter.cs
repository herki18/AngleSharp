using System;
using AngleSharp.Dom;

namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Adapts DOM documents for the rendering engine.
/// </summary>
public interface IDocumentAdapter
{
    /// <summary>
    /// Gets the document.
    /// </summary>
    IDocument Document { get; }

    /// <summary>
    /// Gets the width of the viewport in CSS pixels.
    /// </summary>
    int ViewportWidth { get; }

    /// <summary>
    /// Gets the height of the viewport in CSS pixels.
    /// </summary>
    int ViewportHeight { get; }

    /// <summary>
    /// Gets the device pixel ratio.
    /// </summary>
    float DevicePixelRatio { get; }

    /// <summary>
    /// Gets an observable sequence of window resize events.
    /// </summary>
    IObservable<EventArgs> WindowResizeEvents { get; }
}