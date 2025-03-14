// namespace AngleSharp.StyleSystem.Tests.Unit;
//
// using AngleSharp.Css;
// using AngleSharp.Css.Dom;
// using AngleSharp.Css.Parser;
// using AngleSharp.Dom;
// using AngleSharp.Html.Parser;
// using AngleSharp.StyleSystem.Computation;
// using AngleSharp.StyleSystem.Integration;
// using Interfaces;
// using Moq;
//
// [TestFixture]
// public class InheritanceProcessorTests
// {
//     private IBrowsingContext _context;
//     private IInheritanceProcessor _inheritanceProcessor;
//     private IHtmlParser _parser;
//     private IDocument _document;
//     private ICssParser _cssParser;
//     private StyleEngine _styleEngine;
//     private Mock<IRenderDevice> _mockRenderDevice;
//
//     [SetUp]
//     public void Setup()
//     {
//         var config = Configuration.Default.WithCss();
//         _context = BrowsingContext.New(config);
//         _inheritanceProcessor = new InheritanceProcessor(_context);
//         _parser = new HtmlParser();
//         _document = _parser.ParseDocument("");
//         _cssParser = new CssParser();
//
//         _mockRenderDevice = new Mock<IRenderDevice>();
//         _mockRenderDevice.Setup(d => d.ViewPortWidth).Returns(1024);
//         _mockRenderDevice.Setup(d => d.ViewPortHeight).Returns(768);
//         _mockRenderDevice.Setup(d => d.FontSize).Returns(16.0);
//
//         _styleEngine = new StyleEngine(_context);
//         _styleEngine.RenderDevice = _mockRenderDevice.Object;
//     }
//
//     [TearDown]
//     public void TearDown()
//     {
//         _context?.Dispose();
//         _document?.Dispose();
//         _styleEngine?.Dispose();
//     }
//
//     [Test]
//     public void ApplyInheritance_WithNoParent_ReturnsOriginalStyle()
//     {
//         // Arrange
//         var style = new CssStyleDeclaration(_context);
//         style.SetProperty("color", "red");
//         style.SetProperty("margin", "10px");
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(style, null);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
//         Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px"));
//     }
//
//     [Test]
//     public void ApplyInheritance_WithParentStyle_InheritsProperties()
//     {
//         // Arrange
//         // Create parent element with style
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "color: blue; font-family: Arial;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style declaration
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("margin", "10px");
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // Inherited
//         Assert.That(result.GetPropertyValue("font-family"), Is.EqualTo("Arial")); // Inherited
//         Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("10px")); // Not inherited
//     }
//
//     [Test]
//     public void ApplyInheritance_WithExplicitInheritKeyword_InheritsSpecifiedProperty()
//     {
//         // Arrange
//         // Create parent element with style
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "margin: 20px; padding: 5px;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style with explicit inherit
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("margin", "inherit"); // Should inherit from parent
//         childStyle.SetProperty("padding", "10px"); // Should keep its own value
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("20px")); // Explicitly inherited
//         Assert.That(result.GetPropertyValue("padding"), Is.EqualTo("10px")); // Not inherited
//     }
//
//     [Test]
//     public void ApplyInheritance_WithAllProperty_InheritsAllProperties()
//     {
//         // Arrange
//         // Create parent element with style
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "color: red; margin: 20px; padding: 5px;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style with 'all' property
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("all", "inherit");
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("all"), Is.EqualTo("inherit")); // 'all' property remains
//         Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // Inherited
//         Assert.That(result.GetPropertyValue("margin"), Is.EqualTo("20px")); // Inherited
//         Assert.That(result.GetPropertyValue("padding"), Is.EqualTo("5px")); // Inherited
//     }
//
//     [Test]
//     [Ignore("Not implemented yet")]
//     public void ApplyInheritance_WithCssVariables_InheritsVariables()
//     {
//         // Arrange
//         // Create parent element with CSS variable
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "--main-color: blue; color: var(--main-color);");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("background-color", "var(--main-color)");
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("--main-color"), Is.EqualTo("blue")); // Variable inherited
//         Assert.That(result.GetPropertyValue("background-color"), Is.EqualTo("var(--main-color)")); // Original property kept
//     }
//
//     [Test]
//     public void ApplyInheritance_WithNonInheritableProperties_DoesNotInherit()
//     {
//         // Arrange
//         // Create parent element with non-inheritable properties
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "margin: 20px; width: 100px; display: block;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style
//         var childStyle = new CssStyleDeclaration(_context);
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("margin"), Is.Empty); // Not inherited
//         Assert.That(result.GetPropertyValue("width"), Is.Empty); // Not inherited
//         Assert.That(result.GetPropertyValue("display"), Is.Empty); // Not inherited
//     }
//
//     [Test]
//     public void ApplyInheritance_WithOverriddenProperties_ChildPropertiesWin()
//     {
//         // Arrange
//         // Create parent element with inheritable properties
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "color: blue; font-size: 16px;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style that overrides properties
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("color", "red"); // Overrides parent's color
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // Child's value kept
//         Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px")); // Inherited
//     }
//
//     [Test]
//     public void ApplyInheritance_WithInitialKeyword_ResetsToInitialValue()
//     {
//         // Arrange
//         // Create parent element with style
//         var parentElement = _document.CreateElement("div");
//         parentElement.SetAttribute("style", "color: blue; font-weight: bold;");
//         _document.Body!.AppendChild(parentElement);
//         var parentComputedStyle = _styleEngine.ComputeElementStyle(parentElement);
//
//         // Create child element style with 'initial' keyword
//         var childStyle = new CssStyleDeclaration(_context);
//         childStyle.SetProperty("color", "initial");
//
//         // Act
//         var result = _inheritanceProcessor.ApplyInheritance(childStyle, parentComputedStyle);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         // This depends on browser's initial value, typically black for color
//         Assert.That(result.GetPropertyValue("color"), Is.Not.EqualTo("rgba(0, 0, 255, 1)"));
//         Assert.That(result.GetPropertyValue("font-weight"), Is.EqualTo("bold")); // Inherited
//     }
// }