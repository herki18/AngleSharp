using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Style;
using NSubstitute;
using Xunit;

namespace LayoutEngine.Core.Tests;

public class LayoutSystemTests
{
    private readonly IStyleSystem _styleSystem;
    private readonly IEventAggregator _eventAggregator;
    private readonly LayoutSystem _layoutSystem;

    public LayoutSystemTests()
    {
        _styleSystem = Substitute.For<IStyleSystem>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _layoutSystem = new LayoutSystem(_styleSystem, _eventAggregator);
    }

    [Fact]
    public void PerformLayout_ComputesDocumentStyles()
    {
        // Arrange
        var document = TestHelpers.CreateMockDocument();

        // Act
        var result = _layoutSystem.PerformLayout(document);

        // Assert
        _styleSystem.Received(1).ComputeDocumentStyles(document);
    }

    [Fact]
    public void PerformLayout_ReturnsLayoutResult_WithRootFragment()
    {
        // Arrange
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);

        SetUpStyleSystemForElement(rootElement);

        // Act
        var result = _layoutSystem.PerformLayout(document);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RootFragment);
        Assert.Equal(rootElement, result.RootFragment.Element);
    }

    [Fact]
    public void PerformLayout_PublishesFragmentTreeUpdatedEvent()
    {
        // Arrange
        var document = TestHelpers.CreateMockDocument();
        SetUpStyleSystemForElement(document.DocumentElement);

        // Act
        _layoutSystem.PerformLayout(document);

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Any<FragmentTreeUpdatedEvent>());
    }

    [Fact]
    public void NeedsLayout_ReturnsFalse_ForInitialElement()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        var result = _layoutSystem.NeedsLayout(element);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsLayout_ReturnsTrue_ForInvalidatedElement()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        _layoutSystem.InvalidateLayout(element, false);

        // Act
        var result = _layoutSystem.NeedsLayout(element);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InvalidateLayout_PublishesLayoutInvalidatedEvent()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        _layoutSystem.InvalidateLayout(element, false);

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Is<LayoutInvalidatedEvent>(e =>
            e.Elements.Count == 1 &&
            e.Elements[0] == element));
    }

    [Fact]
    public void InvalidateLayout_WithRecursive_InvalidatesChildLayouts()
    {
        // Arrange
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var parent = TestHelpers.CreateMockElement("div");

        // Set up element hierarchy using TestHtmlCollection
        var children = new List<IElement> { child1, child2 };
        var htmlCollection = new TestHtmlCollection(children);

        // Configure the parent element to return our TestHtmlCollection
        parent.Children.Returns(htmlCollection);

        // Act
        _layoutSystem.InvalidateLayout(parent, true);

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Is<LayoutInvalidatedEvent>(e =>
            e.Elements.Count == 3 &&
            e.Elements.Contains(parent) &&
            e.Elements.Contains(child1) &&
            e.Elements.Contains(child2)));

        Assert.True(_layoutSystem.NeedsLayout(parent));
        Assert.True(_layoutSystem.NeedsLayout(child1));
        Assert.True(_layoutSystem.NeedsLayout(child2));
    }

    [Fact]
    public void GetFragmentTree_ThrowsException_WhenNoLayoutPerformed()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _layoutSystem.GetFragmentTree());
    }

    [Fact]
    public void GetFragmentTree_ReturnsFragmentTree_AfterLayoutPerformed()
    {
        // Arrange
        var document = TestHelpers.CreateMockDocument();
        SetUpStyleSystemForElement(document.DocumentElement);
        _layoutSystem.PerformLayout(document);

        // Act
        var result = _layoutSystem.GetFragmentTree();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RootFragment);
    }

    [Fact]
    public void GetLayoutInfo_ReturnsNull_WhenNoLayoutPerformed()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        var result = _layoutSystem.GetLayoutInfo(element);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLayoutInfo_ReturnsLayoutInfo_AfterLayoutPerformed()
    {
        // Arrange
        var rootElement = TestHelpers.CreateMockElement("html");
        var childElement = TestHelpers.CreateMockElement("div", rootElement);

        // Set up element hierarchy using TestHtmlCollection
        var children = new List<IElement> { childElement };
        var htmlCollection = new TestHtmlCollection(children);

        // Configure the root element to return our TestHtmlCollection
        rootElement.Children.Returns(htmlCollection);

        var document = TestHelpers.CreateMockDocument(rootElement);

        SetUpStyleSystemForElement(rootElement);
        SetUpStyleSystemForElement(childElement);

        _layoutSystem.PerformLayout(document);

        // Act
        var result = _layoutSystem.GetLayoutInfo(childElement);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(childElement, result.Fragments[0].Element);
    }

    private void SetUpStyleSystemForElement(IElement element, string display = "block")
    {
        var computedStyle = TestHelpers.CreateMockComputedStyle(element, display);
        _styleSystem.GetComputedStyle(element).Returns(computedStyle);
        _styleSystem.ComputeStyle(element).Returns(computedStyle);
    }
}