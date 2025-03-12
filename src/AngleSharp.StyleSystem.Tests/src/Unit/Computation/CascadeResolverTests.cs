namespace AngleSharp.StyleSystem.Tests.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Models;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

/// <summary>
/// Test suite for the CascadeResolver class which is responsible for applying
/// CSS specificity rules and cascade ordering.
/// </summary>
public class CascadeResolverTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange, Act, Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CascadeResolver(null!));
        Assert.That(exception.ParamName, Is.EqualTo("context"));
    }

    [Test]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();

        // Act
        var resolver = new CascadeResolver(contextMock.Object);

        // Assert
        Assert.That(resolver, Is.Not.Null);
    }

    #endregion

    #region ResolveCascade Tests

    [Test]
    public void ResolveCascade_WithNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule>();

        // Act, Assert
        var exception = Assert.Throws<ArgumentNullException>(() => resolver.ResolveCascade(rules, null!));
        Assert.That(exception.ParamName, Is.EqualTo("element"));
    }

    [Test]
    public void ResolveCascade_WithNoRules_ReturnsEmptyDeclaration()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule>();

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void ResolveCascade_WithNullRuleInList_SkipsRule()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        var rules = new List<MatchedRule>
        {
            new MatchedRule { Rule = null } // Null rule should be skipped
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(0));
    }

    #endregion

    #region Style Origin Tests

    [Test]
    public void ResolveCascade_UserAgentOrigin_AppliedWithLowestPrecedence()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with different origins but same specificity
        var userAgentRule = CreateMatchedRule("color: red", StylesheetOrigin.UserAgent, Priority.FromSelector("div"));
        var userRule = CreateMatchedRule("color: green", StylesheetOrigin.User, Priority.FromSelector("div"));
        var authorRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { userAgentRule, userRule, authorRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    [Test]
    public void ResolveCascade_UserOrigin_AppliedWithMiddlePrecedence()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with different origins but same specificity
        var userAgentRule = CreateMatchedRule("color: red", StylesheetOrigin.UserAgent, Priority.FromSelector("div"));
        var userRule = CreateMatchedRule("color: green", StylesheetOrigin.User, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { userAgentRule, userRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("green"));
    }

    [Test]
    public void ResolveCascade_AuthorOrigin_AppliedWithHighestPrecedence()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with different origins but same specificity
        var userAgentRule = CreateMatchedRule("color: red", StylesheetOrigin.UserAgent, Priority.FromSelector("div"));
        var authorRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { userAgentRule, authorRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    #endregion

    #region Important Flag Tests

    [Test]
    public void ResolveCascade_ImportantAuthorOrigin_OverridesNonImportantProperties()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules
        var normalRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"));
        var importantRule = CreateMatchedRule("color: red !important", StylesheetOrigin.Author, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { normalRule, importantRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    [Test]
    public void ResolveCascade_ImportantUserOrigin_OverridesNormalAuthorOrigin()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules
        var authorRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"));
        var importantUserRule = CreateMatchedRule("color: green !important", StylesheetOrigin.User, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { authorRule, importantUserRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("green"));
    }

    [Test]
    public void ResolveCascade_ImportantAuthorOrigin_OverridesImportantUserOrigin()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules
        var importantUserRule = CreateMatchedRule("color: green !important", StylesheetOrigin.User, Priority.FromSelector("div"));
        var importantAuthorRule = CreateMatchedRule("color: blue !important", StylesheetOrigin.Author, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { importantUserRule, importantAuthorRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    [Test]
    public void ResolveCascade_MixedImportantAndNonImportant_CorrectlyHandled()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with mixed important and non-important properties
        var rule1 = CreateMatchedRule("color: red; font-size: 12px !important", StylesheetOrigin.Author, Priority.FromSelector("div"));
        var rule2 = CreateMatchedRule("color: blue !important; font-size: 16px", StylesheetOrigin.Author, Priority.FromSelector("div"));

        var rules = new List<MatchedRule> { rule1, rule2 };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("12px"));
    }

    #endregion

    #region Specificity Tests

    [Test]
    public void ResolveCascade_HigherSpecificity_OverridesLowerSpecificity()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with different specificities
        var lowSpecificityRule = CreateMatchedRule("color: red", StylesheetOrigin.Author, Priority.FromSelector("div"));
        var highSpecificityRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div.class#id"));

        var rules = new List<MatchedRule> { lowSpecificityRule, highSpecificityRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    [Test]
    public void ResolveCascade_EqualSpecificityWithDifferentIndexes_LastOneWins()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with equal specificity but different indexes
        var firstRule = CreateMatchedRule("color: red", StylesheetOrigin.Author, Priority.FromSelector("div.class"), 1);
        var secondRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div.class"), 2);

        var rules = new List<MatchedRule> { firstRule, secondRule };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    #endregion

    #region Inline Style Tests

    [Test]
    public void ResolveCascade_WithInlineStyle_AppliesToResult()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var parserMock = new Mock<ICssParser>();
        contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);

        var elementMock = new Mock<IElement>();
        elementMock.Setup(e => e.GetAttribute("style")).Returns("color: purple");

        var declarations = new List<ICssProperty>();
        var propertyMock = new Mock<ICssProperty>();
        propertyMock.Setup(p => p.Name).Returns("color");
        propertyMock.Setup(p => p.Value).Returns("purple");
        propertyMock.Setup(p => p.IsImportant).Returns(false);
        declarations.Add(propertyMock.Object);

        parserMock.Setup(p => p.ParseDeclaration("color: purple")).Returns(declarations);

        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule> {
            CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"))
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("purple"));
    }

    [Test]
    public void ResolveCascade_InlineStyleWithImportantVsImportantRule_InlineWins()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var parserMock = new Mock<ICssParser>();
        contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);

        var elementMock = new Mock<IElement>();
        elementMock.Setup(e => e.GetAttribute("style")).Returns("color: purple !important");

        var declarations = new List<ICssProperty>();
        var propertyMock = new Mock<ICssProperty>();
        propertyMock.Setup(p => p.Name).Returns("color");
        propertyMock.Setup(p => p.Value).Returns("purple");
        propertyMock.Setup(p => p.IsImportant).Returns(true);
        declarations.Add(propertyMock.Object);

        parserMock.Setup(p => p.ParseDeclaration("color: purple !important")).Returns(declarations);

        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule> {
            CreateMatchedRule("color: blue !important", StylesheetOrigin.Author, Priority.FromSelector("div"))
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("purple"));
    }

    [Test]
    public void ResolveCascade_InlineStyleVsImportantRule_ImportantRuleWins()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var parserMock = new Mock<ICssParser>();
        contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);

        var elementMock = new Mock<IElement>();
        elementMock.Setup(e => e.GetAttribute("style")).Returns("color: purple");

        var declarations = new List<ICssProperty>();
        var propertyMock = new Mock<ICssProperty>();
        propertyMock.Setup(p => p.Name).Returns("color");
        propertyMock.Setup(p => p.Value).Returns("purple");
        propertyMock.Setup(p => p.IsImportant).Returns(false);
        declarations.Add(propertyMock.Object);

        parserMock.Setup(p => p.ParseDeclaration("color: purple")).Returns(declarations);

        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule> {
            CreateMatchedRule("color: blue !important", StylesheetOrigin.Author, Priority.FromSelector("div"))
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    #endregion

    #region Multiple Properties Tests

    [Test]
    public void ResolveCascade_MultipleProperties_AppliesAllCorrectly()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create mock rules with multiple properties
        var rule1 = CreateMatchedRule("color: red; font-size: 12px; margin: 10px",
            StylesheetOrigin.Author, Priority.FromSelector("div"));
        var rule2 = CreateMatchedRule("color: blue; padding: 5px; border: 1px solid black",
            StylesheetOrigin.Author, Priority.FromSelector("div.class"));

        var rules = new List<MatchedRule> { rule1, rule2 };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("12px"));
        Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px"));
        Assert.That(result.GetPropertyValue("padding"), Is.EqualTo("5px"));
        Assert.That(result.GetPropertyValue("border"), Is.EqualTo("1px solid black"));
    }

    [Test]
    public void ResolveCascade_MultipleRulesWithSameProperty_LastRuleWithHighestSpecificityWins()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        // Create multiple rules with same property but different specificities
        var rule1 = CreateMatchedRule("color: red", StylesheetOrigin.Author, Priority.FromSelector("div"), 1);
        var rule2 = CreateMatchedRule("color: green", StylesheetOrigin.Author, Priority.FromSelector("div.class"), 2);
        var rule3 = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"), 3);

        var rules = new List<MatchedRule> { rule1, rule2, rule3 };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("green"));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void ResolveCascade_RuleWithNullStyle_SkipsRule()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        var resolver = new CascadeResolver(contextMock.Object);

        var styleMock = new Mock<ICssStyleDeclaration>();
        styleMock.Setup(s => s.Any(It.IsAny<Func<ICssProperty, bool>>())).Returns(false);

        var ruleMock = new Mock<ICssStyleRule>();
        ruleMock.Setup(r => r.Style).Returns((ICssStyleDeclaration)null!);

        var rules = new List<MatchedRule> {
            new MatchedRule {
                Rule = ruleMock.Object,
                Origin = StylesheetOrigin.Author,
                Specificity = Priority.FromSelector("div")
            }
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void ResolveCascade_NullInlineStyleParser_SkipsInlineStyle()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        contextMock.Setup(c => c.GetService<ICssParser>()).Returns((ICssParser)null!);

        var elementMock = new Mock<IElement>();
        elementMock.Setup(e => e.GetAttribute("style")).Returns("color: purple");

        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule> {
            CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"))
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    [Test]
    public void ResolveCascade_EmptyInlineStyle_SkipsInlineStyle()
    {
        // Arrange
        var contextMock = new Mock<IBrowsingContext>();
        var elementMock = new Mock<IElement>();
        elementMock.Setup(e => e.GetAttribute("style")).Returns(string.Empty);

        var resolver = new CascadeResolver(contextMock.Object);
        var rules = new List<MatchedRule> {
            CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.FromSelector("div"))
        };

        // Act
        var result = resolver.ResolveCascade(rules, elementMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
    }

    #endregion

    #region Helper Methods

    private static MatchedRule CreateMatchedRule(string cssText, StylesheetOrigin origin, Priority specificity, int originalIndex = 0)
    {
        var styleMock = new Mock<ICssStyleDeclaration>();
        var properties = ParseCssTextToProperties(cssText);

        styleMock.Setup(s => s.GetEnumerator()).Returns(properties.GetEnumerator());
        styleMock.Setup(s => s.Any(It.IsAny<Func<ICssProperty, bool>>())).Returns<Func<ICssProperty, bool>>(func => properties.Any(func));

        var ruleMock = new Mock<ICssStyleRule>();
        ruleMock.Setup(r => r.Style).Returns(styleMock.Object);

        return new MatchedRule
        {
            Rule = ruleMock.Object,
            Origin = origin,
            Specificity = specificity,
            OriginalIndex = originalIndex
        };
    }

    private static List<ICssProperty> ParseCssTextToProperties(string cssText)
    {
        var properties = new List<ICssProperty>();
        var declarations = cssText.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var declaration in declarations)
        {
            var parts = declaration.Trim().Split(':', 2);
            if (parts.Length != 2) continue;

            string propertyName = parts[0].Trim();
            string propertyValue = parts[1].Trim();
            bool isImportant = false;

            if (propertyValue.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
            {
                isImportant = true;
                propertyValue = propertyValue.Substring(0, propertyValue.Length - 10).Trim();
            }

            var propertyMock = new Mock<ICssProperty>();
            propertyMock.Setup(p => p.Name).Returns(propertyName);
            propertyMock.Setup(p => p.Value).Returns(propertyValue);
            propertyMock.Setup(p => p.IsImportant).Returns(isImportant);

            properties.Add(propertyMock.Object);
        }

        return properties;
    }

    #endregion
}