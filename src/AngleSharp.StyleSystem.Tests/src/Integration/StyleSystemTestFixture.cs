using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Tests.Integration;

using Services;

/// <summary>
/// Base test fixture for StyleSystem integration tests.
/// Provides common functionality for creating, parsing, and styling documents.
/// </summary>
[TestFixture]
public class StyleSystemTestFixture
{
    #region Properties

    /// <summary>
    /// Gets the AngleSharp browsing context.
    /// </summary>
    protected IBrowsingContext Context { get; private set; }

    /// <summary>
    /// Gets the StyleEngine instance.
    /// </summary>
    protected StyleEngine StyleEngine { get; private set; }

    /// <summary>
    /// Gets the current document.
    /// </summary>
    protected IDocument Document { get; private set; }

    /// <summary>
    /// Gets the style system service.
    /// </summary>
    protected StyleSystemService StyleSystemService { get; private set; }

    #endregion

    #region Setup and Teardown

    [SetUp]
    public virtual void SetupFixture()
    {
        // Create configuration with CSS and StyleSystem support
        var config = Configuration.Default
            .WithCss()
            .WithStyleSystem();

        Context = BrowsingContext.New(config);
        Assert.IsNotNull(Context);
        Document = CreateEmptyDocument();

        Assert.IsNotNull(Document);
        var styleSystemService = Context.GetStyleSystem();
        Assert.NotNull(styleSystemService);
        StyleSystemService = styleSystemService;
        var styleEngine = StyleSystemService.StyleEngine as StyleEngine;
        StyleEngine = styleEngine;

        Assert.That(StyleEngine, Is.Not.Null, "Failed to initialize StyleEngine");
    }

    [TearDown]
    public virtual void TeardownFixture()
    {
        StyleSystemService?.Dispose();
        Document?.Dispose();
        Context?.Dispose();
    }

    #endregion

    #region Document Creation Helpers

    /// <summary>
    /// Creates an empty HTML document.
    /// </summary>
    protected IDocument CreateEmptyDocument()
    {
        return Context.OpenAsync(req => req.Content("<html><head></head><body></body></html>")).Result;
    }

    /// <summary>
    /// Creates a document from HTML content.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    protected IDocument CreateDocument(string html)
    {
        return Context.OpenAsync(req => req.Content(html)).Result;
    }

    /// <summary>
    /// Creates a document from HTML content asynchronously.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    protected Task<IDocument> CreateDocumentAsync(string html)
    {
        return Context.OpenAsync(req => req.Content(html));
    }

    /// <summary>
    /// Creates a document with HTML content and embedded CSS.
    /// </summary>
    /// <param name="html">The HTML content for the body.</param>
    /// <param name="css">The CSS content to include in a style tag.</param>
    protected IDocument CreateDocumentWithCss(string html, string css)
    {
        var htmlWithCss = $@"
                <html>
                <head>
                    <style>{css}</style>
                </head>
                <body>
                    {html}
                </body>
                </html>";

        return CreateDocument(htmlWithCss);
    }

    /// <summary>
    /// Creates a document with HTML content and embedded CSS asynchronously.
    /// </summary>
    /// <param name="html">The HTML content for the body.</param>
    /// <param name="css">The CSS content to include in a style tag.</param>
    protected Task<IDocument> CreateDocumentWithCssAsync(string html, string css)
    {
        var htmlWithCss = $@"
                <html>
                <head>
                    <style>{css}</style>
                </head>
                <body>
                    {html}
                </body>
                </html>";

        return CreateDocumentAsync(htmlWithCss);
    }

    /// <summary>
    /// Creates a document with multiple CSS stylesheets.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="cssSheets">Array of CSS content strings, each will be a separate stylesheet.</param>
    protected IDocument CreateDocumentWithMultipleCss(string html, params string[] cssSheets)
    {
        var styleElements = string.Join("", cssSheets.Select(css => $"<style>{css}</style>"));

        var htmlWithCss = $@"
                <html>
                <head>
                    {styleElements}
                </head>
                <body>
                    {html}
                </body>
                </html>";

        return CreateDocument(htmlWithCss);
    }

    /// <summary>
    /// Creates a document with multiple CSS stylesheets asynchronously.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="cssSheets">Array of CSS content strings, each will be a separate stylesheet.</param>
    protected Task<IDocument> CreateDocumentWithMultipleCssAsync(string html, params string[] cssSheets)
    {
        var styleElements = string.Join("", cssSheets.Select(css => $"<style>{css}</style>"));

        var htmlWithCss = $@"
                <html>
                <head>
                    {styleElements}
                </head>
                <body>
                    {html}
                </body>
                </html>";

        return CreateDocumentAsync(htmlWithCss);
    }

