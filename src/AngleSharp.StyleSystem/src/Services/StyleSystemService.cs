using System;
using System.Collections.Generic;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Observers;
using AngleSharp.StyleSystem.Storage;
using AngleSharp.StyleSystem.Tasks;
using AngleSharp.StyleSystem.Threading;

namespace AngleSharp.StyleSystem.Services
{
    using Properties;

    /// <summary>
    /// Entry point for using the style system, implementing the observer pattern and task-based architecture.
    /// </summary>
    public sealed class StyleSystemService : IDisposable
    {
        #region Fields
        // Core components
        private StyleEngine? _styleEngine;
        private DocumentLifecycleCoordinator? _lifecycleCoordinator;
        private DomMutationTracker? _mutationTracker;

        // Style invalidation and scheduling
        private StyleInvalidationTracker? _invalidationTracker;
        private StyleRecalcScheduler? _recalcScheduler;
        private StyleTaskScheduler? _taskScheduler;

        // Threading components
        private IWorkerThreadStylePool? _workerThreadPool;
        private IMainThreadStyleWork? _mainThreadWork;

        // Style computation pipeline
        private StyleTreeResolver? _styleTreeResolver;
        private StylePropertyMapper? _stylePropertyMapper;
        private ValueCalculator? _valueCalculator;
        private VariableResolver? _variableResolver;
        private RuleCollector? _ruleCollector;
        private CascadeResolver? _cascadeResolver;
        private InheritanceProcessor? _inheritanceProcessor;
        private ComputedStyleBuilder? _computedStyleBuilder;

        // Storage components
        private PropertyTreeManager? _propertyTreeManager;
        private StyleCache? _styleCache;
        private StyleSheetManager? _stylesheetManager;

        // Context and state
        private IBrowsingContext? _context;
        private IDocument? _currentDocument;
        private bool _isDisposed;
        private readonly object _initLock = new object();

        // Configuration
        private StyleSystemOptions _options = new StyleSystemOptions();
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new StyleSystemService.
        /// </summary>
        public StyleSystemService()
        {
        }

        /// <summary>
        /// Creates a new StyleSystemService with the specified options.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public StyleSystemService(StyleSystemOptions options)
        {
            _options = options ?? new StyleSystemOptions();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the style engine.
        /// </summary>
        public IStyleEngine? StyleEngine => GetOrCreateStyleEngine();

        /// <summary>
        /// Gets the document lifecycle coordinator.
        /// </summary>
        public DocumentLifecycleCoordinator? LifecycleCoordinator => _lifecycleCoordinator;

        /// <summary>
        /// Gets whether the system is initialized.
        /// </summary>
        public bool IsInitialized => _styleEngine != null;

        /// <summary>
        /// Gets whether optimization is enabled.
        /// </summary>
        public bool OptimizationEnabled => _styleEngine?.OptimizationEnabled ?? false;

        /// <summary>
        /// Gets the browsing context.
        /// </summary>
        public IBrowsingContext? Context => _context;
        #endregion

        #region Initialization Methods
        /// <summary>
        /// Initializes the style system.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        public void Initialize(IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_initLock)
            {
                if (_context == context && _styleEngine != null)
                    return;

                if (_context != null && _context != context)
                {
                    CleanupCurrentContext();
                }

                _context = context;
                InitializeComponents();
                HookDocumentEvents();
            }
        }

