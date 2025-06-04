namespace LayoutEngine.NG.Dom;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using LayoutEngine.NG.Style;

/// <summary>
/// Per-element engine state container, attached to each DOM element.
/// Inherits from NodeEngineData and adds element-specific fields such as
/// inline style cache and pseudo-element style data.
///
/// Used by the engine to store all per-element state needed for style resolution,
/// layout, and rendering, without modifying the DOM element itself.
///
/// This is NOT a layout object and NOT a DOM element.
/// It is purely an internal data holder for the engine.
/// </summary>
public class ElementEngineData : NodeEngineData
{
    private ComputedStyle? _oldStyle;
    private ICssStyleDeclaration? _inlineStyle;
    private bool _inlineStyleDirty = true;
    private string? _lastStyleAttributeValue;

    // Pseudo-element styles (::before, ::after, etc.)
    private Dictionary<string, ComputedStyle>? _pseudoElementStyles;

    public ElementEngineData(IElement element, LayoutDataManager manager) : base(element, manager)
    {
    }

    public new IElement Node => (IElement)base._node;

    #region Style Data Storage

    /// <summary>
    /// Gets or sets the cached inline style declaration.
    /// In BlinkNG, this is stored in ElementData::m_inline_style_
    /// </summary>
    public ICssStyleDeclaration? InlineStyle
    {
        get
        {
            if (_inlineStyleDirty)
            {
                UpdateInlineStyleCache();
            }
            return _inlineStyle;
        }
        internal set
        {
            _inlineStyle = value;
            _inlineStyleDirty = false;
        }
    }