    /// <summary>
    /// Adds a stylesheet to the current document.
    /// </summary>
    /// <param name="css">The CSS content.</param>
    protected void AddStyleSheet(string css)
    {
        var styleElement = Document.CreateElement("style");
        styleElement.TextContent = css;
        Assert.IsNotNull(Document.Head);
        Document.Head.AppendChild(styleElement);

        // Force stylesheet refresh
        StyleEngine.StylesheetManager.RefreshDocumentStylesheets();
    }

    #endregion

    #region Element Manipulation Helpers

    /// <summary>
    /// Creates an element with specified tag, ID, and class.
    /// </summary>
    /// <param name="tagName">The tag name.</param>
    /// <param name="id">Optional ID.</param>
    /// <param name="className">Optional class name.</param>
    /// <param name="parentElement">Optional parent element. If null, adds to document body.</param>
    /// <returns>The created element.</returns>
    protected IElement CreateTestElement(string tagName, string? id = null, string? className = null, IElement? parentElement = null)
    {
        var element = Document.CreateElement(tagName);

        if (!string.IsNullOrEmpty(id))
            element.Id = id;

        if (!string.IsNullOrEmpty(className))
            element.ClassName = className;

        (parentElement ?? Document.Body)?.AppendChild(element);
        return element;
    }

    /// <summary>
    /// Sets inline style on an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="styleText">CSS style text.</param>
    protected void SetInlineStyle(IElement element, string styleText)
    {
        element.SetAttribute("style", styleText);
    }

    /// <summary>
    /// Finds an element by ID in the current document.
    /// </summary>
    protected IElement? GetElementById(string id)
    {
        return Document.GetElementById(id);
    }

    /// <summary>
    /// Finds an element by selector in the current document.
    /// </summary>
    protected IElement? QuerySelector(string selector)
    {
        return Document.QuerySelector(selector);
    }

    /// <summary>
    /// Finds elements by selector in the current document.
    /// </summary>
    protected IHtmlCollection<IElement> QuerySelectorAll(string selector)
    {
        return Document.QuerySelectorAll(selector);
    }

    #endregion

    #region Style Computation Helpers

    /// <summary>
    /// Gets the computed style for an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The computed style.</returns>
    protected IComputedStyle GetComputedStyle(IElement element)
    {
        return StyleEngine.ComputeElementStyle(element);
    }

    /// <summary>
    /// Gets the computed style for an element with a pseudo-element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="pseudoElement">The pseudo-element (e.g., "::before").</param>
    /// <returns>The computed style.</returns>
    protected IComputedStyle GetComputedPseudoElementStyle(IElement element, string pseudoElement)
    {
        return StyleEngine.ComputeElementStyle(element, pseudoElement);
    }

    /// <summary>
    /// Updates the viewport size of the render device.
    /// </summary>
    /// <param name="width">Viewport width in pixels.</param>
    /// <param name="height">Viewport height in pixels.</param>
    protected void SetViewport(int width, int height)
    {
        StyleEngine.NotifyViewportChanged(width, height);
    }

    /// <summary>
    /// Forces a style recalculation for the document or element.
    /// </summary>
    /// <param name="root">The root element to update styles for. If null, uses document element.</param>
    protected void RecalculateStyles(IElement? root = null)
    {
        StyleEngine.UpdateStyles(root ?? Document.DocumentElement);
    }

    #endregion

    #region Style Assertion Helpers

