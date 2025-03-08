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
public class InheritanceProcessor
{
    private readonly IBrowsingContext _context;

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

        // Convert IComputedStyle to ICssStyleDeclaration (if available)
        var parentStyle = parentComputedStyle.Declaration;

        // Check for direct 'all' property usage first
        var allValue = elementStyle.GetPropertyValue("all");
        if (!string.IsNullOrEmpty(allValue))
        {
            return HandleAllProperty(allValue, elementStyle, parentStyle);
        }

        // Clone the element's style to hold the result
        var result = CloneStyleDeclaration(elementStyle);

        // Let's handle the parent style inheritance
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
    /// Extracts a style declaration from a ComputedStyle, if possible.
    /// </summary>
    private ICssStyleDeclaration? GetStyleDeclarationFromComputedStyle(IComputedStyle computedStyle)
    {
        // In a real implementation, we would have a way to access the style declaration
        // from a ComputedStyle. For now, we'll create a new style declaration and
        // populate it with the computed values.

        var declaration = new CssStyleDeclaration(_context);

        // Copy inheritable properties
        foreach (var propertyName in GetInheritablePropertyNames())
        {
            var value = computedStyle.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(propertyName, value);
            }
        }

        // Copy CSS custom properties (variables)
        // This would need to be expanded in a real implementation to get all variables

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
        var isCssResult = result is CssStyleDeclaration cssResult;

        switch (allValue.ToLowerInvariant())
        {
            case "inherit":
                var parentProperties = GetParentPropertiesExceptAll(parentStyle);
                if (isCssResult)
                    ((CssStyleDeclaration)result).SetDeclarations(parentProperties);
                else
                    CopyProperties(parentProperties, result);
                break;

            case "initial":
                // Just leave with only the 'all' property
                break;

            case "unset":
                var inheritableProps = parentStyle.Where(p =>
                    p.Name != "all" && (IsCssVariable(p) || (p is ICssProperty cssP && cssP.CanBeInherited))).ToList();

                if (isCssResult)
                    ((CssStyleDeclaration)result).UpdateDeclarations(inheritableProps);
                else
                    CopyProperties(inheritableProps, result);
                break;
        }

        return result;
    }

    private void CopyProperties(IEnumerable<ICssProperty> properties, ICssStyleDeclaration target)
    {
        foreach (var prop in properties)
        {
            target.SetProperty(
                prop.Name,
                prop.Value,
                prop is ICssProperty cssProp && cssProp.IsImportant ? "important" : string.Empty);
        }
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
            if (prop.Name.StartsWith("--") || (prop is ICssProperty cssProp && cssProp.CanBeInherited))
            {
                result.Add(prop);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all properties from parent except 'all'.
    /// </summary>
    private List<ICssProperty> GetParentPropertiesExceptAll(ICssStyleDeclaration parentStyle)
    {
        return parentStyle.Where(p => p.Name != "all").ToList();
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
            if (parentProp.Name.StartsWith("--"))
            {
                result.SetProperty(
                    parentProp.Name,
                    parentStyle.GetPropertyValue(parentProp.Name),
                    parentStyle.GetPropertyPriority(parentProp.Name));
                continue;
            }

            // Only inherit naturally inheritable properties
            if (parentProp is ICssProperty cssProp && cssProp.CanBeInherited)
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
        // Create a new style declaration
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

    /// <summary>
    /// Gets names of CSS properties that are inheritable.
    /// </summary>
    private IEnumerable<string> GetInheritablePropertyNames()
    {
        return new[]
        {
            // Text properties
            "color", "direction", "font-family", "font-size", "font-style", "font-variant",
            "font-weight", "font-size-adjust", "font-stretch", "font", "letter-spacing",
            "line-height", "text-align", "text-indent", "text-transform", "white-space",
            "word-spacing", "text-shadow", "text-emphasis", "text-emphasis-color",
            "text-emphasis-style", "text-emphasis-position",

            // List properties
            "list-style-image", "list-style-position", "list-style-type", "list-style",

            // Table properties
            "border-collapse", "border-spacing", "caption-side", "empty-cells",

            // Other properties
            "cursor", "visibility", "quotes", "orphans", "widows"
        };
    }
}