        /// <summary>
        /// Initializes all components and their dependencies.
        /// </summary>
        private void InitializeComponents()
        {
            if (_context == null)
                return;

            // Create or get core services
            var renderDevice = _context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();

            // Initialize components in dependency order

            // 1. Storage components (no dependencies)
            _propertyTreeManager ??= new PropertyTreeManager();
            _styleCache ??= new StyleCache();
            _stylesheetManager ??= new StyleSheetManager(_context, _options.LoadUserAgentStylesheets);

            // 2. Core engine and observers
            _styleEngine ??= new StyleEngine(_context);
            _styleEngine.RenderDevice = renderDevice;
            _styleEngine.OptimizationEnabled = _options.EnableOptimization;
            _styleEngine.CollectMetrics = _options.CollectMetrics;

            // 3. Style computation components
            _stylePropertyMapper ??= new StylePropertyMapper();
            _valueCalculator ??= new ValueCalculator(_context, _styleEngine.RenderDevice);
            _variableResolver ??= new VariableResolver(_context);
            _ruleCollector ??= new RuleCollector(_context, _stylesheetManager);
            _cascadeResolver ??= new CascadeResolver(_context);
            _inheritanceProcessor ??= new InheritanceProcessor(_context);

            // 4. Invalidation tracking
            _invalidationTracker ??= new StyleInvalidationTracker();

            // 5. Style application strategy
            var styleApplicationStrategy = new BasicStyleApplicationStrategy(_styleEngine);

            // 6. Create components with dependencies
            _computedStyleBuilder ??= new ComputedStyleBuilder(
                _context,
                _styleEngine,
                _variableResolver,
                _valueCalculator,
                _stylePropertyMapper,
                _propertyTreeManager,
                _styleEngine.RenderDevice);

            _styleTreeResolver ??= new StyleTreeResolver(
                _styleEngine,
                styleApplicationStrategy,
                _styleCache,
                _invalidationTracker);

            // 7. Threading and scheduling components
            _mutationTracker ??= new DomMutationTracker(_context, _invalidationTracker);
            _mainThreadWork ??= new MainThreadStyleWork(_styleEngine);

            // Only create worker thread pool if enabled
            if (_options.MaxWorkerThreads > 0)
            {
                _workerThreadPool ??= new WorkerThreadStylePool(_styleEngine, _options.MaxWorkerThreads);
            }

            _taskScheduler ??= new StyleTaskScheduler(
                _styleEngine,
                _options.ThrottleIntervalMs,
                _options.BatchSize);

            _recalcScheduler ??= new StyleRecalcScheduler(
                _styleEngine,
                _context,
                _taskScheduler,
                _mainThreadWork,
                _workerThreadPool);

            // 8. Document lifecycle coordinator
            _lifecycleCoordinator ??= new DocumentLifecycleCoordinator(
                _context,
                _mutationTracker);

            // Connect observers
            _invalidationTracker.AddObserver(_recalcScheduler);
            _lifecycleCoordinator.AddObserver(_styleEngine);
            _styleEngine.AddComputationObserver(_styleEngine);  // Self-observation for optimization

            // Inject dependencies into StyleEngine
            _styleEngine.SetInvalidationTracker(_invalidationTracker);
            _styleEngine.SetStyleTreeResolver(_styleTreeResolver);
            _styleEngine.SetStylesheetManager(_stylesheetManager);
            _styleEngine.SetValueCalculator(_valueCalculator);
            _styleEngine.SetVariableResolver(_variableResolver);
            _styleEngine.SetCascadeResolver(_cascadeResolver);
            _styleEngine.SetRuleCollector(_ruleCollector);
            _styleEngine.SetInheritanceProcessor(_inheritanceProcessor);
            _styleEngine.SetComputedStyleBuilder(_computedStyleBuilder);
            _styleEngine.SetPropertyTreeManager(_propertyTreeManager);
            _styleEngine.SetStyleCache(_styleCache);

            // If immediate style updates are enabled, connect stylesheet changes to updates
            if (_options.UpdateStylesImmediately)
            {
                _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
            }
        }

        private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
        {
            if (_context?.Active?.DocumentElement != null && _styleEngine != null)
            {
                _styleEngine.UpdateStyles(_context.Active.DocumentElement);
            }
        }
        #endregion

        #region Configuration Methods
        /// <summary>
        /// Configures optimization settings.
        /// </summary>
        /// <param name="enable">Whether to enable optimization.</param>
        /// <param name="collectMetrics">Whether to collect metrics.</param>
        public void SetOptimization(bool enable, bool collectMetrics = false)
        {
            _options.EnableOptimization = enable;
            _options.CollectMetrics = collectMetrics;

            if (_styleEngine != null)
            {
                _styleEngine.OptimizationEnabled = enable;
                _styleEngine.CollectMetrics = collectMetrics;
            }
        }

