namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;

public class ComputedStyle : IComputedStyle
{
    private readonly Dictionary<string, string> _properties = new();

    public ComputedStyle(IElement element, IComputedStyle? parentStyle)
    {
        Element = element;

        // Apply default styles
        ApplyDefaultStyles();

        // Apply parent styles (inheritance)
        ApplyInheritedStyles(parentStyle);

        // Apply styles from stylesheets
        ApplyStylesheetStyles();

        // Apply inline styles (highest priority)
        ApplyInlineStyles();
    }

    public IElement Element { get; }

    public string GetValue(string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : string.Empty;
    }

    public bool HasValue(string propertyName)
    {
        return _properties.ContainsKey(propertyName);
    }

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public void SetProperty(string name, string value)
    {
        _properties[name] = value;
    }

    // Computed properties
    public DisplayType Display
    {
        get
        {
            var display = GetValue("display");
            return display switch
            {
                "block" => DisplayType.Block,
                "flex" => DisplayType.Flex,
                "grid" => DisplayType.Grid,
                "inline" => DisplayType.Inline,
                "inline-block" => DisplayType.InlineBlock,
                "none" => DisplayType.None,
                _ => DisplayType.Block // Default
            };
        }
    }

    public PositionType Position
    {
        get
        {
            var position = GetValue("position");
            return position switch
            {
                "relative" => PositionType.Relative,
                "absolute" => PositionType.Absolute,
                "fixed" => PositionType.Fixed,
                "sticky" => PositionType.Sticky,
                _ => PositionType.Static // Default
            };
        }
    }

    // Style calculation methods
    private void ApplyDefaultStyles()
    {
        // Apply user agent (browser) default styles
        // This would depend on the element type
        if (Element.TagName == "DIV")
        {
            _properties["display"] = "block";
        }
        else if (Element.TagName == "SPAN")
        {
            _properties["display"] = "inline";
        }
        // And so on for other elements...
    }

    private void ApplyInheritedStyles(IComputedStyle? parentStyle)
    {
        if (parentStyle == null) return;

        // Apply inherited properties from parent
        // Examples of inherited properties: color, font-family, etc.
        foreach (var prop in InheritedProperties)
        {
            if (parentStyle.HasValue(prop))
            {
                _properties[prop] = parentStyle.GetValue(prop);
            }
        }
    }

    private void ApplyStylesheetStyles()
    {
        // This would involve:
        // 1. Finding all matching rules for this element
        // 2. Sorting them by specificity
        // 3. Applying them in order of increasing specificity

        // For future implementation
    }

    private void ApplyInlineStyles()
    {
        var style = Element.GetAttribute("style");
        if (string.IsNullOrEmpty(style)) return;

        // Parse inline styles and apply them
        // This has highest specificity
        var declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', 2);
            if (parts.Length == 2)
            {
                var prop = parts[0].Trim();
                var value = parts[1].Trim();
                _properties[prop] = value;
            }
        }
    }

    // List of CSS properties that are inherited
    private static readonly HashSet<string> InheritedProperties = new()
    {
        "color",
        "font-family",
        "font-size",
        "font-weight",
        "line-height",
        "text-align",
        // More inherited properties...
    };
}


public enum DisplayType
{
    None,
    Block,
    Inline,
    InlineBlock,
    Flex,
    Grid
    // Other display types...
}

public enum PositionType
{
    Static,
    Relative,
    Absolute,
    Fixed,
    Sticky
}
