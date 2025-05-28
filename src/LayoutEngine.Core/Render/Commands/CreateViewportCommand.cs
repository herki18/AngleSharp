namespace LayoutEngine.Core.Render.Commands;

using System;
using Layout.Public;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Viewport;

public class CreateViewportCommand : IRenderCommand
{
    public ILayoutFragment Fragment => null!; // TODO: This command doesn't use a fragment directly
    public RenderCommandType CommandType => RenderCommandType.CreateViewport;
    public Viewport Viewport { get; }

    public CreateViewportCommand(Viewport viewport)
    {
        Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
    }
}