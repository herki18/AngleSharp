using AngleSharp.Html.Parser;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;

namespace AngleSharp.StyleSystem.Tests;

using Integration;
using Models;

[TestFixture]
public class StyleSheetManagerTests
{
    private IBrowsingContext _context;
    private StyleSheetManager _stylesheetManager;
    private IHtmlParser _parser;
    private ICssParser _cssParser;
    private bool _eventRaised;
    private StylesheetChangedEventArgs? _eventArgs;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _stylesheetManager = new StyleSheetManager(_context, false);
        _parser = new HtmlParser();
        _cssParser = new CssParser();

        _eventRaised = false;
        _eventArgs = null;

        _stylesheetManager.StylesheetChanged += (sender, args) =>
        {
            _eventRaised = true;
            _eventArgs = args;
        };
    }

    [TearDown]
    public void Teardown()
    {
        _stylesheetManager.Dispose();
        _context.Dispose();
    }

    [Test]
    public void RegisterStylesheet_AddsStylesheet()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet("div { color: red; }");

        // Act
        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        var sheets = _stylesheetManager.GetStylesheets().ToList();

        // Assert
        Assert.That(sheets.Count, Is.EqualTo(1), "Should contain exactly one stylesheet");
        Assert.That(sheets[0].Stylesheet, Is.EqualTo(stylesheet), "Registered stylesheet should match");
        Assert.That(sheets[0].Origin, Is.EqualTo(StylesheetOrigin.Author), "Origin should be Author");

        // Event verification
        Assert.That(_eventRaised, Is.True, "StylesheetChanged event should be raised");
        Assert.That(_eventArgs, Is.Not.Null);
        Assert.That(_eventArgs.Stylesheet, Is.EqualTo(stylesheet), "Event should reference correct stylesheet");
        Assert.That(_eventArgs.ChangeType, Is.EqualTo(StylesheetChangeType.Added), "Change type should be Added");
    }

    [Test]
    public void UnregisterStylesheet_RemovesStylesheet()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet("div { color: red; }");
        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Reset event tracking
        _eventRaised = false;
        _eventArgs = null;

        // Act
        _stylesheetManager.UnregisterStylesheet(stylesheet);
        var sheets = _stylesheetManager.GetStylesheets().ToList();

        // Assert
        Assert.That(sheets.Count, Is.EqualTo(0), "Should have no stylesheets after unregistering");

        // Event verification
        Assert.That(_eventRaised, Is.True, "StylesheetChanged event should be raised");
        Assert.That(_eventArgs, Is.Not.Null);
        Assert.That(_eventArgs.Stylesheet, Is.EqualTo(stylesheet), "Event should reference correct stylesheet");
        Assert.That(_eventArgs.ChangeType, Is.EqualTo(StylesheetChangeType.Removed), "Change type should be Removed");
    }

    [Test]
    public void GetStylesheets_ReturnsInCascadeOrder()
    {
        // Arrange - Add stylesheets in non-cascade order
        var stylesheet1 = _cssParser.ParseStyleSheet("div { color: red; }");
        var stylesheet2 = _cssParser.ParseStyleSheet("div { color: blue; }");
        var stylesheet3 = _cssParser.ParseStyleSheet("div { color: green; }");

        _stylesheetManager.RegisterStylesheet(stylesheet3, StylesheetOrigin.Author);    // Added third
        _stylesheetManager.RegisterStylesheet(stylesheet1, StylesheetOrigin.UserAgent); // Added first
        _stylesheetManager.RegisterStylesheet(stylesheet2, StylesheetOrigin.User);      // Added second

        // Act
        var sheets = _stylesheetManager.GetStylesheets().ToList();

        // Assert - Should be in UserAgent, User, Author order regardless of registration order
        Assert.That(sheets.Count, Is.EqualTo(3), "Should contain all three stylesheets");
        Assert.That(sheets[0].Origin, Is.EqualTo(StylesheetOrigin.UserAgent), "First should be UserAgent origin");
        Assert.That(sheets[1].Origin, Is.EqualTo(StylesheetOrigin.User), "Second should be User origin");
        Assert.That(sheets[2].Origin, Is.EqualTo(StylesheetOrigin.Author), "Third should be Author origin");
    }

    [Test]
    public void GetStylesheetsByOrigin_FiltersCorrectly()
    {
        // Arrange
        var stylesheet1 = _cssParser.ParseStyleSheet("div { color: red; }");
        var stylesheet2 = _cssParser.ParseStyleSheet("div { color: blue; }");

        _stylesheetManager.RegisterStylesheet(stylesheet1, StylesheetOrigin.UserAgent);
        _stylesheetManager.RegisterStylesheet(stylesheet2, StylesheetOrigin.Author);

        // Act
        var userAgentSheets = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent).ToList();
        var authorSheets = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).ToList();
        var userSheets = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.User).ToList();

        // Assert
        Assert.That(userAgentSheets.Count, Is.EqualTo(1), "Should have one UserAgent stylesheet");
        Assert.That(userAgentSheets[0], Is.EqualTo(stylesheet1), "UserAgent stylesheet should match");

        Assert.That(authorSheets.Count, Is.EqualTo(1), "Should have one Author stylesheet");
        Assert.That(authorSheets[0], Is.EqualTo(stylesheet2), "Author stylesheet should match");

        Assert.That(userSheets.Count, Is.EqualTo(0), "Should have no User stylesheets");
    }

    [Test]
    public void GetStylesheetOrigin_ReturnsCorrectOrigin()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet("div { color: red; }");
        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Act
        var origin = _stylesheetManager.GetStylesheetOrigin(stylesheet);

        // Assert
        Assert.That(origin, Is.EqualTo(StylesheetOrigin.Author), "Should return the origin the stylesheet was registered with");
    }

    [Test]
    public void AttachToDocument_LoadsDocumentStylesheets()
    {
        // Arrange
        var document = _parser.ParseDocument(@"
                <html>
                <head>
                    <style>div { color: red; }</style>
                </head>
                <body></body>
                </html>
            ");

        // Act
        _stylesheetManager.AttachToDocument(document);
        var sheets = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).ToList();

        // Assert
        Assert.That(sheets.Count, Is.EqualTo(1), "Should load one stylesheet from document");

        // Verify stylesheet content contains the expected rule
        var rules = sheets[0].Rules.OfType<ICssStyleRule>().ToList();
        Assert.That(rules.Count, Is.EqualTo(1), "Stylesheet should contain one rule");
        Assert.That(rules[0].SelectorText, Is.EqualTo("div"), "Selector should be 'div'");
    }

    [Test]
    public void DetachFromDocument_ClearsAuthorStylesheets()
    {
        // Arrange
        var document = _parser.ParseDocument(@"
                <html>
                <head>
                    <style>div { color: red; }</style>
                </head>
                <body></body>
                </html>
            ");

        _stylesheetManager.AttachToDocument(document);
        var sheetsBeforeDetach = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).ToList();
        Assert.That(sheetsBeforeDetach.Count, Is.EqualTo(1), "Should have one stylesheet before detaching");

        // Act
        _stylesheetManager.DetachFromDocument(document);
        var sheetsAfterDetach = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).ToList();

        // Assert
        Assert.That(sheetsAfterDetach.Count, Is.EqualTo(0), "Should have no stylesheets after detaching");
    }

    [Test]
    public void RefreshDocumentStylesheets_UpdatesStylesheets()
    {
        // Arrange
        var document = _parser.ParseDocument(@"
                <html>
                <head>
                    <style>div { color: red; }</style>
                </head>
                <body></body>
                </html>
            ");

        _stylesheetManager.AttachToDocument(document);

        // Add another style element
        var newStyle = document.CreateElement("style");
        newStyle.TextContent = "span { color: blue; }";
        document.Head!.AppendChild(newStyle);

        // Act
        _stylesheetManager.RefreshDocumentStylesheets();
        var sheets = _stylesheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).ToList();

        // Assert
        Assert.That(sheets.Count, Is.EqualTo(2), "Should have two stylesheets after refresh");
    }

    [Test]
    public void GetAllRules_ReturnsAllRulesFromAllStylesheets()
    {
        // Arrange
        var stylesheet1 = _cssParser.ParseStyleSheet("div { color: red; }");
        var stylesheet2 = _cssParser.ParseStyleSheet("span { color: blue; }");

        _stylesheetManager.RegisterStylesheet(stylesheet1, StylesheetOrigin.Author);
        _stylesheetManager.RegisterStylesheet(stylesheet2, StylesheetOrigin.Author);

        // Act
        var rules = _stylesheetManager.GetAllRules().ToList();

        // Assert
        Assert.That(rules.Count, Is.EqualTo(2), "Should return rules from all stylesheets");
        Assert.That(rules.Count(r => r is ICssStyleRule), Is.EqualTo(2), "Both rules should be style rules");
    }

    [Test]
    public void GetAllStyleRules_ReturnsOnlyStyleRules()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet(@"
                @media screen { div { color: red; } }
                span { color: blue; }
            ");

        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Act
        var styleRules = _stylesheetManager.GetAllStyleRules().ToList();

        // Assert
        Assert.That(styleRules.Count, Is.EqualTo(2), "Should return two style rules (one from media query, one direct)");
        Assert.That(styleRules.All(r => r is ICssStyleRule), Is.True, "All rules should be ICssStyleRule instances");

        // Verify rule selectors
        var selectors = styleRules.Select(r => r.SelectorText).ToArray();
        Assert.That(selectors, Is.EquivalentTo(new[] { "div", "span" }), "Rules should have correct selectors");
    }

    [Test]
    public void RegisterStylesheet_DuplicateStylesheet_RegistersOnlyOnce()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet("div { color: red; }");

        // Act
        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        _stylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author); // Attempt duplicate
        var sheets = _stylesheetManager.GetStylesheets().ToList();

        // Assert
        Assert.That(sheets.Count, Is.EqualTo(1), "Should contain exactly one stylesheet even after duplicate registration");
    }

    [Test]
    public void UnregisterStylesheet_NonexistentStylesheet_DoesNotThrow()
    {
        // Arrange
        var stylesheet = _cssParser.ParseStyleSheet("div { color: red; }");

        // Act & Assert
        Assert.DoesNotThrow(() => _stylesheetManager.UnregisterStylesheet(stylesheet),
            "Unregistering a non-existent stylesheet should not throw");
    }
}