    /// <summary>
    /// Marks the inline style cache as dirty.
    /// Called when the style attribute changes.
    /// </summary>
    public void InvalidateInlineStyle()
    {
        _inlineStyleDirty = true;
        // Also mark for style recalc
        SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);
    }

    /// <summary>
    /// Updates the cached inline style from the style attribute.
    /// </summary>
    private void UpdateInlineStyleCache()
    {
        var styleAttr = Node.GetAttribute("style");
        if (styleAttr != _lastStyleAttributeValue)
        {
            _lastStyleAttributeValue = styleAttr;
            if (string.IsNullOrWhiteSpace(styleAttr))
            {
                _inlineStyle = null;
            }
            else
            {
                // Parse inline style
                _inlineStyle = ParseInlineStyle(styleAttr);
            }
        }
        _inlineStyleDirty = false;
    }

    /// <summary>
    /// Gets the computed style for a pseudo-element.
    /// In BlinkNG, this is stored in ElementRareData.
    /// </summary>
    public ComputedStyle? GetPseudoElementStyle(string pseudoElement)
    {
        if (_pseudoElementStyles == null)
            return null;

        _pseudoElementStyles.TryGetValue(pseudoElement, out var style);
        return style;
    }

    /// <summary>
    /// Sets the computed style for a pseudo-element.
    /// </summary>
    public void SetPseudoElementStyle(string pseudoElement, ComputedStyle? style)
    {
        if (style == null)
        {
            _pseudoElementStyles?.Remove(pseudoElement);
            return;
        }

        _pseudoElementStyles ??= new Dictionary<string, ComputedStyle>();
        _pseudoElementStyles[pseudoElement] = style;
    }

    #endregion

    #region Style Recalculation

    /// <summary>
    /// Gets the computed style from the layout object.
    /// </summary>
    public ComputedStyle? GetComputedStyle() => LayoutObject?.Style;

    /// <summary>
    /// Performs style recalculation for this element.
    /// </summary>
    public void RecalcStyle(StyleRecalcChange change, StyleRecalcContext context)
    {
        // Store old style for comparison
        _oldStyle = GetComputedStyle();

        // Check if we need to recalc this element's style
        if (change.ShouldRecalcStyleFor(Node, _manager))
        {
            // Resolve new style
            var newStyle = ResolveStyle(context);

            // Check what kind of change occurred
            var styleChange = ComputeStyleChange(_oldStyle, newStyle);

            // Handle the style change
            HandleStyleChange(styleChange, newStyle, change, context);
        }

        // Clear style recalc flag
        ClearNeedsStyleRecalc();

        // Process children if needed
        if (change.TraverseChildren(Node, _manager))
        {
            RecalcStyleForChildren(change, context);
        }
    }

    /// <summary>
    /// Resolves the style for this element.
    /// In BlinkNG: StyleResolver::ResolveStyle
    /// </summary>
    private ComputedStyle ResolveStyle(StyleRecalcContext context)
    {
        // Get the document's style engine
        var docLayout = _manager.GetOrCreate(Node.OwnerDocument!);
        var styleEngine = docLayout.StyleEngine;
        var styleResolver = styleEngine.StyleResolver;

        // Resolve style using the style resolver
        var resolvedStyle = styleResolver.ResolveStyle(Node, context);

        return resolvedStyle;
    }

    /// <summary>
    /// Computes the difference between old and new styles.
    /// In BlinkNG: ComputedStyle::ComputeDifference
    /// </summary>
    private StyleDifference ComputeStyleChange(ComputedStyle? oldStyle, ComputedStyle? newStyle)
    {
        // No old style means we need to attach layout tree
        if (oldStyle == null)
            return StyleDifference.NeedsReattachLayoutTree;

        // No new style means we need to detach
        if (newStyle == null)
            return StyleDifference.NeedsReattachLayoutTree;

        // Compare display values - display change requires reattach
        if (oldStyle.Display != newStyle.Display)
            return StyleDifference.NeedsReattachLayoutTree;

        // Compare position values - position change might require reattach
        if (oldStyle.Position != newStyle.Position)
        {
            // Static <-> non-static requires reattach
            if ((oldStyle.Position == PositionMode.Static) != (newStyle.Position == PositionMode.Static))
                return StyleDifference.NeedsReattachLayoutTree;
        }

        // Check if layout is needed
        // Simplified - in real BlinkNG this would check many properties
        if (!AreBoxPropertiesEqual(oldStyle, newStyle))
            return StyleDifference.NeedsFullLayout;

        // Check if only paint is needed
        if (oldStyle.Color != newStyle.Color || oldStyle.BackgroundColor != newStyle.BackgroundColor)
            return StyleDifference.NeedsSimplifiedLayout;

        return StyleDifference.Equal;
    }

    /// <summary>
    /// Handles the style change by updating the layout tree as needed.
    /// In BlinkNG: Element::RecalcStyle continuation
    /// </summary>
    private void HandleStyleChange(
        StyleDifference difference,
        ComputedStyle? newStyle,
        StyleRecalcChange change,
        StyleRecalcContext context)
    {
        switch (difference)
        {
            case StyleDifference.Equal:
                // No change needed, but update the style reference
                if (LayoutObject != null && newStyle != null)
                {
                    LayoutObject.Style = newStyle;
                }
                break;

            case StyleDifference.NeedsReattachLayoutTree:
                // Mark for layout tree rebuild
                SetNeedsReattachLayoutTree();
                // Force children to be recalced too
                change = change.ForceRecalcDescendants();
                break;

            case StyleDifference.NeedsFullLayout:
            case StyleDifference.NeedsPositionedMovementLayout:
            case StyleDifference.NeedsSimplifiedLayout:
                // Update the style
                if (LayoutObject != null && newStyle != null)
                {
                    LayoutObject.Style = newStyle;
                    LayoutObject.SetNeedsLayout();
                }
                break;
        }
    }

    /// <summary>
    /// Recalculates styles for children.
    /// In BlinkNG: Element::RecalcStyleForChildren
    /// </summary>
    private void RecalcStyleForChildren(StyleRecalcChange change, StyleRecalcContext context)
    {
        // Create child context with this element's computed style as parent
        var childContext = context.CreateChildContext(GetComputedStyle());

        // Get change for children
        var childChange = change.ForChildren(Node, _manager);

        // Process each child
        foreach (var child in Node.ChildNodes)
        {
            if (child is IElement childElement)
            {
                var childLayout = _manager.GetOrCreate(childElement);
                childLayout.RecalcStyle(childChange, childContext);
            }
            else if (child is IText textNode)
            {
                // Text nodes might need reattachment based on parent style changes
                if (childChange.TraverseChild(textNode, _manager))
                {
                    var textLayout = _manager.GetOrCreate(textNode);
                    // Check if text node needs reattachment
                    // In BlinkNG, this checks whitespace handling, etc.
                    if (ShouldReattachTextNode(textNode, _oldStyle, GetComputedStyle()))
                    {
                        textLayout.SetNeedsReattachLayoutTree();
                    }
                    textLayout.ClearNeedsStyleRecalc();
                }
            }
        }

        // Clear child needs style recalc flag
        ClearChildNeedsStyleRecalc();
    }

    /// <summary>
    /// Checks if a text node needs reattachment due to style changes.
    /// </summary>
    private bool ShouldReattachTextNode(IText textNode, ComputedStyle? oldStyle, ComputedStyle? newStyle)
    {
        if (oldStyle == null || newStyle == null)
            return true;

        // Check if white-space handling changed
        if (oldStyle.WhiteSpace != newStyle.WhiteSpace)
            return true;

        // Check if display changed in a way that affects text
        if (oldStyle.Display != newStyle.Display)
        {
            // Block to inline or vice versa affects text layout
            if ((IsBlockLevel(oldStyle.Display) != IsBlockLevel(newStyle.Display)))
                return true;
        }

        return false;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses inline style text using AngleSharp's CSS parser.
    /// </summary>
    private ICssStyleDeclaration? ParseInlineStyle(string styleText)
    {
        if (string.IsNullOrWhiteSpace(styleText))
            return null;

        try
        {
            // Get the CSS parser from the document's context
            var context = Node.OwnerDocument?.Context;
            if (context == null)
                return null;

            // Try to get ICssParser from the context
            var cssParser = context.GetService<ICssParser>();
            if (cssParser == null)
            {
                // Create a default CSS parser if not available
                var options = new CssParserOptions
                {
                    IsIncludingUnknownDeclarations = true,
                    IsIncludingUnknownRules = true,
                    // Note: These properties might not exist in the current version
                    // Commented out to fix build errors
                    // IsToleratingInvalidValues = true,
                    // IsToleratingInvalidConstraints = true
                };
                cssParser = new CssParser(options);
            }

            // Parse the inline style declaration
            return cssParser.ParseDeclaration(styleText);
        }
        catch
        {
            // Return null on parse errors
            return null;
        }
    }

    /// <summary>
    /// Called when the element's attributes change.
    /// In BlinkNG, this is Element::AttributeChanged
    /// </summary>
    public void AttributeChanged(string name, string? oldValue, string? newValue)
    {
        switch (name.ToLowerInvariant())
        {
            case "style":
                InvalidateInlineStyle();
                break;

            case "class":
                // Mark for style recalc - class changes can affect matching
                SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);
                break;

            case "id":
                // Mark for style recalc - ID changes can affect matching
                SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);
                break;

            // Other attributes that might affect style
            case "type":  // for <input>
            case "disabled":
            case "readonly":
            case "checked":
                SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);
                break;
        }
    }

    /// <summary>
    /// Checks if two styles have equal box properties.
    /// </summary>
    private bool AreBoxPropertiesEqual(ComputedStyle style1, ComputedStyle style2)
    {
        // Simplified comparison - in real BlinkNG this would be comprehensive
        return style1.Margin.Equals(style2.Margin) &&
               style1.Padding.Equals(style2.Padding) &&
               style1.Border.Equals(style2.Border);
    }

    /// <summary>
    /// Checks if a display type is block-level.
    /// </summary>
    private bool IsBlockLevel(DisplayMode display)
    {
        return display == DisplayMode.Block ||
               display == DisplayMode.Flex ||
               display == DisplayMode.Grid ||
               display == DisplayMode.Table ||
               display == DisplayMode.ListItem ||
               display == DisplayMode.FlowRoot;
    }

    #endregion
}