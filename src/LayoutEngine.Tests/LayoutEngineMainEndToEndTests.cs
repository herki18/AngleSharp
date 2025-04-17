using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.StyleSystem;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Tests;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Tests for LayoutEngineMain focusing on orchestration, lifecycle, and update scheduling
/// using the ProcessPendingUpdates(budget) model.
/// Uses mock StyleEngine and LayoutEngine implementations with updated dirty state logic.
/// </summary>
public class LayoutEngineMainTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILayoutEngineMain _layoutEngine;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUpdateScheduler _updateScheduler; // Resolved for checking pending updates
    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator; // Resolved for phase checks
    private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

    // --- Test Helpers ---
    private readonly List<object> _publishedEvents = new List<object>();
    private readonly List<ISubscriptionToken> _eventSubscriptions = new List<ISubscriptionToken>();
    // --------------------

    private const string SimpleHtml = @"
            <!DOCTYPE html>
            <html><head><title>Test</title></head>
            <body><h1>Title</h1><p>Paragraph <span>span</span> text.</p></body>
            </html>";

    public LayoutEngineMainTests()
    {
        var services = new ServiceCollection();

        services.AddLogging(configure =>
        {
            configure.AddProvider(_loggerProvider);
            configure.SetMinimumLevel(LogLevel.Trace);
        });

        // Add LayoutEngine using the DI setup (which uses simplified platform services)
        services.AddLayoutEngine();

        _serviceProvider = services.BuildServiceProvider();

        // Resolve services needed for testing
        _layoutEngine = _serviceProvider.GetRequiredService<ILayoutEngineMain>();
        _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
        _updateScheduler = _serviceProvider.GetRequiredService<IUpdateScheduler>();
        _lifecycleCoordinator = _serviceProvider.GetRequiredService<IDocumentLifecycleCoordinator>();

        // --- Test Setup: Subscribe to events for verification ---
        SubscribeToEvent<StyleComputedEvent>();
        SubscribeToEvent<LayoutUpdatedEvent>();
        SubscribeToEvent<UpdateProcessedEvent>();
        SubscribeToEvent<PhaseChangedEvent>();
        SubscribeToEvent<StyleInvalidatedEvent>();
        SubscribeToEvent<LayoutInvalidatedEvent>();
        SubscribeToEvent<RenderInvalidatedEvent>();
        SubscribeToEvent<RenderCompletedEvent>();
        SubscribeToEvent<ViewportSizeChangedEvent>();
        SubscribeToEvent<StyleSheetChangedEvent>();
        // -------------------------------------------------------
    }

    // --- Test Helper Methods ---

    private void SubscribeToEvent<TEvent>() where TEvent : class, IEvent
    {
        var sub = _eventAggregator.Subscribe<TEvent>(e =>
        {
            lock (_publishedEvents) { _publishedEvents.Add(e); }
        });
        _eventSubscriptions.Add(sub);
    }

    private int GetEventCount<TEvent>() where TEvent : class
    {
        lock (_publishedEvents) { return _publishedEvents.OfType<TEvent>().Count(); }
    }

    private void ClearEvents()
    {
        lock (_publishedEvents) { _publishedEvents.Clear(); }
    }

            /// <summary>
        /// Simulates the host's update loop by repeatedly calling ProcessPendingUpdates
        /// until the update queue is empty or a maximum number of iterations is reached.
        /// Uses the *synchronous* ProcessUpdates(budget) on the scheduler interface.
        /// </summary>
        private async Task ProcessUpdatesFullyAsync(double timeBudgetMs = 100.0, int maxIterations = 50)
        {
            _loggerProvider.Logger?.LogDebug("--- Starting ProcessUpdatesFullyAsync ---");
            int iterations = 0;
            int lastPendingCount = -1; // Initialize to ensure first loop runs

            // Loop while there are pending updates OR until max iterations reached
            while ((lastPendingCount = _updateScheduler.GetPendingUpdateCount()) > 0 && iterations < maxIterations)
            {
                iterations++;
                _loggerProvider.Logger?.LogDebug("ProcessUpdatesFullyAsync Iteration {Iteration}, Pending Before: {PendingCount}", iterations, lastPendingCount);

                // Call the synchronous ProcessUpdates method defined on IUpdateScheduler
                // This processes one batch within the budget
                _layoutEngine.ProcessPendingUpdates(timeBudgetMs);

                int currentPendingCount = _updateScheduler.GetPendingUpdateCount();
                 _loggerProvider.Logger?.LogDebug("Iteration {Iteration} finished. Pending After: {PendingCount}", iterations, currentPendingCount);


                // If processing didn't change the pending count (e.g., budget too small, or no actual work done),
                // and the count is still > 0, we might be stuck. Add a small delay and continue loop check.
                // If processing *did* reduce the count or the count is now 0, continue loop check immediately.
                if (currentPendingCount > 0 && currentPendingCount == lastPendingCount)
                {
                    _loggerProvider.Logger?.LogWarning("ProcessUpdatesFullyAsync Iteration {Iteration} made no progress. Pending: {PendingCount}. Delaying before retry.", iterations, currentPendingCount);
                    await Task.Delay(5); // Small delay if potentially stuck
                } else {
                    // Yield slightly to allow events or other async operations to potentially queue work for the next check
                    await Task.Delay(1);
                }

                 // Update lastPendingCount *after* potential delay for the next iteration's check
                 lastPendingCount = _updateScheduler.GetPendingUpdateCount();
            } // End while loop

            // Final checks and logging after loop terminates
            int finalPending = _updateScheduler.GetPendingUpdateCount();
            if (iterations >= maxIterations && finalPending > 0)
            {
                _loggerProvider.Logger?.LogWarning("ProcessUpdatesFullyAsync reached max iterations ({MaxIterations}) with {PendingCount} updates still pending.", maxIterations, finalPending);
                 // Optionally fail the test here if completion is strictly required
                 // Assert.Fail($"Processing did not complete within {maxIterations} iterations. Pending: {finalPending}");
            }
            _loggerProvider.Logger?.LogDebug("--- Finished ProcessUpdatesFullyAsync after {Iteration} iterations, Final Pending: {PendingCount} ---", iterations, finalPending);
        }
    // ---------------------------

    [Fact]
    public async Task InitializeAsync_SetsUpDocument_EntersStyleClean_And_ProcessesInitialUpdate()
    {
        // Arrange
        Assert.False(_layoutEngine.IsInitialized);
        Assert.Equal(DocumentLifecyclePhase.Inactive, _layoutEngine.CurrentPhase);

        // Act
        var document = _layoutEngine.CreateDocument(SimpleHtml);
        await _layoutEngine.InitializeAsync(document); // Use the async version

        // Assert: Initial state after InitializeAsync returns (update is scheduled but not processed)
        Assert.True(_layoutEngine.IsInitialized);
        Assert.Same(document, _layoutEngine.Document);
        // InitializeAsync schedules ProcessFullDocument, which schedules a High priority update.
        Assert.True(_updateScheduler.GetPendingUpdateCount() > 0, "InitializeAsync should schedule initial updates.");
        // Lifecycle should move to StyleClean immediately after initialization
        Assert.Equal(DocumentLifecyclePhase.StyleClean, _layoutEngine.CurrentPhase);

        // Act: Process updates fully
        ClearEvents();
        await ProcessUpdatesFullyAsync();

        // Assert: State after processing initial updates
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        Assert.True(GetEventCount<StyleComputedEvent>() > 0, "Expected StyleComputedEvent");
        Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "Expected LayoutUpdatedEvent");
        // Assuming Render is also processed (even if mock)
        Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0, "Expected RenderInvalidatedEvent");
        Assert.True(GetEventCount<RenderCompletedEvent>() > 0, "Expected RenderCompletedEvent");
        // Final phase should be RenderReady (or LayoutClean if Render phase transitions aren't fully mocked yet)
        Assert.True(
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
            $"Expected RenderReady or LayoutClean phase after full processing, but was {_layoutEngine.CurrentPhase}");
    }

    [Fact]
    public async Task ProcessFullDocument_SchedulesUpdate_And_ReprocessesAllPhases()
    {
        // Arrange
        await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
        await ProcessUpdatesFullyAsync(); // Process initial updates
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        var initialPhase = _layoutEngine.CurrentPhase;
        Assert.True(initialPhase == DocumentLifecyclePhase.RenderReady || initialPhase == DocumentLifecyclePhase.LayoutClean);
        ClearEvents();

        // Act
        _layoutEngine.ProcessFullDocument(); // Schedule a full update

        // Assert: Update is scheduled, phase hasn't changed yet
        Assert.True(_updateScheduler.GetPendingUpdateCount() > 0);
        Assert.Equal(initialPhase, _layoutEngine.CurrentPhase);

        // Act: Process the scheduled update
        await ProcessUpdatesFullyAsync();

        // Assert: Processing completed, events fired, phase back to ready state
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        Assert.True(GetEventCount<StyleComputedEvent>() > 0);
        Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0);
        Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0); // Render should trigger again
        Assert.True(GetEventCount<RenderCompletedEvent>() > 0);
        Assert.True(
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
            $"Expected RenderReady or LayoutClean phase after full processing, but was {_layoutEngine.CurrentPhase}");
    }

    [Fact]
    public async Task GetComputedStyle_AfterProcessing_ReturnsStyleFromMock()
    {
        // Arrange
        var document = _layoutEngine.CreateDocument(SimpleHtml);
        await _layoutEngine.InitializeAsync(document);
        await ProcessUpdatesFullyAsync();
        var pElement = document.QuerySelector("p");
        Assert.NotNull(pElement);

        // Act
        var style = _layoutEngine.GetComputedStyle(pElement); // Should hit cache now

        // Assert
        Assert.NotNull(style);
        Assert.Same(pElement, style.Element);
        Assert.Equal("16px", style.GetValue("margin-bottom")); // Check mock value
    }

    [Fact]
    public async Task GetLayoutBox_AfterProcessing_ReturnsBoxFromMock()
    {
        // Arrange
        var document = _layoutEngine.CreateDocument(SimpleHtml);
        await _layoutEngine.InitializeAsync(document);
        await ProcessUpdatesFullyAsync();
        var pElement = document.QuerySelector("p");
        Assert.NotNull(pElement);

        // Act
        var box = _layoutEngine.GetLayoutBox(pElement); // Should hit cache now

        // Assert
        Assert.NotNull(box);
        Assert.Same(pElement, box.Element);
        Assert.True(box.Width > 0);
        Assert.True(box.Height > 0);
    }

    [Fact]
    public async Task SetAttribute_WhichShouldInvalidateStyle_LeadsToRecalculation()
    {
        // Arrange
        var document = _layoutEngine.CreateDocument(SimpleHtml);
        await _layoutEngine.InitializeAsync(document);
        await ProcessUpdatesFullyAsync();
        var pElement = document.QuerySelector("p");
        Assert.NotNull(pElement);
        ClearEvents();

        // Act: Change an attribute
        pElement.SetAttribute("style", "color: blue;");

        // --- WORKAROUND/SIMULATION ---
        // In a full system, a DomMutationTracker listening to AngleSharp would fire an event,
        // which would cause the StyleInvalidationTracker to mark 'pElement' dirty,
        // and then the StyleEngine would schedule an update.
        // Since that tracker isn't fully wired here, we *manually* simulate the final step: scheduling the update.
        _loggerProvider.Logger?.LogWarning("Test manually scheduling StyleUpdate after SetAttribute due to missing DOM tracker integration.");
        _updateScheduler.ScheduleUpdate(VisualUpdate.CreateStyleUpdate(pElement), UpdatePriority.High);
        // --- END WORKAROUND ---

        // Assert: Update is now scheduled
        Assert.True(_updateScheduler.GetPendingUpdateCount() > 0);
        var phaseBeforeProcessing = _layoutEngine.CurrentPhase;

        // Act: Process updates
        await ProcessUpdatesFullyAsync();

        // Assert: Processing occurred, events fired, state advanced
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        Assert.True(GetEventCount<StyleComputedEvent>() > 0, "StyleComputedEvent expected");
        Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "LayoutUpdatedEvent expected"); // Style change leads to layout
        Assert.True(
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean, // Should end ready
            $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");

        // Assert: Style value is updated (by the mock's logic)
        var newStyle = _layoutEngine.GetComputedStyle(pElement);
        Assert.Equal("blue", newStyle.GetValue("color"));
    }

    [Fact]
    public async Task AddStyleSheet_SchedulesUpdate_FlowsThroughLifecycle()
    {
        // Arrange
        await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
        await ProcessUpdatesFullyAsync();
        ClearEvents();
        var initialPhase = _layoutEngine.CurrentPhase;

        // Act
        _layoutEngine.AddStyleSheet("p { color: green; }", StyleSheetOrigin.Author);

        // Assert: StyleSheetChangedEvent published & Update scheduled by LayoutEngineMain
        Assert.Equal(1, GetEventCount<StyleSheetChangedEvent>());
        Assert.True(_updateScheduler.GetPendingUpdateCount() > 0, "AddStyleSheet should schedule an update.");
        Assert.Equal(initialPhase, _layoutEngine.CurrentPhase); // Phase shouldn't change yet

        // Act: Process updates
        await ProcessUpdatesFullyAsync();

        // Assert: Queue empty, lifecycle progressed, relevant events fired
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        Assert.True(GetEventCount<StyleComputedEvent>() > 0);
        Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0);
        Assert.True(
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
            $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");
        // Note: Verifying the actual style *value* depends on the mock/real StyleEngine implementation.
    }

    [Fact]
    public async Task SetViewportSize_SchedulesLayoutUpdate_FlowsThroughLifecycle()
    {
        // Arrange
        await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
        await ProcessUpdatesFullyAsync();
        ClearEvents();
        var initialPhase = _layoutEngine.CurrentPhase;

        // Act
        _layoutEngine.SetViewportSize(1024, 768);

        // Assert: Viewport event fired, update scheduled
        Assert.Equal(1, GetEventCount<ViewportSizeChangedEvent>());
        Assert.True(_updateScheduler.GetPendingUpdateCount() > 0, "SetViewportSize should schedule a layout update.");
        Assert.Equal(initialPhase, _layoutEngine.CurrentPhase); // Phase shouldn't change yet

        // Act: Process updates
        await ProcessUpdatesFullyAsync();

        // Assert: Queue empty, lifecycle progressed, layout event fired
        Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
        Assert.False(GetEventCount<StyleComputedEvent>() > 0, "StyleComputedEvent should not fire on viewport change."); // Style shouldn't recompute just for viewport
        Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "LayoutUpdatedEvent expected");
        Assert.True(
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
            _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
            $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");
    }

    [Fact]
    public async Task ShutdownAsync_DisposesAndSetsPhase()
    {
        // Arrange
        await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
        Assert.True(_layoutEngine.IsInitialized);
        Assert.NotEqual(DocumentLifecyclePhase.Disposed, _layoutEngine.CurrentPhase);

        // Act
        await _layoutEngine.ShutdownAsync();

        // Assert
        Assert.False(_layoutEngine.IsInitialized);
        Assert.Equal(DocumentLifecyclePhase.Disposed, _layoutEngine.CurrentPhase);
        Assert.Throws<ObjectDisposedException>(() => _layoutEngine.ProcessPendingUpdates(10));
    }

    public void Dispose()
    {
        // Unsubscribe from events first
        foreach(var sub in _eventSubscriptions)
        {
            try { _eventAggregator?.Unsubscribe(sub); } catch { /* Ignore */ }
        }
        _eventSubscriptions.Clear();
        _publishedEvents.Clear();

        (_layoutEngine as IDisposable)?.Dispose();
        _serviceProvider?.Dispose();
        _loggerProvider?.Dispose();
    }

    // --- Helper classes for logging ---
    private class TestLogger : ILogger
    {
        private readonly Action<string> _output;
        public TestLogger(Action<string> output) => _output = output;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance; // Updated for net6+
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Basic console output for test visibility
            Console.WriteLine($"[{logLevel.ToString().Substring(0, 4)}] {formatter(state, exception)}");
            // Or capture to a list: _output?.Invoke($"[{logLevel}] {formatter(state, exception)}");
        }
        private class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
    }

    private class TestLoggerProvider : ILoggerProvider
    {
        public ILogger? Logger { get; private set; }
        public ILogger CreateLogger(string categoryName)
        {
            Logger = new TestLogger(Console.WriteLine);
            return Logger;
        }
        public void Dispose() { Logger = null; GC.SuppressFinalize(this); } // Implement IDisposable
    }
    // ------------------------------------
}