        /// <summary>
        /// Gets optimization metrics.
        /// </summary>
        /// <returns>The optimization metrics or null if not available.</returns>
        public OptimizationMetrics? GetOptimizationMetrics()
        {
            return _styleEngine?.GetOptimizationMetrics();
        }

        /// <summary>
        /// Gets a service from the style system.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service or null if not available.</returns>
        public T? GetService<T>() where T : class
        {
            return GetServiceInternal(typeof(T)) as T;
        }

        private object? GetServiceInternal(Type serviceType)
        {
            if (serviceType == typeof(IStyleEngine))
                return _styleEngine;

            if (serviceType == typeof(IStyleInvalidationTracker))
                return _invalidationTracker;

            if (serviceType == typeof(IStyleTreeResolver))
                return _styleTreeResolver;

            if (serviceType == typeof(IStyleSheetManager))
                return _stylesheetManager;

            if (serviceType == typeof(IValueCalculator))
                return _valueCalculator;

            if (serviceType == typeof(IVariableResolver))
                return _variableResolver;

            if (serviceType == typeof(ICascadeResolver))
                return _cascadeResolver;

            if (serviceType == typeof(IRuleCollector))
                return _ruleCollector;

            if (serviceType == typeof(IInheritanceProcessor))
                return _inheritanceProcessor;

            if (serviceType == typeof(IComputedStyleBuilder))
                return _computedStyleBuilder;

            if (serviceType == typeof(IPropertyTreeManager))
                return _propertyTreeManager;

            if (serviceType == typeof(IStyleCache))
                return _styleCache;

            if (serviceType == typeof(IStyleTaskScheduler))
                return _taskScheduler;

            if (serviceType == typeof(IStyleRecalcScheduler))
                return _recalcScheduler;

            if (serviceType == typeof(IMainThreadStyleWork))
                return _mainThreadWork;

            if (serviceType == typeof(IWorkerThreadStylePool))
                return _workerThreadPool;

            return null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Forces a style update for the document.
        /// </summary>
        /// <param name="context">The browsing context containing the document.</param>
        public void ForceStyleUpdate(IBrowsingContext context)
        {
            if (context?.Active?.DocumentElement == null)
                return;

            if (_context != context)
            {
                Initialize(context);
            }

            _styleEngine?.UpdateStyles(context.Active.DocumentElement);
        }

        /// <summary>
        /// Notifies the system of a document change.
        /// </summary>
        /// <param name="document">The changed document.</param>
        public void NotifyDocumentChanged(IDocument document)
        {
            if (document == null || document == _currentDocument)
                return;

            HookDocumentEvents();
        }

        /// <summary>
        /// Processes any pending style tasks immediately.
        /// </summary>
        public void ProcessPendingStyleTasksImmediately()
        {
            _recalcScheduler?.ProcessImmediately();
        }

