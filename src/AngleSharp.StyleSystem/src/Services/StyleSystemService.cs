using System;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using AngleSharp.StyleSystem.Threading;

namespace AngleSharp.StyleSystem.Services;

/// <summary>
/// Provides style system capabilities to AngleSharp.
/// </summary>
public sealed class StyleSystemService : IDisposable
{
    private StyleEngine? _styleEngine;
    private DocumentLifecycleCoordinator? _lifecycleCoordinator;
    private DomMutationTracker? _mutationTracker;
    private StyleRecalcScheduler? _recalcScheduler;
    private IWorkerThreadStylePool? _workerThreadPool;
    private IMainThreadStyleWork? _mainThreadWork;
    private IBrowsingContext? _context;
    private IDocument? _currentDocument;
    private bool _isDisposed;

    /// <summary>
    /// Gets the registered style engine instance.
    /// </summary>
    public IStyleEngine? StyleEngine => GetOrCreateStyleEngine();

    /// <summary>
    /// Gets the document lifecycle coordinator.
    /// </summary>
    public DocumentLifecycleCoordinator? LifecycleCoordinator => _lifecycleCoordinator;

    /// <summary>
    /// Gets whether the style system service has been initialized.
    /// </summary>
    public bool IsInitialized => _styleEngine != null;

    /// <summary>
    /// Gets whether style optimization is enabled.
    /// </summary>
    public bool OptimizationEnabled => _styleEngine?.OptimizationEnabled ?? false;

    /// <summary>
    /// Gets the associated browsing context.
    /// </summary>
    public IBrowsingContext? Context => _context;

    /// <summary>
    /// Initializes the style system for the given browsing context.
    /// </summary>
    /// <param name="context">The browsing context to use.</param>
    public void Initialize(IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // If already initialized with this context, just return
        if (_context == context && _styleEngine != null)
            return;

        // If switching contexts, clean up old resources
        if (_context != null && _context != context)
        {
            CleanupCurrentContext();
        }

        _context = context;

        // Create or update components
        InitializeComponents();

        // Watch for document changes
        HookDocumentEvents();
    }

    private void InitializeComponents()
    {
        if (_context == null)
            return;

        // Create the StyleEngine if needed
        if (_styleEngine == null)
        {
            _styleEngine = new StyleEngine(_context);
            _styleEngine.OptimizationEnabled = true;
        }

        // Create the mutation tracker to observe DOM changes
        if (_mutationTracker == null)
        {
            _mutationTracker = new DomMutationTracker(_context, _styleEngine.InvalidationTracker);
        }

        // Create the work schedulers
        if (_mainThreadWork == null)
        {
            _mainThreadWork = new MainThreadStyleWork(_styleEngine);
        }

        if (_workerThreadPool == null)
        {
            _workerThreadPool = new WorkerThreadStylePool(_styleEngine);
        }

        // Create the recalc scheduler
        if (_recalcScheduler == null)
        {
            _recalcScheduler = new StyleRecalcScheduler(
                _styleEngine,
                _context,
                _mainThreadWork,
                _workerThreadPool);

            // Connect the invalidation tracker to the scheduler
            if (_styleEngine.InvalidationTracker is StyleInvalidationTracker invalidationTracker)
            {
                invalidationTracker.StyleRecalcScheduler = _recalcScheduler;
            }
        }

        // Create the lifecycle coordinator
        if (_lifecycleCoordinator == null)
        {
            _lifecycleCoordinator = new DocumentLifecycleCoordinator(
                _context,
                _styleEngine,
                _mutationTracker);
        }
    }

    private void HookDocumentEvents()
    {
        // Update the current document and hook events
        var activeDocument = _context?.Active;
        if (activeDocument != null && activeDocument != _currentDocument)
        {
            // Clean up old document events
            if (_currentDocument != null)
            {
                _currentDocument.ReadyStateChanged -= Document_ReadyStateChanged;

                if (_currentDocument.DefaultView != null)
                {
                    UnhookWindowEvents(_currentDocument.DefaultView);
                }
            }

            // Set up new document events
            _currentDocument = activeDocument;
            _currentDocument.ReadyStateChanged += Document_ReadyStateChanged;

            if (_currentDocument.DefaultView != null)
            {
                HookWindowEvents(_currentDocument.DefaultView);
            }

            // Initialize styles if document is ready
            if (_currentDocument.ReadyState == DocumentReadyState.Interactive ||
                _currentDocument.ReadyState == DocumentReadyState.Complete)
            {
                if (_currentDocument.DocumentElement != null && _styleEngine != null)
                {
                    _styleEngine.UpdateStyles(_currentDocument.DocumentElement);
                }
            }
        }
    }

