namespace LayoutEngine.Core.Style;

using AngleSharp.Dom;

public interface IComputedStyle
{
    // Element this style applies to
    IElement Element { get; }

    // Get a specific style property value
    string GetValue(string propertyName);

    // Check if a property is explicitly defined
    bool HasValue(string propertyName);
    // Get the display type (block, inline, flex, etc.)
    DisplayType Display { get; }

    // Get the position type (static, relative, absolute, etc.)
    PositionType Position { get; }
}