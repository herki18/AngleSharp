using System;
using System.Threading.Tasks;
using AngleSharp.Dom;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.StyleSystem;

namespace LayoutEngine;

/// <summary>
/// Defines the main interface for the LayoutEngine rendering system.
/// </summary>
public interface ILayoutEngine : IDisposable
{
    /// <summary>
    /// Gets the AngleSharp browsing context.
    /// </summary>
    // IBrowsingContext BrowsingContext { get; }

    /// <summary>
    /// Gets the active document.
    /// </summary>
    IDocument? Document { get; }

    /// <summary>
    /// Gets the current document lifecycle phase.
    /// </summary>
    DocumentLifecyclePhase CurrentPhase { get; }

    /// <summary>
    /// Gets the current viewport dimensions.
    /// </summary>
    Rect Viewport { get; }

    /// <summary>
    /// Gets whether the engine is currently initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Opens a document from a string of HTML.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="baseUrl">Optional base URL for the document.</param>
    /// <returns>A task representing the asynchronous open operation.</returns>
    Task<IDocument> OpenAsync(string html, string? baseUrl = null);

    /// <summary>
    /// Opens a document from a file.
    /// </summary>
    /// <param name="filePath">The path to the HTML file.</param>
    /// <returns>A task representing the asynchronous open operation.</returns>
    Task<IDocument> OpenFileAsync(string filePath);

    /// <summary>
    /// Opens a document from a URL.
    /// </summary>
    /// <param name="url">The URL to load.</param>
    /// <returns>A task representing the asynchronous open operation.</returns>
    Task<IDocument> OpenUrlAsync(string url);

    /// <summary>
    /// Initializes the LayoutEngine with the specified document.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task InitializeAsync(IDocument document);

    /// <summary>
    /// Creates a new document with the specified HTML.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>The created document.</returns>
    IDocument CreateDocument(string html);

    /// <summary>
    /// Shuts down the LayoutEngine and releases all resources.
    /// </summary>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    Task ShutdownAsync();

    /// <summary>
    /// Gets the style computed for the specified element.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The computed style for the element.</returns>
    Task<IComputedStyle> GetComputedStyleAsync(IElement element);

    /// <summary>
    /// Gets the layout box for the specified element.
    /// </summary>
    /// <param name="element">The element to get the layout box for.</param>
    /// <returns>The layout box for the element.</returns>
    Task<ILayoutBox> GetLayoutBoxAsync(IElement element);

    /// <summary>
    /// Processes all pending updates in the style and layout systems.
    /// </summary>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessUpdatesAsync();

    /// <summary>
    /// Processes the full document by invalidating all styles and layout.
    /// </summary>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessFullDocumentAsync();

    /// <summary>
    /// Sets the viewport size.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    void SetViewportSize(float width, float height);

    /// <summary>
    /// Gets the element at the specified point.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The element at the specified point, or null if no element is found.</returns>
    IElement? ElementFromPoint(float x, float y);

    /// <summary>
    /// Adds a style sheet to the document.
    /// </summary>
    /// <param name="styleSheet">The style sheet content.</param>
    /// <param name="origin">The style sheet origin.</param>
    /// <param name="mediaQuery">Optional media query.</param>
    /// <returns>The style sheet ID.</returns>
    Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null);

    /// <summary>
    /// Removes a style sheet from the document.
    /// </summary>
    /// <param name="styleSheetId">The style sheet ID to remove.</param>
    /// <returns>True if the style sheet was removed, false otherwise.</returns>
    bool RemoveStyleSheet(string styleSheetId);

    /// <summary>
    /// Preloads a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    void PreloadResource(string url);
}