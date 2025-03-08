```
namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Interfaces;

/// <summary>
/// Processes property inheritance according to CSS specification.
/// </summary>
public class InheritanceProcessor : IInheritanceProcessor
{
    private readonly IBrowsingContext _context;
    
    // Common CSS properties (including non-inheritable ones)
    private static readonly HashSet<string> CommonCssProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Inheritable text properties
        "color", "direction", "font-family", "font-size", "font-style", "font-variant",
        "font-weight", "font-size-adjust", "font-stretch", "font", "letter-spacing",
        "line-height", "text-align", "text-indent", "text-transform", "white-space",
        "word-spacing", "text-shadow",
        
        // List properties
        "list-style-image", "list-style-position", "list-style-type", "list-style",
        
        // Table properties
        "border-collapse", "border-spacing", "caption-side", "empty-cells",
        
        // Non-inheritable box properties
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "border", "border-top", "border-right", "border-bottom", "border-left",
        "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
        "border-style", "border-top-style", "border-right-style", "border-bottom-style", "border-left-style",
        "border-color", "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
        "width", "height", "min-width", "min-height", "max-width", "max-height",
        
        // Positioning properties
        "position", "top", "right", "bottom", "left", "z-index",
        
        // Display properties
        "display", "visibility", "overflow", "float", "clear"
    };
    
    // Properties that are inheritable by default
    private static readonly HashSet<string> InheritableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "direction", "font-family", "font-size", "font-style", "font-variant",
        "font-weight", "font-size-adjust", "font-stretch", "font", "letter-spacing",
        "line-height", "text-align", "text-indent", "text-transform", "white-space",
        "word-spacing", "text-shadow", "text-emphasis", "text-emphasis-color",
        "text-emphasis-style", "text-emphasis-position",
        "list-style-image", "list-style-position", "list-style-type", "list-style",
        "border-collapse", "border-spacing", "caption-side", "empty-cells",
        "cursor", "visibility", "quotes", "orphans", "widows"
    };

    /// <summary>
    /// Creates a new InheritanceProcessor.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    public InheritanceProcessor(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Applies inheritance to the element's style based on the parent's style.
    /// </summary>
    /// <param name="elementStyle">The element's cascaded style.</param>
    /// <param name="parentComputedStyle">The parent element's computed style.</param>
    /// <returns>The element's style with inheritance applied.</returns>
    public ICssStyleDeclaration ApplyInheritance(ICssStyleDeclaration elementStyle, IComputedStyle? parentComputedStyle)
    {
        if (elementStyle == null)
            throw new ArgumentNullException(nameof(elementStyle));

        // Nothing to inherit if there's no parent (root element)
        if (parentComputedStyle == null)
            return CloneStyleDeclaration(elementStyle);

        // Convert IComputedStyle to ICssStyleDeclaration
        var parentStyle = GetStyleDeclarationFromComputedStyle(parentComputedStyle);

        // Check for direct 'all' property usage first
        var allValue = elementStyle.GetPropertyValue("all");
        if (!string.IsNullOrEmpty(allValue))
        {
            return HandleAllProperty(allValue, elementStyle, parentStyle);
        }

        // Clone the element's style to hold the result
        var result = CloneStyleDeclaration(elementStyle);

        // Handle the parent style inheritance
        if (result is CssStyleDeclaration cssResult)
        {
            // First, handle explicit 'inherit' values on properties
            var inheritPropertiesFromParent = GetPropertiesWithExplicitInherit(elementStyle, parentStyle);
            if (inheritPropertiesFromParent.Any())
            {
                // Use SetDeclarations for properties explicitly set to 'inherit'
                cssResult.SetDeclarations(inheritPropertiesFromParent);
            }

            // Then, handle regular inheritance and CSS variables
            var inheritableProperties = GetInheritableProperties(elementStyle, parentStyle);
            if (inheritableProperties.Any())
            {
                // Use UpdateDeclarations which is specifically designed for inheritance
                cssResult.UpdateDeclarations(inheritableProperties);
            }
        }
        else
        {
            // Fallback implementation for non-CssStyleDeclaration implementations
            HandleInheritanceFallback(result, elementStyle, parentStyle);
        }

        return result;
    }

    /// <summary>
    /// Extracts a style declaration from a ComputedStyle.
    /// </summary>
    private ICssStyleDeclaration GetStyleDeclarationFromComputedStyle(IComputedStyle computedStyle)
    {
        var declaration = new CssStyleDeclaration(_context);

        // Copy ALL properties, not just inheritable ones - crucial for 'all: inherit'
        foreach (var propertyName in CommonCssProperties)
        {
            var value = computedStyle.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(propertyName, value);
            }
        }

        // Handle CSS custom properties (variables)
        // In a real implementation, we'd need to extract all CSS variables from computedStyle
        // This simplified version assumes no custom properties for now

        return declaration;
    }

    /// <summary>
    /// Handles 'all' property special cases (inherit, initial, unset).
    /// </summary>
    private ICssStyleDeclaration HandleAllProperty(string allValue, ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        var result = new CssStyleDeclaration(_context);

        // Keep the 'all' property value
        result.SetProperty("all", allValue, elementStyle.GetPropertyPriority("all"));

        switch (allValue.ToLowerInvariant())
        {
            case "inherit":
                // For 'all: inherit', we directly copy ALL properties from parent
                foreach (var prop in parentStyle)
                {
                    if (prop.Name == "all") continue; // Skip 'all' property
                    
                    result.SetProperty(
                        prop.Name,
                        prop.Value,
                        prop is ICssProperty cssProp && cssProp.IsImportant ? "important" : string.Empty);
                }
                break;

            case "initial":
                // For 'all: initial', everything gets initial values
                // Just keep the 'all' property since initial values are browser defaults
                break;

            case "unset":
                // For 'all: unset', inheritable properties inherit, non-inheritable go to initial
                foreach (var prop in parentStyle)
                {
                    if (prop.Name == "all") continue;
                    
                    // Only inherit naturally inheritable properties or CSS variables
                    if (IsCssVariable(prop) || IsInheritable(prop))
                    {
                        result.SetProperty(
                            prop.Name,
                            prop.Value,
                            prop is ICssProperty cssProp && cssProp.IsImportant ? "important" : string.Empty);
                    }
                }
                break;
        }

        return result;
    }

    private bool IsInheritable(ICssProperty property)
    {
        // Check if property implements ICssProperty and has CanBeInherited flag
        if (property is ICssProperty cssProp)
        {
            return cssProp.CanBeInherited;
        }
        
        // Fallback for non-ICssProperty implementations
        return InheritableProperties.Contains(property.Name);
    }

    private bool IsCssVariable(ICssProperty property) => property.Name.StartsWith("--");

    /// <summary>
    /// Gets properties from parent for properties explicitly set to 'inherit' in element style.
    /// </summary>
    private List<ICssProperty> GetPropertiesWithExplicitInherit(ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        var result = new List<ICssProperty>();

        foreach (var prop in elementStyle)
        {
            if (prop.Value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
            {
                var parentProp = parentStyle.GetProperty(prop.Name);
                if (parentProp != null && !string.IsNullOrEmpty(parentProp.Value))
                {
                    result.Add(parentProp);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets properties from parent that should be inherited (inheritable properties not in element).
    /// </summary>
    private List<ICssProperty> GetInheritableProperties(ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        var result = new List<ICssProperty>();

        foreach (var prop in parentStyle)
        {
            // Skip if property already exists in element style
            if (!string.IsNullOrEmpty(elementStyle[prop.Name]))
                continue;

            // Add property if it's a CSS variable or inheritable
            if (IsCssVariable(prop) || IsInheritable(prop))
            {
                result.Add(prop);
            }
        }

        return result;
    }

    /// <summary>
    /// Fallback implementation for non-CssStyleDeclaration objects.
    /// </summary>
    private void HandleInheritanceFallback(ICssStyleDeclaration result, ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        // Handle explicit inherit keyword
        foreach (var prop in elementStyle)
        {
            if (prop.Value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
            {
                var value = parentStyle.GetPropertyValue(prop.Name);
                var priority = parentStyle.GetPropertyPriority(prop.Name);

                if (!string.IsNullOrEmpty(value))
                {
                    result.SetProperty(prop.Name, value, priority);
                }
            }
        }

        // Handle natural inheritance and CSS variables
        foreach (var parentProp in parentStyle)
        {
            // Skip properties already in element style
            if (elementStyle.GetProperty(parentProp.Name) != null)
                continue;

            // CSS variables always inherit
            if (IsCssVariable(parentProp))
            {
                result.SetProperty(
                    parentProp.Name,
                    parentStyle.GetPropertyValue(parentProp.Name),
                    parentStyle.GetPropertyPriority(parentProp.Name));
                continue;
            }

            // Only inherit naturally inheritable properties
            if (IsInheritable(parentProp))
            {
                result.SetProperty(
                    parentProp.Name,
                    parentStyle.GetPropertyValue(parentProp.Name),
                    parentStyle.GetPropertyPriority(parentProp.Name));
            }
        }
    }

    /// <summary>
    /// Creates a clone of a style declaration.
    /// </summary>
    private ICssStyleDeclaration CloneStyleDeclaration(ICssStyleDeclaration style)
    {
        var clone = new CssStyleDeclaration(_context);

        // Clone all properties
        if (clone is CssStyleDeclaration cssClone)
        {
            // More efficient to use SetDeclarations
            cssClone.SetDeclarations(style.ToList());
        }
        else
        {
            // Fallback
            foreach (var property in style)
            {
                clone.SetProperty(
                    property.Name,
                    style.GetPropertyValue(property.Name),
                    style.GetPropertyPriority(property.Name));
            }
        }

        return clone;
    }
}
```