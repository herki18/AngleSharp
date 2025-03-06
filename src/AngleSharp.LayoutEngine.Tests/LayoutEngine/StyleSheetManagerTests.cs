namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using Css.Dom;
using Css.Parser;
using StyleSystem;

[TestFixture]
public class StyleSheetManagerTests
{
    private IBrowsingContext _context;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public void RegisterStylesheet_AddsToCollection()
    {
        // Arrange
        var manager = new StyleSheetManager(null);
        var stylesheet = CreateStylesheet("div { color: red; }");

        // Act
        manager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        var stylesheets = manager.GetStylesheets().ToList();

        // Assert
        Assert.That(stylesheets, Has.Count.EqualTo(1));
        Assert.That(stylesheets[0].Stylesheet, Is.SameAs(stylesheet));
        Assert.That(stylesheets[0].Origin, Is.EqualTo(StylesheetOrigin.Author));
    }

    [Test]
    public void UnregisterStylesheet_RemovesFromCollection()
    {
        // Arrange
        var manager = new StyleSheetManager(null);
        var stylesheet1 = CreateStylesheet("div { color: red; }");
        var stylesheet2 = CreateStylesheet("p { color: blue; }");
        manager.RegisterStylesheet(stylesheet1, StylesheetOrigin.Author);
        manager.RegisterStylesheet(stylesheet2, StylesheetOrigin.Author);

        // Act
        manager.UnregisterStylesheet(stylesheet1);
        var stylesheets = manager.GetStylesheets().ToList();

        // Assert
        Assert.That(stylesheets, Has.Count.EqualTo(1));
        Assert.That(stylesheets[0].Stylesheet, Is.SameAs(stylesheet2));
    }

    [Test]
    public async Task SetDocument_LoadsDocumentStylesheets()
    {
        // Arrange
        var manager = new StyleSheetManager(null);
        var document = await _context.OpenNewAsync();
        var head = document.CreateElement("head");
        document.DocumentElement.AppendChild(head);

        // Add a style element
        var style = document.CreateElement("style");
        style.TextContent = "div { color: red; }";
        head.AppendChild(style);

        // Act
        manager.SetDocument(document);
        var stylesheets = manager.GetStylesheets().ToList();

        // Assert
        Assert.That(stylesheets, Has.Count.GreaterThan(0));
        Assert.That(stylesheets.All(s => s.Origin == StylesheetOrigin.Author), Is.True);
    }

    [Test]
    public void GetStylesheets_ReturnsInOrderOfOrigin()
    {
        // Arrange
        var manager = new StyleSheetManager(null);
        var userAgentStylesheet = CreateStylesheet("div { color: black; }");
        var userStylesheet = CreateStylesheet("div { color: blue; }");
        var authorStylesheet = CreateStylesheet("div { color: red; }");

        manager.RegisterStylesheet(authorStylesheet, StylesheetOrigin.Author);
        manager.RegisterStylesheet(userAgentStylesheet, StylesheetOrigin.UserAgent);
        manager.RegisterStylesheet(userStylesheet, StylesheetOrigin.User);

        // Act
        var stylesheets = manager.GetStylesheets().ToList();

        // Assert
        Assert.That(stylesheets, Has.Count.EqualTo(3));
        Assert.That(stylesheets[0].Origin, Is.EqualTo(StylesheetOrigin.UserAgent));
        Assert.That(stylesheets[1].Origin, Is.EqualTo(StylesheetOrigin.User));
        Assert.That(stylesheets[2].Origin, Is.EqualTo(StylesheetOrigin.Author));
    }

    // Helper method to create stylesheets for testing
    private ICssStyleSheet CreateStylesheet(string css)
    {
        var parser = _context.GetService<ICssParser>();
        Assert.IsNotNull(parser);
        return parser.ParseStyleSheet(css);
    }
}