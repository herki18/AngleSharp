// using LayoutEngine.Contracts.Platform.Dom;
// using LayoutEngine.Contracts.Platform.Events;
// using LayoutEngine.Contracts.Platform.Updates;
//
// namespace LayoutEngine.Platform.Tests.Integration;
//
// using Unit.Helpers;
//
// public class DomManipulationIntegrationTests : IDisposable
// {
//     private readonly PlatformTestEnvironment _testEnvironment;
//     private readonly TestEventAggregator _eventAggregator;
//     private readonly TestDocument _document;
//     private readonly TestElement _rootElement;
//
//     public DomManipulationIntegrationTests()
//     {
//         _testEnvironment = new PlatformTestEnvironment();
//         _eventAggregator = _testEnvironment.EventAggregator;
//         _document = new TestDocument();
//         _rootElement = (TestElement)_document.DocumentElement;
//
//         // Start DOM mutation tracking
//         _testEnvironment.DomMutationTracker.StartTracking(_document);
//     }
//
//     [Fact]
//     public void AttributeChanges_ShouldTriggerStyleInvalidation()
//     {
//         // Arrange
//         var element = _document.CreateElement("div");
//         _rootElement.AppendChild(element);
//         _eventAggregator.ClearPublishedEvents();
//
//         // Act
//         element.SetAttribute("style", "width: 100px; height: 50px;");
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(
//             element, "style", null, "width: 100px; height: 50px;");
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var attributeEvents = _eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
//         Assert.Single(attributeEvents);
//         Assert.Equal("style", attributeEvents[0].AttributeName);
//     }
//
//     [Fact]
//     public void ElementInsertion_ShouldTriggerNodeAddedEvent()
//     {
//         // Arrange
//         var parent = _document.CreateElement("div");
//         _rootElement.AppendChild(parent);
//         var child = _document.CreateElement("span");
//         _eventAggregator.ClearPublishedEvents();
//
//         // Act
//         parent.AppendChild(child);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(child, parent);
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var nodeAddedEvents = _eventAggregator.GetPublishedEvents<DomNodeAddedEvent>();
//         Assert.Single(nodeAddedEvents);
//         Assert.Equal(child, nodeAddedEvents[0].Node);
//         Assert.Equal(parent, nodeAddedEvents[0].Parent);
//     }
//
//     [Fact]
//     public void ElementRemoval_ShouldTriggerNodeRemovedEvent()
//     {
//         // Arrange
//         var parent = _document.CreateElement("div");
//         _rootElement.AppendChild(parent);
//         var child = _document.CreateElement("span");
//         parent.AppendChild(child);
//         _eventAggregator.ClearPublishedEvents();
//
//         // Act
//         parent.RemoveChild(child);
//         _testEnvironment.DomMutationTracker.SignalNodeRemoved(child, parent);
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var nodeRemovedEvents = _eventAggregator.GetPublishedEvents<DomNodeRemovedEvent>();
//         Assert.Single(nodeRemovedEvents);
//         Assert.Equal(child, nodeRemovedEvents[0].Node);
//         Assert.Equal(parent, nodeRemovedEvents[0].Parent);
//     }
//
//     [Fact]
//     public void ComplexDomConstruction_ShouldTriggerAppropriateEvents()
//     {
//         // Create a more complex DOM structure with multiple elements and attributes
//         // and verify all the appropriate events are triggered
//
//         // Arrange
//         _eventAggregator.ClearPublishedEvents();
//
//         // Create container
//         var container = _document.CreateElement("div");
//         container.SetAttribute("id", "container");
//         container.SetAttribute("class", "main-container");
//         _rootElement.AppendChild(container);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(container, _rootElement);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(container, "id", null, "container");
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(container, "class", null, "main-container");
//
//         // Create header
//         var header = _document.CreateElement("header");
//         header.SetAttribute("id", "main-header");
//         container.AppendChild(header);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(header, container);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(header, "id", null, "main-header");
//
//         // Create content
//         var content = _document.CreateElement("div");
//         content.SetAttribute("id", "content");
//         content.SetAttribute("class", "content-area");
//         container.AppendChild(content);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(content, container);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(content, "id", null, "content");
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(content, "class", null, "content-area");
//
//         // Create items in content
//         for (int i = 0; i < 3; i++)
//         {
//             var item = _document.CreateElement("div");
//             item.SetAttribute("class", "item");
//             item.SetAttribute("data-index", i.ToString());
//             content.AppendChild(item);
//             _testEnvironment.DomMutationTracker.SignalNodeAdded(item, content);
//             _testEnvironment.DomMutationTracker.SignalAttributeChanged(item, "class", null, "item");
//             _testEnvironment.DomMutationTracker.SignalAttributeChanged(item, "data-index", null, i.ToString());
//         }
//
//         // Create footer
//         var footer = _document.CreateElement("footer");
//         footer.SetAttribute("id", "main-footer");
//         container.AppendChild(footer);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(footer, container);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(footer, "id", null, "main-footer");
//
//         // Act - Process all events
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var nodeAddedEvents = _eventAggregator.GetPublishedEvents<DomNodeAddedEvent>();
//         var attributeEvents = _eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
//
//         Assert.Equal(6, nodeAddedEvents.Count); // container, header, content, 3 items, footer
//         Assert.Equal(10, attributeEvents.Count); // Various attributes
//     }
//
//     [Fact]
//     public void DomUpdateWithAttributeChanges_ShouldTriggerStyleAndLayoutUpdates()
//     {
//         // Arrange
//         var container = _document.CreateElement("div");
//         container.SetAttribute("id", "container");
//         _rootElement.AppendChild(container);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(container, _rootElement);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(container, "id", null, "container");
//
//         var updateScheduler = _testEnvironment.GetService<IUpdateScheduler>();
//         _eventAggregator.ClearPublishedEvents();
//
//         // Act - Change style attributes
//         container.SetAttribute("style", "width: 200px; height: 150px; margin: 10px;");
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(
//             container, "style", null, "width: 200px; height: 150px; margin: 10px;");
//
//         // Create a style update
//         var update = new TestVisualUpdate(
//             Guid.NewGuid(),
//             UpdateType.Style,
//             container,
//             new[] { "width", "height", "margin" });
//         updateScheduler.ScheduleUpdate(update);
//
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var attributeEvents = _eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
//         var styleEvents = _eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
//         var processedEvents = _eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
//
//         Assert.Single(attributeEvents);
//         Assert.Single(styleEvents);
//         Assert.Single(processedEvents);
//     }
//
//     [Fact]
//     public void DomStructureChanges_ShouldTriggerUpdates()
//     {
//         // Arrange
//         var parent = _document.CreateElement("div");
//         parent.SetAttribute("id", "parent");
//         _rootElement.AppendChild(parent);
//         _testEnvironment.DomMutationTracker.SignalNodeAdded(parent, _rootElement);
//         _testEnvironment.DomMutationTracker.SignalAttributeChanged(parent, "id", null, "parent");
//
//         var updateScheduler = _testEnvironment.GetService<IUpdateScheduler>();
//         _eventAggregator.ClearPublishedEvents();
//
//         // Act - Add and remove children
//         for (int i = 0; i < 3; i++)
//         {
//             var child = _document.CreateElement("div");
//             child.SetAttribute("class", "child");
//             parent.AppendChild(child);
//             _testEnvironment.DomMutationTracker.SignalNodeAdded(child, parent);
//             _testEnvironment.DomMutationTracker.SignalAttributeChanged(child, "class", null, "child");
//         }
//
//         // Remove the middle child
//         var children = parent.ChildNodes;
//         parent.RemoveChild(children[1]);
//         _testEnvironment.DomMutationTracker.SignalNodeRemoved(children[1], parent);
//
//         // Schedule layout update
//         var update = new TestVisualUpdate(
//             Guid.NewGuid(),
//             UpdateType.Layout,
//             parent,
//             new[] { "childNodes" });
//         updateScheduler.ScheduleUpdate(update);
//
//         _testEnvironment.AdvanceTimeAndRunFrame(16);
//
//         // Assert
//         var nodeAddedEvents = _eventAggregator.GetPublishedEvents<DomNodeAddedEvent>();
//         var nodeRemovedEvents = _eventAggregator.GetPublishedEvents<DomNodeRemovedEvent>();
//         var attributeEvents = _eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
//         var layoutEvents = _eventAggregator.GetPublishedEvents<LayoutInvalidatedEvent>();
//
//         Assert.Equal(3, nodeAddedEvents.Count);
//         Assert.Single(nodeRemovedEvents);
//         Assert.Equal(3, attributeEvents.Count);
//         Assert.Single(layoutEvents);
//     }
//
//     public void Dispose()
//     {
//         _testEnvironment.Dispose();
//     }
// }
//
// // Helper class for testing
// public class TestVisualUpdate : IVisualUpdate
// {
//     public TestVisualUpdate(Guid id, UpdateType type, IElement element, IReadOnlyList<string> changedProperties)
//     {
//         Id = id;
//         Type = type;
//         Element = element;
//         ChangedProperties = changedProperties;
//         Timestamp = DateTime.UtcNow;
//     }
//
//     public Guid Id { get; }
//     public UpdateType Type { get; }
//     public IElement Element { get; }
//     public IReadOnlyList<string> ChangedProperties { get; }
//     public DateTime Timestamp { get; }
// }