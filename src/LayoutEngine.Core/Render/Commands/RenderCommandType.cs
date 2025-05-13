namespace LayoutEngine.Core.Render.Commands;

/// <summary>
/// Enumeration of render command types
/// </summary>
public enum RenderCommandType
{
    Create,
    Update,
    Delete,
    SetProperty,
    SetChildren,
    SetText,
    SetImage,
    SetLayout,
    CreateViewport,
    PopViewport,
    SetScrollOffset,
}