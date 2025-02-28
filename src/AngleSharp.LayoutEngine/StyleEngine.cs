#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using Css.Dom;

/// <summary>
/// Processes CSS style rules and computes final styles for elements in the layout tree.
/// Handles cascading, inheritance, value computation, and conversion to layout properties.
/// </summary>
public class StyleEngine
{
    private readonly ICssStyleSheet[] _styleSheets;
    private readonly Dictionary<IRenderNode, ICssStyleDeclaration> _computedStyles = new();

    /// <summary>
    /// Creates a new style engine with the specified style sheets.
    /// </summary>
    /// <param name="styleSheets">The style sheets to use for style computation.</param>
    public StyleEngine(ICssStyleSheet[] styleSheets)
    {
        _styleSheets = styleSheets ?? Array.Empty<ICssStyleSheet>();
    }

    /// <summary>
    /// Computes styles for all nodes in the layout tree.
    /// </summary>
    /// <param name="layoutTree">The layout tree to compute styles for.</param>
    public void ComputeStyles(LayoutTree layoutTree)
    {
        // Clear any existing computed styles
        _computedStyles.Clear();

        // Process the tree from top to bottom for inheritance
        ComputeStylesRecursive(layoutTree.Root, null);

        // Apply computed styles to layout properties
        ApplyLayoutProperties(layoutTree);
    }

    /// <summary>
    /// Updates styles for the specified nodes and their descendants.
    /// Used for incremental updates when the DOM changes.
    /// </summary>
    /// <param name="dirtyNodes">The nodes whose styles need to be recomputed.</param>
    public void UpdateStyles(IEnumerable<LayoutNode> dirtyNodes)
    {
        foreach (var node in dirtyNodes)
        {
            // Remove existing computed styles for this node and its descendants
            RemoveComputedStylesRecursive(node);

            // Recompute styles for this node and its descendants
            ComputeStylesRecursive(node, GetParentComputedStyle(node));

            // Update layout properties for this node
            ApplyLayoutPropertiesToNode(node);
        }
    }

    /// <summary>
    /// Gets the computed style for a DOM node.
    /// </summary>
    /// <param name="node">The node to get computed style for.</param>
    /// <returns>The computed style, or null if not available.</returns>
    public ICssStyleDeclaration GetComputedStyle(IRenderNode node)
    {
        if (_computedStyles.TryGetValue(node, out var style))
        {
            return style;
        }
        return null;
    }

    /// <summary>
    /// Recursively computes styles for a node and its descendants.
    /// </summary>
    private void ComputeStylesRecursive(LayoutNode node, ICssStyleDeclaration parentStyle)
    {
        var domNode = node.DomNode;

        // Skip non-element nodes for style computation
        if (domNode is ElementNode element)
        {
            // Compute style for this element
            var computedStyle = ComputeStyleForElement(element, parentStyle);

            // Store computed style
            _computedStyles[domNode] = computedStyle;

            // Update element's computed style reference
            element.ComputedStyle = computedStyle;

            // Process children with this computed style as parent
            foreach (var child in node.Children)
            {
                ComputeStylesRecursive(child, computedStyle);
            }
        }
        else if (domNode is TextNode)
        {
            // Text nodes inherit parent style but don't have their own style
            _computedStyles[domNode] = parentStyle;

            // Process any children (though text nodes typically don't have children)
            foreach (var child in node.Children)
            {
                ComputeStylesRecursive(child, parentStyle);
            }
        }
    }

    /// <summary>
    /// Recursively removes computed styles for a node and its descendants.
    /// </summary>
    private void RemoveComputedStylesRecursive(LayoutNode node)
    {
        var domNode = node.DomNode;

        // Remove computed style for this node
        _computedStyles.Remove(domNode);

        // If it's an element, clear its computed style reference
        if (domNode is ElementNode element)
        {
            element.ComputedStyle = null;
        }

        // Remove for all descendants
        foreach (var child in node.Children)
        {
            RemoveComputedStylesRecursive(child);
        }
    }

