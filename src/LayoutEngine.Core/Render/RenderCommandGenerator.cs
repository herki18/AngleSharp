using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render.Commands;
using LayoutEngine.Core.Layout.Public;

namespace LayoutEngine.Core.Render;

using System.Linq;

public class RenderCommandGenerator
{
    public IEnumerable<IRenderCommand> GenerateFragmentCommands(
        ILayoutFragment fragment,
        FragmentStatus fragmentStatus)
    {
        var commands = new List<IRenderCommand>();
        var props = fragment.VisualProperties;

        if (fragmentStatus.IsNew)
        {
            string elementType = DetermineElementType(fragment);
            commands.Add(new CreateElementCommand(fragment, elementType));
        }

        // Set layout bounds
        commands.Add(new SetLayoutCommand(fragment, fragment.Bounds));

        // Set visual properties
        if (!string.IsNullOrEmpty(props.BackgroundColor) && props.BackgroundColor != "transparent")
            commands.Add(new SetPropertyCommand(fragment, "backgroundColor", props.BackgroundColor));

        // Set border properties
        if (props.BorderTopWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderTopWidth", props.BorderTopWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderTopColor", props.BorderTopColor));
        }
        if (props.BorderRightWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderRightWidth", props.BorderRightWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderRightColor", props.BorderRightColor));
        }
        if (props.BorderBottomWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderBottomWidth", props.BorderBottomWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderBottomColor", props.BorderBottomColor));
        }
        if (props.BorderLeftWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderLeftWidth", props.BorderLeftWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderLeftColor", props.BorderLeftColor));
        }

        // Set text properties
        commands.Add(new SetPropertyCommand(fragment, "color", props.Color));
        commands.Add(new SetPropertyCommand(fragment, "fontSize", props.FontSize));
        if (!string.IsNullOrEmpty(props.FontFamily))
            commands.Add(new SetPropertyCommand(fragment, "fontFamily", props.FontFamily));
        if (!string.IsNullOrEmpty(props.FontWeight))
            commands.Add(new SetPropertyCommand(fragment, "fontWeight", props.FontWeight));

        // Set z-index
        commands.Add(new SetPropertyCommand(fragment, "zIndex", props.ZIndex));

        // Handle text content
        if (fragment.Element != null)
        {
            // Check if the element has text content
            if (!string.IsNullOrWhiteSpace(fragment.Element.TextContent) &&
                fragment.Element.ChildNodes.Length > 0 &&
                fragment.Element.ChildNodes.Any(n => n.NodeType == (int)NodeType.Text))
            {
                // Get just the direct text content (not from child elements)
                var directTextContent = GetDirectTextContent(fragment.Element);
                if (!string.IsNullOrWhiteSpace(directTextContent))
                {
                    commands.Add(new SetPropertyCommand(fragment, "textContent", directTextContent));
                }
            }
        }
        else
        {
            // This might be a text fragment without an element
            // In mock layout, text fragments have null elements
            // We need a way to pass the text content through the fragment
            // For now, we'll skip this case as it needs a design change
        }

        return commands;
    }

    private string GetDirectTextContent(IElement element)
    {
        var directText = "";
        foreach (var node in element.ChildNodes)
        {
            if (node.NodeType == (int)NodeType.Text)
            {
                directText += node.TextContent;
            }
        }
        return directText.Trim();
    }

    private string DetermineElementType(ILayoutFragment fragment)
    {
        if (fragment.Element == null)
            return "container";

        switch (fragment.Element.TagName?.ToUpperInvariant())
        {
            case "DIV": return "container";
            case "SPAN": return "text";
            case "IMG": return "image";
            case "INPUT":
                var type = fragment.Element.GetAttribute("type") ?? "text";
                return $"input-{type}";
            case "BUTTON": return "button";
            case "P": return "paragraph";
            case "H1":
            case "H2":
            case "H3":
            case "H4":
            case "H5":
            case "H6": return "heading";
            default: return "container";
        }
    }
}