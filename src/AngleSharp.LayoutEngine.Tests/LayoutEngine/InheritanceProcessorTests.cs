namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using System;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using NUnit.Framework;
using StyleSystem;

[TestFixture]
public class InheritanceProcessorTests
{
    private IBrowsingContext _context;
    private InheritanceProcessor _processor;
    private ICssParser _cssParser;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _processor = new InheritanceProcessor(_context);
        _cssParser = _context.GetService<ICssParser>()!;
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public void ApplyInheritance_NullParentStyle_ReturnsClonedElementStyle()
    {
        // Arrange
        var elementStyle = CreateStyleDeclaration("color: red; font-size: 16px;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.SameAs(elementStyle));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public void ApplyInheritance_InheritableProperty_InheritsFromParent()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red; font-family: Arial;");
        var elementStyle = CreateStyleDeclaration("background-color: blue;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(result.GetPropertyValue("font-family"), Is.EqualTo("Arial"));
        Assert.That(result.GetPropertyValue("background-color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }

    [Test]
    public void ApplyInheritance_NonInheritableProperty_DoesNotInheritFromParent()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("background-color: red; border: 1px solid black;");
        var elementStyle = CreateStyleDeclaration("color: blue;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(result.GetPropertyValue("background-color"), Is.Empty);
        Assert.That(result.GetPropertyValue("border"), Is.Empty);
    }

    [Test]
    public void ApplyInheritance_ExplicitInheritKeyword_InheritsPropertyFromParent()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("background-color: red;");
        var elementStyle = CreateStyleDeclaration("background-color: inherit;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("background-color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ApplyInheritance_CssVariables_AlwaysInheritFromParent()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("--theme-color: blue;");
        var elementStyle = CreateStyleDeclaration("color: var(--theme-color);");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("--theme-color"), Is.EqualTo("blue"));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("var(--theme-color)"));
    }

    [Test]
    public void ApplyInheritance_ImportantFlags_PreservedWhenInheriting()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red !important;");
        var elementStyle = CreateStyleDeclaration("background-color: blue;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(result.GetPropertyPriority("color"), Is.EqualTo("important"));
    }

    [Test]
    public void ApplyInheritance_AllPropertyInherit_InheritsAllProperties()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red; background-color: blue; font-size: 16px;");
        var elementStyle = CreateStyleDeclaration("all: inherit;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("all"), Is.EqualTo("inherit"));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(result.GetPropertyValue("background-color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public void ApplyInheritance_AllPropertyInitial_ClearsAllPropertiesExceptAll()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red; background-color: blue;");
        var elementStyle = CreateStyleDeclaration("all: initial; color: green; background-color: yellow;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("all"), Is.EqualTo("initial"));
        Assert.That(result.GetPropertyValue("color"), Is.Empty);
        Assert.That(result.GetPropertyValue("background-color"), Is.Empty);
    }

    [Test]
    public void ApplyInheritance_AllPropertyUnset_InheritsOnlyInheritableProperties()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red; background-color: blue;");
        var elementStyle = CreateStyleDeclaration("all: unset; color: green; background-color: yellow;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("all"), Is.EqualTo("unset"));
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(result.GetPropertyValue("background-color"), Is.Empty);
    }

    [Test]
    public void ApplyInheritance_ElementPropertyOverridesInheritance()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red; font-size: 16px;");
        var elementStyle = CreateStyleDeclaration("color: blue;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(result.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public void ApplyInheritance_ElementImportantOverridesParentRegular()
    {
        // Arrange
        var parentStyle = CreateStyleDeclaration("color: red;");
        var elementStyle = CreateStyleDeclaration("color: blue !important;");

        // Act
        var result = _processor.ApplyInheritance(elementStyle, parentStyle);

        // Assert
        Assert.That(result.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(result.GetPropertyPriority("color"), Is.EqualTo("important"));
    }

    // Helper method to create style declarations for testing
    private ICssStyleDeclaration CreateStyleDeclaration(string cssText)
    {
        Assert.IsNotNull(_cssParser);
        return _cssParser.ParseDeclaration(cssText);
    }
}