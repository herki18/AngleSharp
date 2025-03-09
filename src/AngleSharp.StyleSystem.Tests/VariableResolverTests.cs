using Moq;
using AngleSharp.Dom;
using AngleSharp.Css.Values;

namespace AngleSharp.StyleSystem.Tests;

using Core;

[TestFixture]
public class VariableResolverTests
{
    private Mock<IBrowsingContext> _contextMock;
    private VariableResolver _resolver;
    private Mock<IElement> _elementMock;
    private Mock<IElement> _parentElementMock;
    private Mock<IElement> _rootElementMock;
    private Mock<IDocument> _documentMock;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<IBrowsingContext>();
        _resolver = new VariableResolver(_contextMock.Object);

        // Setup element hierarchy
        _elementMock = new Mock<IElement>();
        _parentElementMock = new Mock<IElement>();
        _rootElementMock = new Mock<IElement>();
        _documentMock = new Mock<IDocument>();

        _elementMock.Setup(e => e.ParentElement).Returns(_parentElementMock.Object);
        _parentElementMock.Setup(e => e.ParentElement).Returns(_rootElementMock.Object);
        _rootElementMock.Setup(e => e.ParentElement).Returns((IElement)null!);

        _documentMock.Setup(d => d.DocumentElement).Returns(_rootElementMock.Object);
        _elementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
        _parentElementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
        _rootElementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
    }

    [Test]
    public void ResolveVariable_SimpleVariable_ReturnsValue()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        _resolver.RegisterVariable(_elementMock.Object, "--primary-color", colorValue);

        // Act
        var result = _resolver.ResolveVariable("--primary-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(255));
        Assert.That(resultColor.G, Is.EqualTo(0));
        Assert.That(resultColor.B, Is.EqualTo(0));
    }

    [Test]
    public void ResolveVariable_WithFallback_ReturnsOriginalValue()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue
        _resolver.RegisterVariable(_elementMock.Object, "--primary-color", colorValue);

        // Act
        var result = _resolver.ResolveVariable("--primary-color", _elementMock.Object, fallbackValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(255)); // Should get red, not the fallback blue
    }

    [Test]
    public void ResolveVariable_WithFallback_UsesFallbackWhenVariableNotFound()
    {
        // Arrange
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue

        // Act
        var result = _resolver.ResolveVariable("--missing-color", _elementMock.Object, fallbackValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(0));
        Assert.That(resultColor.G, Is.EqualTo(0));
        Assert.That(resultColor.B, Is.EqualTo(255)); // Should get fallback blue
    }

    [Test]
    public void ResolveVariable_FromParentElement_ReturnsParentValue()
    {
        // Arrange
        var parentColorValue = new CssColorValue(0, 255, 0, 1); // green
        _resolver.RegisterVariable(_parentElementMock.Object, "--accent-color", parentColorValue);

        // Act
        var result = _resolver.ResolveVariable("--accent-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(0));
        Assert.That(resultColor.G, Is.EqualTo(255));
        Assert.That(resultColor.B, Is.EqualTo(0));
    }

    [Test]
    public void ResolveVariable_FromRootElement_ReturnsRootValue()
    {
        // Arrange
        var rootColorValue = new CssColorValue(255, 255, 0, 1); // yellow
        _resolver.RegisterVariable(_rootElementMock.Object, "--global-color", rootColorValue);

        // Act
        var result = _resolver.ResolveVariable("--global-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(255));
        Assert.That(resultColor.G, Is.EqualTo(255));
        Assert.That(resultColor.B, Is.EqualTo(0));
    }

    [Test]
    public void ResolveVarFunction_SimpleVarFunction_ResolvesCorrectly()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        _resolver.RegisterVariable(_elementMock.Object, "--primary-color", colorValue);
        var varValue = new CssVarValue("--primary-color", null);

        // Act
        var result = _resolver.ResolveVarFunction(varValue, _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
    }

    [Test]
    public void ResolveVarFunction_WithFallback_UsesFallbackWhenVariableNotFound()
    {
        // Arrange
        var fallbackValue = new CssColorValue(0, 0, 255, 1); // blue
        var varValue = new CssVarValue("--missing-color", fallbackValue);

        // Act
        var result = _resolver.ResolveVarFunction(varValue, _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(0));
        Assert.That(resultColor.G, Is.EqualTo(0));
        Assert.That(resultColor.B, Is.EqualTo(255)); // Should get fallback blue
    }

    [Test]
    public void ResolveVariablesInValue_NestedVariables_ResolvesAllVariables()
    {
        // Arrange
        var fontSize = new CssLengthValue(16, CssLengthValue.Unit.Px);
        var spacing = new CssLengthValue(8, CssLengthValue.Unit.Px);
        _resolver.RegisterVariable(_elementMock.Object, "--base-font-size", fontSize);
        _resolver.RegisterVariable(_elementMock.Object, "--base-spacing", spacing);

        // Create a value that references another variable: var(--computed-spacing, var(--base-spacing))
        var innerVarValue = new CssVarValue("--base-spacing", null);
        var outerVarValue = new CssVarValue("--computed-spacing", innerVarValue);

        // Act
        var result = _resolver.ResolveVariablesInValue(outerVarValue, _elementMock.Object, "margin");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssLengthValue>(result);
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(8));
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
    }

    [Test]
    public void ResolveVariable_CircularReference_ReturnsNull()
    {
        // Arrange - Create a circular reference: A -> B -> C -> A
        var varValueA = new CssVarValue("--var-b", null);
        var varValueB = new CssVarValue("--var-c", null);
        var varValueC = new CssVarValue("--var-a", null);

        _resolver.RegisterVariable(_elementMock.Object, "--var-a", varValueA);
        _resolver.RegisterVariable(_elementMock.Object, "--var-b", varValueB);
        _resolver.RegisterVariable(_elementMock.Object, "--var-c", varValueC);

        // Act
        var result = _resolver.ResolveVariable("--var-a", _elementMock.Object);

        // Assert
        Assert.IsNull(result); // Should break the circular reference and return null
    }
}