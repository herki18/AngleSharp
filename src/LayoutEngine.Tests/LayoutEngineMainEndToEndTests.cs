using Xunit;
using Xunit.Abstractions; // Required for ITestOutputHelper
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using LayoutEngine; // Assuming LayoutEngineMain is here
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.StyleSystem;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Platform.Update; // For VisualUpdate

namespace LayoutEngine.Tests
{
    using Infrastructure.EventAggregator.API.Events;

    /// <summary>
    /// Tests for LayoutEngineMain focusing on orchestration, lifecycle, and update scheduling
    /// using the ProcessPendingUpdates(budget) model.
    /// Uses mock StyleEngine and LayoutEngine implementations with updated dirty state logic.
    /// Includes logging directed to xUnit test output.
    /// </summary>
    public class LayoutEngineMainTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ILayoutEngineMain _layoutEngine;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUpdateScheduler _updateScheduler;
        private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
        private readonly ITestOutputHelper _output; // xUnit output helper
        private readonly ILogger<LayoutEngineMainTests> _testLogger; // Logger for test methods

        // --- Test Helpers ---
        private readonly List<object> _publishedEvents = new List<object>();
        private readonly List<ISubscriptionToken> _eventSubscriptions = new List<ISubscriptionToken>();
        // --------------------


        // Test HTML content
        private const string SimpleHtml = @"
            <!DOCTYPE html>
            <html><head><title>Test</title></head>
            <body><h1>Title</h1><p>Paragraph <span>span</span> text.</p></body>
            </html>";

        public LayoutEngineMainTests(ITestOutputHelper output) // xUnit injects this
        {
            _output = output; // Store the helper
            var services = new ServiceCollection();

            // --- Configure logging to use TestLoggerProvider with the output helper ---
            LogLevel minLogLevel = LogLevel.Trace; // Set desired level for test output
            services.AddLogging(configure =>
            {
                // Clear other providers like console if they interfere
                // configure.ClearProviders();
                configure.AddProvider(new TestLoggerProvider(_output, minLogLevel)); // Pass the helper
                configure.SetMinimumLevel(minLogLevel);
            });
            // --------------------------------------------------------------------------

            // Add LayoutEngine using the DI setup (which uses simplified platform services)
            services.AddLayoutEngine();

            _serviceProvider = services.BuildServiceProvider();

            // Resolve services needed for testing
            _layoutEngine = _serviceProvider.GetRequiredService<ILayoutEngineMain>();
            _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
            _updateScheduler = _serviceProvider.GetRequiredService<IUpdateScheduler>();
            _lifecycleCoordinator = _serviceProvider.GetRequiredService<IDocumentLifecycleCoordinator>();
            _testLogger = _serviceProvider.GetRequiredService<ILogger<LayoutEngineMainTests>>(); // Get logger instance

            // --- Setup event subscriptions for verification ---
            SubscribeToEvent<StyleComputedEvent>();
            SubscribeToEvent<LayoutUpdatedEvent>();
            SubscribeToEvent<UpdateProcessedEvent>();
            SubscribeToEvent<PhaseChangedEvent>();
            SubscribeToEvent<StyleInvalidatedEvent>();
            SubscribeToEvent<LayoutInvalidatedEvent>();
            SubscribeToEvent<RenderInvalidatedEvent>();
            SubscribeToEvent<RenderCompletedEvent>(); // Ensure this event type exists and is published by mock/simplified setup if asserted
            SubscribeToEvent<ViewportSizeChangedEvent>();
            SubscribeToEvent<StyleSheetChangedEvent>();
            // ------------------------------------------------
        }

        // --- Test Helper Methods ---

        private void SubscribeToEvent<TEvent>() where TEvent : class, IEvent
        {
            var sub = _eventAggregator.Subscribe<TEvent>(e =>
            {
                lock (_publishedEvents) { _publishedEvents.Add(e); }
                // Optional: Log event occurrence directly to test output
                 _testLogger.LogTrace("[EVENT CAPTURED] {EventType}", typeof(TEvent).Name);
            });
            _eventSubscriptions.Add(sub);
        }

        private int GetEventCount<TEvent>() where TEvent : class
        {
            lock (_publishedEvents) { return _publishedEvents.OfType<TEvent>().Count(); }
        }

         private TEvent? GetLastEventOfType<TEvent>() where TEvent : class
        {
            lock (_publishedEvents)
            {
                return _publishedEvents.OfType<TEvent>().LastOrDefault();
            }
        }


        private void ClearEvents()
        {
            lock (_publishedEvents) { _publishedEvents.Clear(); }
        }

