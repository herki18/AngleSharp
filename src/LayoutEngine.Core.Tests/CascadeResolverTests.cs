namespace LayoutEngine.Core.Tests;

using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using Xunit;

public class CascadeResolverTests : IDisposable
{
    private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
    private readonly ICssParser _cssParser;
    private readonly CascadeResolver _cascadeResolver;
    private readonly IBrowsingContext _context;

    public CascadeResolverTests()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _cssParser = _context.GetService<ICssParser>() ?? new CssParser();
        _styleDeclarationFactory = new CssStyleDeclarationFactory(_context);
        _cascadeResolver = new CascadeResolver(_styleDeclarationFactory, _cssParser);
    }

    [Fact]
    public async void ResolveCascade_WithImportantDeclarations_PrioritizesCorrectly()
    {
        // Arrange
        var html = "<div class='test'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".test") as IElement;

        var normalRule = CreateMatchedRule("div { color: blue; }", StylesheetOrigin.Author, 0);
        var importantRule = CreateMatchedRule("div { color: red !important; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { normalRule, importantRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("red", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithSpecificityConflicts_AppliesToMostSpecific()
    {
        // Arrange
        var html = "<div id='test' class='example'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("#test") as IElement;

        // ID selector has higher specificity than class selector
        var classRule = CreateMatchedRule(".example { color: blue; }", StylesheetOrigin.Author, 0);
        var idRule = CreateMatchedRule("#test { color: red; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { classRule, idRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("red", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithOriginConflicts_FollowsCascadeOrder()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        // Author origin should override user agent origin
        var userAgentRule = CreateMatchedRule("div { color: black; }", StylesheetOrigin.UserAgent, 0);
        var authorRule = CreateMatchedRule("div { color: blue; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { userAgentRule, authorRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("blue", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithInlineStyles_GivesHighestPriority()
    {
        // Arrange
        var html = "<div style='color: green;' class='test'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".test") as IElement;

        var authorRule = CreateMatchedRule(".test { color: red; }", StylesheetOrigin.Author, 0);
        var matchedRules = new[] { authorRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("green", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithDocumentOrder_LastWins()
    {
        // Arrange
        var html = "<div class='test'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".test") as IElement;

        // Same specificity, same origin - document order should decide
        var firstRule = CreateMatchedRule(".test { color: blue; }", StylesheetOrigin.Author, 0);
        var secondRule = CreateMatchedRule(".test { color: red; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { firstRule, secondRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("red", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithUserAgentStyles_AppliesAsLowestPriority()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var userAgentRule = CreateMatchedRule("div { display: block; margin: 0; }", StylesheetOrigin.UserAgent, 0);
        var authorRule = CreateMatchedRule("div { margin: 10px; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { userAgentRule, authorRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("block", result.GetPropertyValue("display")); // From user agent
        Assert.Equal("10px", result.GetPropertyValue("margin"));   // Overridden by author
    }

    [Fact]
    public async void ResolveCascade_WithImportantUserAgentStyles_OverridesNormalAuthorStyles()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var userAgentImportantRule = CreateMatchedRule("div { color: black !important; }", StylesheetOrigin.UserAgent, 0);
        var authorNormalRule = CreateMatchedRule("div { color: red; }", StylesheetOrigin.Author, 1);

        var matchedRules = new[] { userAgentImportantRule, authorNormalRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("black", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithMixedImportanceAndOrigins_ResolvesCorrectly()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var rules = new[]
        {
            CreateMatchedRule("div { color: black; font-size: 12px; }", StylesheetOrigin.UserAgent, 0),
            CreateMatchedRule("div { color: blue !important; }", StylesheetOrigin.UserAgent, 1),
            CreateMatchedRule("div { color: red; font-size: 16px; }", StylesheetOrigin.Author, 2),
            CreateMatchedRule("div { font-size: 14px !important; }", StylesheetOrigin.Author, 3)
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(rules, element!);

        // Assert
        // Important author should win for font-size
        Assert.Equal("14px", result.GetPropertyValue("font-size"));
        // Important user agent should win for color (important declarations reverse cascade order)
        Assert.Equal("blue", result.GetPropertyValue("color"));
    }

    [Fact]
    public async void ResolveCascade_WithInlineImportantStyles_OverridesEverything()
    {
        // Arrange
        var html = "<div style='color: green !important;'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var authorImportantRule = CreateMatchedRule("div { color: red !important; }", StylesheetOrigin.Author, 0);
        var matchedRules = new[] { authorImportantRule };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element!);

        // Assert
        Assert.Equal("green", result.GetPropertyValue("color"));
    }

    private MatchedRule CreateMatchedRule(string cssText, StylesheetOrigin origin, int documentOrder)
    {
        var stylesheet = _cssParser.ParseStyleSheet(cssText);
        var styleRule = stylesheet.Rules[0] as ICssStyleRule;

        var specificity = new Priority(0, 0, 1, 0); // Default specificity for testing
        if (cssText.Contains("#"))
            specificity = new Priority(0, 1, 0, 0); // ID selector

        return new MatchedRule(styleRule, specificity, origin, documentOrder);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}