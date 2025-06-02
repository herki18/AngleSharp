using AngleSharp.Dom;
namespace LayoutEngine.NG.Layout.Dom;
using Style;

/// <summary>
/// Layout data specific to elements.
/// </summary>
public class ElementLayout : NodeLayout
{
    private ComputedStyle? _oldStyle;

    public ElementLayout(IElement element, LayoutDataManager manager) : base(element, manager)
    {
    }

    public new IElement Node => (IElement)base._node;

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

    private ComputedStyle? ResolveStyle(StyleRecalcContext context)
    {
        // Get style resolver from document
        var docLayout = _manager.GetOrCreate(Node.OwnerDocument!);
        var styleResolver = docLayout.StyleEngine.StyleResolver;

        // Resolve style for this element
        return styleResolver.ResolveStyle(Node, context);
    }

    private StyleDifference ComputeStyleChange(ComputedStyle? oldStyle, ComputedStyle? newStyle)
    {
        if (oldStyle == null && newStyle == null)
            return StyleDifference.Equal;

        if (oldStyle == null || newStyle == null)
            return StyleDifference.NeedsReattachLayoutTree;

        // Check display change
        if (oldStyle.Display != newStyle.Display)
        {
            // display:none <-> !none always needs reattachment
            if (oldStyle.Display == DisplayType.None || newStyle.Display == DisplayType.None)
                return StyleDifference.NeedsReattachLayoutTree;

            // display:contents <-> !contents needs reattachment
            if (oldStyle.Display == DisplayType.Contents || newStyle.Display == DisplayType.Contents)
                return StyleDifference.NeedsReattachLayoutTree;

            // Other display changes need reattachment
            return StyleDifference.NeedsReattachLayoutTree;
        }

        // Simplified difference calculation
        return StyleDifference.NeedsFullLayout;
    }

    private void HandleStyleChange(StyleDifference difference, ComputedStyle? newStyle,
        StyleRecalcChange change, StyleRecalcContext context)
    {
        switch (difference)
        {
            case StyleDifference.NeedsReattachLayoutTree:
                // Mark for reattachment
                SetNeedsReattachLayoutTree();
                // Store new style for when we reattach
                if (LayoutObject != null)
                    LayoutObject.Style = newStyle;
                break;

            case StyleDifference.NeedsFullLayout:
            case StyleDifference.NeedsPositionedMovementLayout:
                // Update style and mark for layout
                if (LayoutObject != null)
                {
                    LayoutObject.Style = newStyle;
                    LayoutObject.SetNeedsLayout();
                }
                break;

            case StyleDifference.NeedsSimplifiedLayout:
                // Update style with simplified layout
                if (LayoutObject != null)
                {
                    LayoutObject.Style = newStyle;
                    LayoutObject.SetNeedsLayout();
                }
                break;

            case StyleDifference.Equal:
                // Nothing to do
                break;
        }
    }

    private void RecalcStyleForChildren(StyleRecalcChange change, StyleRecalcContext context)
    {
        var childChange = change.ForChildren(Node, _manager);
        var childContext = context.CreateChildContext(GetComputedStyle());

        foreach (var child in Node.ChildNodes)
        {
            if (child is IElement childElement)
            {
                var childLayout = _manager.GetOrCreate(childElement);
                childLayout.RecalcStyle(childChange, childContext);
            }
            else if (child is IText textNode && childChange.ShouldRecalcStyleFor(textNode, _manager))
            {
                // Text nodes might need reattachment based on parent style changes
                HandleTextNodeStyleChange(textNode, childContext);
            }
        }

        ClearChildNeedsStyleRecalc();
    }

    private void HandleTextNodeStyleChange(IText textNode, StyleRecalcContext context)
    {
        // Check if text node attachment needs to change
        var parentStyle = GetComputedStyle();
        if (parentStyle != null && _oldStyle != null)
        {
            if (parentStyle.WhiteSpace != _oldStyle.WhiteSpace)
            {
                var textLayout = _manager.GetOrCreate(textNode);
                textLayout.SetNeedsReattachLayoutTree();
            }
        }

        var nodeLayout = _manager.GetOrCreate((INode)textNode);
        nodeLayout.ClearNeedsStyleRecalc();
    }
}