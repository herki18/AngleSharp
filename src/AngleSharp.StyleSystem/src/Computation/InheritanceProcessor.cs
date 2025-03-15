namespace AngleSharp.StyleSystem.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.StyleSystem.Interfaces;

/// <summary>
/// Implements the CSS inheritance processing model.
/// </summary>
public class InheritanceProcessor : IInheritanceProcessor
{
    private readonly ICssStyleDeclarationFactory _cssStyleDeclarationFactory;

    /// <summary>
    /// Creates a new inheritance processor.
    /// </summary>
    public InheritanceProcessor(ICssStyleDeclarationFactory cssStyleDeclarationFactory)
    {
        _cssStyleDeclarationFactory = cssStyleDeclarationFactory;
    }

    /// <summary>
    /// Applies inheritance to the element's style based on the parent's computed style.
    /// </summary>
    /// <param name="elementStyle">The element's own style declaration.</param>
    /// <param name="parentComputedStyle">The parent element's computed style, if available.</param>
    /// <returns>A new style declaration with inherited properties applied.</returns>
    public ICssStyleDeclaration ApplyInheritance(ICssStyleDeclaration elementStyle, IComputedStyle? parentComputedStyle)
    {
        if (elementStyle == null)
            throw new ArgumentNullException(nameof(elementStyle));

        if (parentComputedStyle == null)
            return CloneStyleDeclaration(elementStyle);

        var parentStyle = parentComputedStyle.Declaration;

        var allValue = elementStyle.GetPropertyValue("all");
        if (!string.IsNullOrEmpty(allValue))
        {
            return HandleAllProperty(allValue, elementStyle, parentStyle);
        }

        var result = CloneStyleDeclaration(elementStyle);

        if (result is CssStyleDeclaration cssResult)
        {
            var inheritPropertiesFromParent = GetPropertiesWithExplicitInherit(elementStyle, parentStyle);
            if (inheritPropertiesFromParent.Any())
            {
                cssResult.SetDeclarations(inheritPropertiesFromParent);
            }

            var inheritableProperties = GetInheritableProperties(elementStyle, parentStyle);
            if (inheritableProperties.Any())
            {
                cssResult.UpdateDeclarations(inheritableProperties);
            }
        }
        else
        {
            HandleInheritanceFallback(result, elementStyle, parentStyle);
        }

        return result;
    }

    private ICssStyleDeclaration? GetStyleDeclarationFromComputedStyle(IComputedStyle computedStyle)
    {
        var declaration = _cssStyleDeclarationFactory.Create();

        foreach (var propertyName in GetInheritablePropertyNames())
        {
            var value = computedStyle.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(propertyName, value);
            }
        }

        return declaration;
    }

    private ICssStyleDeclaration HandleAllProperty(string allValue, ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        var result = _cssStyleDeclarationFactory.Create();
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

    private List<ICssProperty> GetInheritableProperties(ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
        var result = new List<ICssProperty>();

        foreach (var prop in parentStyle)
        {
            if (!string.IsNullOrEmpty(elementStyle[prop.Name]))
                continue;

            if (prop.Name.StartsWith("--") || (prop is ICssProperty cssProp && cssProp.CanBeInherited))
            {
                result.Add(prop);
            }
        }

        return result;
    }

    private List<ICssProperty> GetParentPropertiesExceptAll(ICssStyleDeclaration parentStyle)
    {
        return parentStyle.Where(p => p.Name != "all").ToList();
    }

    private void HandleInheritanceFallback(ICssStyleDeclaration result, ICssStyleDeclaration elementStyle, ICssStyleDeclaration parentStyle)
    {
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

        foreach (var parentProp in parentStyle)
        {
            if (elementStyle.GetProperty(parentProp.Name) != null)
                continue;

            if (parentProp.Name.StartsWith("--"))
            {
                result.SetProperty(
                    parentProp.Name,
                    parentStyle.GetPropertyValue(parentProp.Name),
                    parentStyle.GetPropertyPriority(parentProp.Name));
                continue;
            }

            if (parentProp is ICssProperty cssProp && cssProp.CanBeInherited)
            {
                result.SetProperty(
                    parentProp.Name,
                    parentStyle.GetPropertyValue(parentProp.Name),
                    parentStyle.GetPropertyPriority(parentProp.Name));
            }
        }
    }

    private ICssStyleDeclaration CloneStyleDeclaration(ICssStyleDeclaration style)
    {
        var clone = _cssStyleDeclarationFactory.Create();

        if (clone is CssStyleDeclaration cssClone)
        {
            cssClone.SetDeclarations(style.ToList());
        }
        else
        {
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

    private IEnumerable<string> GetInheritablePropertyNames()
    {
        return new[]
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
    }
}