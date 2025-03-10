namespace AngleSharp.StyleSystem.Tests.Unit;

using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;

[TestFixture]
public class RuleCollectorTests
{
    private IBrowsingContext _context;
    private RuleCollector _ruleCollector;
    private IHtmlParser _parser;
    private IDocument _document;
    private ICssParser _cssParser;
    private StyleSheetManager _stylesheetManager;
    private ICssStyleSheet _stylesheet;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _stylesheetManager = new StyleSheetManager(_context, false); // Don't load user agent stylesheets
        _ruleCollector = new RuleCollector(_context, _stylesheetManager);
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");
        _cssParser = new CssParser(_context);
        _stylesheet = _cssParser.ParseStyleSheet("");
    }

    [TearDown]
    public void Teardown()
    {
        _context?.Dispose();
        _document?.Dispose();
        _stylesheetManager?.Dispose();
    }

    [Test]
    public void CollectMatchingRules_WithNoStylesheets_ReturnsEmptyCollection()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element);

        // Assert
        Assert.That(matchedRules, Is.Empty);
    }

    [Test]
    public void CollectMatchingRules_WithMatchingRule_ReturnsMatchedRule()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.ClassList.Add("test");
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet("div { color: red; }");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element).ToList();

        // Assert
        Assert.That(matchedRules, Has.Count.EqualTo(1));
        Assert.That(matchedRules[0].Rule?.SelectorText, Is.EqualTo("div"));
        Assert.That(matchedRules[0].Origin, Is.EqualTo(StylesheetOrigin.Author));
    }

    [Test]
    public void CollectMatchingRules_WithNonMatchingRule_ReturnsEmptyCollection()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet("span { color: red; }");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element);

        // Assert
        Assert.That(matchedRules, Is.Empty);
    }

    [Test]
    public void CollectMatchingRules_WithMultipleMatchingRules_ReturnsSortedBySpecificity()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.ClassList.Add("test");
        element.Id = "myDiv";
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet(@"
            div { color: red; }
            div.test { color: green; }
            #myDiv { color: blue; }
        ");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element).ToList();

        // Assert
        Assert.That(matchedRules, Has.Count.EqualTo(3));

        // Rules should be ordered by specificity (low to high)
        Assert.That(matchedRules[0].Rule?.SelectorText, Is.EqualTo("div"));
        Assert.That(matchedRules[1].Rule?.SelectorText, Is.EqualTo("div.test"));
        Assert.That(matchedRules[2].Rule?.SelectorText, Is.EqualTo("#myDiv"));

        // Check that specificity values are calculated correctly
        Assert.That(matchedRules[0].Specificity.Tags, Is.EqualTo(1)); // div (0,0,0,1)
        Assert.That(matchedRules[1].Specificity.Classes, Is.EqualTo(1)); // div.test (0,0,1,1)
        Assert.That(matchedRules[2].Specificity.Ids, Is.EqualTo(1)); // #myDiv (0,1,0,0)
    }

    [Test]
    public void CollectMatchingRules_WithDifferentOrigins_SortsByOrigin()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var userAgentSheet = _cssParser.ParseStyleSheet("div { color: red; }");
        var userSheet = _cssParser.ParseStyleSheet("div { color: green; }");
        var authorSheet = _cssParser.ParseStyleSheet("div { color: blue; }");

        _stylesheetManager.RegisterStylesheet(userAgentSheet, StylesheetOrigin.UserAgent);
        _stylesheetManager.RegisterStylesheet(userSheet, StylesheetOrigin.User);
        _stylesheetManager.RegisterStylesheet(authorSheet, StylesheetOrigin.Author);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element).ToList();

        // Assert
        Assert.That(matchedRules, Has.Count.EqualTo(3));

        // Order should be UserAgent, User, Author
        Assert.That(matchedRules[0].Origin, Is.EqualTo(StylesheetOrigin.UserAgent));
        Assert.That(matchedRules[1].Origin, Is.EqualTo(StylesheetOrigin.User));
        Assert.That(matchedRules[2].Origin, Is.EqualTo(StylesheetOrigin.Author));
    }

    [Test]
    public void CollectMatchingRules_WithSameSpecificityInSameOrigin_SortsBySourceOrder()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet(@"
            div { color: red; }
            div { color: green; }
            div { color: blue; }
        ");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act
        var matchedRules = _ruleCollector.CollectMatchingRules(element).ToList();

        // Assert
        Assert.That(matchedRules, Has.Count.EqualTo(3));

        // Rules with same specificity should maintain source order
        Assert.That(matchedRules[0].OriginalIndex, Is.LessThan(matchedRules[1].OriginalIndex));
        Assert.That(matchedRules[1].OriginalIndex, Is.LessThan(matchedRules[2].OriginalIndex));
    }

    [Test]
    public void CalculateSpecificity_TagSelector_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "div" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(0));
        Assert.That(specificity.Classes, Is.EqualTo(0));
        Assert.That(specificity.Tags, Is.EqualTo(1));
    }

    [Test]
    public void CalculateSpecificity_ClassSelector_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { ".test" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(0));
        Assert.That(specificity.Classes, Is.EqualTo(1));
        Assert.That(specificity.Tags, Is.EqualTo(0));
    }

    [Test]
    public void CalculateSpecificity_IdSelector_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "#myId" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(1));
        Assert.That(specificity.Classes, Is.EqualTo(0));
        Assert.That(specificity.Tags, Is.EqualTo(0));
    }

    [Test]
    public void CalculateSpecificity_ComplexSelector_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "div.test#myId:hover" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(1)); // 1 ID selector
        Assert.That(specificity.Classes, Is.EqualTo(2)); // 1 class + 1 pseudo-class
        Assert.That(specificity.Tags, Is.EqualTo(1)); // 1 tag
    }

    [Test]
    public void CalculateSpecificity_AttributeSelector_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "div[type=\"text\"]" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(0));
        Assert.That(specificity.Classes, Is.EqualTo(1)); // Attribute selector counts as class
        Assert.That(specificity.Tags, Is.EqualTo(1)); // 1 tag
    }

    [Test]
    public void CalculateSpecificity_PseudoElement_ReturnsCorrectValue()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "div::before" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(0));
        Assert.That(specificity.Classes, Is.EqualTo(0));
        Assert.That(specificity.Tags, Is.EqualTo(2)); // 1 tag + 1 pseudo-element
    }

    [Test]
    public void CalculateSpecificity_UniversalSelector_ReturnsZeroForUniversal()
    {
        // Using reflection to access the private method
        var methodInfo = typeof(RuleCollector).GetMethod("CalculateSpecificity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Call the private method
        var specificity = (Priority)methodInfo!.Invoke(_ruleCollector, new object[] { "*" })!;

        // Assert
        Assert.That(specificity.Inlines, Is.EqualTo(0));
        Assert.That(specificity.Ids, Is.EqualTo(0));
        Assert.That(specificity.Classes, Is.EqualTo(0));
        Assert.That(specificity.Tags, Is.EqualTo(0)); // Universal selector doesn't count
    }

    [Test]
    public void CollectMatchingRules_WithPseudoElement_MatchesPseudoElementRules()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet(@"
            div { color: red; }
            div::before { content: 'test'; color: green; }
        ");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act
        var elementRules = _ruleCollector.CollectMatchingRules(element).ToList();
        var pseudoRules = _ruleCollector.CollectMatchingRules(element, "before").ToList();

        // Assert
        Assert.That(elementRules, Has.Count.EqualTo(1));
        Assert.That(pseudoRules, Has.Count.EqualTo(1));
        Assert.That(pseudoRules[0].Rule?.SelectorText, Is.EqualTo("div::before"));
    }

    [Test]
    public void ClearCache_AfterCaching_CachesAreCleared()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var styleSheet = _cssParser.ParseStyleSheet("div { color: red; }");
        _stylesheetManager.RegisterStylesheet(styleSheet, StylesheetOrigin.Author);

        // Act - First call caches the result
        var rules1 = _ruleCollector.CollectMatchingRules(element).ToList();

        // Clear the cache
        _ruleCollector.ClearCache();

        // Add a new stylesheet that would match
        var styleSheet2 = _cssParser.ParseStyleSheet("div { margin: 10px; }");
        _stylesheetManager.RegisterStylesheet(styleSheet2, StylesheetOrigin.Author);

        // Act - Second call should get fresh results
        var rules2 = _ruleCollector.CollectMatchingRules(element).ToList();

        // Assert
        Assert.That(rules1, Has.Count.EqualTo(1));
        Assert.That(rules2, Has.Count.EqualTo(2)); // Should now have 2 rules
    }
}