using System;
using System.Threading.Tasks; // Added for Task
using AngleSharp;
using AngleSharp.Dom;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Updates; // Added for IUpdateScheduler reference if needed
using LayoutEngine.Contracts.StyleSystem;

namespace LayoutEngine;

/// <summary>
/// Defines the main interface for the LayoutEngine rendering system.
/// This orchestrates the various subsystems (Style, Layout, etc.).
/// </summary>
public interface ILayoutEngineMain : IDisposable
{
    /// <summary>
    /// Gets the AngleSharp browsing context used for parsing and DOM operations.
    /// </summary>
    IBrowsingContext BrowsingContext { get; }

    /// <summary>
    /// Gets the active AngleSharp document currently being processed.
    /// </summary>
    IDocument? Document { get; }

    /// <summary>
    /// Gets the current phase of the document rendering lifecycle (e.g., StyleClean, InLayout).
    /// </summary>
    DocumentLifecyclePhase CurrentPhase { get; }

    /// <summary>
    /// Gets the current viewport dimensions used for layout calculations.
    /// </summary>
    Rect Viewport { get; }

    /// <summary>
    /// Gets whether the engine has been initialized with a document.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Opens a document from an HTML string content.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="baseUrl">Optional base URL for resolving relative paths.</param>
    /// <returns>The loaded and parsed AngleSharp document.</returns>
    IDocument Open(string html, string? baseUrl = null);

    /// <summary>
    /// Opens a document by loading it from a specified file path.
    /// </summary>
    /// <param name="filePath">The path to the HTML file.</param>
    /// <returns>The loaded and parsed AngleSharp document.</returns>
    IDocument OpenFile(string filePath);

    /// <summary>
    /// Initializes the LayoutEngine with the specified document, setting up subsystems.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>A task representing the asynchronous initialization process.</returns>
    Task InitializeAsync(IDocument document);

    /// <summary>
    /// Synchronous version of InitializeAsync. Use with caution as it may block.
    /// </summary>
    /// <param name="document">The document to render.</param>
    [Obsolete("Prefer InitializeAsync to avoid potential blocking.", false)]
    void Initialize(IDocument document);

    /// <summary>
    /// Creates a new AngleSharp document instance from an HTML string without initializing the engine.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>The created AngleSharp document.</returns>
    IDocument CreateDocument(string html);

    /// <summary>
    /// Shuts down the LayoutEngine, releases resources, and cleans up subsystems.
    /// </summary>
    /// <returns>A task representing the asynchronous shutdown process.</returns>
    Task ShutdownAsync();

    /// <summary>
    /// Synchronous version of ShutdownAsync. Use with caution as it may block.
    /// </summary>
    [Obsolete("Prefer ShutdownAsync to avoid potential blocking.", false)]
    void Shutdown();

    /// <summary>
    /// Gets the final computed style for the specified element. May trigger style computation if needed.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The computed style object.</returns>
    IComputedStyle GetComputedStyle(IElement element);

    /// <summary>
    /// Gets the calculated layout box for the specified element. Ensures styles are computed first and may trigger layout computation.
    /// </summary>
    /// <param name="element">The element to get the layout box for.</param>
    /// <returns>The layout box object.</returns>
    ILayoutBox GetLayoutBox(IElement element);

    /// <summary>
    /// Explicitly processes all pending updates immediately, bypassing time budgeting.
    /// Primarily intended for testing or non-real-time scenarios.
    /// </summary>
    [Obsolete("Prefer calling ProcessPendingUpdates with a specific budget in a loop. This method bypasses budgeting.", false)]
    void ProcessUpdates();

    /// <summary>
    /// Schedules a full recalculation of styles and layout for the entire document.
    /// Processing occurs during subsequent calls to ProcessPendingUpdates.
    /// </summary>
    void ProcessFullDocument();

    /// <summary>
    /// Sets the viewport size used for layout calculations and media query evaluation.
    /// </summary>
    /// <param name="width">The new viewport width in pixels.</param>
    /// <param name="height">The new viewport height in pixels.</param>
    void SetViewportSize(float width, float height);

    /// <summary>
    /// Finds the topmost element at the specified viewport coordinates. Ensures layout is up-to-date first.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The element at the point, or null if no element is found.</returns>
    IElement? ElementFromPoint(float x, float y);

    /// <summary>
    /// Adds a stylesheet to the document and schedules a style update.
    /// </summary>
    /// <param name="styleSheet">The CSS content of the stylesheet.</param>
    /// <param name="origin">The origin of the stylesheet (Author, User, UserAgent).</param>
    /// <param name="mediaQuery">Optional media query string for the stylesheet.</param>
    /// <returns>A unique ID for the added stylesheet.</returns>
    string AddStyleSheet(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null);

    /// <summary>
    /// Removes a previously added stylesheet by its ID and schedules a style update.
    /// </summary>
    /// <param name="styleSheetId">The ID returned by AddStyleSheet.</param>
    /// <returns>True if the stylesheet was found and removed, false otherwise.</returns>
    bool RemoveStyleSheet(string styleSheetId);

    /// <summary>
    /// Hints that a resource should be loaded ahead of time (implementation specific).
    /// </summary>
    /// <param name="url">The URL of the resource to preload.</param>
    void PreloadResource(string url);

    /// <summary>
    /// Processes pending style, layout, and other updates within a given time budget.
    /// This should be called repeatedly by the host application (e.g., game engine loop).
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time allowed for processing in this call.</param>
    void ProcessPendingUpdates(double timeBudgetMilliseconds);
}