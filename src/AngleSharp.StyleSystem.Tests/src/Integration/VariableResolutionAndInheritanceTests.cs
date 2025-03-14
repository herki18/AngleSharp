// using AngleSharp.Css;
// using AngleSharp.Dom;
// using AngleSharp.Html.Parser;
// using AngleSharp.StyleSystem.Integration;
//
// namespace AngleSharp.StyleSystem.Tests.Integration
// {
//     using Css.Parser;
//     using Interfaces;
//
//     [TestFixture]
//     public class VariableResolutionAndInheritanceTests
//     {
//         private IBrowsingContext _context;
//         private IHtmlParser _htmlParser;
//         private ICssParser _cssParser;
//         private IStyleEngine _styleEngine;
//
//         [SetUp]
//         public void Setup()
//         {
//             // Configure the browsing context with CSS and StyleSystem support
//             var config = Configuration.Default
//                 .WithCss()
//                 .WithRenderDevice(new DefaultRenderDevice()
//                 {
//                     ViewPortWidth = 1024,
//                     ViewPortHeight = 768
//                 });
//
//             _context = BrowsingContext.New(config);
//             _htmlParser = _context.GetService<IHtmlParser>()!;
//             _cssParser = _context.GetService<ICssParser>()!;
//
//             // Get or create style engine
//             _styleEngine = _context.GetService<StyleEngine>() ??
//                 new StyleEngine(_context);
//
//             if (_context.GetService<StyleEngine>() == null)
//             {
//                 // Register the style engine if not automatically registered
//                 var services = new Dictionary<Type, object>
//                 {
//                     { typeof(StyleEngine), _styleEngine }
//                 };
//                 _context = BrowsingContext.New(config.With(services));
//             }
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             _context?.Dispose();
//         }
//
//         private IDocument SetupTestDocument(string html, string css)
//         {
//             var document = _htmlParser.ParseDocument("");
//             var styleElement = document.CreateElement("style");
//             styleElement.TextContent = css;
//             document.Head!.AppendChild(styleElement);
//
//             var bodyContent = _htmlParser.ParseFragment(html, document.Body!).ToArray();
//             foreach (var node in bodyContent)
//             {
//                 document.Body?.AppendChild(node);
//             }
//
//             // Attach style engine to document
//             _styleEngine.StylesheetManager.AttachToDocument(document);
//
//             return document;
//         }
//
//         [Test]
//         public void BasicVariableDefinitionAndUsage()
//         {
//             // Test basic CSS variable definition and usage
//             var html = "<div id='test'>Variable test</div>";
//             var css = ":root { --main-color: #ff0000; } #test { color: var(--main-color); }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // The color should be the resolved value of --main-color (red)
//             Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
//         }
//
//         [Test]
//         public void VariableInheritanceFromParentToChild()
//         {
//             // Test CSS variable inheritance from parent to child
//             var html = @"
//                 <div id='parent'>
//                     <div id='child'>Child text</div>
//                 </div>";
//             var css = @"
//                 #parent { --parent-var: 20px; }
//                 #child { margin-left: var(--parent-var); }";
//
//             var document = SetupTestDocument(html, css);
//             var child = document.GetElementById("child");
//
//             var style = _styleEngine.ComputeElementStyle(child!);
//
//             // The margin-left should inherit the --parent-var value
//             Assert.That(style.GetPropertyValue("margin-left"), Is.EqualTo("20px"));
//         }
//
//         [Test]
//         public void VariableFallbackValues()
//         {
//             // Test fallback values in var() function
//             var html = "<div id='test'>Fallback test</div>";
//             var css = "#test { font-size: var(--undefined-var, 16px); }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Should use the fallback value since --undefined-var is not defined
//             Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("16px"));
//         }
//
//         [Test]
//         public void NestedVariables()
//         {
//             // Test nested variable references
//             var html = "<div id='test'>Nested variables</div>";
//             var css = @"
//                 :root {
//                     --base-size: 10px;
//                     --large-size: calc(var(--base-size) * 2);
//                 }
//                 #test { padding: var(--large-size); }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Should resolve to 20px (10px * 2)
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("20px"));
//         }
//
//         [Test]
//         public void CircularReferences()
//         {
//             // Test circular variable references
//             var html = "<div id='test'>Circular references</div>";
//             var css = @"
//                 :root {
//                     --var1: var(--var2);
//                     --var2: var(--var1, 30px);
//                 }
//                 #test { margin: var(--var1, 20px); }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Should resolve to the fallback value 20px because of circular reference
//             Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("20px"));
//         }
//
//         [Test]
//         public void VariablesInDifferentPropertyTypes()
//         {
//             // Test variables in different property types
//             var html = "<div id='test'>Different property types</div>";
//             var css = @"
//                 :root {
//                     --color-value: blue;
//                     --size-value: 25px;
//                     --integer-value: 2;
//                 }
//                 #test {
//                     color: var(--color-value);
//                     padding: var(--size-value);
//                     font-weight: calc(300 * var(--integer-value));
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Check each property type
//             Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("25px"));
//             Assert.That(style.GetPropertyValue("font-weight"), Is.EqualTo("600"));
//         }
//
//         [Test]
//         public void VariableScoping()
//         {
//             // Test variable scoping (overriding variables in child elements)
//             var html = @"
//                 <div id='parent'>
//                     <div id='child'>Child with overridden variable</div>
//                 </div>";
//             var css = @"
//                 #parent {
//                     --scoped-var: red;
//                     color: var(--scoped-var);
//                 }
//                 #child {
//                     --scoped-var: blue;
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var parent = document.GetElementById("parent");
//             var child = document.GetElementById("child");
//
//             var parentStyle = _styleEngine.ComputeElementStyle(parent!);
//             var childStyle = _styleEngine.ComputeElementStyle(child!);
//
//             // Parent should use its defined value
//             Assert.That(parentStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
//
//             // Child should override the variable but inherit the color property
//             Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
//         }
//
//         [Test]
//         public void ComplexVariableInheritance()
//         {
//             // Test complex inheritance scenarios
//             var html = @"
//                 <div id='grandparent'>
//                     <div id='parent'>
//                         <div id='child'>Complex inheritance</div>
//                     </div>
//                 </div>";
//             var css = @"
//                 #grandparent {
//                     --base-size: 10px;
//                     --font-family: Arial;
//                 }
//                 #parent {
//                     --spacing: calc(var(--base-size) * 2);
//                     font-family: var(--font-family);
//                 }
//                 #child {
//                     --font-family: Verdana;
//                     margin: var(--spacing);
//                     padding: var(--base-size);
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var child = document.GetElementById("child");
//
//             var style = _styleEngine.ComputeElementStyle(child!);
//
//             // Check inheritance and override
//             Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("20px"));
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("10px"));
//             Assert.That(style.GetPropertyValue("font-family"), Is.EqualTo("Verdana"));
//         }
//
//         [Test]
//         public void CSSWideKeywordsWithVariables()
//         {
//             // Test CSS-wide keywords (initial, inherit, unset) with variables
//             var html = @"
//                 <div id='parent'>
//                     <div id='child'>CSS-wide keywords</div>
//                 </div>";
//             var css = @"
//                 #parent {
//                     --custom-color: red;
//                     color: var(--custom-color);
//                     font-size: 20px;
//                 }
//                 #child {
//                     --custom-color: inherit;
//                     color: var(--custom-color);
//                     font-size: initial;
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var child = document.GetElementById("child");
//
//             var style = _styleEngine.ComputeElementStyle(child!);
//
//             // --custom-color with value 'inherit' should be treated as a string, not a keyword
//             // So color should be 'inherit' string or fallback to inheritance behavior
//             // font-size should be initial (browser default, typically 16px)
//             Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("16px"));
//
//             // This is a complex case - implementations differ, but our system should
//             // either interpret 'inherit' as a string and use color: inherit
//             // or follow inheritance and get red from parent
//             var colorValue = style.GetPropertyValue("color");
//             Assert.That(colorValue, Is.AnyOf("inherit", "rgba(255, 0, 0, 1)"));
//         }
//
//         [Test]
//         public void VariablesWithMediaQueries()
//         {
//             // Test variables with media queries
//             var html = "<div id='test'>Media query test</div>";
//             var css = @"
//                 :root {
//                     --base-padding: 10px;
//                 }
//                 @media (min-width: 800px) {
//                     :root {
//                         --base-padding: 20px;
//                     }
//                 }
//                 #test { padding: var(--base-padding); }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             // Default viewport is 1024px wide, so the media query should apply
//             var style = _styleEngine.ComputeElementStyle(element!);
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("20px"));
//
//             // Change viewport to 700px, media query should not apply
//             _styleEngine.RenderDevice.SetViewport(700, 768);
//             _styleEngine.InvalidationTracker.InvalidateElement(element!);
//             style = _styleEngine.ComputeElementStyle(element!);
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("10px"));
//         }
//
//         [Test]
//         public void MixedVariablesAndStaticValues()
//         {
//             // Test mixing variables with static values
//             var html = "<div id='test'>Mixed values</div>";
//             var css = @"
//                 :root {
//                     --main-color: blue;
//                     --accent-padding: 15px;
//                 }
//                 #test {
//                     border: 1px solid var(--main-color);
//                     margin: var(--accent-padding) 20px;
//                     padding: calc(var(--accent-padding) - 5px);
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Variables should be resolved within complex values
//             Assert.That(style.GetPropertyValue("border"), Is.EqualTo("1px solid rgba(0, 0, 255, 1)"));
//             Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("15px 20px"));
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("10px"));
//         }
//
//         [Test]
//         public void InvalidVariableValues()
//         {
//             // Test handling of invalid variable values
//             var html = "<div id='test'>Invalid values</div>";
//             var css = @"
//                 :root {
//                     --invalid-color: not-a-color;
//                     --empty-var: ;
//                 }
//                 #test {
//                     color: var(--invalid-color, black);
//                     margin: var(--empty-var, 10px);
//                     padding: var(--undefined-var);
//                 }";
//
//             var document = SetupTestDocument(html, css);
//             var element = document.GetElementById("test");
//
//             var style = _styleEngine.ComputeElementStyle(element!);
//
//             // Should use fallback for invalid color
//             Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)"));
//
//             // Should use fallback for empty variable
//             Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("10px"));
//
//             // Should use initial value for undefined variable with no fallback
//             Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("0px"));
//         }
//     }
// }