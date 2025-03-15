using System;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AngleSharp.StyleSystem.Services;
using Tasks;

public sealed class StyleSystemService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StyleSystemOptions _options;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISubscriptionToken[] _subscriptionTokens;
    private IBrowsingContext? _context;
    private IDocument? _currentDocument;
    private bool _isDisposed;
    private readonly object _initLock = new object();

    public StyleSystemService(
        IServiceProvider serviceProvider,
        StyleSystemOptions options,
        IEventAggregator eventAggregator)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new StyleSystemOptions();
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Subscribe to document events
        _subscriptionTokens = new[]
        {
            _eventAggregator.Subscribe<DocumentAttachedEvent>(OnDocumentAttached),
            _eventAggregator.Subscribe<DocumentDetachedEvent>(OnDocumentDetached),
            _eventAggregator.Subscribe<ReadyStateChangedEvent>(OnReadyStateChanged)
        };

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
        }
    }

    private void OnDocumentAttached(DocumentAttachedEvent eventData)
    {
        _currentDocument = eventData.Document;
    }

    private void OnDocumentDetached(DocumentDetachedEvent eventData)
    {
        if (_currentDocument == eventData.Document)
        {
            _currentDocument = null;
        }
    }

    private void OnReadyStateChanged(ReadyStateChangedEvent eventData)
    {
        if (eventData.ReadyState == DocumentReadyState.Interactive ||
            eventData.ReadyState == DocumentReadyState.Complete)
        {
            if (eventData.Document.DocumentElement != null && StyleEngine != null)
            {
                StyleEngine.UpdateStyles(eventData.Document.DocumentElement);
            }
        }
    }

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

    public IStyleEngine? StyleEngine => GetService<IStyleEngine>();
    public IDocumentLifecycleCoordinator? LifecycleCoordinator => GetService<IDocumentLifecycleCoordinator>();
    public bool OptimizationEnabled => StyleEngine?.OptimizationEnabled ?? false;
    public IBrowsingContext? Context => _context;

    public T? GetService<T>() where T : class
    {
        try
        {
            return _serviceProvider.GetService<T>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

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
                InitializeComponents();
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

    public void ProcessPendingStyleTasksImmediately()
    {
        GetService<IStyleRecalcScheduler>()?.ProcessImmediately();
    }

    public OptimizationMetrics? GetOptimizationMetrics()
    {
        return StyleEngine?.GetOptimizationMetrics();
    }

    public void ClearStyleCache()
    {
        GetService<IStyleCache>()?.Clear();
    }

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

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_currentDocument != null)
        {
            UnhookDocumentEvents(_currentDocument);
        }

        // Unsubscribe from all events
        foreach (var token in _subscriptionTokens)
        {
            token.Dispose();
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