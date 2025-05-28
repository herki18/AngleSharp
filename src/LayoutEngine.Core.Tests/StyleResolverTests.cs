namespace LayoutEngine.Core.Tests;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class StyleResolverTests
{
    private readonly IElementRuleCollector _ruleCollector;
    private readonly IStyleBuilder _styleBuilder;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleResolver> _logger;
    private readonly StyleResolver _styleResolver;

    public StyleResolverTests()
    {
        _ruleCollector = Substitute.For<IElementRuleCollector>();
        _styleBuilder = Substitute.For<IStyleBuilder>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _logger = Substitute.For<ILogger<StyleResolver>>();
        _styleResolver = new StyleResolver(_ruleCollector, _styleBuilder, _eventAggregator, _logger);
    }

    [Fact]
    public void ResolveStyle_WithMatchingRules_AppliesCorrectSpecificity()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement("div");
        var document = TestHelpers.CreateMockDocument(element);
        var context = CreateMockStyleRecalcContext(document);
        var matchResult = CreateMockMatchResult();
        var expectedStyle = TestHelpers.CreateMockComputedStyle(element);

        _ruleCollector.CollectMatchingRules(element, Arg.Any<IStyleRecalcContext>()).Returns(matchResult);
        _styleBuilder.TakeStyle(element).Returns(expectedStyle);

        // Act
        var result = _styleResolver.ResolveStyle(element, context);

        // Assert
        Assert.Equal(expectedStyle, result);
        _ruleCollector.Received(1).CollectMatchingRules(element, Arg.Any<IStyleRecalcContext>());
        _styleBuilder.Received(1).ApplyMatchedProperties(matchResult, Arg.Any<IStyleRecalcContext>());
        _styleBuilder.Received(1).TakeStyle(element);
    }

    [Fact]
    public void ResolveStyle_WithParentStyle_AppliesInheritance()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement("span");
        var document = TestHelpers.CreateMockDocument(element);
        var parentDeclaration = StyleTestHelpers.CreateMockCssStyleDeclaration();
        var context = CreateMockStyleRecalcContext(document, parentDeclaration);
        var matchResult = CreateMockMatchResult();
        var expectedStyle = TestHelpers.CreateMockComputedStyle(element);

        _ruleCollector.CollectMatchingRules(element, Arg.Any<IStyleRecalcContext>()).Returns(matchResult);
        _styleBuilder.TakeStyle(element).Returns(expectedStyle);

        // Act
        var result = _styleResolver.ResolveStyle(element, context);

        // Assert
        _styleBuilder.Received(1).ApplyInheritance(parentDeclaration);
    }

    [Fact]
    public void ResolveStyle_CachesComputedStyles()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement("div");
        var document = TestHelpers.CreateMockDocument(element);
        var context = CreateMockStyleRecalcContext(document);
        var expectedStyle = TestHelpers.CreateMockComputedStyle(element);

        var matchResult = CreateMockMatchResult();
        _ruleCollector.CollectMatchingRules(element, Arg.Any<IStyleRecalcContext>()).Returns(matchResult);
        _styleBuilder.TakeStyle(element).Returns(expectedStyle);

        // Act
        var result1 = _styleResolver.ResolveStyle(element, context);
        var result2 = _styleResolver.GetComputedStyle(element);

        // Assert
        Assert.Equal(expectedStyle, result1);
        Assert.Equal(expectedStyle, result2);
    }

    [Fact]
    public void RecalcDocumentStyle_OnlyProcessesElementsNeedingRecalc()
    {
        // Create fresh instances to avoid interference
        var ruleCollector = Substitute.For<IElementRuleCollector>();
        var styleBuilder = Substitute.For<IStyleBuilder>();
        var eventAggregator = Substitute.For<IEventAggregator>();
        var logger = Substitute.For<ILogger<StyleResolver>>();
        var styleResolver = new StyleResolver(ruleCollector, styleBuilder, eventAggregator, logger);

        var parentElement = TestHelpers.CreateMockElement("div");
        var childElement = TestHelpers.CreateMockElement("span", parentElement);
        var grandchildElement = TestHelpers.CreateMockElement("em", childElement);

        var children = new TestHtmlCollection(new[] { childElement });
        var grandchildren = new TestHtmlCollection(new[] { grandchildElement });

        parentElement.Children.Returns(children);
        childElement.Children.Returns(grandchildren);
        grandchildElement.Children.Returns(new TestHtmlCollection(new IElement[0]));

        var document = TestHelpers.CreateMockDocument(parentElement);

        TestHelpers.SetupElementInvalidationFlags(parentElement, needsStyle: true);
        TestHelpers.SetupElementInvalidationFlags(childElement, needsStyle: false, childNeedsStyle: true);
        TestHelpers.SetupElementInvalidationFlags(grandchildElement, needsStyle: true);

        var matchResult = CreateMockMatchResult();
        ruleCollector.CollectMatchingRules(Arg.Any<IElement>(), Arg.Any<IStyleRecalcContext>())
            .Returns(matchResult);

        // Use a function to return different styles for different elements
        styleBuilder.TakeStyle(Arg.Any<IElement>()).Returns(callInfo =>
        {
            var element = callInfo.Arg<IElement>();
            return TestHelpers.CreateMockComputedStyle(element);
        });

        // Act
        styleResolver.RecalcDocumentStyle(document);

        // Assert
        // Should process: parent (needs recalc), child (no existing style, needed for inheritance), grandchild (needs recalc)
        ruleCollector.Received(3).CollectMatchingRules(Arg.Any<IElement>(), Arg.Any<IStyleRecalcContext>());

        // Only elements that explicitly needed recalc should have their flags cleared
        parentElement.Received(1).ClearNeedsStyleRecalc();
        grandchildElement.Received(1).ClearNeedsStyleRecalc();
        childElement.DidNotReceive().ClearNeedsStyleRecalc(); // Didn't explicitly need recalc
    }

    [Fact]
    public void RecalcDocumentStyle_PublishesStyleComputedEvent()
    {
        // Create fresh instances to avoid interference
        var ruleCollector = Substitute.For<IElementRuleCollector>();
        var styleBuilder = Substitute.For<IStyleBuilder>();
        var eventAggregator = Substitute.For<IEventAggregator>();
        var logger = Substitute.For<ILogger<StyleResolver>>();
        var styleResolver = new StyleResolver(ruleCollector, styleBuilder, eventAggregator, logger);

        var element = TestHelpers.CreateMockElement("div");
        var document = TestHelpers.CreateMockDocument(element);

        TestHelpers.SetupElementInvalidationFlags(element, needsStyle: true);

        var matchResult = CreateMockMatchResult();
        ruleCollector.CollectMatchingRules(Arg.Any<IElement>(), Arg.Any<IStyleRecalcContext>())
            .Returns(matchResult);

        // Create the computed style mock directly without TestHelpers
        var computedStyle = Substitute.For<IComputedStyle>();
        computedStyle.Element.Returns(element);

        // Create a mock declaration
        var declaration = StyleTestHelpers.CreateMockCssStyleDeclaration();
        computedStyle.Declaration.Returns(declaration);

        styleBuilder.TakeStyle(Arg.Any<IElement>()).Returns(computedStyle);

        // Act
        styleResolver.RecalcDocumentStyle(document);

        // Assert
        eventAggregator.Received(1).Publish(Arg.Is<StyleComputedEvent>(e =>
            e.Elements.Contains(element) && e.ComputedStyles.ContainsKey(element)));
    }

    [Fact]
    public void ClearStyles_RemovesAllCachedStyles()
    {
        // Create a fresh StyleResolver for this test to avoid interference
        var ruleCollector = Substitute.For<IElementRuleCollector>();
        var styleBuilder = Substitute.For<IStyleBuilder>();
        var eventAggregator = Substitute.For<IEventAggregator>();
        var logger = Substitute.For<ILogger<StyleResolver>>();
        var styleResolver = new StyleResolver(ruleCollector, styleBuilder, eventAggregator, logger);

        var element = TestHelpers.CreateMockElement("div");
        var document = TestHelpers.CreateMockDocument(element);
        var context = CreateMockStyleRecalcContext(document);

        var fakeComputedStyle = Substitute.For<IComputedStyle>();
        var matchResult = CreateMockMatchResult();

        // Configure mocks
        ruleCollector.CollectMatchingRules(Arg.Any<IElement>(), Arg.Any<IStyleRecalcContext>())
            .Returns(matchResult);

        // Use a different approach - configure the style builder to always return the fake style
        styleBuilder.TakeStyle(Arg.Any<IElement>()).Returns(fakeComputedStyle);

        // Act
        var result1 = styleResolver.ResolveStyle(element, context);
        Assert.NotNull(styleResolver.GetComputedStyle(element));

        styleResolver.ClearStyles();

        // Assert
        Assert.Null(styleResolver.GetComputedStyle(element));
    }

    [Fact]
    public void RecalcDocumentStyle_WithNestedElements_ProcessesHierarchyCorrectly()
    {
        // Arrange
        var parentElement = TestHelpers.CreateMockElement("div");
        var childElement = TestHelpers.CreateMockElement("span", parentElement);

        parentElement.Children.Returns(new TestHtmlCollection(new[] { childElement }));
        childElement.Children.Returns(new TestHtmlCollection(new IElement[0]));

        var document = TestHelpers.CreateMockDocument(parentElement);

        TestHelpers.SetupElementInvalidationFlags(parentElement, needsStyle: true);
        TestHelpers.SetupElementInvalidationFlags(childElement, needsStyle: true);

        var parentStyle = TestHelpers.CreateMockComputedStyle(parentElement);
        var childStyle = TestHelpers.CreateMockComputedStyle(childElement);

        var matchResult = CreateMockMatchResult();
        _ruleCollector.CollectMatchingRules(Arg.Any<IElement>(), Arg.Any<IStyleRecalcContext>())
            .Returns(matchResult);

        _styleBuilder.TakeStyle(parentElement).Returns(parentStyle);
        _styleBuilder.TakeStyle(childElement).Returns(childStyle);

        // Act
        _styleResolver.RecalcDocumentStyle(document);

        // Assert
        // Child should receive inheritance from parent
        _styleBuilder.Received(1).ApplyInheritance(parentStyle.Declaration);
    }

    private IStyleRecalcContext CreateMockStyleRecalcContext(IDocument document,
        AngleSharp.Css.Dom.ICssStyleDeclaration? parentStyle = null)
    {
        var context = Substitute.For<IStyleRecalcContext>();
        context.Document.Returns(document);
        context.ParentStyle.Returns(parentStyle);
        context.WithElement(Arg.Any<IElement>()).Returns(context);
        context.WithParent(Arg.Any<AngleSharp.Css.Dom.ICssStyleDeclaration>()).Returns(context);
        return context;
    }

    private IMatchResult CreateMockMatchResult()
    {
        var matchResult = Substitute.For<IMatchResult>();
        matchResult.UserAgentRules.Returns(new List<MatchedRule>());
        matchResult.AuthorRules.Returns(new List<MatchedRule>());
        matchResult.InlineStyle.Returns((AngleSharp.Css.Dom.ICssStyleDeclaration?)null);
        return matchResult;
    }
}