namespace LayoutEngine.StyleSystem;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Contracts.StyleSystem;

/// <summary>
/// Mock implementation of IComputedStyle that returns predefined style data.
/// </summary>
public class ComputedStyle : IComputedStyle
{
    private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IElement Element { get; }

    public ComputedStyle(IElement element)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));

        // Set default style values for all elements
        _properties["color"] = "rgba(0, 0, 0, 1)";
        _properties["background-color"] = "rgba(255, 255, 255, 1)";
        _properties["font-family"] = "Arial, sans-serif";
        _properties["font-size"] = "16px";
        _properties["display"] = "block";
        _properties["margin"] = "0px";
        _properties["padding"] = "0px";
        _properties["width"] = "auto";
        _properties["height"] = "auto";

        // Add tag-specific styles
        var tagName = element.TagName?.ToLowerInvariant() ?? "div";

        switch (tagName)
        {
            case "h1":
                _properties["font-weight"] = "bold";
                _properties["margin-bottom"] = "16px";
                _properties["font-size"] = "32px";
                break;

            case "h2":
                _properties["font-weight"] = "bold";
                _properties["margin-bottom"] = "16px";
                _properties["font-size"] = "24px";
                break;

            case "p":
                _properties["margin-bottom"] = "16px";
                _properties["line-height"] = "1.5";
                break;

            case "a":
                _properties["color"] = "rgba(0, 0, 255, 1)";
                _properties["text-decoration"] = "underline";
                _properties["cursor"] = "pointer";
                break;

            case "span":
                _properties["display"] = "inline";
                break;

            case "body":
                _properties["margin"] = "8px";
                _properties["font-family"] = "Arial, sans-serif";
                break;

            case "div":
                _properties["margin-bottom"] = "8px";
                break;
        }

        // Apply class-specific styles if element has classes
        if (element.ClassList.Contains("container"))
        {
            _properties["width"] = "800px";
            _properties["margin"] = "0 auto";
        }

        // Apply inline styles if present
        var inlineStyle = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(inlineStyle))
        {
            ApplyInlineStyles(inlineStyle);
        }
    }

    public string GetValue(string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : string.Empty;
    }

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public bool HasProperty(string propertyName)
    {
        return _properties.ContainsKey(propertyName);
    }

    private void ApplyInlineStyles(string inlineStyle)
    {
        // Simple inline style parser
        var declarations = inlineStyle.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var property = parts[0].Trim();
                var value = parts[1].Trim();

                if (!string.IsNullOrEmpty(property))
                {
                    _properties[property] = value;
                }
            }
        }
    }
}