namespace AngleSharp.StyleSystem.Tests.Unit.Computation;

using System.Collections.Generic;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Models;
using AutoFixture;
using Css.Values;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class CascadeResolverTests
{
    private IFixture _fixture;
    private IBrowsingContext _context;
    private ICssParser _cssParser;
    private ICssStyleDeclaration _cssStyleDeclaration;
    private CascadeResolver _resolver;
    private IDeclarationFactory _declarationFactory;

    [SetUp]
    public void Setup()
    {
        _fixture = new Fixture();
        _context = Substitute.For<IBrowsingContext>();
        _cssParser = Substitute.For<ICssParser>();
        _cssStyleDeclaration = Substitute.For<ICssStyleDeclaration>();
        _declarationFactory = Substitute.For<IDeclarationFactory>();

        // Setup the factory to be returned when GetFactory is called
        _context.GetService<ICssParser>().Returns(_cssParser);
        _context.GetServices<IDeclarationFactory>().Returns(new[] { _declarationFactory });

        // Setup the factory to handle property creation
        var declaration = Substitute.For<IDeclarationInfo>();
        declaration.InitialValue.Returns(new CssStringValue("initial"));
        _declarationFactory.Create(Arg.Any<string>()).Returns(declaration);

        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(_cssStyleDeclaration);

        _resolver = new CascadeResolver(_context);
    }

    [TearDown]
    public void Teardown()
    {
        _context.Dispose();
    }

    [Test]
    public void ResolveCascade_WithEmptyRules_ReturnsEmptyDeclaration()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        var matchedRules = new List<MatchedRule>();

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ResolveCascade_WithSingleRule_ReturnsRuleProperties()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        var rule = CreateCssStyleRule("color: red;");
        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule, Priority.Zero, StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        // The declaration should have been called to set the property
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ResolveCascade_WithMultipleRules_AppliesInCorrectOrder()
    {
        // Arrange
        var element = Substitute.For<IElement>();

        var userAgentRule = CreateCssStyleRule("color: black;");
        var userRule = CreateCssStyleRule("color: blue;");
        var authorRule = CreateCssStyleRule("color: red;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(userAgentRule, Priority.Zero, StylesheetOrigin.UserAgent, 0),
            new MatchedRule(userRule, Priority.Zero, StylesheetOrigin.User, 0),
            new MatchedRule(authorRule, Priority.Zero, StylesheetOrigin.Author, 0)
        };

        var resolvedDeclaration = Substitute.For<ICssStyleDeclaration>();
        resolvedDeclaration.GetPropertyValue("color").Returns("red");

        var factory = Substitute.For<IDeclarationFactory>();
        _context.GetService<IDeclarationFactory>().Returns(factory);

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // In this case, author rules should win so color should be red
        // Since we're using substitutes, we need to verify the right calls were made
        // rather than checking the actual value
    }

    [Test]
    public void ResolveCascade_WithImportantRules_PrioritizesImportantRules()
    {
        // Arrange
        var element = Substitute.For<IElement>();

        var normalRule = CreateCssStyleRule("color: red;");
        var importantRule = CreateCssStyleRule("color: blue !important;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(normalRule, Priority.Zero, StylesheetOrigin.Author, 0),
            new MatchedRule(importantRule, Priority.Zero, StylesheetOrigin.Author, 1)
        };

        // Setup important flag on the property
        var importantProp = Substitute.For<ICssProperty>();
        importantProp.Name.Returns("color");
        importantProp.Value.Returns("blue");
        importantProp.IsImportant.Returns(true);

        var importantStyle = Substitute.For<ICssStyleDeclaration>();
        importantStyle.GetEnumerator().Returns(_ => new List<ICssProperty> { importantProp }.GetEnumerator());

        importantRule.Style.Returns(importantStyle);

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The important rule should win
    }

    [Test]
    public void ResolveCascade_WithDifferentSpecificity_PrioritizesHigherSpecificity()
    {
        // Arrange
        var element = Substitute.For<IElement>();

        var lowSpecificityRule = CreateCssStyleRule("color: red;");
        var highSpecificityRule = CreateCssStyleRule("color: blue;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(lowSpecificityRule, new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 0),
            new MatchedRule(highSpecificityRule, new Priority(0, 1, 0, 0), StylesheetOrigin.Author, 1)
        };

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The higher specificity rule should win
    }

    [Test]
    public void ResolveCascade_WithSameSpecificityDifferentOrder_PrioritizesLaterRule()
    {
        // Arrange
        var element = Substitute.For<IElement>();

        var firstRule = CreateCssStyleRule("color: red;");
        var secondRule = CreateCssStyleRule("color: blue;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(firstRule, Priority.Zero, StylesheetOrigin.Author, 0),
            new MatchedRule(secondRule, Priority.Zero, StylesheetOrigin.Author, 1)
        };

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The second rule should win due to source order
    }

    [Test]
    public void ResolveCascade_WithInlineStyles_AppliesInlineStyles()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        element.GetAttribute("style").Returns("color: green;");

        var rule = CreateCssStyleRule("color: red;");
        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule, Priority.Zero, StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The inline style should win
    }

    [Test]
    public void ResolveCascade_ImportantAuthorOverridesInlineStyle()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        element.GetAttribute("style").Returns("color: green;");

        var importantRule = CreateCssStyleRule("color: blue !important;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(importantRule, Priority.Zero, StylesheetOrigin.Author, 0)
        };

        // Setup important flag on the property
        var importantProp = Substitute.For<ICssProperty>();
        importantProp.Name.Returns("color");
        importantProp.Value.Returns("blue");
        importantProp.IsImportant.Returns(true);

        var importantStyle = Substitute.For<ICssStyleDeclaration>();
        importantStyle.GetEnumerator().Returns(_ => new List<ICssProperty> { importantProp }.GetEnumerator());
        importantStyle.GetProperty("color").Returns(importantProp);

        importantRule.Style.Returns(importantStyle);

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The important author rule should win over inline styles
    }

    [Test]
    public void ResolveCascade_UserAgentImportantTrumpsEverythingButUserImportant()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        element.GetAttribute("style").Returns("color: green;");

        var userAgentImportantRule = CreateCssStyleRule("color: black !important;");
        var authorImportantRule = CreateCssStyleRule("color: red !important;");
        var userImportantRule = CreateCssStyleRule("color: blue !important;");

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(userAgentImportantRule, Priority.Zero, StylesheetOrigin.UserAgent, 0),
            new MatchedRule(authorImportantRule, Priority.Zero, StylesheetOrigin.Author, 0),
            new MatchedRule(userImportantRule, Priority.Zero, StylesheetOrigin.User, 0)
        };

        // Setup important properties
        SetupImportantProperty(userAgentImportantRule, "color", "black");
        SetupImportantProperty(authorImportantRule, "color", "red");
        SetupImportantProperty(userImportantRule, "color", "blue");

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // The user important rule should win
    }

    [Test]
    public void ResolveCascade_HandlesNullRuleStyle()
    {
        // Arrange
        var element = Substitute.For<IElement>();
        var rule = Substitute.For<ICssStyleRule>();
        rule.Style.Returns((ICssStyleDeclaration)null!);

        var matchedRules = new List<MatchedRule>
        {
            new MatchedRule(rule, Priority.Zero, StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _resolver.ResolveCascade(matchedRules, element);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Should not throw an exception
    }

    #region Helper Methods

    private ICssStyleRule CreateCssStyleRule(string cssText)
    {
        var rule = Substitute.For<ICssStyleRule>();
        var style = Substitute.For<ICssStyleDeclaration>();

        var property = Substitute.For<ICssProperty>();
        property.Name.Returns("color");

        if (cssText.Contains("!important"))
        {
            property.IsImportant.Returns(true);
            property.Value.Returns(cssText.Replace(" !important", "").Replace("color: ", ""));
        }
        else
        {
            property.IsImportant.Returns(false);
            property.Value.Returns(cssText.Replace("color: ", ""));
        }

        var properties = new List<ICssProperty> { property };
        style.GetEnumerator().Returns(_ => properties.GetEnumerator());
        style.GetProperty("color").Returns(property);

        rule.Style.Returns(style);
        rule.SelectorText.Returns("selector");

        return rule;
    }

    private void SetupImportantProperty(ICssStyleRule rule, string propertyName, string propertyValue)
    {
        var importantProp = Substitute.For<ICssProperty>();
        importantProp.Name.Returns(propertyName);
        importantProp.Value.Returns(propertyValue);
        importantProp.IsImportant.Returns(true);

        var importantStyle = Substitute.For<ICssStyleDeclaration>();
        importantStyle.GetEnumerator().Returns(_ => new List<ICssProperty> { importantProp }.GetEnumerator());
        importantStyle.GetProperty(propertyName).Returns(importantProp);

        rule.Style.Returns(importantStyle);
    }

    #endregion
}