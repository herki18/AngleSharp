using System;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AngleSharp.StyleSystem.Services;

using Tasks;

/// <summary>
/// Central service that coordinates StyleSystem functionality.
/// </summary>
public sealed class StyleSystemService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StyleSystemOptions _options;
    private IBrowsingContext? _context;
    private IDocument? _currentDocument;
    private bool _isDisposed;
    private readonly object _initLock = new object();

    /// <summary>
    /// Creates a new StyleSystemService with the given service provider and options.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing registered services.</param>
    /// <param name="options">Style system configuration options.</param>
    public StyleSystemService(IServiceProvider serviceProvider, StyleSystemOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new StyleSystemOptions();

        // Important: Verify the service provider contains the required services
        try
        {
            var context = _serviceProvider.GetService<IBrowsingContext>();
            if (context != null)
            {
                Initialize(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during StyleSystem construction: {ex.Message}");
            // Don't throw here, as the service might be initialized later
        }
    }

    /// <summary>
    /// Gets whether the service has been properly initialized with a browsing context.
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            try
            {
                return GetService<IStyleEngine>() != null && _context != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the StyleEngine instance.
    /// </summary>
    public IStyleEngine? StyleEngine => GetService<IStyleEngine>();

    /// <summary>
    /// Gets the DocumentLifecycleCoordinator instance.
    /// </summary>
    public IDocumentLifecycleCoordinator? LifecycleCoordinator => GetService<IDocumentLifecycleCoordinator>();

    /// <summary>
    /// Gets whether style optimization is enabled.
    /// </summary>
    public bool OptimizationEnabled => StyleEngine?.OptimizationEnabled ?? false;

    /// <summary>
    /// Gets the associated browsing context.
    /// </summary>
    public IBrowsingContext? Context => _context;

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to get.</typeparam>
    /// <returns>The service instance or null if not registered.</returns>
    public T? GetService<T>() where T : class
    {
        try
        {
            return _serviceProvider.GetService<T>();
        }
        catch (InvalidOperationException)
        {
            // Handle dependency resolution errors
            return null;
        }
    }

    /// <summary>
    /// Initializes the StyleSystem with the provided browsing context.
    /// </summary>
    /// <param name="context">The browsing context to associate with this StyleSystem.</param>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    public void Initialize(IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        lock (_initLock)
        {
            if (_context == context && IsInitialized)
                return;

            if (_context != null && _context != context)
            {
                CleanupContext(_context);
            }

            _context = context;

            try
            {
                // Now initialize all components with the context
                InitializeComponents();

                // Check if there's already an active document
                CheckCurrentDocument();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing StyleSystem: {ex.Message}");
                throw;
            }
        }
    }

    private void CheckCurrentDocument()
    {
        if (_context?.Active != null && _context.Active != _currentDocument)
        {
            HookDocumentEvents(_context.Active);
        }
    }

    /// <summary>
    /// Cleans up any resources associated with the given context.
    /// </summary>
    /// <param name="context">The context to clean up.</param>
    public void CleanupContext(IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (_currentDocument != null)
        {
            UnhookDocumentEvents(_currentDocument);
            _currentDocument = null;
        }
    }

    /// <summary>
    /// Forces a style update for all elements in the document.
    /// </summary>
    /// <param name="context">The browsing context to update.</param>
    public void ForceStyleUpdate(IBrowsingContext context)
    {
        if (context?.Active?.DocumentElement == null)
            return;

        if (_context != context)
        {
            Initialize(context);
        }

        StyleEngine?.UpdateStyles(context.Active.DocumentElement);
    }

    /// <summary>
    /// Notifies the StyleSystem about a document change.
    /// </summary>
    /// <param name="document">The new active document.</param>
    public void NotifyDocumentChanged(IDocument document)
    {
        if (document == null || document == _currentDocument)
            return;

        if (_currentDocument != null)
        {
            UnhookDocumentEvents(_currentDocument);
        }

        HookDocumentEvents(document);
    }

    /// <summary>
    /// Processes any pending style tasks immediately.
    /// </summary>
    public void ProcessPendingStyleTasksImmediately()
    {
        GetService<IStyleRecalcScheduler>()?.ProcessImmediately();
    }

    /// <summary>
    /// Gets metrics about the style optimization.
    /// </summary>
    /// <returns>Optimization metrics or null if not available.</returns>
    public OptimizationMetrics? GetOptimizationMetrics()
    {
        return StyleEngine?.GetOptimizationMetrics();
    }

    /// <summary>
    /// Clears the style cache.
    /// </summary>
    public void ClearStyleCache()
    {
        GetService<IStyleCache>()?.Clear();
    }

    /// <summary>
    /// Optimizes all computed styles.
    /// </summary>
    public void OptimizeAllStyles()
    {
        StyleEngine?.OptimizeAllStyles();
    }

    private void InitializeComponents()
    {
        if (_context == null)
            return;

        var styleEngine = GetService<StyleEngine>();
        if (styleEngine != null)
        {
            var renderDevice = _context.GetService<IRenderDevice>() ??
                               _serviceProvider.GetService<IRenderDevice>() ??
                               new DefaultRenderDevice();
            styleEngine.RenderDevice = renderDevice;
            styleEngine.OptimizationEnabled = _options.EnableOptimization;
            styleEngine.CollectMetrics = _options.CollectMetrics;
        }

        var lifecycleCoordinator = GetService<DocumentLifecycleCoordinator>();
        var invalidationTracker = GetService<IStyleInvalidationTracker>();
        var recalcScheduler = GetService<IStyleRecalcScheduler>();

        if (invalidationTracker != null && recalcScheduler != null)
        {
            if (recalcScheduler is Observers.IStyleInvalidationObserver observer)
            {
                invalidationTracker.AddObserver(observer);
            }
        }

        if (lifecycleCoordinator != null && styleEngine != null)
        {
            lifecycleCoordinator.AddObserver(styleEngine);
        }

        var stylesheetManager = GetService<IStyleSheetManager>();
        if (stylesheetManager != null && _options.UpdateStylesImmediately)
        {
            stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
        }
    }

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        if (_context?.Active?.DocumentElement != null && StyleEngine != null)
        {
            StyleEngine.UpdateStyles(_context.Active.DocumentElement);
        }
    }

    private void HookDocumentEvents(IDocument document)
    {
        if (document == null)
            return;

        _currentDocument = document;
        _currentDocument.ReadyStateChanged += Document_ReadyStateChanged;

        if (_currentDocument.DefaultView != null)
        {
            _currentDocument.DefaultView.Resized += Window_Resized;
        }

        var lifecycleCoordinator = GetService<DocumentLifecycleCoordinator>();
        if (lifecycleCoordinator != null)
        {
            lifecycleCoordinator.AttachToDocument(_currentDocument);
        }

        if (_currentDocument.ReadyState == DocumentReadyState.Interactive ||
            _currentDocument.ReadyState == DocumentReadyState.Complete)
        {
            if (_currentDocument.DocumentElement != null && StyleEngine != null)
            {
                StyleEngine.UpdateStyles(_currentDocument.DocumentElement);
            }
        }
    }

    private void UnhookDocumentEvents(IDocument document)
    {
        if (document == null)
            return;

        document.ReadyStateChanged -= Document_ReadyStateChanged;

        if (document.DefaultView != null)
        {
            document.DefaultView.Resized -= Window_Resized;
        }

        var lifecycleCoordinator = GetService<DocumentLifecycleCoordinator>();
        if (lifecycleCoordinator != null)
        {
            lifecycleCoordinator.DetachFromDocument(document);
        }
    }

    private void Window_Resized(object? sender, Event e)
    {
        if (StyleEngine == null)
            return;

        if (sender is IWindow window)
        {
            StyleEngine.NotifyViewportChanged(
                window.OuterWidth > 0 ? window.OuterWidth : 1024,
                window.OuterHeight > 0 ? window.OuterHeight : 768);

            if (_options.UpdateStylesImmediately)
            {
                GetService<IStyleRecalcScheduler>()?.ProcessImmediately();
            }
        }
    }

    private void Document_ReadyStateChanged(object? sender, Event e)
    {
        var document = sender as IDocument;
        if (document?.ReadyState == DocumentReadyState.Interactive ||
            document?.ReadyState == DocumentReadyState.Complete)
        {
            if (document.DocumentElement != null && StyleEngine != null)
            {
                StyleEngine.UpdateStyles(document.DocumentElement);
            }
        }
    }

    /// <summary>
    /// Disposes the StyleSystemService and releases associated resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        var stylesheetManager = GetService<IStyleSheetManager>();
        if (stylesheetManager != null)
        {
            stylesheetManager.StylesheetChanged -= StylesheetManager_StylesheetChanged;
        }

        if (_currentDocument != null)
        {
            UnhookDocumentEvents(_currentDocument);
        }

        (StyleEngine as IDisposable)?.Dispose();
        (GetService<IStyleRecalcScheduler>() as IDisposable)?.Dispose();
        (GetService<IStyleTaskScheduler>() as IDisposable)?.Dispose();
        (GetService<IWorkerThreadStylePool>() as IDisposable)?.Dispose();
        (GetService<DomMutationTracker>() as IDisposable)?.Dispose();
        (GetService<IStyleSheetManager>() as IDisposable)?.Dispose();

        _context = null;
        _currentDocument = null;
        _isDisposed = true;
    }
}