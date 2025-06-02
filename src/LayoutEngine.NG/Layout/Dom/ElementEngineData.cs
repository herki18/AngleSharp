namespace LayoutEngine.NG.Layout.Dom;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
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

    // Animation/transition state (future)
    private object? _animationData;

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
                // TODO: Use AngleSharp's CSS parser
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

    #region Existing ElementLayout Methods

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

    // ... other existing methods ...

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses inline style text.
    /// TODO: Implement using AngleSharp's CSS parser.
    /// </summary>
    private ICssStyleDeclaration? ParseInlineStyle(string styleText)
    {
        // Placeholder - should use AngleSharp's CSS parser
        return null;
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

    #endregion
}