    /// <summary>
    /// Gets the parent's computed style for a node.
    /// </summary>
    private ICssStyleDeclaration GetParentComputedStyle(LayoutNode node)
    {
        if (node.Parent == null || node.Parent.DomNode == null)
            return null;

        return _computedStyles.TryGetValue(node.Parent.DomNode, out var parentStyle) ? parentStyle : null;
    }

    /// <summary>
    /// Computes the style for an element based on the cascade, inheritance, and specificity.
    /// </summary>
    private ICssStyleDeclaration ComputeStyleForElement(ElementNode element, ICssStyleDeclaration parentStyle)
    {
        // Start with the element's specified style (inline styles)
        var computedStyle = element.SpecifiedStyle?.Clone() as ICssStyleDeclaration;

        if (computedStyle == null)
        {
            // Create a new style declaration if none exists
            computedStyle = new CssStyleDeclaration();
        }

        // Merge styles from stylesheets according to specificity
        ApplyMatchingRules(element, computedStyle);

        // Apply inheritance from parent
        InheritStyles(computedStyle, parentStyle);

        // Compute relative values and resolve special values
        ComputeValues(computedStyle, parentStyle);

        return computedStyle;
    }

    /// <summary>
    /// Applies matching style rules from all stylesheets to the element's computed style.
    /// </summary>
    private void ApplyMatchingRules(ElementNode element, ICssStyleDeclaration computedStyle)
    {
        // Collect matching rules from all stylesheets
        var matchingRules = new List<(ICssRule Rule, int Specificity)>();

        foreach (var styleSheet in _styleSheets)
        {
            foreach (var rule in styleSheet.Rules.OfType<ICssStyleRule>())
            {
                // Check if selector matches element
                if (SelectorMatches(rule.Selector, element))
                {
                    int specificity = CalculateSpecificity(rule.Selector);
                    matchingRules.Add((rule, specificity));
                }
            }
        }

        // Sort rules by specificity
        matchingRules.Sort((a, b) => a.Specificity.CompareTo(b.Specificity));

        // Apply rules in order of increasing specificity
        foreach (var (rule, _) in matchingRules)
        {
            var styleRule = rule as ICssStyleRule;
            if (styleRule?.Style != null)
            {
                foreach (var property in styleRule.Style)
                {
                    computedStyle.SetProperty(property.Name, property.Value, property.Priority);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a selector matches an element.
    /// </summary>
    /// <remarks>
    /// This is a simplified implementation. A real-world implementation would handle
    /// complex selectors, pseudo-classes, etc.
    /// </remarks>
    private bool SelectorMatches(ICssSelector selector, ElementNode element)
    {
        // In a real implementation, this would use a proper selector matching algorithm
        // This is a simplified version for demonstration
        var selectorText = selector.Text;

        // Element type selector
        if (selectorText == element.Ref.NodeName.ToLowerInvariant())
            return true;

        // Class selector
        if (selectorText.StartsWith(".") && element.Ref.ClassList.Contains(selectorText.Substring(1)))
            return true;

        // ID selector
        if (selectorText.StartsWith("#") && element.Ref.Id == selectorText.Substring(1))
            return true;

        // Universal selector
        if (selectorText == "*")
            return true;

        return false;
    }

    /// <summary>
    /// Calculates the specificity of a selector.
    /// </summary>
    /// <remarks>
    /// This is a simplified implementation. A real-world implementation would handle
    /// complex selectors and calculate specificity according to the CSS specification.
    /// </remarks>
    private int CalculateSpecificity(ICssSelector selector)
    {
        // In a real implementation, this would calculate specificity according to CSS rules
        // For now, we'll use a simplified algorithm
        var selectorText = selector.Text;

        // ID selectors (a=100)
        int a = selectorText.Count(c => c == '#') * 100;

        // Class selectors, attribute selectors, pseudo-classes (b=10)
        int b = selectorText.Count(c => c == '.') * 10;
        b += selectorText.Count(c => c == '[') * 10;
        b += selectorText.Count(c => c == ':') * 10;

        // Element selectors, pseudo-elements (c=1)
        int c = 0;

        if (!selectorText.StartsWith(".") && !selectorText.StartsWith("#") && selectorText != "*")
        {
            c += 1; // Element selector
        }

        c += selectorText.Count(ch => ch == '::') * 1;

        return a + b + c;
    }

    /// <summary>
    /// Applies inherited properties from parent element.
    /// </summary>
    private void InheritStyles(ICssStyleDeclaration computedStyle, ICssStyleDeclaration parentStyle)
    {
        if (parentStyle == null)
            return;

        // List of inheritable properties
        string[] inheritableProperties = {
            "color", "font-family", "font-size", "font-weight", "font-style",
            "line-height", "text-align", "text-indent", "text-transform",
            "letter-spacing", "word-spacing", "white-space", "direction",
            "visibility"
        };

        foreach (var property in inheritableProperties)
        {
            // Only inherit if not explicitly set
            if (string.IsNullOrEmpty(computedStyle.GetPropertyValue(property)))
            {
                string parentValue = parentStyle.GetPropertyValue(property);
                if (!string.IsNullOrEmpty(parentValue))
                {
                    computedStyle.SetProperty(property, parentValue);
                }
            }
        }
    }

    /// <summary>
    /// Computes absolute values from relative values and resolves special values.
    /// </summary>
    private void ComputeValues(ICssStyleDeclaration computedStyle, ICssStyleDeclaration parentStyle)
    {
        // Compute font-size first since other relative units may depend on it
        ComputeFontSize(computedStyle, parentStyle);

        // Compute other values
        ComputeLengthValues(computedStyle, parentStyle);
    }

    /// <summary>
    /// Computes absolute font size from relative values.
    /// </summary>
    private void ComputeFontSize(ICssStyleDeclaration computedStyle, ICssStyleDeclaration parentStyle)
    {
        string fontSize = computedStyle.GetPropertyValue("font-size");

        if (string.IsNullOrEmpty(fontSize))
            return;

        // Convert named sizes to pixel values
        if (fontSize == "xx-small") computedStyle.SetProperty("font-size", "9px");
        else if (fontSize == "x-small") computedStyle.SetProperty("font-size", "10px");
        else if (fontSize == "small") computedStyle.SetProperty("font-size", "13px");
        else if (fontSize == "medium") computedStyle.SetProperty("font-size", "16px");
        else if (fontSize == "large") computedStyle.SetProperty("font-size", "18px");
        else if (fontSize == "x-large") computedStyle.SetProperty("font-size", "24px");
        else if (fontSize == "xx-large") computedStyle.SetProperty("font-size", "32px");

        // Handle relative sizes
        else if (fontSize == "larger" && parentStyle != null)
        {
            var parentSize = ParsePixelValue(parentStyle.GetPropertyValue("font-size"), 16);
            computedStyle.SetProperty("font-size", $"{parentSize * 1.2}px");
        }
        else if (fontSize == "smaller" && parentStyle != null)
        {
            var parentSize = ParsePixelValue(parentStyle.GetPropertyValue("font-size"), 16);
            computedStyle.SetProperty("font-size", $"{parentSize * 0.8}px");
        }

        // Handle percentage
        else if (fontSize.EndsWith("%") && parentStyle != null)
        {
            if (float.TryParse(fontSize.TrimEnd('%'), out float percentage))
            {
                var parentSize = ParsePixelValue(parentStyle.GetPropertyValue("font-size"), 16);
                computedStyle.SetProperty("font-size", $"{parentSize * percentage / 100}px");
            }
        }

        // Handle em units
        else if (fontSize.EndsWith("em") && parentStyle != null)
        {
            if (float.TryParse(fontSize.TrimEnd('e', 'm'), out float emValue))
            {
                var parentSize = ParsePixelValue(parentStyle.GetPropertyValue("font-size"), 16);
                computedStyle.SetProperty("font-size", $"{parentSize * emValue}px");
            }
        }

        // Handle rem units (using root em)
        else if (fontSize.EndsWith("rem"))
        {
            // For simplicity, assume root font size is 16px
            if (float.TryParse(fontSize.TrimEnd('r', 'e', 'm'), out float remValue))
            {
                computedStyle.SetProperty("font-size", $"{16 * remValue}px");
            }
        }
    }

    /// <summary>
    /// Computes absolute length values from relative values.
    /// </summary>
    private void ComputeLengthValues(ICssStyleDeclaration computedStyle, ICssStyleDeclaration parentStyle)
    {
        // List of properties that can have length values
        string[] lengthProperties = {
            "width", "height", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding-top", "padding-right", "padding-bottom", "padding-left",
            "border-top-width", "border-right-width", "border-bottom-width", "border-left-width"
        };

        float fontSize = ParsePixelValue(computedStyle.GetPropertyValue("font-size"), 16);

        foreach (var property in lengthProperties)
        {
            string value = computedStyle.GetPropertyValue(property);
            if (string.IsNullOrEmpty(value) || value == "auto" || value == "none")
                continue;

            // Convert em units to pixels
            if (value.EndsWith("em"))
            {
                if (float.TryParse(value.TrimEnd('e', 'm'), out float emValue))
                {
                    computedStyle.SetProperty(property, $"{fontSize * emValue}px");
                }
            }

            // Convert rem units to pixels
            else if (value.EndsWith("rem"))
            {
                if (float.TryParse(value.TrimEnd('r', 'e', 'm'), out float remValue))
                {
                    // Assume root font size is 16px
                    computedStyle.SetProperty(property, $"{16 * remValue}px");
                }
            }

            // Convert percentage to pixels for widths
            else if (value.EndsWith("%") && property == "width" && parentStyle != null)
            {
                if (float.TryParse(value.TrimEnd('%'), out float percentage))
                {
                    var parentWidth = ParsePixelValue(parentStyle.GetPropertyValue("width"), 0);
                    if (parentWidth > 0)
                    {
                        computedStyle.SetProperty(property, $"{parentWidth * percentage / 100}px");
                    }
                }
            }

            // Convert percentage to pixels for heights
            else if (value.EndsWith("%") && property == "height" && parentStyle != null)
            {
                if (float.TryParse(value.TrimEnd('%'), out float percentage))
                {
                    var parentHeight = ParsePixelValue(parentStyle.GetPropertyValue("height"), 0);
                    if (parentHeight > 0)
                    {
                        computedStyle.SetProperty(property, $"{parentHeight * percentage / 100}px");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies computed styles to layout properties for all nodes.
    /// </summary>
    private void ApplyLayoutProperties(LayoutTree layoutTree)
    {
        foreach (var node in layoutTree.GetAllNodes())
        {
            ApplyLayoutPropertiesToNode(node);
        }
    }

    /// <summary>
    /// Applies computed styles to layout properties for a specific node.
    /// </summary>
    private void ApplyLayoutPropertiesToNode(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        var style = element.ComputedStyle;

        // Extract display type
        string display = style.GetPropertyValue("display") ?? "inline";
        node.Display = ParseDisplayType(display);

        // Extract position type
        string position = style.GetPropertyValue("position") ?? "static";
        node.Position = ParsePositionType(position);

        // Extract float
        string float_ = style.GetPropertyValue("float") ?? "none";
        node.Float = ParseFloatType(float_);

        // Extract dimensions
        string width = style.GetPropertyValue("width") ?? "auto";
        node.Width = ParseStyleValue(width);

        string height = style.GetPropertyValue("height") ?? "auto";
        node.Height = ParseStyleValue(height);
    }

    /// <summary>
    /// Parses a display value into a DisplayType enum.
    /// </summary>
    private DisplayType ParseDisplayType(string display)
    {
        return display.ToLowerInvariant() switch
        {
            "none" => DisplayType.None,
            "block" => DisplayType.Block,
            "inline" => DisplayType.Inline,
            "inline-block" => DisplayType.InlineBlock,
            "flex" => DisplayType.Flex,
            "inline-flex" => DisplayType.Flex, // Inline-flex still creates a flex formatting context
            "grid" => DisplayType.Grid,
            "inline-grid" => DisplayType.Grid, // Inline-grid still creates a grid formatting context
            "table" => DisplayType.Table,
            "table-cell" => DisplayType.TableCell,
            "table-row" => DisplayType.TableRow,
            _ => DisplayType.Inline // Default
        };
    }

    /// <summary>
    /// Parses a position value into a PositionType enum.
    /// </summary>
    private PositionType ParsePositionType(string position)
    {
        return position.ToLowerInvariant() switch
        {
            "static" => PositionType.Static,
            "relative" => PositionType.Relative,
            "absolute" => PositionType.Absolute,
            "fixed" => PositionType.Fixed,
            "sticky" => PositionType.Sticky,
            _ => PositionType.Static // Default
        };
    }

    /// <summary>
    /// Parses a float value into a FloatType enum.
    /// </summary>
    private FloatType ParseFloatType(string float_)
    {
        return float_.ToLowerInvariant() switch
        {
            "left" => FloatType.Left,
            "right" => FloatType.Right,
            "none" => FloatType.None,
            _ => FloatType.None // Default
        };
    }

    /// <summary>
    /// Parses a style value into a StyleValue object.
    /// </summary>
    private StyleValue ParseStyleValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "auto")
            return StyleValue.Auto;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return StyleValue.FromPixels(pixels);

        if (value.EndsWith("%") && float.TryParse(value.TrimEnd('%'), out float percentage))
            return StyleValue.FromPercentage(percentage);

        return StyleValue.Auto;
    }

    /// <summary>
    /// Parses a pixel value from a CSS length.
    /// </summary>
    private float ParsePixelValue(string value, float defaultValue)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return pixels;

        return defaultValue;
    }
}

/// <summary>
/// A simple CSS style declaration implementation.
/// In a real project, you would use the AngleSharp implementation.
/// </summary>
public class CssStyleDeclaration : ICssStyleDeclaration
{
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _priorities = new(StringComparer.OrdinalIgnoreCase);

    public void SetProperty(string name, string value, string priority = "")
    {
        _properties[name] = value;
        _priorities[name] = priority;
    }

    public string GetPropertyValue(string name)
    {
        return _properties.TryGetValue(name, out var value) ? value : null;
    }

    public string GetPropertyPriority(string name)
    {
        return _priorities.TryGetValue(name, out var priority) ? priority : "";
    }

    public void RemoveProperty(string name)
    {
        _properties.Remove(name);
        _priorities.Remove(name);
    }

    public ICssProperty GetProperty(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return new CssProperty { Name = name, Value = value, Priority = _priorities[name] };
        }
        return null;
    }

    public IEnumerable<ICssProperty> GetAllProperties()
    {
        return _properties.Select(p => new CssProperty
        {
            Name = p.Key,
            Value = p.Value,
            Priority = _priorities[p.Key]
        });
    }

    public IEnumerator<ICssProperty> GetEnumerator()
    {
        return GetAllProperties().GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => _properties.Count;

    public ICssProperty this[int index] => GetAllProperties().ElementAtOrDefault(index);

    public ICssStyleDeclaration Clone()
    {
        var clone = new CssStyleDeclaration();
        foreach (var property in _properties)
        {
            clone.SetProperty(property.Key, property.Value, _priorities[property.Key]);
        }
        return clone;
    }
}

/// <summary>
/// A simple CSS property implementation.
/// In a real project, you would use the AngleSharp implementation.
/// </summary>
public class CssProperty : ICssProperty
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Priority { get; set; }
    public object RawValue => Value;
}