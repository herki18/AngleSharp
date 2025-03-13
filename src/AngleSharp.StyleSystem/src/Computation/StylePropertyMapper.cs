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
            new[] { "margin-top", "margin-bottom" },
            LogicalPropertyType.Block,
            true),
        ["margin-block-start"] = new LogicalPropertyMapping(
            new[] { "margin-top" },
            LogicalPropertyType.BlockStart),
        ["margin-block-end"] = new LogicalPropertyMapping(
            new[] { "margin-bottom" },
            LogicalPropertyType.BlockEnd),
        ["margin-inline"] = new LogicalPropertyMapping(
            new[] { "margin-left", "margin-right" },
            LogicalPropertyType.Inline,
            true),
        ["margin-inline-start"] = new LogicalPropertyMapping(
            new[] { "margin-left" },
            LogicalPropertyType.InlineStart),
        ["margin-inline-end"] = new LogicalPropertyMapping(
            new[] { "margin-right" },
            LogicalPropertyType.InlineEnd),
        ["padding-block"] = new LogicalPropertyMapping(
            new[] { "padding-top", "padding-bottom" },
            LogicalPropertyType.Block,
            true),
        ["padding-block-start"] = new LogicalPropertyMapping(
            new[] { "padding-top" },
            LogicalPropertyType.BlockStart),
        ["padding-block-end"] = new LogicalPropertyMapping(
            new[] { "padding-bottom" },
            LogicalPropertyType.BlockEnd),
        ["padding-inline"] = new LogicalPropertyMapping(
            new[] { "padding-left", "padding-right" },
            LogicalPropertyType.Inline,
            true),
        ["padding-inline-start"] = new LogicalPropertyMapping(
            new[] { "padding-left" },
            LogicalPropertyType.InlineStart),
        ["padding-inline-end"] = new LogicalPropertyMapping(
            new[] { "padding-right" },
            LogicalPropertyType.InlineEnd),
        ["border-block"] = new LogicalPropertyMapping(
            new[] { "border-top", "border-bottom" },
            LogicalPropertyType.Block,
            true),
        ["border-block-width"] = new LogicalPropertyMapping(
            new[] { "border-top-width", "border-bottom-width" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-width"] = new LogicalPropertyMapping(
            new[] { "border-top-width" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-width"] = new LogicalPropertyMapping(
            new[] { "border-bottom-width" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-width"] = new LogicalPropertyMapping(
            new[] { "border-left-width", "border-right-width" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-width"] = new LogicalPropertyMapping(
            new[] { "border-left-width" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-width"] = new LogicalPropertyMapping(
            new[] { "border-right-width" },
            LogicalPropertyType.InlineEnd),
        ["border-block-color"] = new LogicalPropertyMapping(
            new[] { "border-top-color", "border-bottom-color" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-color"] = new LogicalPropertyMapping(
            new[] { "border-top-color" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-color"] = new LogicalPropertyMapping(
            new[] { "border-bottom-color" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-color"] = new LogicalPropertyMapping(
            new[] { "border-left-color", "border-right-color" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-color"] = new LogicalPropertyMapping(
            new[] { "border-left-color" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-color"] = new LogicalPropertyMapping(
            new[] { "border-right-color" },
            LogicalPropertyType.InlineEnd),
        ["border-block-style"] = new LogicalPropertyMapping(
            new[] { "border-top-style", "border-bottom-style" },
            LogicalPropertyType.Block,
            true),
        ["border-block-start-style"] = new LogicalPropertyMapping(
            new[] { "border-top-style" },
            LogicalPropertyType.BlockStart),
        ["border-block-end-style"] = new LogicalPropertyMapping(
            new[] { "border-bottom-style" },
            LogicalPropertyType.BlockEnd),
        ["border-inline-style"] = new LogicalPropertyMapping(
            new[] { "border-left-style", "border-right-style" },
            LogicalPropertyType.Inline,
            true),
        ["border-inline-start-style"] = new LogicalPropertyMapping(
            new[] { "border-left-style" },
            LogicalPropertyType.InlineStart),
        ["border-inline-end-style"] = new LogicalPropertyMapping(
            new[] { "border-right-style" },
            LogicalPropertyType.InlineEnd),
        ["inset-block"] = new LogicalPropertyMapping(
            new[] { "top", "bottom" },
            LogicalPropertyType.Block,
            true),
        ["inset-block-start"] = new LogicalPropertyMapping(
            new[] { "top" },
            LogicalPropertyType.BlockStart),
        ["inset-block-end"] = new LogicalPropertyMapping(
            new[] { "bottom" },
            LogicalPropertyType.BlockEnd),
        ["inset-inline"] = new LogicalPropertyMapping(
            new[] { "left", "right" },
            LogicalPropertyType.Inline,
            true),
        ["inset-inline-start"] = new LogicalPropertyMapping(
            new[] { "left" },
            LogicalPropertyType.InlineStart),
        ["inset-inline-end"] = new LogicalPropertyMapping(
            new[] { "right" },
            LogicalPropertyType.InlineEnd),
        ["inset"] = new LogicalPropertyMapping(
            new[] { "top", "right", "bottom", "left" },
            LogicalPropertyType.All,
            true),
        ["block-size"] = new LogicalPropertyMapping(
            new[] { "height" },
            LogicalPropertyType.BlockSize),
        ["inline-size"] = new LogicalPropertyMapping(
            new[] { "width" },
            LogicalPropertyType.InlineSize),
        ["min-block-size"] = new LogicalPropertyMapping(
            new[] { "min-height" },
            LogicalPropertyType.BlockSize),
        ["min-inline-size"] = new LogicalPropertyMapping(
            new[] { "min-width" },
            LogicalPropertyType.InlineSize),
        ["max-block-size"] = new LogicalPropertyMapping(
            new[] { "max-height" },
            LogicalPropertyType.BlockSize),
        ["max-inline-size"] = new LogicalPropertyMapping(
            new[] { "max-width" },
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
        if (mapping.IsShorthand)
        {
            return MapLogicalShorthandToPhysical(logicalProperty, value, writingMode, mapping);
        }

        switch (mapping.Type)
        {
            case LogicalPropertyType.BlockStart:
                result[GetBlockStartProperty(writingMode, mapping)] = value;
                break;
            case LogicalPropertyType.BlockEnd:
                result[GetBlockEndProperty(writingMode, mapping)] = value;
                break;
            case LogicalPropertyType.InlineStart:
                result[GetInlineStartProperty(writingMode, mapping)] = value;
                break;
            case LogicalPropertyType.InlineEnd:
                result[GetInlineEndProperty(writingMode, mapping)] = value;
                break;
            case LogicalPropertyType.BlockSize:
                result[writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : "width"] = value;
                break;
            case LogicalPropertyType.InlineSize:
                result[writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : "height"] = value;
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
                result[GetInlineStartProperty(writingMode, mapping)] = value;
                result[GetInlineEndProperty(writingMode, mapping)] = value;
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
                var blockSizeProp = writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : "width";
                return physicalProperties.TryGetValue(blockSizeProp, out var blockSizeValue)
                    ? blockSizeValue
                    : null;
            case LogicalPropertyType.InlineSize:
                var inlineSizeProp = writingMode.IsHorizontal ? mapping.PhysicalProperties[0] : "height";
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
                        // Create a combined value for different start and end values
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
                        // Create a combined value for different start and end values
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
                        // Create a combined value for different values
                        return CreateCombinedPeriodicValue(new[] { topValue, rightValue, bottomValue, leftValue });
                    }
                }
                break;
        }
        return null;
    }

    private ICssValue CreateCombinedValue(ICssValue startValue, ICssValue endValue)
    {
        // Create a combined value from start and end values
        // For margin-inline, padding-inline, etc. this should be a CssPeriodicValue with two values
        return new CssPeriodicValue(new[] { startValue, endValue });
    }

    private ICssValue CreateCombinedPeriodicValue(ICssValue[] values)
    {
        // Create a CssPeriodicValue for all four sides (top, right, bottom, left)
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
            return mapping.PhysicalProperties[0];
        }
        else
        {
            bool isRightToLeft = IsVerticalRightToLeft(writingMode);
            return isRightToLeft ?
                mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "right" :
                mapping.PhysicalProperties.ElementAtOrDefault(3) ?? "left";
        }
    }

    private string GetBlockEndProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            // Fix: Use ElementAtOrDefault(1) instead of ElementAtOrDefault(2)
            return mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "bottom";
        }
        else
        {
            bool isRightToLeft = IsVerticalRightToLeft(writingMode);
            return isRightToLeft ?
                mapping.PhysicalProperties.ElementAtOrDefault(3) ?? "left" :
                mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "right";
        }
    }

    private string GetInlineStartProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            if (writingMode.IsRightToLeft)
            {
                // For RTL, if there's only one physical property (like "margin-left"),
                // we need to convert it to the corresponding right property
                if (mapping.PhysicalProperties.Length == 1 && mapping.PhysicalProperties[0].EndsWith("-left"))
                {
                    return mapping.PhysicalProperties[0].Replace("-left", "-right");
                }
                return mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "right";
            }
            else
            {
                return mapping.PhysicalProperties.ElementAtOrDefault(0) ?? "left";
            }
        }
        else
        {
            return mapping.PhysicalProperties.ElementAtOrDefault(0) ?? "top";
        }
    }

    private string GetInlineEndProperty(WritingMode writingMode, LogicalPropertyMapping mapping)
    {
        if (writingMode.IsHorizontal)
        {
            if (writingMode.IsRightToLeft)
            {
                // For RTL, if there's only one physical property (like "margin-right"),
                // we need to convert it to the corresponding left property
                if (mapping.PhysicalProperties.Length == 1 && mapping.PhysicalProperties[0].EndsWith("-right"))
                {
                    return mapping.PhysicalProperties[0].Replace("-right", "-left");
                }
                return mapping.PhysicalProperties.ElementAtOrDefault(0) ?? "left";
            }
            else
            {
                // For LTR, if there's only one physical property (like "margin-left"),
                // we need to convert it to the corresponding right property
                if (mapping.PhysicalProperties.Length == 1 && mapping.PhysicalProperties[0].EndsWith("-left"))
                {
                    return mapping.PhysicalProperties[0].Replace("-left", "-right");
                }
                return mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "right";
            }
        }
        else
        {
            // Fixed to use ElementAtOrDefault(1) instead of ElementAtOrDefault(2)
            return mapping.PhysicalProperties.ElementAtOrDefault(1) ?? "bottom";
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