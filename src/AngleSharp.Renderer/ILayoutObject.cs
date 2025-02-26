#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS0162 // Unreachable code detected
namespace AngleSharp.Renderer;

using Html.Construction;

/// <summary>
/// Each layout object encapsulates the logic for measuring and arranging an IRenderNode.
/// You can have a BlockLayoutObject, InlineLayoutObject, etc.
/// </summary>
public interface ILayoutObject
{
    /// <summary>
    /// The node being laid out (ElementNode, TextNode, etc.).
    /// </summary>
    IRenderNode Node { get; }

    /// <summary>
    /// First pass: figure out the node's *intrinsic* or "preferred" size.
    /// For block layout, you might compute your content width/height here (but not final).
    /// For text or flex, you might measure text or children to determine needed space.
    /// </summary>
    void Measure(LayoutContext context);

    /// <summary>
    /// Second pass: finalize positions/sizes after measuring.
    /// Place children in the container (for block or flex or inline).
    /// Possibly recalculate height if "auto".
    /// </summary>
    void Arrange(LayoutContext context);
}

/// <summary>
/// A factory method that picks which ILayoutObject to use based on the node's style.
/// For now, we do a simple display check: "block" => BlockLayoutObject, else Inline.
/// You could add "flex", "grid", etc. here as you expand.
/// </summary>
public static class LayoutObjectFactory
{
    public static ILayoutObject GetOrCreateLayoutObject(IRenderNode node)
    {
        // If it's an ElementNode, we read display property:
        if (node is ElementNode elem)
        {
            var style = elem.ComputedStyle;
            if (style != null)
            {
                var display = style.GetPropertyValue("display") ?? "inline";
                if (display == "block")
                    return new BlockLayoutObject(elem);
                // else if (display == "flex") return new FlexLayoutObject(elem);
                // else if (display == "inline-block") ...
                // else if (display == "grid") ...
            }

            // default
            return new InlineLayoutObject(elem);
        }
        else if (node is TextNode text)
        {
            // For text, we can treat it as inline
            return new InlineLayoutObject(text);
        }

        // fallback
        return new BlockLayoutObject(node);
    }
}

public class InlineLayoutObject : ILayoutObject
{
    public IRenderNode Node { get; }

    public InlineLayoutObject(IRenderNode node)
    {
        Node = node;
    }

    public void Measure(LayoutContext context)
    {
        if (Node is TextNode text)
        {
            var content = text.Ref.TextContent ?? string.Empty;
            // naive measure
            float approxWidth = content.Length * 7f;
            float lineHeight = 16f;

            if (text.Layout == null)
            {
                text.Layout = new LayoutBox(0, 0, approxWidth, lineHeight);
            }
            else
            {
                text.Layout.BoxWidth = approxWidth;
                text.Layout.BoxHeight = lineHeight;
            }
        }
        else if (Node is ElementNode elem)
        {
            // For an inline element node, you might measure similarly or
            // treat it as inline-block if needed. This sample is extremely naive.
            float fallbackWidth = 50f;
            float fallbackHeight = 16f;

            if (elem.Layout == null)
                elem.Layout = new LayoutBox(0, 0, fallbackWidth, fallbackHeight);
        }
    }

    public void Arrange(LayoutContext context)
    {
        // For a naive inline approach, we just place it at (context.ParentX, context.ParentY).
        // Real inline layout needs line boxes, wrapping, etc.

        if (Node.Layout != null)
        {
            Node.Layout.X = context.ParentX;
            Node.Layout.Y = context.ParentY;

            // If we had multiple inlines in a line, we'd increment X, etc.
        }
    }
}

public class LayoutEngineV2
{
    public void LayoutDocument(IRenderNode rootNode, float viewportWidth, float viewportHeight)
    {
        if (rootNode == null) return;

        // We do a measure pass top-down
        var initialCtx = new LayoutContext
        {
            ParentX = 0,
            ParentY = 0,
            AvailableWidth = viewportWidth,
            PreviousMarginBottom = 0
        };

        MeasurePass(rootNode, initialCtx);

        // Then an arrange pass top-down
        ArrangePass(rootNode, initialCtx);
    }

    private void MeasurePass(IRenderNode node, LayoutContext context)
    {
        var layoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(node);
        layoutObj.Measure(context);
    }

    private void ArrangePass(IRenderNode node, LayoutContext context)
    {
        var layoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(node);
        layoutObj.Arrange(context);
    }
}