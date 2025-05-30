namespace LayoutEngine.Core.Render.Commands;
using System;
using Layout.Public;
using LayoutEngine.Core.Layout;

/// <summary>
/// Command to pop back to the parent container
/// </summary>
public class PopContainerCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.PopContainer;

    public PopContainerCommand(ILayoutFragment fragment)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
    }
}