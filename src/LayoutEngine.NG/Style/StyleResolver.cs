namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Style resolution implementation, following BlinkNG's StyleResolver.
/// Computes styles by finding matching rules and applying the cascade.
/// </summary>
public class StyleResolver : IStyleResolver
{
    private ICascadeResolver _cascadeResolver;
    private readonly LayoutDataManager _layoutDataManager;
    private readonly Dictionary<string, ComputedStyle> _matchedPropertiesCache = new();

    public StyleResolver(ICascadeResolver cascadeResolver, LayoutDataManager layoutDataManager)
    {
        _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
        _layoutDataManager = layoutDataManager ?? throw new ArgumentNullException(nameof(layoutDataManager));
    }

    /// <inheritdoc/>
    public ICascadeResolver CascadeResolver
    {
        get => _cascadeResolver;
        set => _cascadeResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public ComputedStyle ResolveStyle(IElement element, ComputedStyle? parentStyle = null)
    {
        // Create a minimal context for style resolution
        var context = new StyleRecalcContext
        {
            ParentStyle = parentStyle,
            LayoutParentStyle = parentStyle
        };

        return ResolveStyle(element, context);
    }

    /// <inheritdoc/>
    public ComputedStyle ResolveStyle(IElement element, StyleRecalcContext context)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Get the document's style engine
        var docLayout = _layoutDataManager.GetOrCreate(element.OwnerDocument!);
        var styleEngine = docLayout.StyleEngine;

        // Collect matching rules using ElementRuleCollector
        var matchResult = styleEngine.ElementRuleCollector.CollectMatchingRules(element, context);

        // Get all matched rules in cascade order
        var allMatchedRules = new List<MatchedRule>();
        allMatchedRules.AddRange(matchResult.UserAgentRules);
        allMatchedRules.AddRange(matchResult.UserRules);
        allMatchedRules.AddRange(matchResult.AuthorRules);

        // Apply cascade to get resolved style declaration
        var resolvedDeclaration = _cascadeResolver.ResolveCascade(allMatchedRules, element);

        // Create computed style from resolved declaration
        var computedStyle = CreateComputedStyle(element, resolvedDeclaration, context.ParentStyle);

        // Apply inheritance from parent
        ApplyInheritance(computedStyle, context.ParentStyle);

        // Cache the result if possible
        // In real BlinkNG, this would use a more sophisticated cache key
        var cacheKey = GenerateCacheKey(element, allMatchedRules);
        if (!string.IsNullOrEmpty(cacheKey))
        {
            _matchedPropertiesCache[cacheKey] = computedStyle;
        }

        return computedStyle;
    }

