using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.StyleSystem.Core;

namespace AngleSharp.StyleSystem.Tests;

using Core.Interfaces;
using Css;

[TestFixture]
public class CascadeResolverTests
{
    private IBrowsingContext _context;
    private ICascadeResolver _cascadeResolver;
    private IHtmlParser _parser;
    private IDocument _document;
    private ICssParser _cssParser;
    private ICssStyleSheet _stylesheet;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _cascadeResolver = new CascadeResolver(_context);
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("");
        _cssParser = new CssParser();
        _stylesheet = _cssParser.ParseStyleSheet("");
    }

    [TearDown]
    public void Teardown()
    {
        _context?.Dispose();
        _document?.Dispose();
    }

    [Test]
    public void ResolveCascade_WithNoRules_ReturnsEmptyDeclaration()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var matchedRules = new List<MatchedRule>();

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void ResolveCascade_WithSingleRule_AppliesRuleProperties()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var styleRule = _cssParser.ParseRule(_stylesheet, "div { color: red; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(styleRule, new Priority(1), StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ResolveCascade_WithMultipleRules_AppliesCorrectCascade()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule(_stylesheet, "div { color: red; font-size: 12px; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule(_stylesheet, "div { color: blue; margin: 10px; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule1, new Priority(1), StylesheetOrigin.Author, 0),
            new MatchedRule(rule2, new Priority(1), StylesheetOrigin.Author, 1)
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(6));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // Later rule wins
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("12px"));
        Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px"));
    }

    [Test]
    public void ResolveCascade_WithSpecificity_HigherSpecificityWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule(_stylesheet, "div { color: red; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule(_stylesheet, "div.special { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule1, new Priority(1), StylesheetOrigin.Author, 0),
            new MatchedRule(rule2, new Priority(11), StylesheetOrigin.Author, 1) // Higher specificity
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // Higher specificity wins
    }

    [Test]
    public void ResolveCascade_WithOrigin_HigherOriginWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule(_stylesheet, "div { color: red; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule(_stylesheet, "div { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule1, new Priority(1), StylesheetOrigin.UserAgent, 0), // Lower origin
            new MatchedRule(rule2, new Priority(1), StylesheetOrigin.Author, 1) // Higher origin
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // Author origin wins
    }

    [Test]
    public void ResolveCascade_WithImportant_ImportantOverridesNormal()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule(_stylesheet, "div { color: red !important; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule(_stylesheet, "div { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule1, new Priority(1), StylesheetOrigin.Author, 0),
            new MatchedRule(rule2, new Priority(10), StylesheetOrigin.Author, 1) // Higher specificity
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // !important wins
        Assert.That(result.GetPropertyPriority("color"), Is.EqualTo("important"));
    }

    [Test]
    public void ResolveCascade_WithInlineStyle_AppliesInlineStyle()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: green; padding: 5px;");

        var rule = _cssParser.ParseRule(_stylesheet, "div { color: red; margin: 10px; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule, new Priority(1), StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)")); // Inline style wins
        Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px")); // Rule property remains
        Assert.That(result.GetPropertyValue("padding"), Is.EqualTo("5px")); // Inline style added
    }

    [Test]
    public void ResolveCascade_WithImportantInRuleAndInline_ImportantRuleWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: green;");

        var rule = _cssParser.ParseRule(_stylesheet, "div { color: red !important; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule, new Priority(1), StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // !important rule wins over inline
    }
}