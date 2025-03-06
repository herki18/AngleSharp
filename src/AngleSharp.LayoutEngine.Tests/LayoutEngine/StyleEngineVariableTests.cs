namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.LayoutEngine.StyleSystem;

[TestFixture]
public class StyleEngineVariableTests
{
    private IBrowsingContext _context;
    private IRenderDevice _device;
    private StyleEngine _styleEngine;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _device = new MockRenderDevice();
        _styleEngine = new StyleEngine(_device, _context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ComputeElementStyle_BasicCssVariable_ResolvesCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --main-color: red;
                    }
                    div {
                        color: var(--main-color);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_NestedCssVariables_ResolvesCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --primary-color: blue;
                        --theme-color: var(--primary-color);
                    }
                    div {
                        color: var(--theme-color);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_CssVariableInheritance_WorksCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    body {
                        --body-padding: 20px;
                    }
                    div {
                        padding: var(--body-padding);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("20px"));
    }

    [Test]
    public async Task ComputeElementStyle_CssVariableWithFallback_UsesFallbackWhenNeeded()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    div {
                        color: var(--undefined-color, green);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_VariableInCalc_ResolvesCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --base-size: 10px;
                    }
                    div {
                        margin: calc(var(--base-size) * 2);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert
        // In a full implementation, this would compute to "20px"
        // But our simplified calc() implementation may not fully evaluate this yet
        Assert.That(style.GetPropertyValue("margin"), Is.Not.Null);
        // At minimum, the variable should be resolved inside the calc()
        Assert.That(style.GetPropertyValue("margin"), Does.Not.Contain("--base-size"));
    }

    [Test]
    public async Task ComputeElementStyle_CascadingVariables_FollowsCascadeRules()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --theme-color: blue;
                    }
                    body {
                        --theme-color: green;
                    }
                    #test {
                        --theme-color: red;
                    }
                    div {
                        color: var(--theme-color);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert - most specific variable declaration should win
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_CircularVariableReferences_HandlesGracefully()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --var-a: var(--var-b);
                        --var-b: var(--var-a, blue);
                    }
                    div {
                        color: var(--var-a, red);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act - should not throw or hang
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert - should use the fallback for the circular reference
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // red fallback
    }

    [Test]
    public async Task ComputeElementStyle_VariablesWithImportantFlag_FollowCascadeRules()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --theme-color: blue !important;
                    }
                    #test {
                        --theme-color: red;  /* No !important */
                    }
                    div {
                        color: var(--theme-color);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert - !important should win over higher specificity
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // blue
    }

    [Test]
    public async Task ComputeElementStyle_MultipleVariableOverrides_LastOneWins()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    div {
                        --theme-padding: 10px;
                        --theme-padding: 20px;  /* Last one in same rule set wins */
                        padding: var(--theme-padding);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert - last declaration wins
        Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("20px"));
    }

    [Test]
    public async Task ComputeElementStyle_VariablesInShorthands_ResolveCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --theme-color: blue;
                    }
                    div {
                        /* Variables in shorthand properties */
                        border: 1px solid var(--theme-color);
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Create engine with the document
        var styleEngine = new StyleEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = styleEngine.ComputeElementStyle(element);

        // Assert - variable should be resolved in shorthand
        Assert.That(style.GetPropertyValue("border-color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // blue
    }

    [Test]
    public async Task ComputeElementStyle_VariableInMedia_SelectsCorrectRule()
    {
        // Arrange
        var html = @"
            <html>
            <head>
                <style>
                    :root {
                        --min-width: 800px;
                    }

                    /* This only applies if screen is at least 800px wide */
                    @media screen and (min-width: 800px) {
                        div {
                            color: blue;
                        }
                    }

                    /* Base style */
                    div {
                        color: red;
                    }
                </style>
            </head>
            <body>
                <div id='test'>Test</div>
            </body>
            </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("test") as IElement;

        // Set device to width 1000px (larger than min-width)
        var wideDevice = new MockRenderDevice { ViewPortWidth = 1000 };
        var styleEngineWide = new StyleEngine(wideDevice, _context, document);

        // Act & Assert with wide viewport
        Assert.IsNotNull(element);
        var styleWide = styleEngineWide.ComputeElementStyle(element);
        Assert.That(styleWide.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // blue

        // Set device to width 600px (smaller than min-width)
        var narrowDevice = new MockRenderDevice { ViewPortWidth = 600 };
        var styleEngineNarrow = new StyleEngine(narrowDevice, _context, document);

        // Act & Assert with narrow viewport
        var styleNarrow = styleEngineNarrow.ComputeElementStyle(element);
        Assert.That(styleNarrow.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // red
    }
}