    /// <inheritdoc/>
    public void ComputeFont(IElement element, ComputedStyle fontStyle, IEnumerable<ICssProperty> fontProperties)
    {
        // In BlinkNG, font computation happens separately for performance
        // This allows font metrics to be available early in style resolution

        foreach (var property in fontProperties)
        {
            // Apply font-related properties
            switch (property.Name.ToLowerInvariant())
            {
                case "font-family":
                case "font-size":
                case "font-weight":
                case "font-style":
                case "line-height":
                    // Store in computed style
                    fontStyle.SetPropertyValue(property.Name, property.Value);
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public void InvalidateMatchedPropertiesCache()
    {
        _matchedPropertiesCache.Clear();
    }

    /// <summary>
    /// Creates a computed style from a resolved style declaration.
    /// </summary>
    private ComputedStyle CreateComputedStyle(
        IElement element,
        ICssStyleDeclaration declaration,
        ComputedStyle? parentStyle)
    {
        var computedStyle = new ComputedStyle
        {
            Element = element,
            ParentComputedStyle = parentStyle
        };

        // Process each property in the declaration
        foreach (var property in declaration)
        {
            ApplyPropertyToComputedStyle(computedStyle, property, parentStyle);
        }

        // Set default values for essential properties not in declaration
        EnsureDefaultValues(computedStyle);

        // Compute derived properties
        ComputeDerivedProperties(computedStyle);

        return computedStyle;
    }

    /// <summary>
    /// Applies a single CSS property to the computed style.
    /// </summary>
    private void ApplyPropertyToComputedStyle(
        ComputedStyle computedStyle,
        ICssProperty property,
        ComputedStyle? parentStyle)
    {
        var propertyName = property.Name.ToLowerInvariant();
        var value = property.Value;

        // Handle inherit keyword
        if (value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
        {
            if (parentStyle != null)
            {
                var parentValue = parentStyle.GetPropertyValue(propertyName);
                if (parentValue != null)
                {
                    computedStyle.SetPropertyValue(propertyName, parentValue);
                }
            }
            return;
        }

        // Handle initial keyword
        if (value.Equals("initial", StringComparison.OrdinalIgnoreCase))
        {
            // Set to initial value
            SetInitialValue(computedStyle, propertyName);
            return;
        }

        // Map CSS properties to ComputedStyle properties
        switch (propertyName)
        {
            case "display":
                computedStyle.Display = ParseDisplayValue(value);
                break;

            case "position":
                computedStyle.Position = ParsePositionValue(value);
                break;

            case "white-space":
                computedStyle.WhiteSpace = ParseWhiteSpaceValue(value);
                break;

            case "color":
                // Store as string for now - in real implementation would parse color
                computedStyle.SetPropertyValue("color", value);
                break;

            case "background-color":
                // Store as string for now
                computedStyle.SetPropertyValue("background-color", value);
                break;

            // Box model properties
            case "margin":
            case "margin-top":
            case "margin-right":
            case "margin-bottom":
            case "margin-left":
            case "padding":
            case "padding-top":
            case "padding-right":
            case "padding-bottom":
            case "padding-left":
            case "border-width":
            case "border-top-width":
            case "border-right-width":
            case "border-bottom-width":
            case "border-left-width":
                // Store for later processing
                computedStyle.SetPropertyValue(propertyName, value);
                break;

            default:
                // Store any other property
                computedStyle.SetPropertyValue(propertyName, value);
                break;
        }
    }

    /// <summary>
    /// Applies inherited properties from parent style.
    /// In BlinkNG, certain CSS properties inherit by default.
    /// </summary>
    private void ApplyInheritance(ComputedStyle style, ComputedStyle? parentStyle)
    {
        if (parentStyle == null)
            return;

        // List of properties that inherit by default
        var inheritedProperties = new[]
        {
            "color",
            "font-family",
            "font-size",
            "font-style",
            "font-weight",
            "letter-spacing",
            "line-height",
            "text-align",
            "text-indent",
            "text-transform",
            "white-space",
            "word-spacing"
        };

        foreach (var propertyName in inheritedProperties)
        {
            // Only inherit if not explicitly set
            if (style.GetPropertyValue(propertyName) == null)
            {
                var parentValue = parentStyle.GetPropertyValue(propertyName);
                if (parentValue != null)
                {
                    style.SetPropertyValue(propertyName, parentValue);
                }
            }
        }

        // White-space inherits specially
        if (style.GetPropertyValue("white-space") == null)
        {
            style.WhiteSpace = parentStyle.WhiteSpace;
        }
    }

    /// <summary>
    /// Ensures essential properties have default values.
    /// </summary>
    private void EnsureDefaultValues(ComputedStyle style)
    {
        // Ensure display has a value
        if (style.GetPropertyValue("display") == null)
        {
            style.Display = DisplayType.Inline; // Default for unknown elements
        }

        // Ensure position has a value
        if (style.GetPropertyValue("position") == null)
        {
            style.Position = PositionType.Static;
        }

        // Ensure white-space has a value
        if (style.GetPropertyValue("white-space") == null)
        {
            style.WhiteSpace = WhiteSpaceType.Normal;
        }
    }

    /// <summary>
    /// Computes derived properties based on other properties.
    /// </summary>
    private void ComputeDerivedProperties(ComputedStyle style)
    {
        // Compute whether this creates a stacking context
        style.CreatesStackingContext = ComputesStackingContext(style);

        // Compute whether this creates a containing block
        style.CreatesContainingBlock = ComputesContainingBlock(style);
    }

    /// <summary>
    /// Determines if the style creates a new stacking context.
    /// </summary>
    private bool ComputesStackingContext(ComputedStyle style)
    {
        // Elements with position: fixed or position: sticky
        if (style.Position == PositionType.Fixed || style.Position == PositionType.Sticky)
            return true;

        // Elements with position: absolute or position: relative and z-index other than auto
        if ((style.Position == PositionType.Absolute || style.Position == PositionType.Relative) &&
            style.GetPropertyValue("z-index") != null &&
            style.GetPropertyValue("z-index")?.ToString() != "auto")
            return true;

        // Elements with opacity less than 1
        var opacity = style.GetPropertyValue("opacity")?.ToString();
        if (!string.IsNullOrEmpty(opacity) && opacity != "1")
            return true;

        // Elements with transform, filter, etc. (simplified)
        if (style.GetPropertyValue("transform") != null ||
            style.GetPropertyValue("filter") != null)
            return true;

        return false;
    }

    /// <summary>
    /// Determines if the style creates a new containing block.
    /// </summary>
    private bool ComputesContainingBlock(ComputedStyle style)
    {
        // Elements with position: absolute, fixed, or sticky create containing blocks
        return style.Position != PositionType.Static;
    }

    /// <summary>
    /// Sets the initial value for a property.
    /// </summary>
    private void SetInitialValue(ComputedStyle style, string propertyName)
    {
        switch (propertyName)
        {
            case "display":
                style.Display = DisplayType.Inline;
                break;
            case "position":
                style.Position = PositionType.Static;
                break;
            case "white-space":
                style.WhiteSpace = WhiteSpaceType.Normal;
                break;
            case "color":
                style.SetPropertyValue("color", "black");
                break;
            case "background-color":
                style.SetPropertyValue("background-color", "transparent");
                break;
            default:
                // For other properties, remove the value
                style.SetPropertyValue(propertyName, null);
                break;
        }
    }

    /// <summary>
    /// Parses a CSS display value.
    /// </summary>
    private DisplayType ParseDisplayValue(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "none" => DisplayType.None,
            "block" => DisplayType.Block,
            "inline" => DisplayType.Inline,
            "inline-block" => DisplayType.InlineBlock,
            "flex" => DisplayType.Flex,
            "inline-flex" => DisplayType.InlineFlex,
            "grid" => DisplayType.Grid,
            "inline-grid" => DisplayType.InlineGrid,
            "table" => DisplayType.Table,
            "inline-table" => DisplayType.InlineTable,
            "table-row" => DisplayType.TableRow,
            "table-cell" => DisplayType.TableCell,
            "list-item" => DisplayType.ListItem,
            "contents" => DisplayType.Contents,
            "flow-root" => DisplayType.FlowRoot,
            _ => DisplayType.Inline
        };
    }

    /// <summary>
    /// Parses a CSS position value.
    /// </summary>
    private PositionType ParsePositionValue(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "static" => PositionType.Static,
            "relative" => PositionType.Relative,
            "absolute" => PositionType.Absolute,
            "fixed" => PositionType.Fixed,
            "sticky" => PositionType.Sticky,
            _ => PositionType.Static
        };
    }

    /// <summary>
    /// Parses a CSS white-space value.
    /// </summary>
    private WhiteSpaceType ParseWhiteSpaceValue(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "normal" => WhiteSpaceType.Normal,
            "nowrap" => WhiteSpaceType.NoWrap,
            "pre" => WhiteSpaceType.Pre,
            "pre-wrap" => WhiteSpaceType.PreWrap,
            "pre-line" => WhiteSpaceType.PreLine,
            "break-spaces" => WhiteSpaceType.BreakSpaces,
            _ => WhiteSpaceType.Normal
        };
    }

    /// <summary>
    /// Generates a cache key for matched properties.
    /// </summary>
    private string GenerateCacheKey(IElement element, List<MatchedRule> rules)
    {
        // Simplified cache key generation
        // In real BlinkNG, this would be more sophisticated
        var parts = new List<string>
        {
            element.LocalName,
            element.Id ?? "",
            string.Join(",", element.ClassList.OrderBy(c => c))
        };

        // Add rule fingerprint
        parts.Add(rules.Count.ToString());

        return string.Join("|", parts);
    }
}