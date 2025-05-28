namespace LayoutEngine.Core.Tests;

using AngleSharp;
using AngleSharp.Css.Dom;
using LayoutEngine.Core.Style.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class InheritanceResolverTests : IDisposable
{
    private readonly ILogger<InheritanceResolver> _logger;
    private readonly InheritanceResolver _inheritanceResolver;
    private readonly IBrowsingContext _context;

    public InheritanceResolverTests()
    {
        _logger = Substitute.For<ILogger<InheritanceResolver>>();
        _inheritanceResolver = new InheritanceResolver(_logger);

        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
    }

    [Fact]
    public void ApplyInheritance_WithInheritedProperties_CopiesFromParent()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("color", "red"),
            ("font-family", "Arial"),
            ("font-size", "16px"),
            ("line-height", "1.5")
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("red", childDeclaration.GetPropertyValue("color"));
        Assert.Equal("Arial", childDeclaration.GetPropertyValue("font-family"));
        Assert.Equal("16px", childDeclaration.GetPropertyValue("font-size"));
        Assert.Equal("1.5", childDeclaration.GetPropertyValue("line-height"));
    }

    [Fact]
    public void ApplyInheritance_WithNonInheritedProperties_DoesNotCopy()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("background-color", "blue"),
            ("border", "1px solid black"),
            ("margin", "10px"),
            ("padding", "5px"),
            ("display", "block")
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Empty(childDeclaration.GetPropertyValue("background-color"));
        Assert.Empty(childDeclaration.GetPropertyValue("border"));
        Assert.Empty(childDeclaration.GetPropertyValue("margin"));
        Assert.Empty(childDeclaration.GetPropertyValue("padding"));
        Assert.Empty(childDeclaration.GetPropertyValue("display"));
    }

    [Fact]
    public void ApplyInheritance_WithExistingChildProperties_DoesNotOverride()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("color", "red"),
            ("font-size", "16px")
        });

        var childDeclaration = CreateStyleDeclaration(new[]
        {
            ("color", "blue") // Child already has color set
        });

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("blue", childDeclaration.GetPropertyValue("color")); // Should keep child's value
        Assert.Equal("16px", childDeclaration.GetPropertyValue("font-size")); // Should inherit from parent
    }

    [Fact]
    public void ApplyInheritance_WithMixedProperties_OnlyInheritsAppropriateOnes()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("color", "red"),           // Inherited
            ("font-family", "Arial"),   // Inherited
            ("background", "blue"),     // Not inherited
            ("margin", "10px"),         // Not inherited
            ("text-align", "center"),   // Inherited
            ("border", "1px solid")     // Not inherited
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        // Should inherit these
        Assert.Equal("red", childDeclaration.GetPropertyValue("color"));
        Assert.Equal("Arial", childDeclaration.GetPropertyValue("font-family"));
        Assert.Equal("center", childDeclaration.GetPropertyValue("text-align"));

        // Should not inherit these
        Assert.Empty(childDeclaration.GetPropertyValue("background"));
        Assert.Empty(childDeclaration.GetPropertyValue("margin"));
        Assert.Empty(childDeclaration.GetPropertyValue("border"));
    }

    [Fact]
    public void ApplyInheritance_WithEmptyParentDeclaration_DoesNothing()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new (string, string)[0]);
        var childDeclaration = CreateStyleDeclaration(new[]
        {
            ("color", "blue")
        });

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("blue", childDeclaration.GetPropertyValue("color"));
        Assert.Equal(1, childDeclaration.Length); // Should still have original property
    }

    [Fact]
    public void ApplyInheritance_WithTextProperties_InheritsCorrectly()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("text-indent", "2em"),
            ("text-transform", "uppercase"),
            ("white-space", "nowrap"),
            ("word-spacing", "2px"),
            ("letter-spacing", "1px")
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("2em", childDeclaration.GetPropertyValue("text-indent"));
        Assert.Equal("uppercase", childDeclaration.GetPropertyValue("text-transform"));
        Assert.Equal("nowrap", childDeclaration.GetPropertyValue("white-space"));
        Assert.Equal("2px", childDeclaration.GetPropertyValue("word-spacing"));
        Assert.Equal("1px", childDeclaration.GetPropertyValue("letter-spacing"));
    }

    [Fact]
    public void ApplyInheritance_WithFontProperties_InheritsCorrectly()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("font-weight", "bold"),
            ("font-style", "italic"),
            ("font-family", "Times, serif"),
            ("font-size", "18px")
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("bold", childDeclaration.GetPropertyValue("font-weight"));
        Assert.Equal("italic", childDeclaration.GetPropertyValue("font-style"));
        Assert.Equal("Times, serif", childDeclaration.GetPropertyValue("font-family"));
        Assert.Equal("18px", childDeclaration.GetPropertyValue("font-size"));
    }

    [Fact]
    public void ApplyInheritance_WithVisibilityProperty_InheritsCorrectly()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[]
        {
            ("visibility", "hidden")
        });

        var childDeclaration = CreateStyleDeclaration(new (string, string)[0]);

        // Act
        _inheritanceResolver.ApplyInheritance(childDeclaration, parentDeclaration);

        // Assert
        Assert.Equal("hidden", childDeclaration.GetPropertyValue("visibility"));
    }

    private ICssStyleDeclaration CreateStyleDeclaration((string property, string value)[] properties)
    {
        var declaration = new CssStyleDeclaration(_context);

        foreach (var (property, value) in properties)
        {
            declaration.SetProperty(property, value);
        }

        return declaration;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}