namespace LayoutEngine.NG.Layout.Dom;

using AngleSharp.Dom;

/// <summary>
/// Layout data specific to text nodes.
/// </summary>
public class TextNodeLayout : NodeLayout
{
    public TextNodeLayout(IText textNode, LayoutDataManager manager) : base(textNode, manager)
    {
    }

    public new IText Node => (IText)base._node;
}