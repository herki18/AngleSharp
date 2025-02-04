namespace AngleSharp.Renderer;

using System.Text;
using Dom;

public static class RenderEngineExtension
{
    public static string Print(this DocumentRenderer documentRenderer)
    {
        var sb = new StringBuilder();
        PrintNode(documentRenderer.GetRoot(), 0, sb);
        return sb.ToString();
    }

    private static void PrintNode(IRenderNode node, int depth, StringBuilder sb)
    {
        sb.Append(new string(' ', depth * 2));
        if(node is NonRenderableNode nonRenderableNode)
        {
            sb.Append("NR:");
        }

        sb.Append(node.Ref.NodeName);
        // sb.AppendLine();

        if (node.Ref is IElement element)
        {
            var id = element.Id;
            if (!string.IsNullOrWhiteSpace(id))
            {
                sb.AppendLine();
                sb.Append(new string(' ', depth * 2));
                sb.Append(" id=");
                sb.Append(id);
                sb.Append(" ");
            }
        }

        if (node is ElementNode elementNode)
        {
            var css = elementNode.ComputedStyle?.ToCss();
            if (!string.IsNullOrWhiteSpace(css))
            {
                sb.AppendLine();
                sb.Append(new string(' ', depth * 2));
                sb.Append(" computed-style=[ ");
                sb.Append(css);
                sb.Append(" ]");
            }

            if (elementNode.Layout != null)
            {
                sb.AppendLine();
                sb.Append(new string(' ', depth * 2));
                sb.Append(" ");
                sb.Append(elementNode.Layout);
            }
        }

        if (node is TextNode textNode)
        {
            sb.Append(" ");
            sb.Append(textNode.Layout);
        }

        sb.AppendLine();

        foreach (var child in node.Children)
        {
            PrintNode(child, depth + 1, sb);
        }
    }
}