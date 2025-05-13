namespace LayoutEngine.Core.Render.Commands;

using System;
using LayoutEngine.Core.Layout;

/// <summary>
/// Command for setting the layout properties of a visual element
/// </summary>
public class SetLayoutCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.SetLayout;
    public Rect Bounds { get; }

    public SetLayoutCommand(ILayoutFragment fragment, Rect bounds)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        Bounds = bounds;
    }
}