namespace LayoutEngine.Core.Render;

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
    SetLayout
}