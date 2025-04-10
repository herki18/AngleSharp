using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.StyleSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LayoutEngine
{
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
            _logger = logger ?? NullLogger<LayoutEngineMain>.Instance;
            _configuration = configuration ?? LayoutEngineConfiguration.Default;

            // Create browsing context with default configuration
            _browsingContext = AngleSharp.BrowsingContext.New(_configuration.AngleSharpConfiguration);

            // Subscribe to events
            _subscriptions.Add(_eventAggregator.Subscribe<ViewportChangedEvent>(OnViewportChanged));
            _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
            _subscriptions.Add(_eventAggregator.Subscribe<LayoutUpdatedEvent>(OnLayoutUpdated));
            _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
            _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
            _subscriptions.Add(_eventAggregator.Subscribe<FragmentTreeUpdatedEvent>(OnFragmentTreeUpdated));
            _subscriptions.Add(_eventAggregator.Subscribe<UpdateProcessedEvent>(OnUpdateProcessed));
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
                // Initialize subsystems
                await _styleEngine.InitializeAsync(document);
                await _layoutEngine.InitializeAsync(document);

                // Enter inactive -> style clean phase through lifecycle coordinator
                _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

                _isInitialized = true;
                _logger.LogInformation("LayoutEngine initialized successfully");

                // Schedule a full document update using the UpdateScheduler
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
        public IDocument CreateDocument(string html)
        {
            ThrowIfDisposed();

            var parser = new HtmlParser();
            return parser.ParseDocument(html);
        }

        /// <summary>
        /// Shuts down the LayoutEngine and releases all resources.
        /// </summary>
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
                // Use lifecycle coordinator to transition to disposed phase
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.Disposed)
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.Disposed);
                }

                // Wait for lifecycle events to propagate
                await Task.Delay(50);

                // Call shutdown on subsystems to ensure cleanup
                await _layoutEngine.ShutdownAsync();
                await _styleEngine.ShutdownAsync();
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
        public async Task<IComputedStyle> GetComputedStyleAsync(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // First check if we can get the style from cache
            var cachedStyle = _styleEngine.GetCachedStyle(element);
            if (cachedStyle != null)
            {
                return cachedStyle;
            }

            // If not available in cache, schedule style update and wait for the event
            var tcs = new TaskCompletionSource<IComputedStyle>();
            ISubscriptionToken? subscription = null;

            // Create a one-time subscription to StyleComputedEvent
            subscription = _eventAggregator.Subscribe<StyleComputedEvent>(e =>
            {
                if (e.ComputedStyles.TryGetValue(element, out var style))
                {
                    tcs.TrySetResult(style);
                    if (subscription != null)
                        _eventAggregator.Unsubscribe(subscription);
                }
            });

            // Schedule a style update using the UpdateScheduler with high priority
            var update = VisualUpdate.CreateStyleUpdate(element);
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);

            // Set a timeout to prevent indefinite waiting
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Extended timeout
            cts.Token.Register(() =>
            {
                // Instead of directly calling style engine, try re-scheduling with critical priority
                if (!tcs.Task.IsCompleted)
                {
                    _logger.LogWarning("Style computation via events timed out, re-scheduling with critical priority");

                    // Schedule with critical priority
                    var criticalUpdate = VisualUpdate.CreateStyleUpdate(element);
                    _updateScheduler.ScheduleUpdate(criticalUpdate, UpdatePriority.Critical);

                    // Continue waiting for the event response
                    // We don't set exception here, just let it keep waiting
                }
            });

            try
            {
                // Add a longer final timeout as safety measure
                return await TimeoutAfter(tcs.Task, TimeSpan.FromSeconds(10),
                    () => new TimeoutException("Style computation timed out after multiple attempts"));
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Style computation completely failed after multiple scheduling attempts");
                throw new InvalidOperationException("Failed to compute style after multiple attempts", ex);
            }
        }

        /// <summary>
        /// Gets the layout box for the specified element.
        /// </summary>
        public async Task<ILayoutBox> GetLayoutBoxAsync(IElement element)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // First check if we can get the layout from cache
            var cachedLayout = _layoutEngine.GetCachedLayout(element);
            if (cachedLayout != null)
            {
                return cachedLayout;
            }

            // If not available in cache, schedule layout update and wait for the event
            var tcs = new TaskCompletionSource<ILayoutBox>();
            ISubscriptionToken? subscription = null;

            // Create a one-time subscription to LayoutUpdatedEvent
            subscription = _eventAggregator.Subscribe<LayoutUpdatedEvent>(e =>
            {
                if (e.UpdatedBoxes.TryGetValue(element, out var box))
                {
                    tcs.TrySetResult(box);
                    if (subscription != null)
                        _eventAggregator.Unsubscribe(subscription);
                }
            });

            // Schedule a layout update using the UpdateScheduler with high priority
            var update = VisualUpdate.CreateLayoutUpdate(element);
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);

            // Set a timeout to prevent indefinite waiting
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Extended timeout
            cts.Token.Register(() =>
            {
                // Instead of directly calling layout engine, try re-scheduling with critical priority
                if (!tcs.Task.IsCompleted)
                {
                    _logger.LogWarning("Layout computation via events timed out, re-scheduling with critical priority");

                    // Schedule with critical priority
                    var criticalUpdate = VisualUpdate.CreateLayoutUpdate(element);
                    _updateScheduler.ScheduleUpdate(criticalUpdate, UpdatePriority.Critical);

                    // Continue waiting for the event response
                    // We don't set exception here, just let it keep waiting
                }
            });

            try
            {
                // Add a longer final timeout as safety measure
                return await TimeoutAfter(tcs.Task, TimeSpan.FromSeconds(10),
                    () => new TimeoutException("Layout computation timed out after multiple attempts"));
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Layout computation completely failed after multiple scheduling attempts");
                throw new InvalidOperationException("Failed to compute layout after multiple attempts", ex);
            }
        }

        /// <summary>
        /// Processes all pending updates in the style and layout systems.
        /// </summary>
        public async Task ProcessUpdatesAsync()
        {
            ThrowIfDisposed();
            EnsureInitialized();

            if (_document?.Body == null)
                return;

            // Use a single comprehensive update instead of separate style and layout updates
            // This allows the UpdateScheduler to properly sequence the dependencies
            var documentElement = _document.DocumentElement ?? _document.Body;
            var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Layout, documentElement);

            // Schedule through the UpdateScheduler
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);

            _logger.LogDebug("Scheduled document update");

            // Return immediately - the update will be processed by the platform infrastructure
            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes the full document by invalidating all styles and layout.
        /// </summary>
        public async Task ProcessFullDocumentAsync()
        {
            ThrowIfDisposed();
            EnsureInitialized();

            if (_document?.Body == null)
                return;

            // Schedule a full document update through the UpdateScheduler
            var documentElement = _document.DocumentElement ?? _document.Body;
            var update = VisualUpdate.CreateDocumentUpdate(UpdateType.Full, documentElement);

            // Use high priority for full document updates
            _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);

            _logger.LogDebug("Scheduled full document update");

            // Return immediately - the update will be processed by the platform infrastructure
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sets the viewport size.
        /// </summary>
        public void SetViewportSize(float width, float height)
        {
            ThrowIfDisposed();

            var oldViewport = _currentViewport;
            var newViewport = new Rect(0, 0, width, height);

            _currentViewport = newViewport;
            _layoutEngine.SetViewportSize(width, height);

            _logger.LogDebug("Viewport changed from {OldViewport} to {NewViewport}",
                $"{oldViewport.Width}x{oldViewport.Height}",
                $"{newViewport.Width}x{newViewport.Height}");

            _eventAggregator.Publish(new ViewportSizeChangedEvent(oldViewport, newViewport));

            // Schedule layout update due to viewport change
            if (_document?.Body != null && _isInitialized)
            {
                var documentElement = _document.DocumentElement ?? _document.Body;
                var update = VisualUpdate.CreateLayoutUpdate(documentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
        }

        /// <summary>
        /// Gets the element at the specified point.
        /// </summary>
        public IElement? ElementFromPoint(float x, float y)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            return _layoutEngine.ElementFromPoint(x, y);
        }

        /// <summary>
        /// Adds a style sheet to the document.
        /// </summary>
        public async Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            // Add the stylesheet
            var styleSheetId = await _styleEngine.AddStyleSheetAsync(styleSheet, origin, mediaQuery);

            // Schedule style update
            if (_document?.Body != null)
            {
                var documentElement = _document.DocumentElement ?? _document.Body;
                var update = VisualUpdate.CreateStyleUpdate(documentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }

            return styleSheetId;
        }

        /// <summary>
        /// Removes a style sheet from the document.
        /// </summary>
        public bool RemoveStyleSheet(string styleSheetId)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            var result = _styleEngine.RemoveStyleSheet(styleSheetId);

            // Schedule style update
            if (result && _document?.Body != null)
            {
                var documentElement = _document.DocumentElement ?? _document.Body;
                var update = VisualUpdate.CreateStyleUpdate(documentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }

            return result;
        }

        /// <summary>
        /// Preloads a resource.
        /// </summary>
        public void PreloadResource(string url)
        {
            ThrowIfDisposed();

            _resourceLoader.PreloadResource(url);
        }

        #region Private Methods

        /// <summary>
        /// Extension method to add timeout to a Task with a custom exception factory.
        /// </summary>
        private static async Task<T> TimeoutAfter<T>(Task<T> task, TimeSpan timeout, Func<Exception> exceptionFactory)
        {
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw exceptionFactory();
            }

            return await task;
        }

        /// <summary>
        /// Cleans up resources after document shutdown.
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
            if (_isDisposed || !_isInitialized)
                return;

            string elementInfo = e.Update.Element != null
                ? $"{e.Update.Element.TagName} (#{e.Update.Element.Id})"
                : "unknown";

            _logger.LogTrace("Update processed: {UpdateType} for {ElementInfo} in {ProcessingTime:F2}ms",
                e.Update.Type, elementInfo, e.ProcessingTimeMs);

            // If this was a full update, check if we need to trigger phase transitions
            if (e.Update.Type == UpdateType.Full)
            {
                // Depending on the current phase, we might need to trigger next phase
                var currentPhase = _lifecycleCoordinator.CurrentPhase;

                if (currentPhase == DocumentLifecyclePhase.StyleClean &&
                    _lifecycleCoordinator.IsValidTransition(currentPhase, DocumentLifecyclePhase.InLayout))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                }
                else if (currentPhase == DocumentLifecyclePhase.LayoutClean &&
                         _lifecycleCoordinator.IsValidTransition(currentPhase, DocumentLifecyclePhase.RenderReady))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                }
            }
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

            // Schedule layout update due to viewport change
            if (_document?.Body != null)
            {
                var documentElement = _document.DocumentElement ?? _document.Body;
                var update = VisualUpdate.CreateLayoutUpdate(documentElement);
                _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
            }
        }

        /// <summary>
        /// Handles style computed events.
        /// </summary>
        private void OnStyleComputed(StyleComputedEvent e)
        {
            if (_isDisposed || !_isInitialized)
                return;

            _logger.LogTrace("Styles computed for {ElementCount} elements", e.Elements.Count);

            // If we've completed style calculation, schedule layout update
            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.StyleClean ||
                _lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.StyleDirty)
            {
                // Only proceed if layout calculation is allowed
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.LayoutCalculation))
                {
                    // Schedule layout updates for affected elements
                    foreach (var element in e.Elements)
                    {
                        var update = VisualUpdate.CreateLayoutUpdate(element);
                        _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                    }
                }
            }
        }

        /// <summary>
        /// Handles layout updated events.
        /// </summary>
        private void OnLayoutUpdated(LayoutUpdatedEvent e)
        {
            if (_isDisposed || !_isInitialized)
                return;

            _logger.LogTrace("Layout updated for {ElementCount} elements", e.UpdatedElements.Count);

            // If we've completed layout calculation, schedule render update
            if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.LayoutClean ||
                _lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.LayoutDirty)
            {
                // Only proceed if rendering is allowed
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.Rendering))
                {
                    // Schedule render update for the document
                    if (_document?.Body != null)
                    {
                        var documentElement = _document.DocumentElement ?? _document.Body;
                        var update = VisualUpdate.CreateRenderUpdate(documentElement);
                        _updateScheduler.ScheduleUpdate(update, UpdatePriority.Normal);
                    }
                }
            }
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
        /// Handles phase changed events.
        /// </summary>
        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (_isDisposed)
                return;

            _logger.LogDebug("Document lifecycle phase changed: {Phase}, {ChangeType}",
                e.Phase, e.ChangeType);

            // React to specific phase changes
            switch (e.Phase)
            {
                case DocumentLifecyclePhase.StyleClean when e.ChangeType == PhaseChangeType.Enter:
                    _logger.LogTrace("Document styles are clean");
                    // If layout needs updating, enter layout phase
                    if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InLayout))
                    {
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                    }
                    break;

                case DocumentLifecyclePhase.LayoutClean when e.ChangeType == PhaseChangeType.Enter:
                    _logger.LogTrace("Document layout is clean");
                    // If render is needed, enter render ready phase
                    if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.RenderReady))
                    {
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                    }
                    break;

                case DocumentLifecyclePhase.RenderReady when e.ChangeType == PhaseChangeType.Enter:
                    _logger.LogDebug("Document is ready for rendering");
                    // If we need to render, enter render phase
                    if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.InRender))
                    {
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles fragment tree updated events.
        /// </summary>
        private void OnFragmentTreeUpdated(FragmentTreeUpdatedEvent e)
        {
            if (_isDisposed || !_isInitialized)
                return;

            _logger.LogTrace("Fragment tree updated");
        }

        #endregion

        /// <summary>
        /// Disposes the LayoutEngineMain.
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
}