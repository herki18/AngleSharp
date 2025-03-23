namespace LayoutEngine.Contracts.StyleSystem;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Platform.Dom;
using Platform.Lifecycle;

/// <summary>
/// Interface for the style engine responsible for CSS processing and style computation.
/// </summary>
public interface IStyleEngine : IDisposable
{
    /// <summary>
    /// Gets the current document lifecycle phase.
    /// </summary>
    DocumentLifecyclePhase CurrentPhase { get; }

    /// <summary>
    /// Gets whether the style engine has pending updates.
    /// </summary>
    bool HasPendingUpdates { get; }

    /// <summary>
    /// Initializes the style engine with a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(IDocument document);

    /// <summary>
    /// Shuts down the style engine.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShutdownAsync();

    /// <summary>
    /// Processes all pending style updates.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessUpdatesAsync();

    /// <summary>
    /// Computes styles for a specific element.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <returns>The computed style.</returns>
    Task<IComputedStyle> ComputeStyleAsync(IElement element);

    /// <summary>
    /// Invalidates styles for specific elements.
    /// </summary>
    /// <param name="elements">The elements to invalidate.</param>
    void InvalidateStyles(IReadOnlyList<IElement> elements);

    /// <summary>
    /// Invalidates all styles in the document.
    /// </summary>
    void InvalidateAllStyles();

    /// <summary>
    /// Gets the cached computed style for an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The computed style or null if not computed.</returns>
    IComputedStyle? GetCachedStyle(IElement element);

    /// <summary>
    /// Adds a stylesheet to the document.
    /// </summary>
    /// <param name="styleSheet">The stylesheet content.</param>
    /// <param name="origin">The origin of the stylesheet.</param>
    /// <param name="mediaQuery">Optional media query.</param>
    /// <returns>The ID of the added stylesheet.</returns>
    Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null);

    /// <summary>
    /// Removes a stylesheet from the document.
    /// </summary>
    /// <param name="styleSheetId">The ID of the stylesheet to remove.</param>
    /// <returns>True if the stylesheet was removed, false otherwise.</returns>
    bool RemoveStyleSheet(string styleSheetId);
}