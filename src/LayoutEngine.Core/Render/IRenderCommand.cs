namespace LayoutEngine.Core.Render;

using Layout;

/// <summary>
/// Interface for render commands - these are platform-agnostic
/// instructions that will be implemented by the specific UI toolkit
/// </summary>
public interface IRenderCommand
{
    /// <summary>
    /// The fragment this command is associated with
    /// </summary>
    ILayoutFragment Fragment { get; }

    /// <summary>
    /// The type of render command
    /// </summary>
    RenderCommandType CommandType { get; }
}