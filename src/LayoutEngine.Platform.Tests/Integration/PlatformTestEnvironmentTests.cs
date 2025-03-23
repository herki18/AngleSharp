// using LayoutEngine.Contracts.Platform.Updates;
// using LayoutEngine.Contracts.Platform.Dom.Abstractions;
// using LayoutEngine.Contracts.Platform.Events;
//
// namespace LayoutEngine.Platform.Tests.Integration;
//
// using Contracts.Resource;
// using Unit.Helpers;
//
// public class PlatformTestEnvironmentTests : IDisposable
// {
//     private readonly PlatformTestEnvironment _testEnvironment;
//
//     public PlatformTestEnvironmentTests()
//     {
//         _testEnvironment = new PlatformTestEnvironment();
//     }
//
//     [Fact]
//     public void Constructor_ShouldInitializeAllComponents()
//     {
//         // Assert
//         Assert.NotNull(_testEnvironment.ThreadingCoordinator);
//         Assert.NotNull(_testEnvironment.FrameScheduler);
//         Assert.NotNull(_testEnvironment.DomMutationTracker);
//         Assert.NotNull(_testEnvironment.ViewportDetector);
//         Assert.NotNull(_testEnvironment.ResourceLoader);
//         Assert.NotNull(_testEnvironment.TimeProvider);
//         Assert.NotNull(_testEnvironment.EventAggregator);
//     }
//
//     [Fact]
//     public void TimeProvider_ShouldBeInSynchronousMode()
//     {
//         // Arrange
//         var initialTime = _testEnvironment.TimeProvider.GetCurrentTimeMilliseconds();
//
//         // Act
//         _testEnvironment.TimeProvider.AdvanceTime(1000);
//         var newTime = _testEnvironment.TimeProvider.GetCurrentTimeMilliseconds();
//
//         // Assert
//         Assert.Equal(initialTime + 1000, newTime);
//     }
//
//     [Fact]
//     public void ThreadingCoordinator_ShouldExecuteSynchronously()
//     {
//         // Arrange
//         bool executed = false;
//
//         // Act
//         _testEnvironment.ThreadingCoordinator.ScheduleOnMainThread(() => executed = true);
//         _testEnvironment.ExecuteAllPendingWork();
//
//         // Assert
//         Assert.True(executed);
//     }
//
//     [Fact]
//     public void FrameScheduler_ShouldExecuteSynchronously()
//     {
//         // Arrange
//         bool executed = false;
//
//         // Act
//         _testEnvironment.FrameScheduler.RequestAnimationFrame(_ => executed = true);
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         Assert.True(executed);
//     }
//
//     [Fact]
//     public void GetService_ShouldReturnRegisteredService()
//     {
//         // Act
//         var windowProvider = _testEnvironment.GetService<IWindowProvider>();
//         var frameScheduler = _testEnvironment.GetService<IFrameScheduler>();
//         var updateScheduler = _testEnvironment.GetService<IUpdateScheduler>();
//
//         // Assert
//         Assert.NotNull(windowProvider);
//         Assert.NotNull(frameScheduler);
//         Assert.NotNull(updateScheduler);
//         Assert.Same(_testEnvironment.FrameScheduler, frameScheduler);
//     }
//
//     [Fact]
//     public void AdvanceTimeAndRunFrame_ShouldAdvanceTimeAndExecuteFrame()
//     {
//         // Arrange
//         double initialTime = _testEnvironment.TimeProvider.GetCurrentTimeMilliseconds();
//         bool frameExecuted = false;
//         _testEnvironment.FrameScheduler.RequestAnimationFrame(_ => frameExecuted = true);
//
//         // Act
//         _testEnvironment.AdvanceTimeAndRunFrame(100);
//
//         // Assert
//         Assert.True(frameExecuted);
//         Assert.Equal(initialTime + 100, _testEnvironment.TimeProvider.GetCurrentTimeMilliseconds());
//     }
//
//     [Fact]
//     public void ExecuteAllPendingWork_ShouldExecuteQueuedActions()
//     {
//         // Arrange
//         bool mainThreadExecuted = false;
//         bool workerThreadExecuted = false;
//
//         _testEnvironment.ThreadingCoordinator.ScheduleOnMainThread(() => mainThreadExecuted = true);
//         _testEnvironment.ThreadingCoordinator.ScheduleOnWorkerThread(() => workerThreadExecuted = true);
//
//         // Act
//         _testEnvironment.ExecuteAllPendingWork();
//
//         // Assert
//         Assert.True(mainThreadExecuted);
//         Assert.True(workerThreadExecuted);
//     }
//
//     [Fact]
//     public void TestComponents_ShouldBeConfiguredCorrectly()
//     {
//         // Test that the test components are correctly configured
//
//         // Get test components
//         var windowProvider = _testEnvironment.GetService<IWindowProvider>() as TestWindowProvider;
//         var testWindow = windowProvider!.TestWindow;
//
//         // Assert
//         Assert.NotNull(windowProvider);
//         Assert.NotNull(testWindow);
//         Assert.NotNull(testWindow!.TestDocument);
//     }
//
//     [Fact]
//     public void ComplexScenario_ShouldWorkCorrectly()
//     {
//         // This test combines multiple components to verify they work together correctly
//
//         // Arrange
//         var eventAggregator = _testEnvironment.EventAggregator;
//         var frameScheduler = _testEnvironment.FrameScheduler;
//         var windowProvider = _testEnvironment.GetService<IWindowProvider>() as TestWindowProvider;
//         var testWindow = windowProvider!.TestWindow!;
//
//         bool frameCallbackExecuted = false;
//         bool resizeDetected = false;
//
//         // Subscribe to viewport events
//         var token = eventAggregator.Subscribe<ViewportChangedEvent>(_ => resizeDetected = true);
//
//         // Start viewport detection
//         _testEnvironment.ViewportDetector.StartTracking();
//
//         // Request animation frame
//         frameScheduler.RequestAnimationFrame(_ => frameCallbackExecuted = true);
//
//         // Act - Simulate window resize and advance time
//         testWindow.SimulateResize(800, 600);
//         _testEnvironment.AdvanceTimeAndRunFrame(100);
//         _testEnvironment.ViewportDetector.CheckForChanges();
//
//         // Assert
//         Assert.True(frameCallbackExecuted);
//         Assert.True(resizeDetected);
//         Assert.Equal(800, _testEnvironment.ViewportDetector.ViewportSize.Width);
//         Assert.Equal(600, _testEnvironment.ViewportDetector.ViewportSize.Height);
//
//         // Cleanup
//         eventAggregator.Unsubscribe(token);
//     }
//
//     [Fact]
//     public async Task ResourceLoader_ShouldUseTestStrategy()
//     {
//         // Arrange
//         string url = "https://example.com/test-resource.png";
//
//         // Act
//         var resource = await _testEnvironment.ResourceLoader.LoadResourceAsync(url);
//
//         // Assert
//         Assert.NotNull(resource);
//         Assert.Equal(url, resource.Url);
//         Assert.Equal(ResourceType.Image, resource.ResourceType);
//         Assert.True(resource.IsLoaded);
//     }
//
//     public void Dispose()
//     {
//         _testEnvironment.Dispose();
//     }
// }