// namespace LayoutEngine.Platform.Tests.Unit.DOM;
//
// using System;
// using AutoFixture;
// using Helpers;
// using Infrastructure.EventAggregator.API.Aggregation;
// using LayoutEngine.Contracts.Platform.Dom;
// using LayoutEngine.Contracts.Platform.Events;
// using LayoutEngine.Platform.DOM;
// using Xunit;
//
// public class DomMutationTrackerTests : IDisposable
// {
//     private readonly Fixture _fixture;
//     private readonly IEventAggregator _eventAggregator;
//     private readonly IElementAdapter _elementAdapter;
//     private readonly DomMutationTracker _mutationTracker;
//     private readonly TestDocument _document;
//     private readonly TestElement _element;
//
//     public DomMutationTrackerTests()
//     {
//         _fixture = new Fixture();
//         _eventAggregator = new TestEventAggregator();
//         _elementAdapter = new ElementAdapter();
//         _mutationTracker = new DomMutationTracker(_eventAggregator, _elementAdapter);
//         _document = new TestDocument();
//         _element = _document.CreateElement("div");
//         ((TestElement)_document.DocumentElement).AppendChild(_element);
//     }
//
//     [Fact]
//     public void StartTracking_ShouldNotThrow()
//     {
//         // Act & Assert
//         var exception = Record.Exception(() => _mutationTracker.StartTracking(_document));
//         Assert.Null(exception);
//     }
//
//     [Fact]
//     public void StopTracking_AfterStartTracking_ShouldNotThrow()
//     {
//         // Arrange
//         _mutationTracker.StartTracking(_document);
//
//         // Act & Assert
//         var exception = Record.Exception(() => _mutationTracker.StopTracking());
//         Assert.Null(exception);
//     }
//
//     [Fact]
//     public void SignalAttributeChanged_ShouldPublishEvent()
//     {
//         // Arrange
//         var testEventAggregator = (TestEventAggregator)_eventAggregator;
//         string attributeName = "id";
//         string? oldValue = null;
//         string newValue = "test-id";
//
//         // Act
//         _mutationTracker.SignalAttributeChanged(_element, attributeName, oldValue, newValue);
//
//         // Assert
//         var publishedEvents = testEventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
//         Assert.Single(publishedEvents);
//         var attributeEvent = publishedEvents[0];
//         Assert.Equal(_element, attributeEvent.Node);
//         Assert.Equal(attributeName, attributeEvent.AttributeName);
//         Assert.Equal(oldValue, attributeEvent.OldValue);
//         Assert.Equal(newValue, attributeEvent.NewValue);
//     }
//
//     [Fact]
//     public void SignalNodeAdded_ShouldPublishEvent()
//     {
//         // Arrange
//         var testEventAggregator = (TestEventAggregator)_eventAggregator;
//         var childElement = _document.CreateElement("span");
//
//         // Act
//         _mutationTracker.SignalNodeAdded(childElement, _element);
//
//         // Assert
//         var publishedEvents = testEventAggregator.GetPublishedEvents<DomNodeAddedEvent>();
//         Assert.Single(publishedEvents);
//         var nodeEvent = publishedEvents[0];
//         Assert.Equal(childElement, nodeEvent.Node);
//         Assert.Equal(_element, nodeEvent.Parent);
//     }
//
//     [Fact]
//     public void SignalNodeRemoved_ShouldPublishEvent()
//     {
//         // Arrange
//         var testEventAggregator = (TestEventAggregator)_eventAggregator;
//         var childElement = _document.CreateElement("span");
//         _element.AppendChild(childElement);
//
//         // Act
//         _mutationTracker.SignalNodeRemoved(childElement, _element);
//
//         // Assert
//         var publishedEvents = testEventAggregator.GetPublishedEvents<DomNodeRemovedEvent>();
//         Assert.Single(publishedEvents);
//         var nodeEvent = publishedEvents[0];
//         Assert.Equal(childElement, nodeEvent.Node);
//         Assert.Equal(_element, nodeEvent.Parent);
//     }
//
//     [Theory]
//     [InlineData(null, "id", "value")]
//     [InlineData("element", null, "value")]
//     public void SignalAttributeChanged_WithInvalidParameters_ShouldThrow(string elementId, string attributeName, string attributeValue)
//     {
//         // Arrange
//         IElement? element = elementId != null ? _element : null;
//         string? attribute = attributeName;
//
//         // Act & Assert
//         if (element == null)
//         {
//             Assert.Throws<ArgumentNullException>(() =>
//                 _mutationTracker.SignalAttributeChanged(element!, attribute!, null, attributeValue));
//         }
//         else if (attribute == null)
//         {
//             Assert.Throws<ArgumentException>(() =>
//                 _mutationTracker.SignalAttributeChanged(element, attribute!, null, attributeValue));
//         }
//     }
//
//     public void Dispose()
//     {
//         _mutationTracker.Dispose();
//     }
// }