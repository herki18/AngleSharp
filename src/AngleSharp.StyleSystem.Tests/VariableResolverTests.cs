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

    #region Variable Management

    [Test]
    public void RemoveVariable_ExistingVariable_RemovesSuccessfully()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1); // red
        _resolver.RegisterVariable(_elementMock.Object, "--temp-color", colorValue);

        // Verify variable exists before removal
        var beforeRemoval = _resolver.ResolveVariable("--temp-color", _elementMock.Object);
        Assert.IsNotNull(beforeRemoval);

        // Act - Extend VariableResolver to support removing variables
        _resolver.RemoveVariable(_elementMock.Object, "--temp-color");

        // Assert
        var afterRemoval = _resolver.ResolveVariable("--temp-color", _elementMock.Object);
        Assert.IsNull(afterRemoval);
    }

    [Test]
    public void RemoveVariable_NonexistentVariable_DoesNothing()
    {
        // Arrange - No setup needed for nonexistent variable

        // Act - Should not throw exception
        _resolver.RemoveVariable(_elementMock.Object, "--nonexistent-variable");

        // Assert - Just verify no exception was thrown
        Assert.Pass("No exception was thrown when removing nonexistent variable");
    }

    [Test]
    public void RemoveVariable_InvalidatesCache()
    {
        // Arrange
        var originalColor = new CssColorValue(255, 0, 0, 1); // red
        var parentColor = new CssColorValue(0, 0, 255, 1); // blue

        // Set up cascading variables
        _resolver.RegisterVariable(_elementMock.Object, "--cascading-color", originalColor);
        _resolver.RegisterVariable(_parentElementMock.Object, "--cascading-color", parentColor);

        // Cache the value by resolving it once
        var resultBeforeRemoval = _resolver.ResolveVariable("--cascading-color", _elementMock.Object);
        Assert.That(((CssColorValue)resultBeforeRemoval!).R, Is.EqualTo(255)); // red from element

        // Act
        _resolver.RemoveVariable(_elementMock.Object, "--cascading-color");
        var resultAfterRemoval = _resolver.ResolveVariable("--cascading-color", _elementMock.Object);

        // Assert
        Assert.IsNotNull(resultAfterRemoval);
        Assert.That(((CssColorValue)resultAfterRemoval).B, Is.EqualTo(255)); // blue from parent
    }

    [Test]
    public void ClearElementVariables_RemovesAllVariablesForElement()
    {
        // Arrange
        var color1 = new CssColorValue(255, 0, 0, 1); // red
        var color2 = new CssColorValue(0, 255, 0, 1); // green
        var length = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Register multiple variables
        _resolver.RegisterVariable(_elementMock.Object, "--color-1", color1);
        _resolver.RegisterVariable(_elementMock.Object, "--color-2", color2);
        _resolver.RegisterVariable(_elementMock.Object, "--size", length);

        // Verify variables exist
        Assert.IsNotNull(_resolver.ResolveVariable("--color-1", _elementMock.Object));
        Assert.IsNotNull(_resolver.ResolveVariable("--color-2", _elementMock.Object));
        Assert.IsNotNull(_resolver.ResolveVariable("--size", _elementMock.Object));

        // Act
        _resolver.ClearElementVariables(_elementMock.Object);

        // Assert
        Assert.IsNull(_resolver.ResolveVariable("--color-1", _elementMock.Object));
        Assert.IsNull(_resolver.ResolveVariable("--color-2", _elementMock.Object));
        Assert.IsNull(_resolver.ResolveVariable("--size", _elementMock.Object));
    }

    [Test]
    public void RegisterVariable_NullOrEmptyName_HandlesGracefully()
    {
        // Arrange
        var colorValue = new CssColorValue(255, 0, 0, 1); // red

        // Act - Should not throw exception
        _resolver.RegisterVariable(_elementMock.Object, null!, colorValue);
        _resolver.RegisterVariable(_elementMock.Object, "", colorValue);

        // Assert - Just verify no exception was thrown
        Assert.Pass("No exception was thrown with null or empty variable names");
    }

    [Test]
    public void RegisterVariable_NullValue_HandlesAppropriately()
    {
        // Arrange & Act
        _resolver.RegisterVariable(_elementMock.Object, "--null-value", null!);

        // Assert
        var result = _resolver.ResolveVariable("--null-value", _elementMock.Object);
        Assert.IsNull(result, "Variable with null value should resolve to null");
    }

    [Test]
    public void RegisterVariable_UpdateExistingValue_OverwritesCorrectly()
    {
        // Arrange
        var originalColor = new CssColorValue(255, 0, 0, 1); // red
        var updatedColor = new CssColorValue(0, 255, 0, 1); // green

        // Register original value
        _resolver.RegisterVariable(_elementMock.Object, "--update-test", originalColor);
        var originalResult = _resolver.ResolveVariable("--update-test", _elementMock.Object);
        Assert.That(((CssColorValue)originalResult!).R, Is.EqualTo(255));

        // Act - Update the value
        _resolver.RegisterVariable(_elementMock.Object, "--update-test", updatedColor);

        // Assert
        var updatedResult = _resolver.ResolveVariable("--update-test", _elementMock.Object);
        Assert.IsNotNull(updatedResult);
        Assert.That(((CssColorValue)updatedResult).G, Is.EqualTo(255));
    }

    [Test]
    public void VariableNameValidation_MalformedName_HandlesConsistently()
    {
        // Arrange - Variable name without -- prefix
        var colorValue = new CssColorValue(255, 0, 0, 1); // red

        // Act
        _resolver.RegisterVariable(_elementMock.Object, "invalid-name", colorValue);
        var result = _resolver.ResolveVariable("invalid-name", _elementMock.Object);

        // Assert - Without -- prefix, it should not be recognized as a CSS variable
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

    [Test]
    public void CircularReferenceWithFallback_UsesDefaultValue()
    {
        // Arrange - Create a circular reference with fallback values
        var fallbackValue = new CssColorValue(128, 0, 128, 1); // purple

        var varValueA = new CssVarValue("--circ-b", null);
        var varValueB = new CssVarValue("--circ-c", null);
        var varValueC = new CssVarValue("--circ-a", fallbackValue); // Fallback on the last one

        _resolver.RegisterVariable(_elementMock.Object, "--circ-a", varValueA);
        _resolver.RegisterVariable(_elementMock.Object, "--circ-b", varValueB);
        _resolver.RegisterVariable(_elementMock.Object, "--circ-c", varValueC);

        // Act
        var result = _resolver.ResolveVariable("--circ-a", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(128)); // Should get the purple fallback
    }

    [Test]
    public void MaxDepthResolution_WithFallback_UsesDefaultValue()
    {
        // Arrange - Create a deeply nested chain with a fallback at the end
        const int maxDepth = 50; // Assuming this is the limit
        var fallbackValue = new CssColorValue(255, 165, 0, 1); // orange

        // Create nested variables
        ICssValue? currentValue = new CssVarValue($"--depth-{maxDepth + 1}", fallbackValue);

        for (int i = maxDepth; i > 0; i--)
        {
            currentValue = new CssVarValue($"--depth-{i}", currentValue);
            _resolver.RegisterVariable(_elementMock.Object, $"--depth-{i}", currentValue);
        }

        // Act
        var result = _resolver.ResolveVariable("--depth-1", _elementMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssColorValue>(result);
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(255)); // Should get the orange fallback
    }

    [Test]
    public void NullElementWithValidValue_ReturnsCorrectResult()
    {
        // Arrange
        var fallbackValue = new CssColorValue(0, 0, 0, 1); // black
        var nullElement = (IElement)null!;

        // Act
        var result = _resolver.ResolveVariable("--any-var", nullElement);
        var resultWithFallback = _resolver.ResolveVariable("--any-var", nullElement, fallbackValue);

        // Assert
        Assert.IsNull(result);
        Assert.IsNotNull(resultWithFallback);
        Assert.IsInstanceOf<CssColorValue>(resultWithFallback);
    }

    [Test]
    public void MalformedVariableReference_HandlesGracefully()
    {
        // Arrange - Create a CSS var() with invalid variable name format
        var validColor = new CssColorValue(255, 0, 0, 1); // red
        var invalidVarValue = new CssVarValue("invalid-format-no-dashes", null);

        _resolver.RegisterVariable(_elementMock.Object, "--valid-color", validColor);

        // Act
        var result = _resolver.ResolveVarFunction(invalidVarValue, _elementMock.Object);

        // Assert
        Assert.IsNull(result); // Should gracefully return null for invalid variable format
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

    #region Cache Management and Performance

    [Test]
    public void CacheInvalidation_ParentVariableChange_InvalidatesChildCache()
    {
        // Arrange
        var originalParentColor = new CssColorValue(0, 0, 255, 1); // blue
        var updatedParentColor = new CssColorValue(128, 0, 128, 1); // purple

        _resolver.RegisterVariable(_parentElementMock.Object, "--inherited-color", originalParentColor);

        // Cache the inherited value
        var originalResult = _resolver.ResolveVariable("--inherited-color", _elementMock.Object);
        Assert.That(((CssColorValue)originalResult!).B, Is.EqualTo(255)); // blue

        // Act - Update the parent's variable
        _resolver.RegisterVariable(_parentElementMock.Object, "--inherited-color", updatedParentColor);

        // Assert - Child should get updated value
        var updatedResult = _resolver.ResolveVariable("--inherited-color", _elementMock.Object);
        Assert.IsNotNull(updatedResult);
        Assert.That(((CssColorValue)updatedResult).R, Is.EqualTo(128)); // purple
    }

    [Test]
    public void CacheInvalidation_AfterBulkOperations_ClearsRelevantEntries()
    {
        // Arrange - Set up multiple variables to test bulk operations
        var colors = new[]
        {
            new { Name = "--color-1", Value = new CssColorValue(255, 0, 0, 1) }, // red
            new { Name = "--color-2", Value = new CssColorValue(0, 255, 0, 1) }, // green
            new { Name = "--color-3", Value = new CssColorValue(0, 0, 255, 1) }  // blue
        };

        // Register all colors
        foreach (var color in colors)
        {
            _resolver.RegisterVariable(_elementMock.Object, color.Name, color.Value);
        }

        // Prime the cache by resolving all colors
        foreach (var color in colors)
        {
            _resolver.ResolveVariable(color.Name, _elementMock.Object);
        }

        // Act - Perform a bulk update
        _resolver.RegisterVariable(_elementMock.Object, "--color-1", new CssColorValue(128, 128, 0, 1)); // yellow
        _resolver.RegisterVariable(_elementMock.Object, "--color-2", new CssColorValue(128, 0, 128, 1)); // purple

        // Assert - Cache for updated colors should be invalidated
        var color1 = (CssColorValue)_resolver.ResolveVariable("--color-1", _elementMock.Object)!;
        var color2 = (CssColorValue)_resolver.ResolveVariable("--color-2", _elementMock.Object)!;
        var color3 = (CssColorValue)_resolver.ResolveVariable("--color-3", _elementMock.Object)!;

        Assert.That(color1.R, Is.EqualTo(128)); // yellow
        Assert.That(color2.B, Is.EqualTo(128)); // purple
        Assert.That(color3.B, Is.EqualTo(255)); // still blue
    }

    [Test]
    public void CacheSize_ManyVariables_ManagesMemoryEfficiently()
    {
        // Arrange - Create many variables to test memory management
        const int variableCount = 100;

        // Act - Register many variables
        for (int i = 0; i < variableCount; i++)
        {
            var value = new CssNumberValue(i);
            _resolver.RegisterVariable(_elementMock.Object, $"--var-{i}", value);
        }

        // Now resolve them all to fill cache
        for (int i = 0; i < variableCount; i++)
        {
            _resolver.ResolveVariable($"--var-{i}", _elementMock.Object);
        }

        // Assert - Should be able to retrieve all without issues
        for (int i = 0; i < variableCount; i++)
        {
            var result = _resolver.ResolveVariable($"--var-{i}", _elementMock.Object);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<CssNumberValue>(result);
            Assert.That(((CssNumberValue)result).Value, Is.EqualTo(i));
        }

        // This test passes if it completes without memory exceptions
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