        /// <summary>
        /// Simulates the host's update loop by repeatedly calling ProcessPendingUpdatesAsync
        /// until the update queue is empty or a maximum number of iterations is reached.
        /// </summary>
        private async Task ProcessUpdatesFullyAsync(double timeBudgetMs = 100.0, int maxIterations = 50)
        {
            _testLogger.LogInformation("--- Starting ProcessUpdatesFullyAsync (Async Scheduler Call) ---");
            int iterations = 0;
            int totalUpdatesProcessed = 0;
            bool processedAnyInLastPass;

            do
            {
                iterations++;
                int pendingBefore = _updateScheduler.GetPendingUpdateCount();

                if (pendingBefore == 0)
                {
                    _testLogger.LogDebug("Iteration {Iter}: No updates pending before processing. Exiting loop.", iterations);
                    break;
                }

                _testLogger.LogDebug("Iteration {Iter}, Pending Before: {Count}", iterations, pendingBefore);

                // --- Store count *before* processing ---
                int countBeforeProc = _updateScheduler.GetPendingUpdateCount();

                // --- CALL THE ASYNC SCHEDULER METHOD ---
                await _updateScheduler.ProcessUpdatesAsync(timeBudgetMs);
                // ---------------------------------------

                int countAfterProc = _updateScheduler.GetPendingUpdateCount();
                // Note: Because processing is async, countAfterProc might not reflect
                // items processed *within this specific await*. We rely on the loop condition.
                // Calculate processed based on change, acknowledging potential adds during await.
                int processedEstimate = countBeforeProc - countAfterProc;
                processedAnyInLastPass = processedEstimate > 0 || countBeforeProc > 0; // Assume progress if queue wasn't empty before

                if(processedEstimate > 0) totalUpdatesProcessed += processedEstimate;


                _testLogger.LogDebug("Iteration {Iter} finished. Processed Est: {Proc}, Pending After: {Pend}",
                                 iterations, processedEstimate, countAfterProc);

                // If the queue is empty *after* processing, we can exit.
                if (countAfterProc == 0)
                {
                    _testLogger.LogDebug("Update queue empty after Iteration {Iter}. Exiting loop.", iterations);
                    break;
                }

                // If no apparent progress was made, warn and delay slightly longer
                if (!processedAnyInLastPass && countAfterProc > 0)
                {
                     _testLogger.LogWarning("ProcessUpdatesFullyAsync Iteration {Iter} made no apparent progress, but {Pend} updates remain.", iterations, countAfterProc);
                     await Task.Delay(5);
                } else {
                    // Yield slightly even if progress was made
                    await Task.Yield(); // Use Yield instead of Delay(1)
                }

            } while (iterations < maxIterations); // Continue as long as iterations < max

            // Final checks and logging
            int finalPending = _updateScheduler.GetPendingUpdateCount();
            if (iterations >= maxIterations && finalPending > 0)
            {
                _testLogger.LogError("ProcessUpdatesFullyAsync reached max iterations ({MaxIter}) with {Pend} updates still pending.", maxIterations, finalPending);
                Assert.Fail($"Processing did not complete within {maxIterations} iterations. Pending: {finalPending}");
            }
            _testLogger.LogInformation("--- Finished ProcessUpdatesFullyAsync after {Iter} iterations, Processed Total Est: {Total}, Final Pending: {Pend} ---", iterations, totalUpdatesProcessed, finalPending);
        }

        // --- Test Methods ---

