using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render.Commands;

namespace LayoutEngine.Core.Render;

using Layout.Public;

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

        commands.Add(new SetLayoutCommand(fragment, fragment.Bounds));

        if (!string.IsNullOrEmpty(props.BackgroundColor) && props.BackgroundColor != "transparent")
            commands.Add(new SetPropertyCommand(fragment, "backgroundColor", props.BackgroundColor));

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

        commands.Add(new SetPropertyCommand(fragment, "color", props.Color));
        commands.Add(new SetPropertyCommand(fragment, "fontSize", props.FontSize));
        if (!string.IsNullOrEmpty(props.FontFamily))
            commands.Add(new SetPropertyCommand(fragment, "fontFamily", props.FontFamily));
        if (!string.IsNullOrEmpty(props.FontWeight))
            commands.Add(new SetPropertyCommand(fragment, "fontWeight", props.FontWeight));

        commands.Add(new SetPropertyCommand(fragment, "zIndex", props.ZIndex));

        if (fragment.Element?.NodeType == (int)NodeType.Text)
            commands.Add(new SetPropertyCommand(fragment, "textContent", fragment.Element.TextContent));

        return commands;
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
            default: return "container";
        }
    }
}