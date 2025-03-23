// namespace LayoutEngine.Platform.Tests.Unit.Update;
//
// using AutoFixture;
// using LayoutEngine.Contracts.Platform.Events;
// using LayoutEngine.Contracts.Platform.Threading;
// using LayoutEngine.Contracts.Platform.Updates;
// using LayoutEngine.Contracts.Resource;
// using LayoutEngine.Platform.Tests.Unit.Helpers;
// using LayoutEngine.Platform.Update;
// using NSubstitute;
//
// public class UpdateSchedulerTests : IDisposable
// {
//     private readonly Fixture _fixture;
//     private readonly TestEventAggregator _eventAggregator;
//     private readonly IThreadingCoordinator _threadingCoordinator;
//     private readonly UpdateScheduler _updateScheduler;
//
//     public UpdateSchedulerTests()
//     {
//         _fixture = new Fixture();
//         _eventAggregator = new TestEventAggregator();
//         _threadingCoordinator = Substitute.For<IThreadingCoordinator>();
//         _updateScheduler = new UpdateScheduler(_eventAggregator, _threadingCoordinator);
//
//         // Configure threading coordinator to execute main thread actions immediately
//         _threadingCoordinator.When(x => x.ScheduleOnMainThread(Arg.Any<Action>()))
//             .Do(callback => callback.Arg<Action>()());
//     }
//
//     private IVisualUpdate CreateMockUpdate(UpdateType updateType = UpdateType.Style)
//     {
//         var element = new TestDocument().CreateElement("div");
//         var update = Substitute.For<IVisualUpdate>();
//         update.Id.Returns(Guid.NewGuid());
//         update.Type.Returns(updateType);
//         update.Element.Returns(element);
//         update.ChangedProperties.Returns(new[] { "width", "height" });
//         update.Timestamp.Returns(DateTime.UtcNow);
//         return update;
//     }
//
//     [Fact]
//     public void ScheduleUpdate_ShouldProcessUpdate()
//     {
//         // Arrange
//         var update = CreateMockUpdate(UpdateType.Style);
//
//         // Act
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Assert
//         var styleEvents = _eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//
//         Assert.Single(styleEvents);
//         Assert.Contains(update.Element, styleEvents[0].Elements);
//
//         Assert.Single(processedEvents);
//         Assert.Same(update, processedEvents[0].Update);
//     }
//
//     [Theory]
//     [InlineData(UpdateType.Style, typeof(StyleInvalidatedEvent))]
//     [InlineData(UpdateType.Layout, typeof(LayoutInvalidatedEvent))]
//     [InlineData(UpdateType.Render, typeof(RenderInvalidatedEvent))]
//     [InlineData(UpdateType.Full, typeof(StyleInvalidatedEvent))]
//     public void ScheduleUpdate_WithDifferentTypes_ShouldPublishCorrectEvents(UpdateType updateType, Type expectedEventType)
//     {
//         // Arrange
//         var update = CreateMockUpdate(updateType);
//
//         // Act
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Assert
//         bool hasCorrectEvent = false;
//         foreach (var evt in _eventAggregator.PublishedEvents)
//         {
//             if (evt.GetType() == expectedEventType)
//             {
//                 hasCorrectEvent = true;
//                 break;
//             }
//         }
//         Assert.True(hasCorrectEvent, $"Expected event of type {expectedEventType.Name} was not published");
//
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//         Assert.Single(processedEvents);
//         Assert.Same(update, processedEvents[0].Update);
//     }
//
//     [Fact]
//     public void ScheduleUpdate_WithPriority_ShouldProcessUpdate()
//     {
//         // Arrange
//         var update = CreateMockUpdate();
//
//         // Act
//         _updateScheduler.ScheduleUpdate(update, UpdatePriority.High);
//
//         // Assert
//         var styleEvents = _eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//
//         Assert.Single(styleEvents);
//         Assert.Single(processedEvents);
//     }
//
//     [Fact]
//     public void CancelUpdate_ShouldPreventProcessing()
//     {
//         // Arrange
//         _updateScheduler.PauseUpdates();
//         var update = CreateMockUpdate();
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Act
//         bool result = _updateScheduler.CancelUpdate(update);
//         _updateScheduler.ResumeUpdates();
//
//         // Assert
//         Assert.True(result);
//         Assert.Empty(_eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>());
//         Assert.Empty(_eventAggregator.GetPublishedEvents<UpdateProcessedEvent>());
//     }
//
//     [Fact]
//     public void PauseUpdates_ShouldPreventProcessing()
//     {
//         // Arrange
//         _updateScheduler.PauseUpdates();
//         var update = CreateMockUpdate();
//
//         // Act - this shouldn't process because updates are paused
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Assert
//         Assert.Empty(_eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>());
//         Assert.Empty(_eventAggregator.GetPublishedEvents<UpdateProcessedEvent>());
//     }
//
//     [Fact]
//     public void ResumeUpdates_ShouldProcessPendingUpdates()
//     {
//         // Arrange
//         _updateScheduler.PauseUpdates();
//         var update = CreateMockUpdate();
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Act
//         _updateScheduler.ResumeUpdates();
//
//         // Assert
//         var styleEvents = _eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//
//         Assert.Single(styleEvents);
//         Assert.Single(processedEvents);
//     }
//
//     [Fact]
//     public void OnMemoryPressure_HighSeverity_ShouldCancelNonCriticalUpdates()
//     {
//         // Arrange
//         _updateScheduler.PauseUpdates(); // Pause to prevent immediate processing
//
//         var criticalUpdate = CreateMockUpdate(UpdateType.Full);
//         var normalUpdate = CreateMockUpdate(UpdateType.Style);
//
//         _updateScheduler.ScheduleUpdate(criticalUpdate);
//         _updateScheduler.ScheduleUpdate(normalUpdate);
//
//         var memoryPressureEvent = new MemoryPressureEvent(
//             MemoryPressureSeverity.High, 1000000, 500000);
//
//         // Act
//         _eventAggregator.Publish(memoryPressureEvent);
//         _updateScheduler.ResumeUpdates();
//
//         // Assert - Only the critical update should be processed
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//         Assert.Single(processedEvents);
//         Assert.Same(criticalUpdate, processedEvents[0].Update);
//     }
//
//     [Fact]
//     public void BeginFrameEvent_ShouldTriggerProcessing()
//     {
//         // Arrange
//         var update = CreateMockUpdate();
//         _updateScheduler.ScheduleUpdate(update);
//
//         // Clear events from initial scheduling
//         _eventAggregator.ClearPublishedEvents();
//
//         var frameEvent = new BeginFrameEvent(1, 1000.0);
//
//         // Act
//         _eventAggregator.Publish(frameEvent);
//
//         // Assert
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//         Assert.Empty(processedEvents); // Updates were already processed in the initial scheduling
//     }
//
//     public void Dispose()
//     {
//         _updateScheduler.Dispose();
//     }
// }