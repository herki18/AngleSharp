using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks; // Added for Task
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.StyleSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LayoutEngine
{
    /// <summary>
    /// Main implementation of the LayoutEngine rendering system.
    /// Orchestrates subsystems (Style, Layout) and manages the document lifecycle.
    /// Relies on the host calling ProcessPendingUpdates periodically.
    /// </summary>
    public class LayoutEngineMain : ILayoutEngineMain // Interface updated previously
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ICacheManager _cacheManager;
        private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
        private readonly IThreadingCoordinator _threadingCoordinator; // Kept but might be unused with simplified scheduler
        private readonly IUpdateScheduler _updateScheduler;
        // IFrameScheduler is no longer directly used for the main processing loop in Option 4
        // private readonly IFrameScheduler _frameScheduler;
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

        // Constructor: Keep IFrameScheduler for now if other parts might use it, but it's not driving the loop.
        public LayoutEngineMain(
            IEventAggregator eventAggregator,
            ICacheManager cacheManager,
            IDocumentLifecycleCoordinator lifecycleCoordinator,
            IThreadingCoordinator threadingCoordinator,
            IUpdateScheduler updateScheduler,
            IFrameScheduler frameScheduler, // Keep param for now
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
            // _frameScheduler = frameScheduler; // Store if needed elsewhere, but not for main loop
            _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
            _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
            _logger = logger ?? NullLogger<LayoutEngineMain>.Instance;
            _configuration = configuration ?? LayoutEngineConfiguration.Default;

            _browsingContext = AngleSharp.BrowsingContext.New(_configuration.AngleSharpConfiguration);

            // Subscribe to events needed for orchestration
            _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
            _subscriptions.Add(_eventAggregator.Subscribe<LayoutUpdatedEvent>(OnLayoutUpdated));
            // Add RenderCompleted when needed
            _subscriptions.Add(_eventAggregator.Subscribe<UpdateProcessedEvent>(OnUpdateProcessed));
            _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
            _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
        }

        // --- Properties ---
        public IBrowsingContext BrowsingContext => _browsingContext;
        public IDocument? Document => _document;
        public DocumentLifecyclePhase CurrentPhase => _lifecycleCoordinator.CurrentPhase;
        public Rect Viewport => _currentViewport;
        public bool IsInitialized => _isInitialized;

        // --- Document Loading ---
        public IDocument Open(string html, string? baseUrl = null)
        {
            // Uses InitializeAsync internally now
            return OpenAsync(html, baseUrl).GetAwaiter().GetResult();
        }

        public async Task<IDocument> OpenAsync(string html, string? baseUrl = null)
        {
            ThrowIfDisposed();
            if (_isInitialized) await ShutdownAsync();
            _logger.LogInformation("Opening document from HTML string");
            try
            {
                var document = await _browsingContext.OpenAsync(req => req.Content(html).Address(baseUrl ?? "about:blank"));
                await InitializeAsync(document);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening document from HTML string");
                throw;
            }
        }

        public IDocument OpenFile(string filePath)
        {
             // Uses InitializeAsync internally now
            return OpenFileAsync(filePath).GetAwaiter().GetResult();
        }

         public async Task<IDocument> OpenFileAsync(string filePath)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("HTML file not found", filePath);
            if (_isInitialized) await ShutdownAsync();
            _logger.LogInformation("Opening document from file: {FilePath}", filePath);
            try
            {
                var document = await _browsingContext.OpenAsync(filePath);
                await InitializeAsync(document);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening document from file: {FilePath}", filePath);
                throw;
            }
        }

        // --- Initialization and Shutdown (Now Async) ---
        public async Task InitializeAsync(IDocument document)
        {
            ThrowIfDisposed();
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (_isInitialized)
            {
                _logger.LogWarning("InitializeAsync called while already initialized. Shutting down previous document first.");
                await ShutdownAsync();
            }
            _logger.LogInformation("Initializing LayoutEngine for document: {DocumentUrl}", document.Url);
            _document = document;
            try
            {
                await _styleEngine.InitializeAsync(document);
                await _layoutEngine.InitializeAsync(document);

                if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.StyleClean))
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                else
                    _logger.LogWarning("Could not transition from Inactive to StyleClean upon initialization.");

                _isInitialized = true;
                _logger.LogInformation("LayoutEngine initialized successfully.");
                ProcessFullDocument(); // Schedule initial update
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LayoutEngine initialization.");
                Cleanup();
                throw;
            }
        }

         // Sync version for interface compatibility (use with caution)
        [Obsolete("Prefer InitializeAsync to avoid potential blocking.", false)]
        public void Initialize(IDocument document) => InitializeAsync(document).GetAwaiter().GetResult();

        public async Task ShutdownAsync()
        {
            ThrowIfDisposed();
            if (!_isInitialized) return;
            _logger.LogInformation("Shutting down LayoutEngine...");
            var initialPhase = _lifecycleCoordinator.CurrentPhase; // Capture phase before shutdown

            try
            {
                if (initialPhase != DocumentLifecyclePhase.Disposed)
                {
                    if (_lifecycleCoordinator.IsValidTransition(initialPhase, DocumentLifecyclePhase.Disposed))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.Disposed);
                    else
                        _logger.LogWarning("Could not transition from {CurrentPhase} to Disposed during shutdown.", initialPhase);
                }

                await _layoutEngine.ShutdownAsync();
                await _styleEngine.ShutdownAsync();
                _logger.LogInformation("Subsystems shut down.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LayoutEngine shutdown process.");
            }
            finally
            {
                Cleanup();
                _logger.LogInformation("LayoutEngine shutdown complete.");
            }
        }

        // Sync version for interface compatibility (use with caution)
        [Obsolete("Prefer ShutdownAsync to avoid potential blocking.", false)]
        public void Shutdown() => ShutdownAsync().GetAwaiter().GetResult();

        // --- Core API Methods ---
        public IDocument CreateDocument(string html)
        {
            ThrowIfDisposed();
            var parser = new HtmlParser();
            return parser.ParseDocument(html);
        }

        public IComputedStyle GetComputedStyle(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Try cache first (engine checks dirty state)
            var cachedStyle = _styleEngine.GetCachedStyle(element);
            if (cachedStyle != null) return cachedStyle;

            _logger.LogDebug("Style not cached/dirty for {Tag}. Requesting computation (mock computes directly).", element.TagName);
            // Mock engine computes directly. Real engine would need ProcessPendingUpdates called by host.
            // We might need to force processing in tests or simple hosts.
            return _styleEngine.ComputeStyleAsync(element).GetAwaiter().GetResult();
        }

        public ILayoutBox GetLayoutBox(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Ensure styles are ready first
            GetComputedStyle(element);

            // Try layout cache (engine checks dirty state)
            var cachedLayout = _layoutEngine.GetCachedLayout(element);
            if (cachedLayout != null) return cachedLayout;

            _logger.LogDebug("Layout not cached/dirty for {Tag}. Requesting computation (mock computes directly).", element.TagName);
            // Mock engine computes directly. Real engine would need ProcessPendingUpdates called by host.
            return _layoutEngine.ComputeLayoutAsync(element).GetAwaiter().GetResult();
        }

        [Obsolete("Prefer calling ProcessPendingUpdates with a specific budget in a loop.", false)]
        public void ProcessUpdates()
        {
            _logger.LogWarning("Deprecated ProcessUpdates() called. Processing all pending updates without budget.");
            ProcessPendingUpdates(double.MaxValue);
        }

        public void ProcessFullDocument()
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (_document?.DocumentElement == null) return;

            _logger.LogInformation("Scheduling full document update.");
            var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Full, _document.DocumentElement);
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);
            // Processing happens via host calls to ProcessPendingUpdates
        }

        public void SetViewportSize(float width, float height)
        {
            ThrowIfDisposed();
            if (Math.Abs(_currentViewport.Width - width) < 0.01f && Math.Abs(_currentViewport.Height - height) < 0.01f)
                return;

            var oldViewport = _currentViewport;
            _currentViewport = new Rect(0, 0, width, height);
            _logger.LogInformation("Viewport set to {W}x{H}. Notifying LayoutEngine and scheduling layout update.", width, height);

            _layoutEngine.SetViewportSize(width, height); // Layout engine invalidates internally

            if (_document?.DocumentElement != null && _isInitialized)
            {
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Layout, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            _eventAggregator.Publish(new ViewportSizeChangedEvent(oldViewport, _currentViewport));
        }

        public IElement? ElementFromPoint(float x, float y)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            _logger.LogDebug("Hit testing at ({X}, {Y}). Layout must be up-to-date.", x, y);
            // Assume host calls ProcessPendingUpdates regularly, so layout should be current.
            // If the mock GetLayoutTree still forces processing, that's okay for now.
            // _layoutEngine.GetLayoutTree(); // Might be needed if mock doesn't rely on host loop
            return _layoutEngine.ElementFromPoint(x, y);
        }

        public string AddStyleSheet(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
        {
            ThrowIfDisposed(); EnsureInitialized();
            _logger.LogInformation("Adding stylesheet (Origin: {Origin}). Scheduling style update.", origin);
            var styleSheetId = _styleEngine.AddStyleSheetAsync(styleSheet, origin, mediaQuery).GetAwaiter().GetResult(); // Style engine invalidates internally
            if (_document?.DocumentElement != null)
            {
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Style, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            return styleSheetId;
        }

        public bool RemoveStyleSheet(string styleSheetId)
        {
            ThrowIfDisposed(); EnsureInitialized();
            _logger.LogInformation("Removing stylesheet ID: {Id}.", styleSheetId);
            var result = _styleEngine.RemoveStyleSheet(styleSheetId); // Style engine invalidates internally
            if (result && _document?.DocumentElement != null)
            {
                _logger.LogDebug("Stylesheet removed. Scheduling style update.");
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Style, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            return result;
        }

        public void PreloadResource(string url)
        {
            _logger.LogWarning("PreloadResource called for {Url} but is not implemented.", url);
        }

        // --- NEW: Primary processing method called by host ---
        /// <summary>
        /// Processes pending updates within the given time budget by invoking the update scheduler.
        /// </summary>
        public void ProcessPendingUpdates(double timeBudgetMilliseconds)
        {
            ThrowIfDisposed();
            if (!_isInitialized) return; // Don't process if not ready

            _logger.LogTrace("Host requested processing with budget: {Budget}ms", timeBudgetMilliseconds);
            // Delegate directly to the scheduler's budgeted processing method
             _updateScheduler.ProcessUpdates(timeBudgetMilliseconds);
            // If the scheduler needed to be async, we'd await:
            // await _updateScheduler.ProcessUpdatesAsync(timeBudgetMilliseconds);
        }

        #region Private Methods (Cleanup, EnsureInitialized, ThrowIfDisposed)
        private void Cleanup()
        {
            _document = null;
            _isInitialized = false;
            // GC.Collect(); // Generally avoid explicit GC
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized || _document == null)
            {
                throw new InvalidOperationException("LayoutEngine is not initialized. Call InitializeAsync or Open first.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(LayoutEngineMain));
        }
        #endregion

        #region Event Handlers (Orchestration Logic)

        // Event handlers remain largely the same, reacting to *completion* events
        // to manage lifecycle and schedule the *next* type of update.

        private void OnStyleComputed(StyleComputedEvent e)
        {
            if (_isDisposed || !_isInitialized) return;
            _logger.LogInformation("Reacting to StyleComputedEvent ({Count} elements).", e.Elements.Count);

            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
            {
                if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean))
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                else
                     _logger.LogWarning("Could not transition from InStyleRecalc to StyleClean.");
            }

            if (e.Elements.Count > 0 && _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.LayoutCalculation))
            {
                if (_document?.DocumentElement != null)
                {
                    _logger.LogDebug("Scheduling Layout update after StyleComputedEvent.");
                    var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Layout, _document.DocumentElement);
                    _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                }
            }
        }

        private void OnLayoutUpdated(LayoutUpdatedEvent e)
        {
            if (_isDisposed || !_isInitialized) return;
             _logger.LogInformation("Reacting to LayoutUpdatedEvent ({Count} elements).", e.UpdatedElements.Count);

            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InLayout)
            {
                 if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean))
                     _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
                 else
                      _logger.LogWarning("Could not transition from InLayout to LayoutClean.");
            }

            if (e.UpdatedElements.Count > 0 && _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.Rendering))
            {
                if (_document?.DocumentElement != null)
                {
                     _logger.LogDebug("Scheduling Render update after LayoutUpdatedEvent.");
                    var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Render, _document.DocumentElement);
                    _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                }
            }
        }

        // OnUpdateProcessed, OnPhaseChanged, OnResourceError remain useful for logging/debugging
        private void OnUpdateProcessed(UpdateProcessedEvent e)
        {
             if (_isDisposed || !_isInitialized) return;
             string elementInfo = e.Update.Element != null ? $"{e.Update.Element.TagName}#{e.Update.Element.Id ?? "n/a"}" : "document";
             if (e.Success)
                 _logger.LogTrace("Update processed: {Type} for {Elem} ({Time:F2}ms)", e.Update.Type, elementInfo, e.ProcessingTimeMs);
             else
                 _logger.LogError(e.Error, "Error processing update {Type} for {Elem} ({Time:F2}ms)", e.Update.Type, elementInfo, e.ProcessingTimeMs);
        }

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (_isDisposed) return;
            _logger.LogDebug("Aware of lifecycle phase change: {Phase}, {ChangeType}", e.Phase, e.ChangeType);
            // No longer trigger processing directly here; scheduler handles entry into "In..." phases.
        }

        private void OnResourceError(ResourceErrorEvent e)
        {
            if (_isDisposed) return;
            _logger.LogWarning("Resource error for URL {Url}: {Error}", e.Url, e.Error.Message);
        }

        #endregion

        // Dispose method remains mostly the same, calls async Shutdown
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _logger.LogInformation("LayoutEngineMain disposing...");
            try
            {
                if (_isInitialized)
                {
                    ShutdownAsync().GetAwaiter().GetResult(); // Block on async shutdown in Dispose
                }
                foreach (var subscription in _subscriptions) { _eventAggregator.Unsubscribe(subscription); }
                _subscriptions.Clear();
                _browsingContext?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LayoutEngineMain disposal.");
            }
            _logger.LogInformation("LayoutEngineMain disposed.");
        }
    }
}