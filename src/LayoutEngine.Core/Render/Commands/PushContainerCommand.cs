namespace LayoutEngine.Core.Render.Commands;
using System;
using Layout.Public;
using LayoutEngine.Core.Layout;

/// <summary>
/// Command to push an element as the current container for subsequent children
/// </summary>
public class PushContainerCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.PushContainer;

    public PushContainerCommand(ILayoutFragment fragment)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
    }
}