namespace LayoutEngine.Core.Render.Commands;

using System;
using LayoutEngine.Core.Layout;

/// <summary>
/// Create command for creating a new visual element
/// </summary>
public class CreateElementCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.Create;
    public string ElementType { get; }

    public CreateElementCommand(ILayoutFragment fragment, string elementType)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
    }
}