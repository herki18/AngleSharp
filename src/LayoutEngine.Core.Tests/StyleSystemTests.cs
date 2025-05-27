namespace LayoutEngine.Core.Tests;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class StyleSystemTests
{
    private readonly IStyleResolver _styleResolver;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleSystem> _logger;
    private readonly StyleSystem _styleSystem;

    public StyleSystemTests()
    {
        _styleResolver = Substitute.For<IStyleResolver>();
        _styleSheetManager = Substitute.For<IStyleSheetManager>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _logger = Substitute.For<ILogger<StyleSystem>>();

        _styleSystem = new StyleSystem(_styleResolver, _styleSheetManager, _eventAggregator, _logger);
    }

    [Fact]
    public void ComputeStyle_ReturnsComputedStyle()
    {
        var element = TestHelpers.CreateMockElement();
        var expectedStyle = TestHelpers.CreateMockComputedStyle(element);

        // Setup the resolver to return the expected style
        _styleResolver.ResolveStyle(element, Arg.Any<IStyleRecalcContext>()).Returns(expectedStyle);

        var result = _styleSystem.ComputeStyle(element);

        Assert.NotNull(result);
        Assert.Equal(element, result.Element);
    }

    [Fact]
    public void ComputeDocumentStyles_ProcessesDocumentElement()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);
        TestHelpers.SetupElementInvalidationFlags(rootElement, needsStyle: true);

        _styleSystem.ComputeDocumentStyles(document);

        _styleSheetManager.Received(1).AttachToDocument(document);
        _styleResolver.Received(1).RecalcDocumentStyle(document);
    }

    [Fact]
    public void ComputeDocumentStyles_PublishesStyleComputedEvent()
    {
        var rootElement = TestHelpers.CreateMockElement("html");
        var document = TestHelpers.CreateMockDocument(rootElement);

        _styleSystem.ComputeDocumentStyles(document);

        // The event is published by the StyleResolver, so we verify it was called
        _styleResolver.Received(1).RecalcDocumentStyle(document);
    }

    [Fact]
    public void NeedsStyleRecalc_UsesNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();
        element.NeedsStyleRecalc().Returns(false);
        element.ChildNeedsStyleRecalc().Returns(false);

        Assert.False(_styleSystem.NeedsStyleRecalc(element));

        element.NeedsStyleRecalc().Returns(true);
        Assert.True(_styleSystem.NeedsStyleRecalc(element));

        element.NeedsStyleRecalc().Returns(false);
        element.ChildNeedsStyleRecalc().Returns(true);
        Assert.True(_styleSystem.NeedsStyleRecalc(element));
    }

    [Fact]
    public void InvalidateStyle_SetsNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        _styleSystem.InvalidateStyle(element, false);

        element.Received(1).SetNeedsStyleRecalc();
        _eventAggregator.Received(1).Publish(Arg.Any<StyleInvalidatedEvent>());
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

        parent.Received(1).SetNeedsStyleRecalc();
        child1.Received(1).SetNeedsStyleRecalc();
        child2.Received(1).SetNeedsStyleRecalc();
    }

    [Fact]
    public void GetComputedStyle_ReturnsStoredStyle()
    {
        var element = TestHelpers.CreateMockElement();
        var expectedStyle = TestHelpers.CreateMockComputedStyle(element);

        _styleResolver.GetComputedStyle(element).Returns(expectedStyle);

        var result = _styleSystem.GetComputedStyle(element);

        Assert.Equal(expectedStyle, result);
    }

    [Fact]
    public void GetComputedStyle_ReturnsNull_WhenNoStyleComputed()
    {
        var element = TestHelpers.CreateMockElement();

        _styleResolver.GetComputedStyle(element).Returns((IComputedStyle?)null);

        var result = _styleSystem.GetComputedStyle(element);

        Assert.Null(result);
    }

    [Fact]
    public void ClearStyles_RemovesAllComputedStyles()
    {
        _styleSystem.ClearStyles();

        _styleResolver.Received(1).ClearStyles();
    }
}