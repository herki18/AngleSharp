namespace AngleSharp.StyleSystem.Computation;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using Interfaces;
using Models;
public class StylePropertyMapper : IStylePropertyMapper
{
    private static readonly Dictionary<string, LogicalPropertyMapping> _logicalPropertyMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["margin-block"] = new LogicalPropertyMapping(
            new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" },
            LogicalPropertyType.Block,
            true),
        ["margin-block-start"] = new LogicalPropertyMapping(
            new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" },
            LogicalPropertyType.BlockStart),
        ["margin-block-end"] = new LogicalPropertyMapping(
            new[] { "margin-bottom", "margin-left", "margin-top", "margin-right" },
            LogicalPropertyType.BlockEnd),
        ["margin-inline"] = new LogicalPropertyMapping(
            new[] { "margin-left", "margin-right", "margin-left", "margin-right" },
            LogicalPropertyType.Inline,
            true),
        ["margin-inline-start"] = new LogicalPropertyMapping(
            new[] { "margin-left", "margin-top", "margin-right", "margin-bottom" },
            LogicalPropertyType.InlineStart),
        ["margin-inline-end"] = new LogicalPropertyMapping(
            new[] { "margin-right", "margin-bottom", "margin-left", "margin-top" },
            LogicalPropertyType.InlineEnd),
        ["padding-block"] = new LogicalPropertyMapping(
            new[] { "padding-top", "padding-right", "padding-bottom", "padding-left" },
            LogicalPropertyType.Block,
            true),
        ["padding-block-start"] = new LogicalPropertyMapping(
            new[] { "padding-top", "padding-right", "padding-bottom", "padding-left" },
            LogicalPropertyType.BlockStart),
        ["padding-block-end"] = new LogicalPropertyMapping(
            new[] { "padding-bottom", "padding-left", "padding-top", "padding-right" },
            LogicalPropertyType.BlockEnd),
        ["padding-inline"] = new LogicalPropertyMapping(
            new[] { "padding-left", "padding-right", "padding-left", "padding-right" },
            LogicalPropertyType.Inline,
            true),
        ["padding-inline-start"] = new LogicalPropertyMapping(
            new[] { "padding-left", "padding-top", "padding-right", "padding-bottom" },
            LogicalPropertyType.InlineStart),
        ["padding-inline-end"] = new LogicalPropertyMapping(
            new[] { "padding-right", "padding-bottom", "padding-left", "padding-top" },
            LogicalPropertyType.InlineEnd),
        ["border-block"] = new LogicalPropertyMapping(
            new[] { "border-top", "border-right", "border-bottom", "border-left" },
            LogicalPropertyType.Block,
            true),
        ["border-block-width"] = new LogicalPropertyMapping(
            new[] { "border-top-width", "border-right-width", "border-bottom-width", "border-left-width" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-width"] = new LogicalPropertyMapping(
            new[] { "border-top-width", "border-right-width", "border-bottom-width", "border-left-width" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-width"] = new LogicalPropertyMapping(
            new[] { "border-bottom-width", "border-left-width", "border-top-width", "border-right-width" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-width"] = new LogicalPropertyMapping(
            new[] { "border-left-width", "border-right-width", "border-left-width", "border-right-width" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-width"] = new LogicalPropertyMapping(
            new[] { "border-left-width", "border-top-width", "border-right-width", "border-bottom-width" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-width"] = new LogicalPropertyMapping(
            new[] { "border-right-width", "border-bottom-width", "border-left-width", "border-top-width" },
            LogicalPropertyType.InlineEnd),
        ["border-block-color"] = new LogicalPropertyMapping(
            new[] { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-color"] = new LogicalPropertyMapping(
            new[] { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-color"] = new LogicalPropertyMapping(
            new[] { "border-bottom-color", "border-left-color", "border-top-color", "border-right-color" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-color"] = new LogicalPropertyMapping(
            new[] { "border-left-color", "border-right-color", "border-left-color", "border-right-color" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-color"] = new LogicalPropertyMapping(
            new[] { "border-left-color", "border-top-color", "border-right-color", "border-bottom-color" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-color"] = new LogicalPropertyMapping(
            new[] { "border-right-color", "border-bottom-color", "border-left-color", "border-top-color" },
            LogicalPropertyType.InlineEnd),
        ["border-block-style"] = new LogicalPropertyMapping(
            new[] { "border-top-style", "border-right-style", "border-bottom-style", "border-left-style" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-style"] = new LogicalPropertyMapping(
            new[] { "border-top-style", "border-right-style", "border-bottom-style", "border-left-style" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-style"] = new LogicalPropertyMapping(
            new[] { "border-bottom-style", "border-left-style", "border-top-style", "border-right-style" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-style"] = new LogicalPropertyMapping(
            new[] { "border-left-style", "border-right-style", "border-left-style", "border-right-style" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-style"] = new LogicalPropertyMapping(
            new[] { "border-left-style", "border-top-style", "border-right-style", "border-bottom-style" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-style"] = new LogicalPropertyMapping(
            new[] { "border-right-style", "border-bottom-style", "border-left-style", "border-top-style" },
            LogicalPropertyType.InlineEnd),
        ["inset-block"] = new LogicalPropertyMapping(
            new[] { "top", "right", "bottom", "left" },
            LogicalPropertyType.Block,
            true),
        ["inset-block-start"] = new LogicalPropertyMapping(
            new[] { "top", "right", "bottom", "left" },
            LogicalPropertyType.BlockStart),
        ["inset-block-end"] = new LogicalPropertyMapping(
            new[] { "bottom", "left", "top", "right" },
            LogicalPropertyType.BlockEnd),
        ["inset-inline"] = new LogicalPropertyMapping(
            new[] { "left", "right", "left", "right" },
            LogicalPropertyType.Inline,
            true),
        ["inset-inline-start"] = new LogicalPropertyMapping(
            new[] { "left", "top", "right", "bottom" },
            LogicalPropertyType.InlineStart),
        ["inset-inline-end"] = new LogicalPropertyMapping(
            new[] { "right", "bottom", "left", "top" },
            LogicalPropertyType.InlineEnd),
        ["inset"] = new LogicalPropertyMapping(
            new[] { "top", "right", "bottom", "left" },
            LogicalPropertyType.All,
            true),
        ["block-size"] = new LogicalPropertyMapping(
            new[] { "height", "width", "height", "width" },
            LogicalPropertyType.BlockSize),
        ["inline-size"] = new LogicalPropertyMapping(
            new[] { "width", "height", "width", "height" },
            LogicalPropertyType.InlineSize),
        ["min-block-size"] = new LogicalPropertyMapping(
            new[] { "min-height", "min-width", "min-height", "min-width" },
            LogicalPropertyType.BlockSize),
        ["min-inline-size"] = new LogicalPropertyMapping(
            new[] { "min-width", "min-height", "min-width", "min-height" },
            LogicalPropertyType.InlineSize),
        ["max-block-size"] = new LogicalPropertyMapping(
            new[] { "max-height", "max-width", "max-height", "max-width" },
            LogicalPropertyType.BlockSize),
        ["max-inline-size"] = new LogicalPropertyMapping(
            new[] { "max-width", "max-height", "max-width", "max-height" },
            LogicalPropertyType.InlineSize),
    };

    private static readonly Dictionary<string, IEnumerable<string>> _physicalToLogicalMap = BuildPhysicalToLogicalMap();

    private static Dictionary<string, IEnumerable<string>> BuildPhysicalToLogicalMap()
    {
        var map = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in _logicalPropertyMappings)
        {
            foreach (var physicalProperty in mapping.Value.PhysicalProperties)
            {
                if (!map.TryGetValue(physicalProperty, out var logicalProperties))
                {
                    map[physicalProperty] = new[] { mapping.Key };
                }
                else
                {
                    map[physicalProperty] = logicalProperties.Append(mapping.Key);
                }
            }
        }
        return map;
    }

    public IDictionary<string, ICssValue> MapLogicalToPhysical(string logicalProperty, ICssValue value, WritingMode writingMode)
    {
        if (string.IsNullOrEmpty(logicalProperty))
            throw new ArgumentException("Logical property name cannot be null or empty", nameof(logicalProperty));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (!_logicalPropertyMappings.TryGetValue(logicalProperty, out var mapping))
            return new Dictionary<string, ICssValue> { [logicalProperty] = value };

        var result = new Dictionary<string, ICssValue>();

        // Handle shorthand properties
        if (mapping.IsShorthand)
        {
            return MapLogicalShorthandToPhysical(logicalProperty, value, writingMode, mapping);
        }

        // Direct mapping for each logical property type with consistent handling for different writing modes
        string baseProperty = ExtractBaseProperty(logicalProperty);
        string suffix = ExtractPropertySuffix(logicalProperty);

        // Handle block and inline direction properties
        if (logicalProperty.Contains("-block-"))
        {
            if (logicalProperty.Contains("-block-start"))
            {
                if (writingMode.IsHorizontal)
                {
                    // In horizontal modes, block-start is top
                    result[$"{baseProperty}-top{suffix}"] = value;
                }
                else if (IsVerticalRightToLeft(writingMode))
                {
                    // In vertical-rl, block-start is right
                    result[$"{baseProperty}-right{suffix}"] = value;
                }
                else
                {
                    // In vertical-lr, block-start is left
                    result[$"{baseProperty}-left{suffix}"] = value;
                }
            }
            else if (logicalProperty.Contains("-block-end"))
            {
                if (writingMode.IsHorizontal)
                {
                    // In horizontal modes, block-end is bottom
                    result[$"{baseProperty}-bottom{suffix}"] = value;
                }
                else if (IsVerticalRightToLeft(writingMode))
                {
                    // In vertical-rl, block-end is left
                    result[$"{baseProperty}-left{suffix}"] = value;
                }
                else
                {
                    // In vertical-lr, block-end is right
                    result[$"{baseProperty}-right{suffix}"] = value;
                }
            }
            return result;
        }
        else if (logicalProperty.Contains("-inline-"))
        {
            if (logicalProperty.Contains("-inline-start"))
            {
                if (writingMode.IsHorizontal)
                {
                    if (writingMode.IsRightToLeft)
                    {
                        // In horizontal-rtl, inline-start is right
                        result[$"{baseProperty}-right{suffix}"] = value;
                    }
                    else
                    {
                        // In horizontal-ltr, inline-start is left
                        result[$"{baseProperty}-left{suffix}"] = value;
                    }
                }
                else
                {
                    // In vertical modes, inline-start is top
                    result[$"{baseProperty}-top{suffix}"] = value;
                }
            }
            else if (logicalProperty.Contains("-inline-end"))
            {
                if (writingMode.IsHorizontal)
                {
                    if (writingMode.IsRightToLeft)
                    {
                        // In horizontal-rtl, inline-end is left
                        result[$"{baseProperty}-left{suffix}"] = value;
                    }
                    else
                    {
                        // In horizontal-ltr, inline-end is right
                        result[$"{baseProperty}-right{suffix}"] = value;
                    }
                }
                else
                {
                    // In vertical modes, inline-end is bottom
                    result[$"{baseProperty}-bottom{suffix}"] = value;
                }
            }
            return result;
        }

        // Use the switch for other property types
        switch (mapping.Type)
        {
            case LogicalPropertyType.BlockSize:
                result[writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : mapping.PhysicalProperties[1]] = value;
                break;
            case LogicalPropertyType.InlineSize:
                result[writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : mapping.PhysicalProperties[1]] = value;
                break;
            default:
                result[logicalProperty] = value;
                break;
        }

        return result;
    }

    private IDictionary<string, ICssValue> MapLogicalShorthandToPhysical(
        string logicalProperty,
        ICssValue value,
        WritingMode writingMode,
        LogicalPropertyMapping mapping)
    {
        var result = new Dictionary<string, ICssValue>();

        switch (mapping.Type)
        {
            case LogicalPropertyType.Block:
                result[GetBlockStartProperty(writingMode, mapping)] = value;
                result[GetBlockEndProperty(writingMode, mapping)] = value;
                break;
            case LogicalPropertyType.Inline:
                // Handle special cases for different writing modes and property types

                // Handle inset-inline in vertical writing modes
                if (logicalProperty.StartsWith("inset-inline") && !writingMode.IsHorizontal)
                {
                    // In vertical modes, inline axis is top-bottom
                    result["top"] = value;
                    result["bottom"] = value;
                    return result;
                }

                // For horizontal writing modes, directly map to left/right properties for clarity and consistency
                if (writingMode.IsHorizontal && logicalProperty.Contains("-inline"))
                {
                    // Extract the base property type (margin, padding, border)
                    string baseProperty = ExtractBaseProperty(logicalProperty);
                    string suffix = ExtractPropertySuffix(logicalProperty);

                    // Special handling for inset properties
                    if (logicalProperty.StartsWith("inset"))
                    {
                        result["left"] = value;
                        result["right"] = value;
                        return result;
                    }

                    // Construct the appropriate physical property names
                    string leftProp = $"{baseProperty}-left{suffix}";
                    string rightProp = $"{baseProperty}-right{suffix}";

                    result[leftProp] = value;
                    result[rightProp] = value;
                }
                else
                {
                    // Standard handling for other writing modes or properties
                    result[GetInlineStartProperty(writingMode, mapping)] = value;
                    result[GetInlineEndProperty(writingMode, mapping)] = value;
                }
                break;
            case LogicalPropertyType.All:
                var blockStartProp = GetBlockStartProperty(writingMode, mapping);
                var blockEndProp = GetBlockEndProperty(writingMode, mapping);
                var inlineStartProp = GetInlineStartProperty(writingMode, mapping);
                var inlineEndProp = GetInlineEndProperty(writingMode, mapping);

                result[blockStartProp] = value;
                result[blockEndProp] = value;
                result[inlineStartProp] = value;
                result[inlineEndProp] = value;
                break;
        }

        return result;
    }

    private string ExtractBaseProperty(string logicalProperty)
    {
        // Extract the base property (margin, padding, border, etc.)
        if (logicalProperty.StartsWith("margin-"))
            return "margin";
        else if (logicalProperty.StartsWith("padding-"))
            return "padding";
        else if (logicalProperty.StartsWith("border-"))
        {
            // For border properties, we need to handle the compound properties
            if (logicalProperty.Contains("-width"))
                return "border";
            else if (logicalProperty.Contains("-style"))
                return "border";
            else if (logicalProperty.Contains("-color"))
                return "border";
            return "border";
        }
        else if (logicalProperty.StartsWith("inset-"))
            return "";

        // Default fallback
        int dashIndex = logicalProperty.IndexOf('-');
        return dashIndex > 0 ? logicalProperty.Substring(0, dashIndex) : logicalProperty;
    }

    private string ExtractPropertySuffix(string logicalProperty)
    {
        // Extract the property suffix (e.g., -width, -style, -color)
        if (logicalProperty.Contains("-width"))
            return "-width";
        else if (logicalProperty.Contains("-style"))
            return "-style";
        else if (logicalProperty.Contains("-color"))
            return "-color";

        // No specific suffix
        return "";
    }

    public ICssValue? MapPhysicalToLogical(IDictionary<string, ICssValue> physicalProperties, string logicalProperty, WritingMode writingMode)
    {
        if (physicalProperties == null || physicalProperties.Count == 0)
            throw new ArgumentException("Physical properties dictionary cannot be null or empty", nameof(physicalProperties));
        if (string.IsNullOrEmpty(logicalProperty))
            throw new ArgumentException("Logical property name cannot be null or empty", nameof(logicalProperty));

        if (!_logicalPropertyMappings.TryGetValue(logicalProperty, out var mapping))
            return null;

        if (mapping.IsShorthand)
        {
            return MapPhysicalToLogicalShorthand(physicalProperties, mapping, writingMode);
        }

        switch (mapping.Type)
        {
            case LogicalPropertyType.BlockStart:
                var blockStartProp = GetBlockStartProperty(writingMode, mapping);
                return physicalProperties.TryGetValue(blockStartProp, out var blockStartValue)
                    ? blockStartValue
                    : null;
            case LogicalPropertyType.BlockEnd:
                var blockEndProp = GetBlockEndProperty(writingMode, mapping);
                return physicalProperties.TryGetValue(blockEndProp, out var blockEndValue)
                    ? blockEndValue
                    : null;
            case LogicalPropertyType.InlineStart:
                var inlineStartProp = GetInlineStartProperty(writingMode, mapping);
                return physicalProperties.TryGetValue(inlineStartProp, out var inlineStartValue)
                    ? inlineStartValue
                    : null;
            case LogicalPropertyType.InlineEnd:
                var inlineEndProp = GetInlineEndProperty(writingMode, mapping);
                return physicalProperties.TryGetValue(inlineEndProp, out var inlineEndValue)
                    ? inlineEndValue
                    : null;
            case LogicalPropertyType.BlockSize:
                var blockSizeProp = writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : mapping.PhysicalProperties[1];
                return physicalProperties.TryGetValue(blockSizeProp, out var blockSizeValue)
                    ? blockSizeValue
                    : null;
            case LogicalPropertyType.InlineSize:
                var inlineSizeProp = writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : mapping.PhysicalProperties[1];
                return physicalProperties.TryGetValue(inlineSizeProp, out var inlineSizeValue)
                    ? inlineSizeValue
                    : null;
        }

        return null;
    }

    private ICssValue? MapPhysicalToLogicalShorthand(
        IDictionary<string, ICssValue> physicalProperties,
        LogicalPropertyMapping mapping,
        WritingMode writingMode)
    {
        switch (mapping.Type)
        {
            case LogicalPropertyType.Block:
                var blockStartProp = GetBlockStartProperty(writingMode, mapping);
                var blockEndProp = GetBlockEndProperty(writingMode, mapping);

                if (physicalProperties.TryGetValue(blockStartProp, out var startValue) &&
                    physicalProperties.TryGetValue(blockEndProp, out var endValue))
                {
                    if (AreValuesEquivalent(startValue, endValue))
                    {
                        return startValue;
                    }
                    else
                    {
                        return CreateCombinedValue(startValue, endValue);
                    }
                }
                break;
            case LogicalPropertyType.Inline:
                var inlineStartProp = GetInlineStartProperty(writingMode, mapping);
                var inlineEndProp = GetInlineEndProperty(writingMode, mapping);

                if (physicalProperties.TryGetValue(inlineStartProp, out var inlineStartValue) &&
                    physicalProperties.TryGetValue(inlineEndProp, out var inlineEndValue))
                {
                    if (AreValuesEquivalent(inlineStartValue, inlineEndValue))
                    {
                        return inlineStartValue;
                    }
                    else
                    {
                        return CreateCombinedValue(inlineStartValue, inlineEndValue);
                    }
                }
                break;
            case LogicalPropertyType.All:
                var topProp = GetBlockStartProperty(writingMode, mapping);
                var bottomProp = GetBlockEndProperty(writingMode, mapping);
                var leftProp = GetInlineStartProperty(writingMode, mapping);
                var rightProp = GetInlineEndProperty(writingMode, mapping);

                if (physicalProperties.TryGetValue(topProp, out var topValue) &&
                    physicalProperties.TryGetValue(bottomProp, out var bottomValue) &&
                    physicalProperties.TryGetValue(leftProp, out var leftValue) &&
                    physicalProperties.TryGetValue(rightProp, out var rightValue))
                {
                    if (AreValuesEquivalent(topValue, bottomValue) &&
                        AreValuesEquivalent(topValue, leftValue) &&
                        AreValuesEquivalent(topValue, rightValue))
                    {
                        return topValue;
                    }
                    else
                    {
                        return CreateCombinedPeriodicValue(new[] { topValue, rightValue, bottomValue, leftValue });
                    }
                }
                break;
        }

        return null;
    }

    private ICssValue CreateCombinedValue(ICssValue startValue, ICssValue endValue)
    {
        return new CssPeriodicValue(new[] { startValue, endValue });
    }

    private ICssValue CreateCombinedPeriodicValue(ICssValue[] values)
    {
        return new CssPeriodicValue(values);
    }

    public bool IsLogicalProperty(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;
        return _logicalPropertyMappings.ContainsKey(propertyName);
    }

    public IEnumerable<string> GetPhysicalProperties(string logicalProperty)
    {
        if (string.IsNullOrEmpty(logicalProperty))
            return Enumerable.Empty<string>();
        if (_logicalPropertyMappings.TryGetValue(logicalProperty, out var mapping))
        {
            return mapping.PhysicalProperties;
        }
        return Enumerable.Empty<string>();
    }

    private string GetBlockStartProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            return mapping.PhysicalProperties[0]; // top
        }
        else
        {
            return IsVerticalRightToLeft(writingMode)
                ? mapping.PhysicalProperties[1] // right
                : mapping.PhysicalProperties[3]; // left
        }
    }

    private string GetBlockEndProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            return mapping.PhysicalProperties[2]; // bottom
        }
        else
        {
            return IsVerticalRightToLeft(writingMode)
                ? mapping.PhysicalProperties[3] // left
                : mapping.PhysicalProperties[1]; // right
        }
    }

    private string GetInlineStartProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            return writingMode.IsRightToLeft
                ? mapping.PhysicalProperties[1] // right
                : mapping.PhysicalProperties[3]; // left
        }
        else
        {
            return mapping.PhysicalProperties[0]; // top
        }
    }

    private string GetInlineEndProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            return writingMode.IsRightToLeft
                ? mapping.PhysicalProperties[3] // left
                : mapping.PhysicalProperties[1]; // right
        }
        else
        {
            return mapping.PhysicalProperties[2]; // bottom
        }
    }

    private bool IsVerticalRightToLeft(WritingMode writingMode)
    {
        return writingMode.Mode == WritingModeType.VerticalRightToLeft ||
               writingMode.Mode == WritingModeType.SidewaysRightToLeft;
    }

    private bool AreValuesEquivalent(ICssValue value1, ICssValue value2)
    {
        if (ReferenceEquals(value1, value2))
            return true;
        if (value1 == null || value2 == null)
            return false;
        return string.Equals(value1.CssText, value2.CssText, StringComparison.OrdinalIgnoreCase);
    }

    private enum LogicalPropertyType
    {
        BlockStart,
        BlockEnd,
        InlineStart,
        InlineEnd,
        Block,
        Inline,
        BlockSize,
        InlineSize,
        All
    }

    private class LogicalPropertyMapping
    {
        public string[] PhysicalProperties { get; }
        public LogicalPropertyType Type { get; }
        public bool IsShorthand { get; }

        public LogicalPropertyMapping(string[] physicalProperties, LogicalPropertyType type, bool isShorthand = false)
        {
            PhysicalProperties = physicalProperties;
            Type = type;
            IsShorthand = isShorthand;
        }
    }
}