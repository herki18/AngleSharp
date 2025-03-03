using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.LayoutEngine.StyleComputation;

namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

[TestFixture]
public class SelectorMatcherTests
{
    private SelectorMatcher _matcher;
    private IRenderDevice _device;
    private IBrowsingContext _context;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _device = new MockRenderDevice { ViewPortWidth = 1024, ViewPortHeight = 768 };
        _matcher = new SelectorMatcher(_device);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task MatchRules_SimpleSelectors_ReturnsCorrectMatches()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<html><body></body></html>"));
        var element = document.CreateElement("div");
        element.ClassName = "test";

        var stylesheet = CreateStylesheet("div { color: red; } .test { font-size: 16px; } div.test { margin: 10px; }");
        var entry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);

        // Act
        var matches = _matcher.MatchRules(element, new[] { entry }).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(3));
        Assert.That(matches.Select(m => m.Rule.SelectorText),
            Is.EquivalentTo(new[] { "div", ".test", "div.test" }));
    }

    [Test]
    public async Task MatchRules_ComplexSelectors_MatchesCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<html><body><div><p class='para'>Text</p></div></body></html>"));
        Assert.IsNotNull(document.Body);
        document.Body.InnerHtml = "<div><p class='para'>Text</p></div>";
        var element = document.QuerySelector("p.para");

        var stylesheet = CreateStylesheet("div p { color: blue; } p.para { color: green; } div > p.para { font-weight: bold; }");
        var entry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);

        // Act
        Assert.IsNotNull(element);
        var matches = _matcher.MatchRules(element, new[] { entry }).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task MatchRules_PseudoElements_MatchesCorrectly()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var stylesheet = CreateStylesheet("div::before { content: 'test'; } div { color: red; }");
        var entry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);

        // Act
        var matches = _matcher.MatchRules(element, new[] { entry }, "::before").ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].Rule.SelectorText, Is.EqualTo("div::before"));
    }

    [Test]
    public async Task MatchRules_MediaQueries_FiltersRulesByMediaQuery()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<html><body><div id='test'>Test</div></body></html>"));
        var element = document.GetElementById("test"); // Use the existing element from the document

        var stylesheet = CreateStylesheet(
            "@media screen and (min-width: 800px) { div { color: red; } }" +
            "@media screen and (max-width: 500px) { div { color: blue; } }");
        var entry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);

        // Act - first with device width 1024px (this should match min-width: 800px)
        Assert.IsNotNull(element);
        var matches = _matcher.MatchRules(element, new[] { entry }).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(1), "Only the min-width: 800px rule should match");
        Assert.That(matches[0].Rule.Parent.CssText.Contains("min-width: 800px"), Is.True, "The matched rule should be from the min-width media query");

        // Act - now with device width 400px (this should match max-width: 500px)
        _device.SetViewport(400, _device.ViewPortHeight);
        matches = _matcher.MatchRules(element, new[] { entry }).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(1), "Only the max-width: 500px rule should match");
        Assert.That(matches[0].Rule.Parent.CssText.Contains("max-width: 500px"), Is.True, "The matched rule should be from the max-width media query");
    }

    [Test]
    public async Task MatchRules_SpecificityCalculation_SetsCorrectPriority()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<html><body></body></html>"));
        var element = document.CreateElement("div");
        element.Id = "test";
        element.ClassName = "item";

        var stylesheet = CreateStylesheet(
            "div { color: black; }" +            // 0,0,1
            ".item { color: blue; }" +           // 0,1,0
            "#test { color: red; }" +            // 1,0,0
            "div.item { color: green; }" +       // 0,1,1
            "div#test.item { color: yellow; }"); // 1,1,1
        var entry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);

        // Act
        var matches = _matcher.MatchRules(element, new[] { entry }).OrderBy(m => m.Specificity).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(5));
        // Check specificity ordering is correct
        Assert.That(matches[0].Rule.SelectorText, Is.EqualTo("div"));
        Assert.That(matches[1].Rule.SelectorText, Is.EqualTo(".item"));
        Assert.That(matches[2].Rule.SelectorText, Is.EqualTo("div.item"));
        Assert.That(matches[3].Rule.SelectorText, Is.EqualTo("#test"));
        Assert.That(matches[4].Rule.SelectorText, Is.EqualTo("div#test.item"));
    }

    [Test]
    public async Task MatchRules_MultipleOrigins_PreservesOriginInformation()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var userAgentStylesheet = CreateStylesheet("div { color: black; }");
        var userStylesheet = CreateStylesheet("div { color: blue; }");
        var authorStylesheet = CreateStylesheet("div { color: red; }");

        var entries = new[] {
            new StylesheetEntry(userAgentStylesheet, StylesheetOrigin.UserAgent),
            new StylesheetEntry(userStylesheet, StylesheetOrigin.User),
            new StylesheetEntry(authorStylesheet, StylesheetOrigin.Author)
        };

        // Act
        var matches = _matcher.MatchRules(element, entries).ToList();

        // Assert
        Assert.That(matches, Has.Count.EqualTo(3));
        Assert.That(matches.Select(m => m.Origin),
            Is.EquivalentTo(new[] { StylesheetOrigin.UserAgent, StylesheetOrigin.User, StylesheetOrigin.Author }));
    }

    // Helper method to create stylesheets for testing
    private ICssStyleSheet CreateStylesheet(string css)
    {
        var parser = _context.GetService<ICssParser>();
        Assert.IsNotNull(parser);
        return parser.ParseStyleSheet(css);
    }
}