        [Fact]
        public async Task InitializeAsync_SetsUpDocument_EntersStyleClean_And_ProcessesInitialUpdate()
        {
            _testLogger.LogInformation("Starting test: InitializeAsync_SetsUpDocument...");
            // Arrange
            Assert.False(_layoutEngine.IsInitialized);
            Assert.Equal(DocumentLifecyclePhase.Inactive, _layoutEngine.CurrentPhase);

            // Act
            var document = _layoutEngine.CreateDocument(SimpleHtml);
            await _layoutEngine.InitializeAsync(document);

            // Assert: Initial state after InitializeAsync returns
            Assert.True(_layoutEngine.IsInitialized);
            Assert.Same(document, _layoutEngine.Document);
            Assert.True(_updateScheduler.GetPendingUpdateCount() > 0, "InitializeAsync should schedule initial updates.");
            Assert.Equal(DocumentLifecyclePhase.StyleClean, _layoutEngine.CurrentPhase);

            // Act: Process updates fully
            ClearEvents();
            await ProcessUpdatesFullyAsync();

            // Assert: State after processing initial updates
            Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
            Assert.True(GetEventCount<StyleComputedEvent>() > 0, "Expected StyleComputedEvent");
            Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "Expected LayoutUpdatedEvent");
            Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0, "Expected RenderInvalidatedEvent");
            Assert.True(GetEventCount<RenderCompletedEvent>() > 0, "Expected RenderCompletedEvent");
            Assert.True(
                _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
                _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
                 $"Expected RenderReady or LayoutClean phase after full processing, but was {_layoutEngine.CurrentPhase}");
             _testLogger.LogInformation("Finished test: InitializeAsync_SetsUpDocument...");
        }

        [Fact]
        public async Task ProcessFullDocument_SchedulesUpdate_And_ReprocessesAllPhases()
        {
             _testLogger.LogInformation("Starting test: ProcessFullDocument_SchedulesUpdate...");
            // Arrange
            await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
            await ProcessUpdatesFullyAsync();
            Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
            var initialPhase = _layoutEngine.CurrentPhase;
            Assert.True(initialPhase == DocumentLifecyclePhase.RenderReady || initialPhase == DocumentLifecyclePhase.LayoutClean);
            ClearEvents();

            // Act
            _layoutEngine.ProcessFullDocument();

            // Assert: Update is scheduled, phase hasn't changed yet
            Assert.True(_updateScheduler.GetPendingUpdateCount() > 0);
            Assert.Equal(initialPhase, _layoutEngine.CurrentPhase);

            // Act: Process the scheduled update
            await ProcessUpdatesFullyAsync();

            // Assert: Processing completed, events fired, phase back to ready state
            Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
            Assert.True(GetEventCount<StyleComputedEvent>() > 0);
            Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0);
            Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0);
            Assert.True(GetEventCount<RenderCompletedEvent>() > 0);
            Assert.True(
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
                  $"Expected RenderReady or LayoutClean phase after full processing, but was {_layoutEngine.CurrentPhase}");
             _testLogger.LogInformation("Finished test: ProcessFullDocument_SchedulesUpdate...");
        }

        [Fact]
        public async Task GetComputedStyle_AfterProcessing_ReturnsStyleFromMock()
        {
             _testLogger.LogInformation("Starting test: GetComputedStyle_AfterProcessing...");
            // Arrange
            var document = _layoutEngine.CreateDocument(SimpleHtml);
            await _layoutEngine.InitializeAsync(document);
            await ProcessUpdatesFullyAsync();
            var pElement = document.QuerySelector("p"); Assert.NotNull(pElement);

            // Act
            var style = _layoutEngine.GetComputedStyle(pElement);

            // Assert
            Assert.NotNull(style);
            Assert.Same(pElement, style.Element);
            Assert.Equal("16px", style.GetValue("margin-bottom"));
             _testLogger.LogInformation("Finished test: GetComputedStyle_AfterProcessing...");
        }

        [Fact]
        public async Task GetLayoutBox_AfterProcessing_ReturnsBoxFromMock()
        {
            _testLogger.LogInformation("Starting test: GetLayoutBox_AfterProcessing...");
            // Arrange
            var document = _layoutEngine.CreateDocument(SimpleHtml);
            await _layoutEngine.InitializeAsync(document);
            await ProcessUpdatesFullyAsync();
            var pElement = document.QuerySelector("p"); Assert.NotNull(pElement);

            // Act
            var box = _layoutEngine.GetLayoutBox(pElement);

            // Assert
            Assert.NotNull(box);
            Assert.Same(pElement, box.Element);
            Assert.True(box.Width > 0);
            Assert.True(box.Height > 0);
            _testLogger.LogInformation("Finished test: GetLayoutBox_AfterProcessing...");
        }

        [Fact]
        public async Task SetAttribute_WhichShouldInvalidateStyle_LeadsToRecalculation()
        {
            _testLogger.LogInformation("Starting test: SetAttribute_LeadsToRecalculation...");
            // Arrange
            var document = _layoutEngine.CreateDocument(SimpleHtml);
            await _layoutEngine.InitializeAsync(document);
            await ProcessUpdatesFullyAsync();
            var pElement = document.QuerySelector("p"); Assert.NotNull(pElement);
            ClearEvents();

            // Act: Change an attribute
            pElement.SetAttribute("style", "color: blue;");

            // --- SIMULATION of DOM Mutation triggering update scheduling ---
            _testLogger.LogWarning("Test manually scheduling StyleUpdate after SetAttribute.");
            _updateScheduler.ScheduleUpdate(new VisualUpdate(UpdateType.Style, pElement), UpdatePriority.High);
            // --- END SIMULATION ---

            // Assert: Update is now scheduled
            Assert.True(_updateScheduler.GetPendingUpdateCount() > 0);
            var phaseBeforeProcessing = _layoutEngine.CurrentPhase;

            // Act: Process updates
            await ProcessUpdatesFullyAsync();

            // Assert: Processing occurred, events fired, state advanced
            Assert.Equal(0, _updateScheduler.GetPendingUpdateCount());
            Assert.True(GetEventCount<StyleComputedEvent>() > 0, "StyleComputedEvent expected");
            Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "LayoutUpdatedEvent expected");
            Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0, "RenderInvalidatedEvent expected");
            Assert.True(GetEventCount<RenderCompletedEvent>() > 0, "RenderCompletedEvent expected");
            Assert.True(
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
                  $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");

            // Assert: Style value is updated (by the mock's logic for inline styles)
            var newStyle = _layoutEngine.GetComputedStyle(pElement);
            Assert.Equal("blue", newStyle.GetValue("color"));
             _testLogger.LogInformation("Finished test: SetAttribute_LeadsToRecalculation...");
        }

        [Fact]
        public async Task AddStyleSheet_SchedulesUpdate_FlowsThroughLifecycle()
        {
            _testLogger.LogInformation("Starting test: AddStyleSheet_SchedulesUpdate...");
            // Arrange
            await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
            await ProcessUpdatesFullyAsync();
            var initialPhase = _layoutEngine.CurrentPhase;
            ClearEvents();

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
            Assert.True(GetEventCount<StyleComputedEvent>() > 0, "Expected StyleComputedEvent");
            Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "Expected LayoutUpdatedEvent"); // <<< This should now pass
            Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0, "Expected RenderInvalidatedEvent");
            Assert.True(GetEventCount<RenderCompletedEvent>() > 0, "Expected RenderCompletedEvent");
            Assert.True(
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
                  $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");
             _testLogger.LogInformation("Finished test: AddStyleSheet_SchedulesUpdate...");
        }

        [Fact]
        public async Task SetViewportSize_SchedulesLayoutUpdate_FlowsThroughLifecycle()
        {
            _testLogger.LogInformation("Starting test: SetViewportSize_SchedulesLayoutUpdate...");
            // Arrange
            await _layoutEngine.InitializeAsync(_layoutEngine.CreateDocument(SimpleHtml));
            await ProcessUpdatesFullyAsync();
            var initialPhase = _layoutEngine.CurrentPhase;
            ClearEvents();

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
            Assert.False(GetEventCount<StyleComputedEvent>() > 0, "StyleComputedEvent should not fire on viewport change.");
            Assert.True(GetEventCount<LayoutUpdatedEvent>() > 0, "LayoutUpdatedEvent expected");
            Assert.True(GetEventCount<RenderInvalidatedEvent>() > 0, "RenderInvalidatedEvent expected");
            Assert.True(GetEventCount<RenderCompletedEvent>() > 0, "RenderCompletedEvent expected");
            Assert.True(
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.RenderReady ||
                 _layoutEngine.CurrentPhase == DocumentLifecyclePhase.LayoutClean,
                  $"Expected RenderReady or LayoutClean phase after processing, but was {_layoutEngine.CurrentPhase}");
            _testLogger.LogInformation("Finished test: SetViewportSize_SchedulesLayoutUpdate...");
        }

        [Fact]
        public async Task ShutdownAsync_DisposesAndSetsPhase()
        {
            _testLogger.LogInformation("Starting test: ShutdownAsync_DisposesAndSetsPhase...");
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
            _testLogger.LogInformation("Finished test: ShutdownAsync_DisposesAndSetsPhase...");
        }


        // --- Dispose Method ---
        public void Dispose()
        {
            _testLogger?.LogTrace("Disposing LayoutEngineMainTests...");
            // Unsubscribe from events
            foreach(var sub in _eventSubscriptions)
            {
                try { _eventAggregator?.Unsubscribe(sub); } catch { /* Ignore */ }
            }
            _eventSubscriptions.Clear();
            _publishedEvents.Clear();

            // Dispose engine and service provider
            (_layoutEngine as IDisposable)?.Dispose();
            _serviceProvider?.Dispose();
            // TestLoggerProvider dispose happens automatically via xUnit for ITestOutputHelper
        }

        // --- Helper classes for logging (same as previous good answer) ---
        private class TestLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly ITestOutputHelper _output;
            private readonly LogLevel _minLevel;
            public TestLogger(string categoryName, ITestOutputHelper output, LogLevel minLevel) { _categoryName = categoryName; _output = output; _minLevel = minLevel; }
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                try { _output.WriteLine($"[{logLevel.ToString().Substring(0, 4)}][{_categoryName}] {formatter(state, exception)}"); } catch (InvalidOperationException) { /* Can happen if test output is already disposed */ }
                if (exception != null) try { _output.WriteLine(exception.ToString()); } catch { }
            }
            private class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }
        private class TestLoggerProvider : ILoggerProvider
        {
            private readonly ITestOutputHelper _output;
            private readonly LogLevel _minLevel;
            public TestLoggerProvider(ITestOutputHelper output, LogLevel minLevel = LogLevel.Trace) { _output = output; _minLevel = minLevel; }
            public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _output, _minLevel);
            public void Dispose() { GC.SuppressFinalize(this); }
        }
        // ------------------------------------
    }
}