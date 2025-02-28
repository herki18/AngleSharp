namespace AngleSharp.LayoutEngine.Style;

using Core;
using FormattingContexts.Enums;

#pragma warning disable CS8603, CS8618
/// <summary>
/// Contains the essential style properties needed for layout calculations.
/// </summary>
public class LayoutNodeStyle
{
    /// <summary>
    /// Gets or sets the element's display type.
    /// </summary>
    public DisplayType Display { get; set; }

    /// <summary>
    /// Gets or sets the element's position type.
    /// </summary>
    public PositionType Position { get; set; }

    /// <summary>
    /// Gets or sets the element's float value.
    /// </summary>
    public FloatType Float { get; set; }

    /// <summary>
    /// Gets or sets the element's width value.
    /// </summary>
    public StyleValue Width { get; set; }

    /// <summary>
    /// Gets or sets the element's height value.
    /// </summary>
    public StyleValue Height { get; set; }
}