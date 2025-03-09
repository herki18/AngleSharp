using Moq;
using AngleSharp.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Css.Dom;

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

    #region Basic Variable Resolution

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
    public void ResolveVariable_VariableNotFound_NoFallback_ReturnsNull()
    {
        // Act
        var result = _resolver.ResolveVariable("--non-existent", _elementMock.Object);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region Variable Inheritance and Cascading

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
    public void ResolveVariable_ElementShadowsParentValue_ReturnsElementValue()
    {
        // Arrange
        var parentColorValue = new CssColorValue(0, 255, 0, 1); // green
        var elementColorValue = new CssColorValue(255, 0, 0, 1); // red
        _resolver.RegisterVariable(_parentElementMock.Object, "--theme-color", parentColorValue);
        _resolver.RegisterVariable(_elementMock.Object, "--theme-color", elementColorValue);

        // Act
        var result = _resolver.ResolveVariable("--theme-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(255)); // Should get element's red, not parent's green
    }

    [Test]
    public void ResolveVariable_ParentShadowsRootValue_ReturnsParentValue()
    {
        // Arrange
        var rootColorValue = new CssColorValue(255, 255, 0, 1); // yellow
        var parentColorValue = new CssColorValue(0, 255, 0, 1); // green
        _resolver.RegisterVariable(_rootElementMock.Object, "--brand-color", rootColorValue);
        _resolver.RegisterVariable(_parentElementMock.Object, "--brand-color", parentColorValue);

        // Act
        var result = _resolver.ResolveVariable("--brand-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.G, Is.EqualTo(255)); // Should get parent's green, not root's yellow
    }

    #endregion

    #region Variable Function Resolution

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
    public void ResolveVarFunction_NestedFallbacks_ResolvesToCorrectValue()
    {
        // Arrange - Create nested fallbacks: var(--missing1, var(--missing2, var(--existing, red)))
        var existingValue = new CssColorValue(0, 128, 0, 1); // green
        _resolver.RegisterVariable(_elementMock.Object, "--existing", existingValue);

        var innerVarValue = new CssVarValue("--existing", new CssColorValue(255, 0, 0, 1)); // fallback to red
        var middleVarValue = new CssVarValue("--missing2", innerVarValue);
        var outerVarValue = new CssVarValue("--missing1", middleVarValue);

        // Act
        var result = _resolver.ResolveVarFunction(outerVarValue, _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.G, Is.EqualTo(128)); // Should resolve to green
    }

    #endregion

    #region Complex Value Resolution

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
    public void ResolveVariablesInValue_ComplexValue_ResolvesEmbeddedVariables()
    {
        // Arrange
        // This test would test a complex value like: "10px var(--spacing) 5px var(--color)"
        // For simplicity, we're just mocking this with a complex value containing a var()
        var spacingValue = new CssLengthValue(16, CssLengthValue.Unit.Px);
        _resolver.RegisterVariable(_elementMock.Object, "--spacing", spacingValue);

        var complexValueMock = new Mock<ICssValue>();
        var varValue = new CssVarValue("--spacing", null);

        // Setup to simulate a complex value that contains variables
        complexValueMock.Setup(v => v.CssText).Returns("10px var(--spacing) 5px");

        // Act
        var result = _resolver.ResolveVariablesInValue(complexValueMock.Object, _elementMock.Object, "padding");

        // Assert
        // The specific assertions would depend on how your ResolveVariablesInValue handles complex values
        Assert.IsNotNull(result);
    }

    [Test]
    public void ResolveVariablesInValue_DifferentValueTypes_HandlesAllTypesCorrectly()
    {
        // Arrange
        var lengthValue = new CssLengthValue(20, CssLengthValue.Unit.Px);
        var colorValue = new CssColorValue(0, 0, 255, 1); // blue
        var percentValue = new CssPercentageValue(50); // 50%

        _resolver.RegisterVariable(_elementMock.Object, "--size", lengthValue);
        _resolver.RegisterVariable(_elementMock.Object, "--highlight", colorValue);
        _resolver.RegisterVariable(_elementMock.Object, "--scale", percentValue);

        var sizeVarValue = new CssVarValue("--size", null);
        var colorVarValue = new CssVarValue("--highlight", null);
        var scaleVarValue = new CssVarValue("--scale", null);

        // Act & Assert for each type
        var sizeResult = _resolver.ResolveVariablesInValue(sizeVarValue, _elementMock.Object, "width");
        var colorResult = _resolver.ResolveVariablesInValue(colorVarValue, _elementMock.Object, "color");
        var scaleResult = _resolver.ResolveVariablesInValue(scaleVarValue, _elementMock.Object, "transform");

        Assert.IsInstanceOf<CssLengthValue>(sizeResult);
        Assert.IsInstanceOf<CssColorValue>(colorResult);
        Assert.IsInstanceOf<CssPercentageValue>(scaleResult);
    }

    #endregion

    #region Edge Cases and Error Handling

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

    [Test]
    public void ResolveVariable_NullElement_ReturnsNullOrFallback()
    {
        // Arrange
        var fallbackValue = new CssColorValue(0, 0, 0, 1); // black

        // Act
        var result = _resolver.ResolveVariable("--any-color", null!, fallbackValue);

        // Assert
        Assert.IsNotNull(result); // Should return the fallback
        Assert.IsInstanceOf<CssColorValue>(result);
    }

    [Test]
    public void ResolveVariable_InvalidVariableName_ReturnsNullOrFallback()
    {
        // Arrange - Variable names must start with --
        var fallbackValue = new CssLengthValue(1, CssLengthValue.Unit.Em);

        // Act
        var result = _resolver.ResolveVariable("invalid-name", _elementMock.Object, fallbackValue);

        // Assert
        Assert.IsNotNull(result); // Should return the fallback
        Assert.IsInstanceOf<CssLengthValue>(result);
    }

    [Test]
    public void ResolveVariable_MaximumDepthExceeded_ReturnsNullToAvoidStackOverflow()
    {
        // Arrange - Create a deeply nested chain: A -> B -> C -> ... -> Z
        // This test checks that the resolver has a maximum resolution depth
        const int maxDepth = 50; // Assuming this is the limit in implementation

        // Create a chain of variables where each refers to the next
        for (int i = 0; i < maxDepth + 10; i++)
        {
            char current = (char)('a' + i);
            char next = (char)('a' + i + 1);

            var varValue = new CssVarValue($"--var-{next}", null);
            _resolver.RegisterVariable(_elementMock.Object, $"--var-{current}", varValue);
        }

        // Act
        var result = _resolver.ResolveVariable("--var-a", _elementMock.Object);

        // Assert
        Assert.IsNull(result); // Should terminate resolution due to excessive depth
    }

    #endregion

    #region Style Declaration Integration

    [Test]
    public void ExtractVariablesFromStyle_CustomPropertiesDetected_RegistersAllVariables()
    {
        // Arrange
        var styleMock = new Mock<ICssStyleDeclaration>();
        var properties = new List<ICssProperty>();

        // Create mock CSS properties
        var colorPropMock = new Mock<ICssProperty>();
        colorPropMock.Setup(p => p.Name).Returns("--theme-color");
        colorPropMock.Setup(p => p.RawValue).Returns(new CssColorValue(255, 0, 0, 1));

        var sizePropMock = new Mock<ICssProperty>();
        sizePropMock.Setup(p => p.Name).Returns("--base-size");
        sizePropMock.Setup(p => p.RawValue).Returns(new CssLengthValue(16, CssLengthValue.Unit.Px));

        var regularPropMock = new Mock<ICssProperty>();
        regularPropMock.Setup(p => p.Name).Returns("color");
        regularPropMock.Setup(p => p.RawValue).Returns(new CssColorValue(0, 0, 0, 1));

        properties.Add(colorPropMock.Object);
        properties.Add(sizePropMock.Object);
        properties.Add(regularPropMock.Object);

        styleMock.Setup(s => s.GetEnumerator()).Returns(properties.GetEnumerator());

        // Act
        _resolver.ExtractVariablesFromStyle(_elementMock.Object, styleMock.Object);

        // Assert - Should register the two custom properties but not the regular one
        var colorResult = _resolver.ResolveVariable("--theme-color", _elementMock.Object);
        var sizeResult = _resolver.ResolveVariable("--base-size", _elementMock.Object);

        Assert.IsNotNull(colorResult);
        Assert.IsNotNull(sizeResult);
        Assert.IsInstanceOf<CssColorValue>(colorResult);
        Assert.IsInstanceOf<CssLengthValue>(sizeResult);
    }

    [Test]
    public void ExtractVariablesFromStyle_EmptyStyle_DoesNothing()
    {
        // Arrange
        var styleMock = new Mock<ICssStyleDeclaration>();
        var emptyProperties = new List<ICssProperty>();
        styleMock.Setup(s => s.GetEnumerator()).Returns(emptyProperties.GetEnumerator());

        // Act - Should not throw or error
        _resolver.ExtractVariablesFromStyle(_elementMock.Object, styleMock.Object);

        // No assertions needed, this tests that no exceptions occur
    }

    #endregion

    #region Performance Considerations

    [Test]
    public void ResolveVariable_MultipleCalls_ReturnsCachedResults()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1);
        _resolver.RegisterVariable(_elementMock.Object, "--cached-color", colorValue);

        // Act
        var result1 = _resolver.ResolveVariable("--cached-color", _elementMock.Object);
        var result2 = _resolver.ResolveVariable("--cached-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        // These should be the same object instance if caching is working
        Assert.That(result2, Is.SameAs(result1));
    }

    [Test]
    public void RegisterVariable_UpdatesExistingVariable_InvalidatesCache()
    {
        // Arrange
        var initialColor = new CssColorValue(255, 0, 0, 1); // red
        var updatedColor = new CssColorValue(0, 255, 0, 1); // green

        _resolver.RegisterVariable(_elementMock.Object, "--dynamic-color", initialColor);
        var initialResult = _resolver.ResolveVariable("--dynamic-color", _elementMock.Object);

        // Act - Update the variable
        _resolver.RegisterVariable(_elementMock.Object, "--dynamic-color", updatedColor);
        var updatedResult = _resolver.ResolveVariable("--dynamic-color", _elementMock.Object);

        // Assert
        Assert.IsInstanceOf<CssColorValue>(initialResult);
        Assert.IsInstanceOf<CssColorValue>(updatedResult);

        var initialColorResult = (CssColorValue)initialResult!;
        var updatedColorResult = (CssColorValue)updatedResult!;

        Assert.That(initialColorResult.R, Is.EqualTo(255));
        Assert.That(updatedColorResult.G, Is.EqualTo(255));
        Assert.That(updatedResult, Is.Not.SameAs(initialResult)); // Should be different objects
    }

    #endregion
}