        /// <summary>
        /// Asynchronously processes pending style tasks.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async System.Threading.Tasks.Task ProcessPendingStyleTasksAsync(
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (_recalcScheduler != null)
            {
                await _recalcScheduler.ProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Cancels any pending style tasks.
        /// </summary>
        public void CancelPendingStyleTasks()
        {
            _recalcScheduler?.CancelPendingWork();
        }

        /// <summary>
        /// Gets whether there are pending style tasks.
        /// </summary>
        public bool HasPendingStyleTasks => _recalcScheduler?.HasPendingWork ?? false;

        /// <summary>
        /// Clears the style cache.
        /// </summary>
        public void ClearStyleCache()
        {
            _styleCache?.Clear();
        }

        /// <summary>
        /// Optimizes all computed styles.
        /// </summary>
        public void OptimizeAllStyles()
        {
            _styleEngine?.OptimizeAllStyles();
        }
        #endregion

        #region Private Methods
        private IStyleEngine? GetOrCreateStyleEngine()
        {
            if (_styleEngine == null && _context != null)
            {
                lock (_initLock)
                {
                    if (_styleEngine == null && _context != null)
                    {
                        InitializeComponents();
                        HookDocumentEvents();
                    }
                }
            }

            return _styleEngine;
        }

        private void HookDocumentEvents()
        {
            var activeDocument = _context?.Active;
            if (activeDocument != null && activeDocument != _currentDocument)
            {
                if (_currentDocument != null)
                {
                    _currentDocument.ReadyStateChanged -= Document_ReadyStateChanged;
                    if (_currentDocument.DefaultView != null)
                    {
                        UnhookWindowEvents(_currentDocument.DefaultView);
                    }
                }

                _currentDocument = activeDocument;
                _currentDocument.ReadyStateChanged += Document_ReadyStateChanged;

                if (_currentDocument.DefaultView != null)
                {
                    HookWindowEvents(_currentDocument.DefaultView);
                }

                if (_lifecycleCoordinator != null)
                {
                    _lifecycleCoordinator.AttachToDocument(_currentDocument);
                }

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
            window.Resized += Window_Resized;
        }

        private void UnhookWindowEvents(IWindow window)
        {
            window.Resized -= Window_Resized;
        }

        private void Window_Resized(object? sender, Event e)
        {
            if (_styleEngine == null)
                return;

            if (sender is IWindow window)
            {
                _styleEngine.NotifyViewportChanged(
                    window.OuterWidth > 0 ? window.OuterWidth : 1024,
                    window.OuterHeight > 0 ? window.OuterHeight : 768);

                if (_options.UpdateStylesImmediately)
                {
                    _recalcScheduler?.ProcessImmediately();
                }
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

                if (_lifecycleCoordinator != null && _currentDocument != null)
                {
                    _lifecycleCoordinator.DetachFromDocument(_currentDocument);
                }

                _currentDocument = null;
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Disposes the style system.
        /// </summary>
        public void Dispose()
        {
            lock (_initLock)
            {
                if (_isDisposed)
                    return;

                CleanupCurrentContext();

                // Unregister event handlers
                if (_stylesheetManager != null)
                {
                    _stylesheetManager.StylesheetChanged -= StylesheetManager_StylesheetChanged;
                }

                // Dispose all disposable components
                _recalcScheduler?.Dispose();
                _taskScheduler?.Dispose();
                _workerThreadPool?.Dispose();
                _mutationTracker?.Dispose();
                _styleEngine?.Dispose();
                _stylesheetManager?.Dispose();

                // Clear all component references
                _context = null;
                _currentDocument = null;
                _styleEngine = null;
                _mutationTracker = null;
                _invalidationTracker = null;
                _mainThreadWork = null;
                _workerThreadPool = null;
                _recalcScheduler = null;
                _taskScheduler = null;
                _lifecycleCoordinator = null;
                _styleTreeResolver = null;
                _stylePropertyMapper = null;
                _valueCalculator = null;
                _variableResolver = null;
                _ruleCollector = null;
                _cascadeResolver = null;
                _inheritanceProcessor = null;
                _computedStyleBuilder = null;
                _propertyTreeManager = null;
                _styleCache = null;
                _stylesheetManager = null;

                _isDisposed = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Options for configuring the style system.
    /// </summary>
    public class StyleSystemOptions
    {
        /// <summary>
        /// Gets or sets whether style optimization is enabled.
        /// </summary>
        public bool EnableOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect optimization metrics.
        /// </summary>
        public bool CollectMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to update styles immediately on changes.
        /// </summary>
        public bool UpdateStylesImmediately { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of worker threads for style computation.
        /// </summary>
        /// <remarks>
        /// Set to 0 to disable worker threads.
        /// </remarks>
        public int MaxWorkerThreads { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to load user agent stylesheets.
        /// </summary>
        public bool LoadUserAgentStylesheets { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval in milliseconds for throttling style calculations.
        /// </summary>
        public int ThrottleIntervalMs { get; set; } = 16;

        /// <summary>
        /// Gets or sets the batch size for style tasks.
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}