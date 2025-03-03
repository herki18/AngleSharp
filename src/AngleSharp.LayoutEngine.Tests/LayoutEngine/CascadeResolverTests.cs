namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using Css;
using Css.Dom;
using Css.Parser;
using StyleComputation;

[TestFixture]
public class CascadeResolverTests
{
    private CascadeResolver _resolver;
    private IBrowsingContext _context;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _resolver = new CascadeResolver();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ResolveCascade_SingleRule_AppliesAllProperties()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var rule = CreateStyleRule("div", "color: red; font-size: 16px;");
        var matchedRule = new MatchedRule(rule, new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 0);

        // Act
        var result = _resolver.ResolveCascade(new[] { matchedRule }, element);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public async Task ResolveCascade_MultipleRulesSameOrigin_AppliesBasedOnSpecificity()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");
        element.Id = "myDiv";
        element.ClassName = "item";

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: blue;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 0),
            new MatchedRule(
                CreateStyleRule(".item", "color: green;"),
                new Priority(0, 0, 1, 0), StylesheetOrigin.Author, 1),
            new MatchedRule(
                CreateStyleRule("#myDiv", "color: red;"),
                new Priority(0, 1, 0, 0), StylesheetOrigin.Author, 2)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - #myDiv has highest specificity, so color should be red
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    [Test]
    public async Task ResolveCascade_MultipleRulesDifferentOrigins_RespectsCascadeOrder()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: black;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.UserAgent, 0),
            new MatchedRule(
                CreateStyleRule("div", "color: blue;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.User, 1),
            new MatchedRule(
                CreateStyleRule("div", "color: red;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 2)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - Author styles should win
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    [Test]
    public async Task ResolveCascade_ImportantProperties_OverrideNormalProperties()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: red !important; font-size: 12px;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.UserAgent, 0),
            new MatchedRule(
                CreateStyleRule("div", "color: blue; font-size: 16px;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 1)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - !important from UserAgent overrides Author for color, but not for font-size
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public async Task ResolveCascade_ImportantProperties_RespectReverseOriginOrder()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: black !important;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.UserAgent, 0),
            new MatchedRule(
                CreateStyleRule("div", "color: blue !important;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.User, 1),
            new MatchedRule(
                CreateStyleRule("div", "color: red !important;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 2)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - !important follows Author > User > UserAgent
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    [Test]
    public async Task ResolveCascade_InlineStyles_OverrideOtherAuthorStyles()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");
        element.SetAttribute("style", "color: green; font-size: 20px;");

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: red; font-size: 16px;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - Inline styles override other author styles
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("green"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("20px"));
    }

    [Test]
    public async Task ResolveCascade_ImportantInStylesheetVsInline_StylesheetWins()
    {
        // Arrange
        var document = await _context.OpenNewAsync();
        var element = document.CreateElement("div");
        element.SetAttribute("style", "color: green;");

        var rules = new[] {
            new MatchedRule(
                CreateStyleRule("div", "color: red !important;"),
                new Priority(0, 0, 0, 1), StylesheetOrigin.Author, 0)
        };

        // Act
        var result = _resolver.ResolveCascade(rules, element);

        // Assert - !important in stylesheet overrides inline style
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("red"));
    }

    // Helper method to create style rules for testing
    private ICssStyleRule CreateStyleRule(string selector, string cssText)
    {
        var parser = _context.GetService<ICssParser>();
        Assert.IsNotNull(parser);
        var stylesheet = parser.ParseStyleSheet($"{selector} {{ {cssText} }}");
        return stylesheet.Rules.OfType<ICssStyleRule>().First();
    }
}