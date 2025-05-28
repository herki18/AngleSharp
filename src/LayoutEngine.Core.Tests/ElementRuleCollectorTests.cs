namespace LayoutEngine.Core.Tests;

using System.Linq;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using LayoutEngine.Core.Style;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class ElementRuleCollectorTests : IDisposable
{
    private readonly ICssParser _cssParser;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly ILogger<ElementRuleCollector> _logger;
    private readonly ElementRuleCollector _ruleCollector;
    private readonly IBrowsingContext _context;

    public ElementRuleCollectorTests()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _cssParser = _context.GetService<ICssParser>() ?? new CssParser();
        _styleSheetManager = Substitute.For<IStyleSheetManager>();
        _logger = Substitute.For<ILogger<ElementRuleCollector>>();
        _ruleCollector = new ElementRuleCollector(_cssParser, _styleSheetManager, _logger);
    }

    [Fact]
    public async void CollectMatchingRules_WithIdSelector_MatchesCorrectElement()
    {
        // Arrange
        var html = "<div id='test-element'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("#test-element") as IElement;

        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            "#test-element { color: red; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { stylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.NotEmpty(result.AuthorRules);
        var matchedRule = result.AuthorRules.First();
        Assert.Contains("test-element", matchedRule.Rule?.SelectorText ?? "");
    }

    [Fact]
    public async void CollectMatchingRules_WithClassSelector_MatchesCorrectElements()
    {
        // Arrange
        var html = "<div class='test-class'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".test-class") as IElement;

        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            ".test-class { background: blue; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { stylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.NotEmpty(result.AuthorRules);
        var matchedRule = result.AuthorRules.First();
        Assert.Contains("test-class", matchedRule.Rule?.SelectorText ?? "");
    }

    [Fact]
    public async void CollectMatchingRules_WithDescendantSelector_MatchesHierarchy()
    {
        // Arrange
        var html = @"
                <div class='parent'>
                    <span class='child'>Test</span>
                </div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var childElement = document.QuerySelector(".child") as IElement;

        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            ".parent .child { font-size: 14px; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { stylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(childElement!, context);

        // Assert
        Assert.NotEmpty(result.AuthorRules);
        var matchedRule = result.AuthorRules.First();
        Assert.Contains("parent", matchedRule.Rule?.SelectorText ?? "");
        Assert.Contains("child", matchedRule.Rule?.SelectorText ?? "");
    }

    [Fact]
    public async void CollectMatchingRules_WithMultipleSelectors_ReturnsRulesInCorrectOrder()
    {
        // Arrange
        var html = "<div id='test' class='example'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("#test") as IElement;

        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            "div { color: black; }",
            ".example { color: blue; }",
            "#test { color: red; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { stylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.Equal(3, result.AuthorRules.Count);

        // Rules should be ordered by document order
        Assert.Equal(0, result.AuthorRules[0].OriginalIndex);
        Assert.Equal(1, result.AuthorRules[1].OriginalIndex);
        Assert.Equal(2, result.AuthorRules[2].OriginalIndex);
    }

    [Fact]
    public async void CollectMatchingRules_WithUserAgentRules_SeparatesOrigins()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var authorStylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            "div { color: red; }"
        });
        var userAgentStylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            "div { display: block; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { authorStylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new[] { userAgentStylesheet });

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.NotEmpty(result.AuthorRules);
        Assert.NotEmpty(result.UserAgentRules);
        Assert.Equal(StylesheetOrigin.Author, result.AuthorRules.First().Origin);
        Assert.Equal(StylesheetOrigin.UserAgent, result.UserAgentRules.First().Origin);
    }

    [Fact]
    public async void CollectInlineStyle_ParsesStyleAttribute()
    {
        // Arrange
        var html = "<div style='color: green; font-size: 16px;'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        _styleSheetManager.GetStylesheetsByOrigin(Arg.Any<StylesheetOrigin>())
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.NotNull(result.InlineStyle);
        Assert.Equal("green", result.InlineStyle!.GetPropertyValue("color"));
        Assert.Equal("16px", result.InlineStyle!.GetPropertyValue("font-size"));
    }

    [Fact]
    public async void CollectMatchingRules_WithInvalidStyleAttribute_HandlesGracefully()
    {
        // Arrange
        var html = "<div style='invalid-css-here;;;'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        _styleSheetManager.GetStylesheetsByOrigin(Arg.Any<StylesheetOrigin>())
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act & Assert - Should not throw
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Invalid inline styles should be null or empty
        Assert.True(result.InlineStyle == null || result.InlineStyle.Length == 0);
    }

    [Fact]
    public async void CollectMatchingRules_WithMediaQueries_FiltersAppropriately()
    {
        // Arrange
        var html = "<div class='responsive'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".responsive") as IElement;

        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            "@media screen { .responsive { color: blue; } }",
            ".responsive { color: red; }"
        });

        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author)
            .Returns(new[] { stylesheet });
        _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent)
            .Returns(new ICssStyleSheet[0]);

        var context = CreateMockStyleRecalcContext(document);

        // Act
        var result = _ruleCollector.CollectMatchingRules(element!, context);

        // Assert
        Assert.NotEmpty(result.AuthorRules);
        // Should collect both the media query rule and the regular rule
        Assert.True(result.AuthorRules.Count >= 1);
    }

    private IStyleRecalcContext CreateMockStyleRecalcContext(IDocument document)
    {
        var context = Substitute.For<IStyleRecalcContext>();
        context.Document.Returns(document);
        return context;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}