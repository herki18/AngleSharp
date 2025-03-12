using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;
using NSubstitute;
using AutoFixture;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace AngleSharp.StyleSystem.Tests.Unit.Utility;

using Interfaces;

/// <summary>
/// Base class for StyleSystem tests that provides common functionality
/// and helper methods for creating test objects.
/// </summary>
public abstract class StyleSystemTestBase
{
    /// <summary>
    /// AutoFixture instance for generating test data.
    /// </summary>
    protected Fixture Fixture { get; private set; }

    /// <summary>
    /// Browsing context for the test.
    /// </summary>
    protected IBrowsingContext Context { get; private set; }

    /// <summary>
    /// CSS parser for parsing test CSS content.
    /// </summary>
    protected ICssParser Parser { get; private set; }

    /// <summary>
    /// Initialize the test fixture with required mocks and configurations.
    /// </summary>
    protected virtual void SetupFixture()
    {
        Fixture = new Fixture();
        Context = Substitute.For<IBrowsingContext>();
        Parser = Substitute.For<ICssParser>();

        // Configure the context to return the parser
        // We need to use When/Then instead of Returns with type parameters
        Context.GetService<ICssParser>().Returns(Parser);

        // Create a dummy declaration factory
        var declarationFactory = Substitute.For<IDeclarationFactory>();

        // Create a dummy declaration for any property, e.g. "color"
        var dummyDeclarationInfo = Substitute.For<IDeclarationInfo>();
        var dummyInitialValue = Substitute.For<ICssValue>();
        dummyInitialValue.CssText.Returns("rgba(0, 0, 0, 1)"); // default value for "color"
        dummyDeclarationInfo.InitialValue.Returns(dummyInitialValue);

        // Configure the factory to return the dummy declaration for any property
        declarationFactory.Create(Arg.Any<string>()).Returns(dummyDeclarationInfo);

        // Register the dummy declaration factory in the context
        Context.GetServices<IDeclarationFactory>().Returns(new [] { declarationFactory });

        // Register common fixture factories
        Fixture.Register<IBrowsingContext>(() => Context);
        Fixture.Register<ICssParser>(() => Parser);
        Fixture.Register<IElement>(() => Substitute.For<IElement>());
        Fixture.Register<ICssStyleDeclaration>(() => Substitute.For<ICssStyleDeclaration>());
        Fixture.Register<ICssStyleRule>(() => Substitute.For<ICssStyleRule>());
        Fixture.Register<IDeclarationFactory>(() => Substitute.For<IDeclarationFactory>());
    }

    /// <summary>
    /// Creates a MatchedRule with the specified CSS text, origin, and specificity.
    /// </summary>
    protected MatchedRule CreateMatchedRule(string cssText, StylesheetOrigin origin, Priority specificity, int originalIndex = 0)
    {
        var properties = ParseCssTextToProperties(cssText);
        var styleMock = Substitute.For<ICssStyleDeclaration>();

        // Configure the style mock to enumerate properties
        styleMock.GetEnumerator().Returns(properties.GetEnumerator());

        // Configure property checks
        foreach (var prop in properties)
        {
            styleMock.GetProperty(prop.Name).Returns(prop);
            styleMock.GetPropertyValue(prop.Name).Returns(prop.Value);

            // Store the important flag in a local variable to avoid NSubstitute chaining issues
            bool isImportant = prop.IsImportant;
            string priority = isImportant ? "important" : string.Empty;
            styleMock.GetPropertyPriority(prop.Name).Returns(priority);
        }

        // Set up the rule mock
        var ruleMock = Substitute.For<ICssStyleRule>();
        ruleMock.Style.Returns(styleMock);

        return new MatchedRule
        {
            Rule = ruleMock,
            Origin = origin,
            Specificity = specificity,
            OriginalIndex = originalIndex
        };
    }

    /// <summary>
    /// Parses CSS text into a list of CSS properties.
    /// </summary>
    protected List<ICssProperty> ParseCssTextToProperties(string cssText)
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

            var propertyMock = Substitute.For<ICssProperty>();
            propertyMock.Name.Returns(propertyName);
            propertyMock.Value.Returns(propertyValue);
            // IsImportant is a boolean
            propertyMock.IsImportant.Returns(isImportant);

            // Setup CssText for the property value
            var valueMock = Substitute.For<ICssValue>();
            valueMock.CssText.Returns(propertyValue);
            propertyMock.RawValue.Returns(valueMock);

            properties.Add(propertyMock);
        }

        return properties;
    }

    /// <summary>
    /// Creates a test element with the specified attributes.
    /// </summary>
    protected IElement CreateTestElement(string nodeName = "div", string id = "", string className = "", IElement? parentElement = null)
    {
        var element = Substitute.For<IElement>();
        element.NodeName.Returns(nodeName.ToUpperInvariant());
        element.Id.Returns(id);
        element.ClassName.Returns(className);
        element.ParentElement.Returns(parentElement);

        // Mock the matches method for common selector types
        element.Matches(Arg.Any<string>()).Returns(info => {
            var selector = info.Arg<string>();
            if (selector == nodeName || selector == nodeName.ToLowerInvariant())
                return true;
            if (!string.IsNullOrEmpty(id) && (selector == $"#{id}" || selector.Contains($"#{id}")))
                return true;
            if (!string.IsNullOrEmpty(className) && (selector == $".{className}" || selector.Contains($".{className}")))
                return true;
            return false;
        });

        return element;
    }

    /// <summary>
    /// Creates a mock computed style with the specified properties.
    /// </summary>
    protected IComputedStyle CreateComputedStyle(IElement element, ICssStyleDeclaration declaration, IComputedStyle? parentStyle = null)
    {
        var computedStyle = Substitute.For<IComputedStyle>();
        computedStyle.Declaration.Returns(declaration);

        computedStyle.GetPropertyValue(Arg.Any<string>()).Returns(info => {
            var propName = info.Arg<string>();
            return declaration.GetPropertyValue(propName);
        });

        // Set up other methods as needed for your tests

        return computedStyle;
    }
}