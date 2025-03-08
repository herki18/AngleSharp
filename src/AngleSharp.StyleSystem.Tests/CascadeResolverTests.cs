using System;
using System.Collections.Generic;
using NUnit.Framework;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.StyleSystem.Core;
using Moq;

namespace AngleSharp.StyleSystem.Tests;

[TestFixture]
public class CascadeResolverTests
{
    private IBrowsingContext _context;
    private CascadeResolver _cascadeResolver;
    private IHtmlParser _parser;
    private IDocument _document;
    private ICssParser _cssParser;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _cascadeResolver = new CascadeResolver(_context);
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("");
        _cssParser = new CssParser();
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
        var styleRule = _cssParser.ParseRule("div { color: red; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = styleRule,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    [Test]
    public void ResolveCascade_WithMultipleRules_AppliesCorrectCascade()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule("div { color: red; font-size: 12px; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule("div { color: blue; margin: 10px; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule1,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            },
            new MatchedRule
            {
                Rule = rule2,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 1
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue")); // Later rule wins
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("12px"));
        Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px"));
    }

    [Test]
    public void ResolveCascade_WithSpecificity_HigherSpecificityWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule("div { color: red; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule("div.special { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule1,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            },
            new MatchedRule
            {
                Rule = rule2,
                Specificity = new Priority(11), // Higher specificity
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 1
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue")); // Higher specificity wins
    }

    [Test]
    public void ResolveCascade_WithOrigin_HigherOriginWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule("div { color: red; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule("div { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule1,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.UserAgent, // Lower origin
                OriginalIndex = 0
            },
            new MatchedRule
            {
                Rule = rule2,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author, // Higher origin
                OriginalIndex = 1
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue")); // Author origin wins
    }

    [Test]
    public void ResolveCascade_WithImportant_ImportantOverridesNormal()
    {
        // Arrange
        var element = _document.CreateElement("div");
        var rule1 = _cssParser.ParseRule("div { color: red !important; }") as ICssStyleRule;
        var rule2 = _cssParser.ParseRule("div { color: blue; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule1,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            },
            new MatchedRule
            {
                Rule = rule2,
                Specificity = new Priority(10), // Higher specificity
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 1
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red")); // !important wins
        Assert.That(result.GetPropertyPriority("color"), Is.EqualTo("important"));
    }

    [Test]
    public void ResolveCascade_WithInlineStyle_AppliesInlineStyle()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: green; padding: 5px;");

        var rule = _cssParser.ParseRule("div { color: red; margin: 10px; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("green")); // Inline style wins
        Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px")); // Rule property remains
        Assert.That(result.GetPropertyValue("padding"), Is.EqualTo("5px")); // Inline style added
    }

    [Test]
    public void ResolveCascade_WithImportantInRuleAndInline_ImportantRuleWins()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: green;");

        var rule = _cssParser.ParseRule("div { color: red !important; }") as ICssStyleRule;

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule
            {
                Rule = rule,
                Specificity = new Priority(1),
                Origin = StylesheetOrigin.Author,
                OriginalIndex = 0
            }
        };

        // Act
        var result = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red")); // !important rule wins over inline
    }
}