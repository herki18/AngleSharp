namespace LayoutEngine.Core.Render.Commands;

using System;
using Layout.Public;
using LayoutEngine.Core.Layout;

public class PopViewportCommand : IRenderCommand
{
    public ILayoutFragment Fragment => null!; // TODO: This command doesn't use a fragment directly
    public RenderCommandType CommandType => RenderCommandType.PopViewport;
    public string ViewportId { get; }

    public PopViewportCommand(string viewportId)
    {
        ViewportId = viewportId ?? throw new ArgumentNullException(nameof(viewportId));
    }
}