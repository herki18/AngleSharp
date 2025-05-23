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
        var document = TestHelpers.CreateMockDocument();

        var result = _layoutSystem.PerformLayout(document);

        _styleSystem.Received(1).ComputeDocumentStyles(document);
    }

    [Fact]
    public void PerformLayout_ReturnsLayoutResult_WithRootFragment()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);
        SetUpStyleSystemForElement(rootElement);

        var result = _layoutSystem.PerformLayout(document);

        Assert.NotNull(result);
        Assert.NotNull(result.RootFragment);
        Assert.Equal(rootElement, result.RootFragment.Element);
    }

    [Fact]
    public void PerformLayout_PublishesFragmentTreeUpdatedEvent()
    {
        var document = TestHelpers.CreateMockDocument();
        SetUpStyleSystemForElement(document.DocumentElement);

        _layoutSystem.PerformLayout(document);

        _eventAggregator.Received(1).Publish(Arg.Any<FragmentTreeUpdatedEvent>());
    }

    [Fact]
    public void PerformLayout_ClearsLayoutFlags()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);
        SetUpStyleSystemForElement(rootElement);

        _layoutSystem.PerformLayout(document);

        // Verify that ClearNeedsLayout was called on the root element
        rootElement.Received(1).ClearNeedsLayout();
    }

    [Fact]
    public void NeedsLayout_UsesNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        // Set up mock to return false initially
        element.NeedsLayout().Returns(false);
        Assert.False(_layoutSystem.NeedsLayout(element));

        // Set up mock to return true after invalidation
        element.NeedsLayout().Returns(true);
        Assert.True(_layoutSystem.NeedsLayout(element));
    }

    [Fact]
    public void InvalidateLayout_SetsNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        _layoutSystem.InvalidateLayout(element, false);

        // Verify that SetNeedsLayout was called
        element.Received(1).SetNeedsLayout();
    }

    [Fact]
    public void InvalidateLayout_PublishesLayoutInvalidatedEvent()
    {
        var element = TestHelpers.CreateMockElement();

        _layoutSystem.InvalidateLayout(element, false);

        _eventAggregator.Received(1).Publish(Arg.Is<LayoutInvalidatedEvent>(e =>
            e.Elements.Count == 1 &&
            e.Elements[0] == element));
    }

    [Fact]
    public void InvalidateLayout_WithRecursive_InvalidatesChildLayouts()
    {
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var parent = TestHelpers.CreateMockElement("div");
        var children = new List<IElement> { child1, child2 };
        var htmlCollection = new TestHtmlCollection(children);
        parent.Children.Returns(htmlCollection);

        _layoutSystem.InvalidateLayout(parent, true);

        // Verify SetNeedsLayout was called on parent and all children
        parent.Received(1).SetNeedsLayout();
        child1.Received(1).SetNeedsLayout();
        child2.Received(1).SetNeedsLayout();

        _eventAggregator.Received(1).Publish(Arg.Is<LayoutInvalidatedEvent>(e =>
            e.Elements.Count == 3 &&
            e.Elements.Contains(parent) &&
            e.Elements.Contains(child1) &&
            e.Elements.Contains(child2)));
    }

    [Fact]
    public void GetFragmentTree_ThrowsException_WhenNoLayoutPerformed()
    {
        Assert.Throws<InvalidOperationException>(() => _layoutSystem.GetFragmentTree());
    }

    [Fact]
    public void GetFragmentTree_ReturnsFragmentTree_AfterLayoutPerformed()
    {
        var document = TestHelpers.CreateMockDocument();
        SetUpStyleSystemForElement(document.DocumentElement);

        _layoutSystem.PerformLayout(document);

        var result = _layoutSystem.GetFragmentTree();

        Assert.NotNull(result);
        Assert.NotNull(result.RootFragment);
    }

    [Fact]
    public void GetLayoutInfo_ReturnsNull_WhenNoLayoutPerformed()
    {
        var element = TestHelpers.CreateMockElement();

        var result = _layoutSystem.GetLayoutInfo(element);

        Assert.Null(result);
    }

    [Fact]
    public void GetLayoutInfo_ReturnsLayoutInfo_AfterLayoutPerformed()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var childElement = TestHelpers.CreateMockElement("div", rootElement);
        var children = new List<IElement> { childElement };
        var htmlCollection = new TestHtmlCollection(children);
        rootElement.Children.Returns(htmlCollection);
        var document = TestHelpers.CreateMockDocument(rootElement);

        SetUpStyleSystemForElement(rootElement);
        SetUpStyleSystemForElement(childElement);

        _layoutSystem.PerformLayout(document);

        var result = _layoutSystem.GetLayoutInfo(childElement);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Fragments);
        Assert.Equal(childElement, result.Fragments[0].Element);
    }

    [Fact]
    public void PerformLayout_WithChildElements_ClearsAllLayoutFlags()
    {
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var rootElement = TestHelpers.CreateMockElement("html");
        var children = new List<IElement> { child1, child2 };
        var htmlCollection = new TestHtmlCollection(children);
        rootElement.Children.Returns(htmlCollection);

        // Set up children to have empty collections too
        var emptyCollection = new TestHtmlCollection(new List<IElement>());
        child1.Children.Returns(emptyCollection);
        child2.Children.Returns(emptyCollection);

        var document = TestHelpers.CreateMockDocument(rootElement);

        SetUpStyleSystemForElement(rootElement);
        SetUpStyleSystemForElement(child1);
        SetUpStyleSystemForElement(child2);

        _layoutSystem.PerformLayout(document);

        // Verify that ClearNeedsLayout was called on all elements
        rootElement.Received(1).ClearNeedsLayout();
        child1.Received(1).ClearNeedsLayout();
        child2.Received(1).ClearNeedsLayout();
    }

    [Fact]
    public void InvalidateLayout_WithoutRecursive_OnlyInvalidatesTargetElement()
    {
        var child = TestHelpers.CreateMockElement("div");
        var parent = TestHelpers.CreateMockElement("div");
        var children = new List<IElement> { child };
        var htmlCollection = new TestHtmlCollection(children);
        parent.Children.Returns(htmlCollection);

        _layoutSystem.InvalidateLayout(parent, false);

        // Only parent should be invalidated
        parent.Received(1).SetNeedsLayout();
        child.DidNotReceive().SetNeedsLayout();
    }

    private void SetUpStyleSystemForElement(IElement element, string display = "block")
    {
        var computedStyle = TestHelpers.CreateMockComputedStyle(element, display);
        _styleSystem.GetComputedStyle(element).Returns(computedStyle);
        _styleSystem.ComputeStyle(element).Returns(computedStyle);
    }
}