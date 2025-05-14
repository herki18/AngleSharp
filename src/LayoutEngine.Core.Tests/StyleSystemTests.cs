// namespace LayoutEngine.Core.Tests;
//
// using System.Collections.Generic;
// using System.Linq;
// using AngleSharp.Dom;
// using Infrastructure.EventAggregator.API.Aggregation;
// using LayoutEngine.Core.Events;
// using LayoutEngine.Core.Style;
// using NSubstitute;
// using Xunit;
//
// public class StyleSystemTests
// {
//     private readonly IEventAggregator _eventAggregator;
//     private readonly StyleSystem _styleSystem;
//
//     public StyleSystemTests()
//     {
//         _eventAggregator = Substitute.For<IEventAggregator>();
//         _styleSystem = new StyleSystem(_eventAggregator);
//     }
//
//     [Fact]
//     public void ComputeStyle_ReturnsComputedStyle()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//
//         // Act
//         var result = _styleSystem.ComputeStyle(element);
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(element, result.Element);
//     }
//
//     [Fact]
//     public void ComputeStyle_SetsDefaultDisplayTypeBasedOnTag()
//     {
//         // Arrange
//         var divElement = TestHelpers.CreateMockElement("div");
//         var spanElement = TestHelpers.CreateMockElement("span");
//
//         // Act
//         var divStyle = _styleSystem.ComputeStyle(divElement);
//         var spanStyle = _styleSystem.ComputeStyle(spanElement);
//
//         // Assert
//         Assert.Equal(DisplayType.Block, divStyle.Display);
//         Assert.Equal(DisplayType.Inline, spanStyle.Display);
//     }
//
//     [Fact]
//     public void ComputeDocumentStyles_PublishesEventEachTimeItsCalled()
//     {
//         // Arrange
//         var child1 = TestHelpers.CreateMockElement("div");
//         var child2 = TestHelpers.CreateMockElement("span");
//         var root = TestHelpers.CreateMockElement("html");
//
//         var children = new List<IElement> { child1, child2 };
//         var htmlCollection = new TestHtmlCollection(children);
//         root.Children.Returns(htmlCollection);
//
//         var document = TestHelpers.CreateMockDocument(root);
//
//         // Act - call twice
//         _styleSystem.ComputeDocumentStyles(document);
//         _styleSystem.ComputeDocumentStyles(document);
//
//         // Assert - expect 2 calls total
//         _eventAggregator.Received(2).Publish(Arg.Is<StyleComputedEvent>(e =>
//             e.Elements.Count >= 3 &&
//             e.ComputedStyles.Count >= 3));
//     }
//
//     [Fact]
//     public void GetComputedStyle_ReturnsNull_WhenStyleNotComputed()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//
//         // Act
//         var result = _styleSystem.GetComputedStyle(element);
//
//         // Assert
//         Assert.Null(result);
//     }
//
//     [Fact]
//     public void GetComputedStyle_ReturnsComputedStyle_WhenStyleComputed()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//         _styleSystem.ComputeStyle(element);
//
//         // Act
//         var result = _styleSystem.GetComputedStyle(element);
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(element, result.Element);
//     }
//
//     [Fact]
//     public void NeedsStyleRecalc_ReturnsFalse_ForInitialElement()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//
//         // Act
//         var result = _styleSystem.NeedsStyleRecalc(element);
//
//         // Assert
//         Assert.False(result);
//     }
//
//     [Fact]
//     public void NeedsStyleRecalc_ReturnsTrue_ForInvalidatedElement()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//         _styleSystem.InvalidateStyle(element, false);
//
//         // Act
//         var result = _styleSystem.NeedsStyleRecalc(element);
//
//         // Assert
//         Assert.True(result);
//     }
//
//     [Fact]
//     public void InvalidateStyle_PublishesStyleInvalidatedEvent()
//     {
//         // Arrange
//         var element = TestHelpers.CreateMockElement();
//
//         // Act
//         _styleSystem.InvalidateStyle(element, false);
//
//         // Assert
//         _eventAggregator.Received(1).Publish(Arg.Is<StyleInvalidatedEvent>(e =>
//             e.Elements.Count == 1 &&
//             e.Elements[0] == element));
//     }
//
//     [Fact]
//     public void InvalidateStyle_WithRecursive_InvalidatesChildrenStyles()
//     {
//         // Arrange
//         var child1 = TestHelpers.CreateMockElement("div");
//         var child2 = TestHelpers.CreateMockElement("span");
//         var parent = TestHelpers.CreateMockElement("div");
//
//         // Create a list of elements (not nodes)
//         var childElements = new List<IElement> { child1, child2 };
//
//         // Use the TestHtmlCollection that worked in the previous test
//         var htmlCollection = new TestHtmlCollection(childElements);
//
//         // Set up the parent to return this collection
//         parent.Children.Returns(htmlCollection);
//
//         // Act
//         _styleSystem.InvalidateStyle(parent, true);
//
//         // Assert
//         _eventAggregator.Received(1).Publish(Arg.Is<StyleInvalidatedEvent>(e =>
//             e.Elements.Count == 3 &&
//             e.Elements.Contains(parent) &&
//             e.Elements.Contains(child1) &&
//             e.Elements.Contains(child2)));
//
//         Assert.True(_styleSystem.NeedsStyleRecalc(parent));
//         Assert.True(_styleSystem.NeedsStyleRecalc(child1));
//         Assert.True(_styleSystem.NeedsStyleRecalc(child2));
//     }
//
//     [Fact]
//     public void ClearStyles_RemovesAllComputedStyles()
//     {
//         // Arrange
//         var element1 = TestHelpers.CreateMockElement();
//         var element2 = TestHelpers.CreateMockElement();
//
//         _styleSystem.ComputeStyle(element1);
//         _styleSystem.ComputeStyle(element2);
//
//         // Verify styles exist
//         Assert.NotNull(_styleSystem.GetComputedStyle(element1));
//         Assert.NotNull(_styleSystem.GetComputedStyle(element2));
//
//         // Act
//         _styleSystem.ClearStyles();
//
//         // Assert
//         Assert.Null(_styleSystem.GetComputedStyle(element1));
//         Assert.Null(_styleSystem.GetComputedStyle(element2));
//     }
// }