    private void HookWindowEvents(IWindow window)
    {
        // Hook window resize event for viewport units
        window.Resized += Window_Resized;
    }

    private void UnhookWindowEvents(IWindow window)
    {
        window.Resized -= Window_Resized;
    }

    /// <summary>
    /// Lazy initialization of the style engine when needed
    /// </summary>
    private IStyleEngine? GetOrCreateStyleEngine()
    {
        if (_styleEngine == null && _context != null)
        {
            InitializeComponents();
            HookDocumentEvents();
        }

        return _styleEngine;
    }

    /// <summary>
    /// Enables or disables style optimization.
    /// </summary>
    /// <param name="enable">Whether to enable optimization.</param>
    /// <param name="collectMetrics">Whether to collect optimization metrics.</param>
    public void SetOptimization(bool enable, bool collectMetrics = false)
    {
        if (_styleEngine != null)
        {
            _styleEngine.OptimizationEnabled = enable;
            _styleEngine.CollectMetrics = collectMetrics;
        }
    }

    /// <summary>
    /// Gets the current optimization metrics if metrics collection is enabled.
    /// </summary>
    /// <returns>The optimization metrics or null if not available.</returns>
    public OptimizationMetrics? GetOptimizationMetrics()
    {
        return _styleEngine?.GetOptimizationMetrics();
    }

    /// <summary>
    /// Forces a style update for all elements in the document.
    /// </summary>
    /// <param name="context">The browsing context to update.</param>
    public void ForceStyleUpdate(IBrowsingContext context)
    {
        if (context?.Active?.DocumentElement == null)
            return;

        // Initialize with this context if needed
        if (_context != context)
        {
            Initialize(context);
        }

        _styleEngine?.UpdateStyles(context.Active.DocumentElement);
    }

    /// <summary>
    /// Called when the active document changes in the browsing context.
    /// </summary>
    /// <param name="document">The new active document.</param>
    public void NotifyDocumentChanged(IDocument document)
    {
        if (document == null || document == _currentDocument)
            return;

        HookDocumentEvents();
    }

    private void Window_Resized(object? sender, Event e)
    {
        if (_styleEngine == null)
            return;

        // Update the render device viewport dimensions
        if (sender is IWindow window)
        {
            _styleEngine.NotifyViewportChanged(
                window.OuterWidth > 0 ? window.OuterWidth : 1024,
                window.OuterHeight > 0 ? window.OuterHeight : 768);

            // Force recalculation for elements with viewport-dependent styles
            _recalcScheduler?.ProcessImmediately();
        }
    }

    private void Document_ReadyStateChanged(object? sender, Event e)
    {
        var document = sender as IDocument;
        if (document?.ReadyState == DocumentReadyState.Interactive ||
            document?.ReadyState == DocumentReadyState.Complete)
        {
            if (document.DocumentElement != null && _styleEngine != null)
            {
                _styleEngine.UpdateStyles(document.DocumentElement);
            }
        }
    }

    private void CleanupCurrentContext()
    {
        if (_currentDocument != null)
        {
            _currentDocument.ReadyStateChanged -= Document_ReadyStateChanged;

            if (_currentDocument.DefaultView != null)
            {
                UnhookWindowEvents(_currentDocument.DefaultView);
            }

            _currentDocument = null;
        }
    }

    /// <summary>
    /// Disposes all resources used by the StyleSystemService.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        CleanupCurrentContext();
        _recalcScheduler?.Dispose();
        _workerThreadPool?.Dispose();
        _mutationTracker?.Dispose();
        _styleEngine?.Dispose();

        _context = null;
        _currentDocument = null;
        _styleEngine = null;
        _mutationTracker = null;
        _mainThreadWork = null;
        _workerThreadPool = null;
        _recalcScheduler = null;
        _lifecycleCoordinator = null;

        _isDisposed = true;
    }
}