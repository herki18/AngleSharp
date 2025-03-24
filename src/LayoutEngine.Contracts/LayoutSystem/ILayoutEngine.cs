namespace LayoutEngine.Contracts.LayoutSystem;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Platform.Lifecycle;

/// <summary>
/// Interface for the layout engine responsible for calculating layout.
/// </summary>
public interface ILayoutEngine : IDisposable
{
    /// <summary>
    /// Gets the current document lifecycle phase.
    /// </summary>
    DocumentLifecyclePhase CurrentPhase { get; }

    /// <summary>
    /// Gets whether the layout engine has pending updates.
    /// </summary>
    bool HasPendingUpdates { get; }

    /// <summary>
    /// Gets the current viewport size.
    /// </summary>
    Rect Viewport { get; }

    /// <summary>
    /// Initializes the layout engine with a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(IDocument document);

    /// <summary>
    /// Shuts down the layout engine.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShutdownAsync();

    /// <summary>
    /// Processes all pending layout updates.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessUpdatesAsync();

    /// <summary>
    /// Computes layout for a specific element.
    /// </summary>
    /// <param name="element">The element to compute layout for.</param>
    /// <returns>The layout box.</returns>
    Task<ILayoutBox> ComputeLayoutAsync(IElement element);

    /// <summary>
    /// Invalidates layout for specific elements.
    /// </summary>
    /// <param name="elements">The elements to invalidate.</param>
    void InvalidateLayout(IReadOnlyList<IElement> elements);

    /// <summary>
    /// Invalidates all layout in the document.
    /// </summary>
    void InvalidateAllLayout();

    /// <summary>
    /// Gets the cached layout for an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The layout box or null if not computed.</returns>
    ILayoutBox? GetCachedLayout(IElement element);

    /// <summary>
    /// Gets the current layout tree.
    /// </summary>
    /// <returns>The root of the layout tree.</returns>
    ILayoutBox GetLayoutTree();

    /// <summary>
    /// Sets the viewport size.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    void SetViewportSize(float width, float height);

    /// <summary>
    /// Finds the element at a specific point.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>The element at the specified point or null if none.</returns>
    IElement? ElementFromPoint(float x, float y);
}