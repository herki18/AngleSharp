namespace LayoutEngine.Core.Render.Commands;

using System;
using LayoutEngine.Core.Layout;

public class SetScrollOffsetCommand : IRenderCommand
{
    public ILayoutFragment Fragment => null!; // TODO: This command doesn't use a fragment directly
    public RenderCommandType CommandType => RenderCommandType.SetScrollOffset;
    public string ViewportId { get; }
    public Point ScrollOffset { get; }

    public SetScrollOffsetCommand(string viewportId, Point scrollOffset)
    {
        ViewportId = viewportId ?? throw new ArgumentNullException(nameof(viewportId));
        ScrollOffset = scrollOffset;
    }
}