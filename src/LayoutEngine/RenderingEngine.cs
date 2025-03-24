using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace LayoutEngine;

using Contracts.LayoutSystem;
using Contracts.Platform.Events;
using Contracts.Platform.Lifecycle;
using Contracts.Platform.Threading;
using Contracts.Platform.Updates;
using Contracts.Resource;
using Contracts.StyleSystem;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;
using StyleComputedEvent = Contracts.StyleSystem.StyleComputedEvent;

/// <summary>
/// Main implementation of the LayoutEngine rendering system.
/// Manages document contexts and coordinates the rendering pipeline.
/// Integrates with AngleSharp for HTML/CSS parsing.
/// </summary>
public class LayoutEngineMain : ILayoutEngineMain
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IUpdateScheduler _updateScheduler;
    private readonly IFrameScheduler _frameScheduler;
    private readonly IResourceLoader _resourceLoader;
    private readonly IStyleEngine _styleEngine;
    private readonly Contracts.LayoutSystem.ILayoutEngine _layoutEngine;
    private readonly ILogger<LayoutEngineMain> _logger;
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

    private IBrowsingContext _browsingContext;
    private IDocument? _document;
    private bool _isInitialized;
    private bool _isDisposed;
    private Rect _currentViewport = new Rect(0, 0, 800, 600);
    private LayoutEngineConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the LayoutEngineMain class.
    /// </summary>
    public LayoutEngineMain(
        IEventAggregator eventAggregator,
        ICacheManager cacheManager,
        IDocumentLifecycleCoordinator lifecycleCoordinator,
        IThreadingCoordinator threadingCoordinator,
        IUpdateScheduler updateScheduler,
        IFrameScheduler frameScheduler,
        IResourceLoader resourceLoader,
        IStyleEngine styleEngine,
        Contracts.LayoutSystem.ILayoutEngine layoutEngine,
        LayoutEngineConfiguration? configuration = null,
        ILogger<LayoutEngineMain>? logger = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _lifecycleCoordinator = lifecycleCoordinator ?? throw new ArgumentNullException(nameof(lifecycleCoordinator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _updateScheduler = updateScheduler ?? throw new ArgumentNullException(nameof(updateScheduler));
        _frameScheduler = frameScheduler ?? throw new ArgumentNullException(nameof(frameScheduler));
        _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LayoutEngineMain>.Instance;
        _configuration = configuration ?? LayoutEngineConfiguration.Default;

        // Create browsing context with default configuration
        _browsingContext = AngleSharp.BrowsingContext.New(_configuration.AngleSharpConfiguration);

        _subscriptions.Add(_eventAggregator.Subscribe<ViewportChangedEvent>(OnViewportChanged));
        _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
        _subscriptions.Add(_eventAggregator.Subscribe<LayoutUpdatedEvent>(OnLayoutUpdated));
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
    }

    /// <summary>
    /// Gets the AngleSharp browsing context.
    /// </summary>
    public IBrowsingContext BrowsingContext => _browsingContext;

    /// <summary>
    /// Gets the active document.
    /// </summary>
    public IDocument? Document => _document;

    /// <summary>
    /// Gets the current document lifecycle phase.
    /// </summary>
    public DocumentLifecyclePhase CurrentPhase => _lifecycleCoordinator.CurrentPhase;

    /// <summary>
    /// Gets the current viewport dimensions.
    /// </summary>
    public Rect Viewport => _currentViewport;

    /// <summary>
    /// Gets whether the engine is currently initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Opens a document from a string of HTML.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="baseUrl">Optional base URL for the document.</param>
    /// <returns>A task representing the asynchronous open operation.</returns>
    public async Task<IDocument> OpenAsync(string html, string? baseUrl = null)
    {
        ThrowIfDisposed();

        // Shutdown any existing document
        if (_isInitialized)
        {
            await ShutdownAsync();
        }

        _logger.LogInformation("Opening document from HTML string");

        try
        {
            // Parse the HTML
            var document = await _browsingContext.OpenAsync(req => req.Content(html).Address(baseUrl ?? "about:blank"));

            // Initialize the engine with the document
            await InitializeAsync(document);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document from HTML string");
            throw;
        }
    }

    /// <summary>
    /// Opens a document from a file.
    /// </summary>
    /// <param name="filePath">The path to the HTML file.</param>
    /// <returns>A task representing the asynchronous open operation.</returns>
    public async Task<IDocument> OpenFileAsync(string filePath)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("HTML file not found", filePath);

        // Shutdown any existing document
        if (_isInitialized)
        {
            await ShutdownAsync();
        }

        _logger.LogInformation("Opening document from file: {FilePath}", filePath);

        try
        {
            // Parse the HTML from file
            var document = await _browsingContext.OpenAsync(filePath);

            // Initialize the engine with the document
            await InitializeAsync(document);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Initializes the LayoutEngine with the specified document.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync(IDocument document)
    {
        ThrowIfDisposed();

        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (_isInitialized)
        {
            await ShutdownAsync();
        }

        _logger.LogInformation("Initializing LayoutEngine");
        _document = document;

        try
        {
            // Initialize viewport dimensions based on document or default
            // var viewportWidth = document.DefaultView?.InnerWidth ?? 800;
            // var viewportHeight = document.DefaultView?.InnerHeight ?? 600;
            // _currentViewport = new Rect(0, 0, viewportWidth, viewportHeight);

            // Initialize subsystems
            await _styleEngine.InitializeAsync(document);
            await _layoutEngine.InitializeAsync(document);

            // Enter initial lifecycle phase
            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

            // Extract and process all style elements in the document
            ProcessDocumentStyleSheets(document);

            _isInitialized = true;
            _logger.LogInformation("LayoutEngine initialized successfully");

            // Trigger initial update to process the document
            await ProcessFullDocumentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing LayoutEngine");
            await CleanupAsync();
            throw;
        }
    }

    /// <summary>
    /// Creates a new document with the specified HTML.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>The created document.</returns>
    public IDocument CreateDocument(string html)
    {
        ThrowIfDisposed();

        var parser = new HtmlParser();
        return parser.ParseDocument(html);
    }

    /// <summary>
    /// Shuts down the LayoutEngine and releases all resources.
    /// </summary>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    public async Task ShutdownAsync()
    {
        ThrowIfDisposed();

        if (!_isInitialized)
        {
            return;
        }

        _logger.LogInformation("Shutting down LayoutEngine");

        try
        {
            // Shutdown subsystems
            await _styleEngine.ShutdownAsync();
            await _layoutEngine.ShutdownAsync();

            // Enter disposed lifecycle phase
            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.Disposed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LayoutEngine shutdown");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Gets the style computed for the specified element.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The computed style for the element.</returns>
    public async Task<IComputedStyle> GetComputedStyleAsync(IElement element)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return await _styleEngine.ComputeStyleAsync(element);
    }

    /// <summary>
    /// Gets the layout box for the specified element.
    /// </summary>
    /// <param name="element">The element to get the layout box for.</param>
    /// <returns>The layout box for the element.</returns>
    public async Task<ILayoutBox> GetLayoutBoxAsync(IElement element)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return await _layoutEngine.ComputeLayoutAsync(element);
    }

    /// <summary>
    /// Processes all pending updates in the style and layout systems.
    /// </summary>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    public async Task ProcessUpdatesAsync()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        await _styleEngine.ProcessUpdatesAsync();
        await _layoutEngine.ProcessUpdatesAsync();
    }

    /// <summary>
    /// Processes the full document by invalidating all styles and layout.
    /// </summary>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    public async Task ProcessFullDocumentAsync()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        _styleEngine.InvalidateAllStyles();
        await _styleEngine.ProcessUpdatesAsync();

        _layoutEngine.InvalidateAllLayout();
        await _layoutEngine.ProcessUpdatesAsync();
    }

    /// <summary>
    /// Sets the viewport size.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    public void SetViewportSize(float width, float height)
    {
        ThrowIfDisposed();
        _currentViewport = new Rect(0, 0, width, height);
        _layoutEngine.SetViewportSize(width, height);

        // Update the document's default view if available
        if (_document?.DefaultView != null)
        {
            // This is where we'd update the document's viewport,
            // but AngleSharp doesn't provide direct property setters for this
        }
    }

    /// <summary>
    /// Gets the element at the specified point.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The element at the specified point, or null if no element is found.</returns>
    public IElement? ElementFromPoint(float x, float y)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return _layoutEngine.ElementFromPoint(x, y);
    }

    /// <summary>
    /// Adds a style sheet to the document.
    /// </summary>
    /// <param name="styleSheet">The style sheet content.</param>
    /// <param name="origin">The style sheet origin.</param>
    /// <param name="mediaQuery">Optional media query.</param>
    /// <returns>The style sheet ID.</returns>
    public async Task<string> AddStyleSheetAsync(
        string styleSheet,
        StyleSheetOrigin origin,
        string? mediaQuery = null)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return await _styleEngine.AddStyleSheetAsync(styleSheet, origin, mediaQuery);
    }

    /// <summary>
    /// Removes a style sheet from the document.
    /// </summary>
    /// <param name="styleSheetId">The style sheet ID to remove.</param>
    /// <returns>True if the style sheet was removed, false otherwise.</returns>
    public bool RemoveStyleSheet(string styleSheetId)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return _styleEngine.RemoveStyleSheet(styleSheetId);
    }

    /// <summary>
    /// Preloads a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    public void PreloadResource(string url)
    {
        ThrowIfDisposed();

        // _resourceLoader.PreloadResource(url);
    }

    /// <summary>
    /// Handles viewport changed events.
    /// </summary>
    private void OnViewportChanged(ViewportChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        var oldViewport = _currentViewport;
        var newViewport = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

        _currentViewport = newViewport;
        _layoutEngine.SetViewportSize(newViewport.Width, newViewport.Height);

        _logger.LogDebug("Viewport changed from {OldViewport} to {NewViewport}",
            $"{oldViewport.Width}x{oldViewport.Height}",
            $"{newViewport.Width}x{newViewport.Height}");

        _eventAggregator.Publish(new ViewportSizeChangedEvent(oldViewport, newViewport));
    }

    /// <summary>
    /// Handles style computed events.
    /// </summary>
    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogTrace("Styles computed for {ElementCount} elements", e.Elements.Count);
    }

    /// <summary>
    /// Handles layout updated events.
    /// </summary>
    private void OnLayoutUpdated(LayoutUpdatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogTrace("Layout updated for {ElementCount} elements", e.UpdatedElements.Count);
    }

    /// <summary>
    /// Handles memory pressure events.
    /// </summary>
    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        if (_isDisposed)
            return;

        _logger.LogInformation("Memory pressure detected: {Severity}, current usage: {CurrentUsage}MB, threshold: {Threshold}MB",
            e.Severity,
            e.CurrentMemoryUsage / (1024 * 1024),
            e.MemoryThreshold / (1024 * 1024));
    }

    /// <summary>
    /// Handles resource error events.
    /// </summary>
    private void OnResourceError(ResourceErrorEvent e)
    {
        if (_isDisposed)
            return;

        _logger.LogWarning("Error loading resource {Url}: {Message}", e.Url, e.Error.Message);
    }

    /// <summary>
    /// Processes all style sheets in the document.
    /// </summary>
    private void ProcessDocumentStyleSheets(IDocument document)
    {
        // Process style elements
        foreach (var styleElement in document.QuerySelectorAll("style"))
        {
            var content = styleElement.TextContent;
            if (!string.IsNullOrEmpty(content))
            {
                var mediaAttr = styleElement.GetAttribute("media");
                _styleEngine.AddStyleSheetAsync(content, StyleSheetOrigin.Author, mediaAttr).Wait();
            }
        }

        // Process link elements with stylesheets
        foreach (var linkElement in document.QuerySelectorAll("link[rel=stylesheet]"))
        {
            var href = linkElement.GetAttribute("href");
            if (!string.IsNullOrEmpty(href))
            {
                // _resourceLoader.PreloadResource(href);
                // The stylesheet content will be processed when loaded
            }
        }
    }

    /// <summary>
    /// Performs cleanup of resources.
    /// </summary>
    private async Task CleanupAsync()
    {
        _document = null;
        _isInitialized = false;

        // Clear memory
        GC.Collect();
        await Task.Yield();
    }

    /// <summary>
    /// Ensures the engine is initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized || _document == null)
        {
            throw new InvalidOperationException("LayoutEngine is not initialized. Call InitializeAsync first.");
        }
    }

    /// <summary>
    /// Throws an exception if the object is disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(LayoutEngineMain));
        }
    }

    /// <summary>
    /// Disposes the LayoutEngine and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        try
        {
            // Shutdown asynchronously and wait for completion
            if (_isInitialized)
            {
                ShutdownAsync().GetAwaiter().GetResult();
            }

            // Unsubscribe from events
            foreach (var subscription in _subscriptions)
            {
                _eventAggregator.Unsubscribe(subscription);
            }
            _subscriptions.Clear();

            // Dispose browsing context
            _browsingContext?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing LayoutEngine");
        }
    }
}

/// <summary>
/// Configuration for the LayoutEngine.
/// </summary>
public class LayoutEngineConfiguration
{
    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static LayoutEngineConfiguration Default => new LayoutEngineConfiguration
    {
        AngleSharpConfiguration = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
    };

    /// <summary>
    /// Gets or sets the AngleSharp configuration.
    /// </summary>
    public IConfiguration AngleSharpConfiguration { get; set; } = Configuration.Default;

    /// <summary>
    /// Gets or sets the target frames per second.
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether background processing is enabled.
    /// </summary>
    public bool EnableBackgroundProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preload resources by default.
    /// </summary>
    public bool PreloadResources { get; set; } = true;
}
