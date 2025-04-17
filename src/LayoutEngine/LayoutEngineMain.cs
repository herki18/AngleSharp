using System;
using System.Collections.Generic;
using System.IO;
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
    using Platform.Update;

    /// <summary>
    /// Main implementation of the LayoutEngine rendering system.
    /// </summary>
    public class LayoutEngineMain : ILayoutEngineMain
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ICacheManager _cacheManager;
        private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
        private readonly IThreadingCoordinator _threadingCoordinator;
        private readonly IUpdateScheduler _updateScheduler;
        private readonly IFrameScheduler _frameScheduler;
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
            _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
            _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
            _logger = logger ?? NullLogger<LayoutEngineMain>.Instance;
            _configuration = configuration ?? LayoutEngineConfiguration.Default;

            // Create browsing context with default configuration
            _browsingContext = AngleSharp.BrowsingContext.New(_configuration.AngleSharpConfiguration);

            // Subscribe to completion events from subsystems to drive lifecycle
            _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
            _subscriptions.Add(_eventAggregator.Subscribe<LayoutUpdatedEvent>(OnLayoutUpdated));
            // Add subscription for RenderCompleted if/when a render system exists
            _subscriptions.Add(_eventAggregator.Subscribe<UpdateProcessedEvent>(OnUpdateProcessed)); // Keep for logging/state checks
            _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged)); // Keep for logging/reacting
            // Remove subscriptions that subsystems handle internally or aren't needed for orchestration here
            // _subscriptions.Add(_eventAggregator.Subscribe<ViewportChangedEvent>(OnViewportChanged)); // Let LayoutEngine handle viewport internally
            // _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
            // _subscriptions.Add(_eventAggregator.Subscribe<FragmentTreeUpdatedEvent>(OnFragmentTreeUpdated));
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
        public IDocument Open(string html, string? baseUrl = null)
        {
            ThrowIfDisposed();

            // Shutdown any existing document
            if (_isInitialized)
            {
                Shutdown();
            }

            _logger.LogInformation("Opening document from HTML string");

            try
            {
                // Parse the HTML
                // Note: Since AngleSharp uses async, we'll use GetAwaiter().GetResult() to get synchronous behavior
                var document = _browsingContext.OpenAsync(req => req.Content(html).Address(baseUrl ?? "about:blank"))
                                              .GetAwaiter().GetResult();

                // Initialize the engine with the document
                Initialize(document);

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
        public IDocument OpenFile(string filePath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("HTML file not found", filePath);

            // Shutdown any existing document
            if (_isInitialized)
            {
                Shutdown();
            }

            _logger.LogInformation("Opening document from file: {FilePath}", filePath);

            try
            {
                // Parse the HTML from file
                // Note: Since AngleSharp uses async, we'll use GetAwaiter().GetResult() to get synchronous behavior
                var document = _browsingContext.OpenAsync(filePath).GetAwaiter().GetResult();

                // Initialize the engine with the document
                Initialize(document);

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
        public void Initialize(IDocument document)
        {
            ThrowIfDisposed();
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (_isInitialized) Shutdown();
            _logger.LogInformation("Initializing LayoutEngine");
            _document = document;
            try
            {
                // Initialize subsystems synchronously (as per their current implementation)
                _styleEngine.InitializeAsync(document).GetAwaiter().GetResult();
                _layoutEngine.InitializeAsync(document).GetAwaiter().GetResult();

                // Enter initial StyleClean phase
                if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.StyleClean))
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                else
                    _logger.LogWarning("Could not enter StyleClean phase from Inactive.");


                _isInitialized = true;
                _logger.LogInformation("LayoutEngine initialized successfully");

                // Schedule a full initial processing of the document
                ProcessFullDocument();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing LayoutEngine");
                Cleanup();
                throw;
            }
        }

        /// <summary>
        /// Creates a new document with the specified HTML.
        /// </summary>
        public IDocument CreateDocument(string html)
        {
            ThrowIfDisposed();

            var parser = new HtmlParser();
            return parser.ParseDocument(html);
        }

        /// <summary>
        /// Shuts down the LayoutEngine and releases all resources.
        /// </summary>
        public void Shutdown()
        {
            ThrowIfDisposed();
            if (!_isInitialized) return;
            _logger.LogInformation("Shutting down LayoutEngine");
            try
            {
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.Disposed)
                {
                    if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.Disposed))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.Disposed);
                    else
                        _logger.LogWarning("Could not enter Disposed phase from {CurrentPhase}", _lifecycleCoordinator.CurrentPhase);
                }

                // Shutdown subsystems
                _layoutEngine.ShutdownAsync().GetAwaiter().GetResult();
                _styleEngine.ShutdownAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LayoutEngine shutdown");
                // Don't rethrow from Shutdown/Dispose
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Gets the style computed for the specified element.
        /// </summary>
        public IComputedStyle GetComputedStyle(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Try cache first
            var cachedStyle = _styleEngine.GetCachedStyle(element);
            if (cachedStyle != null)
            {
                return cachedStyle;
            }

            _logger.LogDebug("Style not cached for {Element}. Requesting compute.", element.TagName);
            // If not cached, it needs computation. The mock engine handles this,
            // a real engine would rely on the update loop.
            // For testing with the mock, we might call ComputeStyleAsync directly,
            // acknowledging this bypasses the ideal scheduling.
            return _styleEngine.ComputeStyleAsync(element).GetAwaiter().GetResult();

            // Ideal flow (commented out for mock):
            // var update = VisualUpdate.CreateStyleUpdate(element);
            // _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);
            // // Need a mechanism to wait for the specific style or throw/return default
            // throw new InvalidOperationException("Style computation pending. Call ProcessUpdates and retry.");
        }

        /// <summary>
        /// Gets the layout box for the specified element.
        /// </summary>
        public ILayoutBox GetLayoutBox(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Ensure styles are up-to-date first (might trigger style recalc if needed)
            GetComputedStyle(element); // Call this to ensure style is computed/cached

            // Try cache first
            var cachedLayout = _layoutEngine.GetCachedLayout(element);
            if (cachedLayout != null)
            {
                return cachedLayout;
            }

            _logger.LogDebug("Layout not cached for {Element}. Requesting compute.", element.TagName);
            // Similar to GetComputedStyle, call directly for mock compatibility
            return _layoutEngine.ComputeLayoutAsync(element).GetAwaiter().GetResult();

            // Ideal flow (commented out for mock):
            // var update = VisualUpdate.CreateLayoutUpdate(element);
            // _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);
            // // Need a mechanism to wait or throw
            // throw new InvalidOperationException("Layout computation pending. Call ProcessUpdates and retry.");
        }

        /// <summary>
        /// Processes all pending updates in the style and layout systems.
        /// </summary>
        public void ProcessUpdates()
        {
            ThrowIfDisposed();
            EnsureInitialized();
            // Explicitly trigger the scheduler to process pending updates
            // This is useful in tests or simple hosts without a continuous frame loop.
            if (_updateScheduler is SimplifiedUpdateScheduler simplifiedScheduler)
            {
                _logger.LogDebug("Explicitly processing updates via SimplifiedUpdateScheduler.");
                simplifiedScheduler.ProcessUpdates();
            }
            else
            {
                _logger.LogWarning("ProcessUpdates called, but using a non-simplified scheduler. Updates might process based on frame loop.");
                // For a real scheduler, this might not do anything if it's frame-driven.
                // Alternatively, schedule a high-priority "ProcessNow" update if needed.
            }
        }

        /// <summary>
        /// Processes the full document by invalidating all styles and layout.
        /// </summary>
        public void ProcessFullDocument()
        {
            ThrowIfDisposed();
            EnsureInitialized();
            if (_document?.DocumentElement == null) return;

            _logger.LogDebug("Scheduling full document update.");
            var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Full, _document.DocumentElement);
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);
            // With SimplifiedUpdateScheduler, this will trigger processing immediately.
            // With a frame-based scheduler, it queues the update.
        }

        public void SetViewportSize(float width, float height)
        {
            ThrowIfDisposed();
            if (_currentViewport.Width == width && _currentViewport.Height == height) return;

            var oldViewport = _currentViewport;
            var newViewport = new Rect(0, 0, width, height);
            _currentViewport = newViewport;

            _logger.LogDebug("Viewport changed from {OldViewport} to {NewViewport}. Notifying LayoutEngine.",
                $"{oldViewport.Width}x{oldViewport.Height}",
                $"{newViewport.Width}x{newViewport.Height}");

            // Notify the layout engine directly
            _layoutEngine.SetViewportSize(width, height);

            // LayoutEngine.SetViewportSize now invalidates layout internally.
            // Schedule an update to process this invalidation.
            if (_document?.DocumentElement != null && _isInitialized)
            {
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Layout, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            // Publish event for external listeners
            _eventAggregator.Publish(new ViewportSizeChangedEvent(oldViewport, newViewport));
        }

        public IElement? ElementFromPoint(float x, float y)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            // Ensure layout is up-to-date before hit testing
            // Calling GetLayoutTree will process updates if needed (in the mock)
            _layoutEngine.GetLayoutTree();
            return _layoutEngine.ElementFromPoint(x, y);
        }

        public string AddStyleSheet(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            _logger.LogDebug("Adding stylesheet (Origin: {Origin})", origin);
            // StyleEngine handles invalidation internally now
            var styleSheetId = _styleEngine.AddStyleSheetAsync(styleSheet, origin, mediaQuery).GetAwaiter().GetResult();
            // Schedule an update to process the style invalidation
            if (_document?.DocumentElement != null)
            {
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Style, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            return styleSheetId;
        }

        public bool RemoveStyleSheet(string styleSheetId)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            _logger.LogDebug("Removing stylesheet ID: {StyleSheetId}", styleSheetId);
            // StyleEngine handles invalidation internally now
            var result = _styleEngine.RemoveStyleSheet(styleSheetId);
            // Schedule an update if removal was successful
            if (result && _document?.DocumentElement != null)
            {
                var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Style, _document.DocumentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
            return result;
        }

        public void PreloadResource(string url)
        {
            // TODO: Implement using a ResourceLoader service if added later
            _logger.LogWarning("PreloadResource called but not implemented yet.");
        }

        #region Private Methods

        /// <summary>
        /// Cleans up resources after document shutdown.
        /// </summary>
        private void Cleanup()
        {
            _document = null;
            _isInitialized = false;

            // Clear memory
            GC.Collect();
        }

        /// <summary>
        /// Ensures the engine is initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized || _document == null)
            {
                throw new InvalidOperationException("LayoutEngine is not initialized. Call Initialize first.");
            }
        }

        /// <summary>
        /// Throws if the engine is disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(LayoutEngineMain));
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles update processed events.
        /// </summary>
        private void OnUpdateProcessed(UpdateProcessedEvent e)
        {
            if (_isDisposed || !_isInitialized) return;

            string elementInfo = e.Update.Element != null
                ? $"{e.Update.Element.TagName} (#{e.Update.Element.Id ?? "no-id"})"
                : "document";

            _logger.LogTrace("Update processed: {UpdateType} for {ElementInfo} in {ProcessingTime:F2}ms (Success: {Success})",
                e.Update.Type, elementInfo, e.ProcessingTimeMs, e.Success);

            if (!e.Success && e.Error != null)
            {
                _logger.LogError(e.Error, "Error processing update {UpdateId} ({UpdateType}) for {ElementInfo}", e.Update.Id, e.Update.Type, elementInfo);
            }

            // Maybe trigger next phase based on update type completion? Handled by specific compute events for now.
        }

        /// <summary>
        /// Handles style computed events.
        /// </summary>
        private void OnStyleComputed(StyleComputedEvent e)
        {
            if (_isDisposed || !_isInitialized) return;

            _logger.LogTrace("Styles computed for {ElementCount} elements", e.Elements.Count);

            // Signal style phase completion
            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
            {
                if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean))
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
            }

            // If styles changed, layout is now dirty (LayoutEngine handles marking internally)
            // Schedule layout update if needed and allowed
            if (e.Elements.Count > 0 && _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.LayoutCalculation))
            {
                // Schedule a layout update for the document - LayoutEngine will figure out what's dirty
                if (_document?.DocumentElement != null)
                {
                    _logger.LogTrace("Scheduling layout update after style computation.");
                    var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Layout, _document.DocumentElement);
                    _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                }
            }
        }

        /// <summary>
        /// Handles layout updated events.
        /// </summary>
        private void OnLayoutUpdated(LayoutUpdatedEvent e)
        {
            if (_isDisposed || !_isInitialized) return;

            _logger.LogTrace("Layout updated for {ElementCount} elements", e.UpdatedElements.Count);

            // Signal layout phase completion
            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InLayout)
            {
                if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean))
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
            }

            // If layout changed, rendering might be needed
            // Schedule render update if needed and allowed
            if (e.UpdatedElements.Count > 0 && _lifecycleCoordinator.IsOperationAllowed(DocumentOperation.Rendering))
            {
                // Schedule a render update for the document
                if (_document?.DocumentElement != null)
                {
                    _logger.LogTrace("Scheduling render update after layout computation.");
                    var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Render, _document.DocumentElement);
                    _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                }
            }
        }

        /// <summary>
        /// Handles phase changed events.
        /// </summary>
        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (_isDisposed) return;

            _logger.LogDebug("Document lifecycle phase changed: {Phase}, {ChangeType}", e.Phase, e.ChangeType);

            // Trigger processing when entering certain phases if needed
            if (e.ChangeType == PhaseChangeType.Enter)
            {
                switch (e.Phase)
                {
                    case DocumentLifecyclePhase.InStyleRecalc:
                        _logger.LogTrace("Entered InStyleRecalc phase, processing style updates.");
                        _styleEngine.ProcessUpdatesAsync(); // Call directly in simplified model
                        break;
                    case DocumentLifecyclePhase.InLayout:
                        _logger.LogTrace("Entered InLayout phase, processing layout updates.");
                        _layoutEngine.ProcessUpdatesAsync(); // Call directly in simplified model
                        break;
                    case DocumentLifecyclePhase.InRender:
                        _logger.LogTrace("Entered InRender phase (no direct action in LayoutEngineMain).");
                        // Render system would handle this
                        break;
                }
            }
        }

        #endregion

        /// <summary>
        /// Disposes the LayoutEngineMain.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _logger.LogInformation("LayoutEngineMain disposing.");
            try
            {
                if (_isInitialized)
                {
                    Shutdown(); // Call synchronous shutdown
                }
                foreach (var subscription in _subscriptions)
                {
                    _eventAggregator.Unsubscribe(subscription);
                }
                _subscriptions.Clear();
                _browsingContext?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing LayoutEngineMain");
            }
        }
    }
}