    /// <summary>
    /// Asserts that a CSS property has the expected value.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectedValue">The expected value.</param>
    protected void AssertPropertyValue(IComputedStyle style, string propertyName, string expectedValue)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        Assert.That(actualValue, Is.EqualTo(expectedValue),
            $"Property '{propertyName}' should be '{expectedValue}' but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that a color property has the expected value.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectedColor">The expected color.</param>
    protected void AssertColorProperty(IComputedStyle style, string propertyName, CssColorValue expectedColor)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        Assert.That(actualValue, Does.Contain(expectedColor.CssText).IgnoreCase,
            $"Property '{propertyName}' should contain '{expectedColor.CssText}' but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that a length property has the expected value and unit.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectedValue">The expected length value.</param>
    /// <param name="expectedUnit">The expected length unit.</param>
    protected void AssertLengthProperty(IComputedStyle style, string propertyName, double expectedValue, CssLengthValue.Unit expectedUnit)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        var expectedString = new CssLengthValue(expectedValue, expectedUnit).CssText;

        Assert.That(actualValue, Is.EqualTo(expectedString),
            $"Property '{propertyName}' should be '{expectedString}' but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that a property equals the 'auto' value.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    protected void AssertPropertyAuto(IComputedStyle style, string propertyName)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        Assert.That(actualValue, Is.EqualTo("auto"),
            $"Property '{propertyName}' should be 'auto' but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that a property has one of the allowed values.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="allowedValues">The allowed values.</param>
    protected void AssertPropertyOneOf(IComputedStyle style, string propertyName, params string[] allowedValues)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        Assert.That(actualValue, Is.AnyOf(allowedValues),
            $"Property '{propertyName}' should be one of [{string.Join(", ", allowedValues)}] but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that a property contains the expected string.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectedSubstring">The expected substring.</param>
    protected void AssertPropertyContains(IComputedStyle style, string propertyName, string expectedSubstring)
    {
        var actualValue = style.GetPropertyValue(propertyName);
        Assert.That(actualValue, Does.Contain(expectedSubstring),
            $"Property '{propertyName}' should contain '{expectedSubstring}' but was '{actualValue}'");
    }

    /// <summary>
    /// Asserts that an element has expected box model properties.
    /// </summary>
    /// <param name="style">The computed style.</param>
    /// <param name="width">Expected width value.</param>
    /// <param name="height">Expected height value.</param>
    /// <param name="marginTop">Expected top margin.</param>
    /// <param name="marginRight">Expected right margin.</param>
    /// <param name="marginBottom">Expected bottom margin.</param>
    /// <param name="marginLeft">Expected left margin.</param>
    protected void AssertBoxModel(IComputedStyle style,
        string width, string height,
        string marginTop, string marginRight, string marginBottom, string marginLeft)
    {
        AssertPropertyValue(style, "width", width);
        AssertPropertyValue(style, "height", height);
        AssertPropertyValue(style, "margin-top", marginTop);
        AssertPropertyValue(style, "margin-right", marginRight);
        AssertPropertyValue(style, "margin-bottom", marginBottom);
        AssertPropertyValue(style, "margin-left", marginLeft);
    }

    /// <summary>
    /// Asserts that an element has expected padding properties.
    /// </summary>
    protected void AssertPadding(IComputedStyle style,
        string paddingTop, string paddingRight, string paddingBottom, string paddingLeft)
    {
        AssertPropertyValue(style, "padding-top", paddingTop);
        AssertPropertyValue(style, "padding-right", paddingRight);
        AssertPropertyValue(style, "padding-bottom", paddingBottom);
        AssertPropertyValue(style, "padding-left", paddingLeft);
    }

    /// <summary>
    /// Asserts that an element has expected border properties.
    /// </summary>
    protected void AssertBorders(IComputedStyle style,
        string borderTopWidth, string borderRightWidth, string borderBottomWidth, string borderLeftWidth)
    {
        AssertPropertyValue(style, "border-top-width", borderTopWidth);
        AssertPropertyValue(style, "border-right-width", borderRightWidth);
        AssertPropertyValue(style, "border-bottom-width", borderBottomWidth);
        AssertPropertyValue(style, "border-left-width", borderLeftWidth);
    }

    /// <summary>
    /// Asserts computed style values for typography-related properties.
    /// </summary>
    protected void AssertTypography(IComputedStyle style,
        string fontSize, string fontFamily, string fontWeight, string color, string textAlign)
    {
        AssertPropertyValue(style, "font-size", fontSize);
        AssertPropertyValue(style, "font-family", fontFamily);
        AssertPropertyValue(style, "font-weight", fontWeight);
        AssertPropertyValue(style, "color", color);
        AssertPropertyValue(style, "text-align", textAlign);
    }

    /// <summary>
    /// Asserts display and positioning of an element.
    /// </summary>
    protected void AssertDisplayAndPosition(IComputedStyle style,
        string display, string position, string zIndex = "auto")
    {
        AssertPropertyValue(style, "display", display);
        AssertPropertyValue(style, "position", position);
        AssertPropertyValue(style, "z-index", zIndex);
    }

    /// <summary>
    /// Asserts that the provided style has been properly inherited from parent.
    /// </summary>
    protected void AssertInheritedFromParent(IComputedStyle childStyle, IComputedStyle parentStyle, string propertyName)
    {
        var parentValue = parentStyle.GetPropertyValue(propertyName);
        var childValue = childStyle.GetPropertyValue(propertyName);

        Assert.That(childValue, Is.EqualTo(parentValue),
            $"Child property '{propertyName}' should inherit value '{parentValue}' from parent but was '{childValue}'");
    }

    /// <summary>
    /// Asserts that the provided style has NOT been inherited from parent.
    /// </summary>
    protected void AssertNotInheritedFromParent(IComputedStyle childStyle, IComputedStyle parentStyle, string propertyName)
    {
        var parentValue = parentStyle.GetPropertyValue(propertyName);
        var childValue = childStyle.GetPropertyValue(propertyName);

        Assert.That(childValue, Is.Not.EqualTo(parentValue),
            $"Child property '{propertyName}' should NOT inherit value '{parentValue}' from parent");
    }

    #endregion

    #region CSS Rule Creation and Cascade Helpers

    /// <summary>
    /// Creates a CSS style rule from a selector and CSS text.
    /// </summary>
    /// <param name="selector">The CSS selector.</param>
    /// <param name="cssText">The CSS declaration text.</param>
    /// <returns>An ICssStyleRule.</returns>
    protected ICssStyleRule CreateStyleRule(string selector, string cssText)
    {
        var parser = Context.GetService<ICssParser>();
        Assert.IsNotNull(parser);
        var stylesheet = parser.ParseStyleSheet($"{selector} {{ {cssText} }}");
        return stylesheet.Rules.OfType<ICssStyleRule>().First();
    }

    /// <summary>
    /// Creates a MatchedRule for testing cascade resolution.
    /// </summary>
    /// <param name="selector">The CSS selector.</param>
    /// <param name="cssText">The CSS declaration text.</param>
    /// <param name="origin">The stylesheet origin.</param>
    /// <param name="specificity">The selector specificity.</param>
    /// <param name="index">The original index of the rule.</param>
    /// <returns>A MatchedRule instance.</returns>
    protected MatchedRule CreateMatchedRule(string selector, string cssText, StylesheetOrigin origin, Priority specificity, int index = 0)
    {
        var rule = CreateStyleRule(selector, cssText);
        return new MatchedRule(rule, specificity, origin, index);
    }

    /// <summary>
    /// Creates a MatchedRule from an existing ICssStyleRule.
    /// </summary>
    /// <param name="rule">The CSS style rule.</param>
    /// <param name="origin">The stylesheet origin.</param>
    /// <param name="specificity">The selector specificity.</param>
    /// <param name="index">The original index of the rule.</param>
    /// <returns>A MatchedRule instance.</returns>
    protected MatchedRule CreateMatchedRule(ICssStyleRule rule, StylesheetOrigin origin, Priority specificity, int index = 0)
    {
        return new MatchedRule(rule, specificity, origin, index);
    }

    /// <summary>
    /// Creates a Priority object representing selector specificity.
    /// </summary>
    /// <param name="a">Style attribute specificity (1 or 0).</param>
    /// <param name="b">ID selector count.</param>
    /// <param name="c">Class, attribute, and pseudo-class selector count.</param>
    /// <param name="d">Element and pseudo-element selector count.</param>
    /// <returns>A Priority object.</returns>
    protected Priority CreateSpecificity(byte a, byte b, byte c, byte d)
    {
        return new Priority(a, b, c, d);
    }

    /// <summary>
    /// Parses CSS text into a list of CSS properties.
    /// </summary>
    protected List<ICssProperty> ParseCssTextToProperties(string cssText)
    {
        var parser = Context.GetService<ICssParser>();
        Assert.IsNotNull(parser);
        var declaration = parser.ParseDeclaration(cssText);
        return declaration.ToList();
    }

    /// <summary>
    /// Creates a set of matched rules for cascade testing.
    /// </summary>
    /// <param name="rules">Tuple containing (selector, cssText, origin, specificity a,b,c,d).</param>
    /// <returns>Collection of MatchedRule objects.</returns>
    protected IEnumerable<MatchedRule> CreateMatchedRuleSet(params (string selector, string cssText, StylesheetOrigin origin, byte a, byte b, byte c, byte d)[] rules)
    {
        var result = new List<MatchedRule>();

        for (int i = 0; i < rules.Length; i++)
        {
            var (selector, cssText, origin, a, b, c, d) = rules[i];
            var specificity = new Priority(a, b, c, d);
            result.Add(CreateMatchedRule(selector, cssText, origin, specificity, i));
        }

        return result;
    }

    #endregion
}