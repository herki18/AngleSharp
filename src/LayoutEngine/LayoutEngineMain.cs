using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.StyleSystem;
using Microsoft.Extensions.Logging;

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
    private readonly ILayoutEngine _layoutEngine;
    private readonly ILogger<LayoutEngineMain> _logger;
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

    private IBrowsingContext _browsingContext;
    private IDocument? _document;
    private bool _isInitialized;
    private bool _isDisposed;
    private Rect _currentViewport = new Rect(0, 0, 800, 600);
    private LayoutEngineConfiguration _configuration;

    public LayoutEngineMain(
        IEventAggregator eventAggregator,
        ICacheManager cacheManager,
        IDocumentLifecycleCoordinator lifecycleCoordinator,
        IThreadingCoordinator threadingCoordinator,
        IUpdateScheduler updateScheduler,
        IFrameScheduler frameScheduler,
        IResourceLoader resourceLoader,
        IStyleEngine styleEngine,
        ILayoutEngine layoutEngine,
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
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
    }

    public IBrowsingContext BrowsingContext => _browsingContext;
    public IDocument? Document => _document;
    public DocumentLifecyclePhase CurrentPhase => _lifecycleCoordinator.CurrentPhase;
    public Rect Viewport => _currentViewport;
    public bool IsInitialized => _isInitialized;

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
            // Enter initial lifecycle phase
            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

            // Initialize subsystems in the correct order
            await _styleEngine.InitializeAsync(document);
            await _layoutEngine.InitializeAsync(document);

            // Now that the style engine is initialized, it can handle the stylesheets
            // We don't need to process them manually here

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

    public IDocument CreateDocument(string html)
    {
        ThrowIfDisposed();

        var parser = new HtmlParser();
        return parser.ParseDocument(html);
    }

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
            // Shutdown subsystems in reverse order
            await _layoutEngine.ShutdownAsync();
            await _styleEngine.ShutdownAsync();

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

    public async Task<IComputedStyle> GetComputedStyleAsync(IElement element)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return await _styleEngine.ComputeStyleAsync(element);
    }

    public async Task<ILayoutBox> GetLayoutBoxAsync(IElement element)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return await _layoutEngine.ComputeLayoutAsync(element);
    }

    public async Task ProcessUpdatesAsync()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        await _styleEngine.ProcessUpdatesAsync();
        await _layoutEngine.ProcessUpdatesAsync();
    }

    public async Task ProcessFullDocumentAsync()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        _styleEngine.InvalidateAllStyles();
        await _styleEngine.ProcessUpdatesAsync();

        _layoutEngine.InvalidateAllLayout();
        await _layoutEngine.ProcessUpdatesAsync();
    }

    public void SetViewportSize(float width, float height)
    {
        ThrowIfDisposed();
        _currentViewport = new Rect(0, 0, width, height);
        _layoutEngine.SetViewportSize(width, height);
    }

    public IElement? ElementFromPoint(float x, float y)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return _layoutEngine.ElementFromPoint(x, y);
    }

    public async Task<string> AddStyleSheetAsync(
        string styleSheet,
        StyleSheetOrigin origin,
        string? mediaQuery = null)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return await _styleEngine.AddStyleSheetAsync(styleSheet, origin, mediaQuery);
    }

    public bool RemoveStyleSheet(string styleSheetId)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        return _styleEngine.RemoveStyleSheet(styleSheetId);
    }

    public void PreloadResource(string url)
    {
        ThrowIfDisposed();

        _resourceLoader.PreloadResource(url);
    }

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

    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogTrace("Styles computed for {ElementCount} elements", e.Elements.Count);
    }

    private void OnLayoutUpdated(LayoutUpdatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogTrace("Layout updated for {ElementCount} elements", e.UpdatedElements.Count);
    }

    private void OnResourceError(ResourceErrorEvent e)
    {
        if (_isDisposed)
            return;

        _logger.LogWarning("Error loading resource {Url}: {Message}", e.Url, e.Error.Message);
    }

    private async Task CleanupAsync()
    {
        _document = null;
        _isInitialized = false;

        // Clear memory
        GC.Collect();
        await Task.Yield();
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _document == null)
        {
            throw new InvalidOperationException("LayoutEngine is not initialized. Call InitializeAsync first.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(LayoutEngineMain));
        }
    }

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