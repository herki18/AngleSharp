using System;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AngleSharp.StyleSystem.Services
{
    using Computation;
    using Observers;
    using Tasks;

    /// <summary>
    /// Main service that orchestrates StyleSystem components and integrates with AngleSharp.
    /// </summary>
    public sealed class StyleSystemService : IDisposable
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly StyleSystemOptions _options;
        private IBrowsingContext? _context;
        private IDocument? _currentDocument;
        private bool _isDisposed;
        private readonly object _initLock = new object();
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new StyleSystemService with the specified service provider and options.
        /// </summary>
        /// <param name="serviceProvider">The DI container's service provider.</param>
        /// <param name="options">The StyleSystem options.</param>
        public StyleSystemService(IServiceProvider serviceProvider, StyleSystemOptions options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? new StyleSystemOptions();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the StyleEngine from the service provider.
        /// </summary>
        public IStyleEngine? StyleEngine => GetService<IStyleEngine>();

        /// <summary>
        /// Gets the DocumentLifecycleCoordinator from the service provider.
        /// </summary>
        public IDocumentLifecycleCoordinator? LifecycleCoordinator => GetService<IDocumentLifecycleCoordinator>();

        /// <summary>
        /// Gets a value indicating whether StyleSystem is initialized.
        /// </summary>
        public bool IsInitialized => StyleEngine != null && _context != null;

        /// <summary>
        /// Gets a value indicating whether style optimization is enabled.
        /// </summary>
        public bool OptimizationEnabled => StyleEngine?.OptimizationEnabled ?? false;

        /// <summary>
        /// Gets the current browsing context.
        /// </summary>
        public IBrowsingContext? Context => _context;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the StyleSystem with the specified browsing context.
        /// </summary>
        /// <param name="context">The browsing context to initialize with.</param>
        public void Initialize(IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_initLock)
            {
                if (_context == context && StyleEngine != null)
                    return;

                if (_context != null && _context != context)
                {
                    CleanupContext(_context);
                }

                _context = context;

                InitializeComponents();
                HookDocumentEvents();
            }
        }

        /// <summary>
        /// Cleans up resources associated with the specified context.
        /// </summary>
        /// <param name="context">The context to clean up.</param>
        public void CleanupContext(IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_currentDocument != null)
            {
                _currentDocument.ReadyStateChanged -= Document_ReadyStateChanged;

                if (_currentDocument.DefaultView != null)
                {
                    UnhookWindowEvents(_currentDocument.DefaultView);
                }

                var lifecycleCoordinator = GetService<DocumentLifecycleCoordinator>();
                if (lifecycleCoordinator != null)
                {
                    lifecycleCoordinator.DetachFromDocument(_currentDocument);
                }

                _currentDocument = null;
            }
        }

        /// <summary>
        /// Forces a style update for all elements in the context.
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
        /// Notifies StyleSystem that the document has changed.
        /// </summary>
        /// <param name="document">The new document.</param>
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
            GetService<IStyleRecalcScheduler>()?.ProcessImmediately();
        }

        /// <summary>
        /// Gets optimization metrics from the StyleSystem.
        /// </summary>
        /// <returns>The optimization metrics.</returns>
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
        /// Optimizes all styles in the system.
        /// </summary>
        public void OptimizeAllStyles()
        {
            StyleEngine?.OptimizeAllStyles();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Gets a service from the DI container.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service instance or null if not registered.</returns>
        private T? GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Initializes StyleSystem components with the current context.
        /// </summary>
        private void InitializeComponents()
        {
            if (_context == null)
                return;

            // Configure StyleEngine
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

            // Configure StyleTreeResolver
            var styleTreeResolver = GetService<StyleTreeResolver>();
            if (styleTreeResolver != null)
            {
                // Any configuration needed
            }

            // Setup lifecycle coordinator
            var lifecycleCoordinator = GetService<DocumentLifecycleCoordinator>();
            var invalidationTracker = GetService<IStyleInvalidationTracker>();
            var recalcScheduler = GetService<IStyleRecalcScheduler>();

            // Connect observers
            if (invalidationTracker != null && recalcScheduler != null)
            {
                if (recalcScheduler is IStyleInvalidationObserver observer)
                {
                    invalidationTracker.AddObserver(observer);
                }
            }

            if (lifecycleCoordinator != null && styleEngine != null)
            {
                lifecycleCoordinator.AddObserver(styleEngine);
            }

            // Connect stylesheet manager
            var stylesheetManager = GetService<IStyleSheetManager>();
            if (stylesheetManager != null && _options.UpdateStylesImmediately)
            {
                stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
            }
        }

        /// <summary>
        /// Handles stylesheet changes.
        /// </summary>
        private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
        {
            if (_context?.Active?.DocumentElement != null && StyleEngine != null)
            {
                StyleEngine.UpdateStyles(_context.Active.DocumentElement);
            }
        }

        /// <summary>
        /// Hooks up events for the active document.
        /// </summary>
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
        }

        /// <summary>
        /// Hooks window events.
        /// </summary>
        private void HookWindowEvents(IWindow window)
        {
            window.Resized += Window_Resized;
        }

        /// <summary>
        /// Unhooks window events.
        /// </summary>
        private void UnhookWindowEvents(IWindow window)
        {
            window.Resized -= Window_Resized;
        }

        /// <summary>
        /// Handles window resize events.
        /// </summary>
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

        /// <summary>
        /// Handles document ready state changes.
        /// </summary>
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
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Releases all resources used by the StyleSystemService.
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

            if (_context != null)
            {
                CleanupContext(_context);
            }

            // Dispose services that implement IDisposable
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
        #endregion
    }
}