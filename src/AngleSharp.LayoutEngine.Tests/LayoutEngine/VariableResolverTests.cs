namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using System.Threading.Tasks;
using AngleSharp.Css;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using StyleSystem;

[TestFixture]
public class VariableResolverTests
{
    private IBrowsingContext _context;
    private ICssParser _cssParser;
    private IHtmlParser _htmlParser;
    private MockRenderDevice _device;
    private VariableRegistry _variableRegistry;
    private VariableResolver _variableResolver;
    private ResolverContext _resolverContext;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _cssParser = _context.GetService<ICssParser>()!;
        _htmlParser = _context.GetService<IHtmlParser>()!;
        _device = new MockRenderDevice
        {
            ViewPortWidth = 1024,
            ViewPortHeight = 768,
            FontSize = 16,
            Resolution = 96
        };

        _variableRegistry = new VariableRegistry();
        _variableResolver = new VariableResolver(_variableRegistry, _context, _device);
        _resolverContext = new ResolverContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public void ResolveVariable_SimpleVariable_ResolvesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        _variableRegistry.RegisterVariable("--main-color", colorValue, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));
        var varValue = new CssVarValue("--main-color", null);

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ResolveVariable_WithFallback_UsesValueWhenFound()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue
        _variableRegistry.RegisterVariable("--main-color", colorValue, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));
        var varValue = new CssVarValue("--main-color", fallbackValue);

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(255, 0, 0, 1)")); // Should use the registered value, not fallback
    }

    [Test]
    public void ResolveVariable_WithFallback_UsesFallbackWhenNotFound()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue
        var varValue = new CssVarValue("--unknown-color", fallbackValue);

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(0, 0, 255, 1)")); // Should use fallback
    }

    [Test]
    public void ResolveVariable_NestedVariables_ResolvesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var primaryColorValue = new CssColorValue(255, 0, 0, 1); // red
        var nestedVarValue = new CssVarValue("--primary-color", null);

        _variableRegistry.RegisterVariable("--primary-color", primaryColorValue, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));
        _variableRegistry.RegisterVariable("--theme-color", nestedVarValue, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));

        var varValue = new CssVarValue("--theme-color", null);

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(255, 0, 0, 1)")); // Should resolve through the nesting
    }

    [Test]
    public void ResolveVariable_CircularReference_DetectsAndUsesFallback()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue

        // Create circular references
        var varA = new CssVarValue("--var-b", null);
        var varB = new CssVarValue("--var-a", null);

        _variableRegistry.RegisterVariable("--var-a", varA, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));
        _variableRegistry.RegisterVariable("--var-b", varB, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));

        var varValue = new CssVarValue("--var-a", fallbackValue);

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(0, 0, 255, 1)")); // Should use fallback
    }

    [Test]
    public void ResolveVariable_CircularReferenceWithoutFallback_ReturnsNull()
    {
        // Arrange
        var element = CreateElement("<div></div>");

        // Create circular references
        var varA = new CssVarValue("--var-b", null);
        var varB = new CssVarValue("--var-a", null);

        _variableRegistry.RegisterVariable("--var-a", varA, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));
        _variableRegistry.RegisterVariable("--var-b", varB, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));

        var varValue = new CssVarValue("--var-a", null); // No fallback

        // Act
        var result = _variableResolver.ResolveVariable(varValue, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Null); // Should return null for circular reference without fallback
    }

    [Test]
    public async Task ResolveVariable_VarInCalcExpression_ResolvesCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<html><body><div></div></body></html>"));
        var element = document.QuerySelector("div");

        // Register variables
        var spacingValue = new CssLengthValue(10, CssLengthValue.Unit.Px);
        _variableRegistry.RegisterVariable("--spacing", spacingValue, StylesheetOrigin.Author, new Priority(0, 0, 0, 1));

        // Create calc expression with var
        var varRef = new CssVarValue("--spacing", null);
        var calcExpr = new CssCalcValue(varRef);

        // Act
        Assert.NotNull(element);
        var result = _variableResolver.ResolveCalcExpression(calcExpr, element, _resolverContext);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssCalcValue>());

        // The expression inside the calc should be resolved to 10px
        var resolvedCalc = result as CssCalcValue;
        Assert.NotNull(resolvedCalc);
        Assert.That(resolvedCalc.Expression, Is.InstanceOf<CssLengthValue>());
        var resolvedLength = resolvedCalc.Expression is CssLengthValue ? (CssLengthValue)resolvedCalc.Expression : default;
        Assert.That(resolvedLength.Value, Is.EqualTo(10));
        Assert.That(resolvedLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
    }

    [Test]
    public void VariableRegistry_CascadeRules_HigherSpecificityWins()
    {
        // Arrange
        var lowSpecificity = new Priority(0, 0, 0, 1); // div
        var highSpecificity = new Priority(0, 1, 0, 0); // #id

        var redValue = new CssColorValue(255, 0, 0, 1); // red
        var blueValue = new CssColorValue(0, 0, 255, 1); // blue

        // Register with different specificities
        _variableRegistry.RegisterVariable("--color", redValue, StylesheetOrigin.Author, lowSpecificity);
        _variableRegistry.RegisterVariable("--color", blueValue, StylesheetOrigin.Author, highSpecificity);

        // Act
        var result = _variableRegistry.GetVariableValue("--color");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(0, 0, 255, 1)")); // Higher specificity (blue) should win
    }

    [Test]
    public void VariableRegistry_CascadeRules_ImportantOverridesSpecificity()
    {
        // Arrange
        var lowSpecificity = new Priority(0, 0, 0, 1); // div
        var highSpecificity = new Priority(0, 1, 0, 0); // #id

        var redValue = new CssColorValue(255, 0, 0, 1); // red
        var blueValue = new CssColorValue(0, 0, 255, 1); // blue

        // Register with different specificities but red has !important
        _variableRegistry.RegisterVariable("--color", redValue, StylesheetOrigin.Author, lowSpecificity, true);
        _variableRegistry.RegisterVariable("--color", blueValue, StylesheetOrigin.Author, highSpecificity, false);

        // Act
        var result = _variableRegistry.GetVariableValue("--color");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CssColorValue>());
        Assert.That(result.CssText, Is.EqualTo("rgba(255, 0, 0, 1)")); // Important (red) should win despite lower specificity
    }

    [Test]
    public void ResolverContext_CircularReferenceDetection_DetectsCircles()
    {
        // Arrange
        _resolverContext.TryEnterVariable("--var-a");
        _resolverContext.TryEnterVariable("--var-b");
        _resolverContext.TryEnterVariable("--var-c");

        // Act
        var (hasCycle, path) = _resolverContext.DetectCycle("--var-a");

        // Assert
        Assert.That(hasCycle, Is.True);
        Assert.That(path, Is.Not.Null);
        Assert.That(path, Does.Contain("--var-a"));
        Assert.That(path, Does.Contain("--var-b"));
        Assert.That(path, Does.Contain("--var-c"));
    }

    [Test]
    public void ResolverContext_MaxDepth_PreventsExcessiveRecursion()
    {
        // Arrange
        // Add many variables to exceed the max depth
        for (int i = 0; i < 50; i++)
        {
            var success = _resolverContext.TryEnterVariable($"--var-{i}");
            if (!success)
            {
                // Act & Assert
                Assert.That(i, Is.GreaterThan(30)); // Should fail after reaching max depth (32)
                return;
            }
        }

        Assert.Fail("Exceeded max depth without detection");
    }

    // Helper method to create an element for testing
    private IElement CreateElement(string html)
    {
        var document = _htmlParser.ParseDocument("");
        var container = document.CreateElement("div");
        container.InnerHtml = html;
        Assert.NotNull(container.FirstElementChild);
        return container.FirstElementChild;
    }
}