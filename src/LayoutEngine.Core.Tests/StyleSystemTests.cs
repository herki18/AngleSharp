namespace LayoutEngine.Core.Tests;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Style.Internal;
using Xunit;

public class StyleSystemTests
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleSystem> _logger;
    private readonly StyleSystem _styleSystem;

    public StyleSystemTests()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _logger = Substitute.For<ILogger<StyleSystem>>();
        _styleSystem = new StyleSystem(_eventAggregator, _logger);
    }

    [Fact]
    public void ComputeStyle_ReturnsComputedStyle()
    {
        var element = TestHelpers.CreateMockElement();

        var result = _styleSystem.ComputeStyle(element);

        Assert.NotNull(result);
        Assert.Equal(element, result.Element);
    }

    [Fact]
    public void ComputeDocumentStyles_ProcessesDocumentElement()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);

        // Set up element to need style recalc
        TestHelpers.SetupElementInvalidationFlags(rootElement, needsStyle: true);

        _styleSystem.ComputeDocumentStyles(document);

        // Verify that ClearNeedsStyleRecalc was called
        rootElement.Received(1).ClearNeedsStyleRecalc();
    }

    [Fact]
    public void ComputeDocumentStyles_PublishesStyleComputedEvent()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);

        _styleSystem.ComputeDocumentStyles(document);

        _eventAggregator.Received(1).Publish(Arg.Any<StyleComputedEvent>());
    }

    [Fact]
    public void NeedsStyleRecalc_UsesNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        // Set up mock to return false initially
        element.NeedsStyleRecalc().Returns(false);
        Assert.False(_styleSystem.NeedsStyleRecalc(element));

        // Set up mock to return true
        element.NeedsStyleRecalc().Returns(true);
        Assert.True(_styleSystem.NeedsStyleRecalc(element));
    }

    [Fact]
    public void InvalidateStyle_SetsNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        _styleSystem.InvalidateStyle(element, false);

        // Verify that SetNeedsStyleRecalc was called
        element.Received(1).SetNeedsStyleRecalc();
    }

    [Fact]
    public void InvalidateStyle_WithRecursive_InvalidatesChildStyles()
    {
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var parent = TestHelpers.CreateMockElement("div");
        var children = new List<IElement> { child1, child2 };
        var htmlCollection = new TestHtmlCollection(children);
        parent.Children.Returns(htmlCollection);

        _styleSystem.InvalidateStyle(parent, true);

        // Verify that SetNeedsStyleRecalc was called on all elements
        parent.Received(1).SetNeedsStyleRecalc();
        child1.Received(1).SetNeedsStyleRecalc();
        child2.Received(1).SetNeedsStyleRecalc();
    }

    [Fact]
    public void GetComputedStyle_ReturnsStoredStyle()
    {
        var element = TestHelpers.CreateMockElement();

        // First compute a style
        var computedStyle = _styleSystem.ComputeStyle(element);

        // Then retrieve it
        var retrievedStyle = _styleSystem.GetComputedStyle(element);

        Assert.Equal(computedStyle, retrievedStyle);
    }

    [Fact]
    public void GetComputedStyle_ReturnsNull_WhenNoStyleComputed()
    {
        var element = TestHelpers.CreateMockElement();

        var result = _styleSystem.GetComputedStyle(element);

        Assert.Null(result);
    }

    [Fact]
    public void ComputeDocumentStyles_ProcessesChildElements()
    {
        var child = TestHelpers.CreateMockElement("div");
        var parent = TestHelpers.CreateMockElement("html");
        var children = new List<IElement> { child };
        var htmlCollection = new TestHtmlCollection(children);
        parent.Children.Returns(htmlCollection);
        var document = TestHelpers.CreateMockDocument(parent);

        // Set up parent to have child that needs style recalc
        TestHelpers.SetupElementInvalidationFlags(parent, childNeedsStyle: true);
        TestHelpers.SetupElementInvalidationFlags(child, needsStyle: true);

        _styleSystem.ComputeDocumentStyles(document);

        // Both parent and child should have their flags cleared
        parent.Received(1).ClearNeedsStyleRecalc();
        child.Received(1).ClearNeedsStyleRecalc();
    }

    [Fact]
    public void ComputeDocumentStyles_SetsLayoutFlags_WhenStyleAffectsLayout()
    {
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Set up element to need style recalc
        TestHelpers.SetupElementInvalidationFlags(element, needsStyle: true);

        _styleSystem.ComputeDocumentStyles(document);

        // Should set layout and paint invalidation flags when style changes
        element.Received(1).SetNeedsLayout();
        element.Received(1).SetNeedsPaintInvalidation();
    }

    [Fact]
    public void ClearStyles_RemovesAllComputedStyles()
    {
        var element = TestHelpers.CreateMockElement();

        // Compute a style first
        _styleSystem.ComputeStyle(element);
        Assert.NotNull(_styleSystem.GetComputedStyle(element));

        // Clear all styles
        _styleSystem.ClearStyles();

        // Style should be gone
        Assert.Null(_styleSystem.GetComputedStyle(element));
    }
}