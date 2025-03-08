namespace AngleSharp.StyleSystem.Core.Interfaces;

using Css.Dom;
using Css.Values;

/// <summary>
/// Represents a computed style with optimized property access.
/// </summary>
public interface IComputedStyle
{
    /// <summary>
    /// Gets a computed value by property name.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The computed value.</returns>
    string GetPropertyValue(string propertyName);

    /// <summary>
    /// Gets a typed value for a specific property.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The typed value.</returns>
    T? GetValue<T>(string propertyName);

    /// <summary>
    /// Gets the display type of the element.
    /// </summary>
    DisplayMode Display { get; }

    /// <summary>
    /// Gets the position type of the element.
    /// </summary>
    PositionMode Position { get; }

    /// <summary>
    /// Gets the computed value of the opacity property.
    /// </summary>
    float Opacity { get; }

    /// <summary>
    /// Gets the computed value of the z-index property.
    /// </summary>
    int ZIndex { get; }

    /// <summary>
    /// Gets the computed value of the font-size property.
    /// </summary>
    CssLengthValue FontSize { get; }

    /// <summary>
    /// Gets the writing mode for the element.
    /// </summary>
    WritingMode WritingMode { get; }

    /// <summary>
    /// Gets box-related properties.
    /// </summary>
    IBoxProperties Box { get; }

    /// <summary>
    /// Gets text-related properties.
    /// </summary>
    ITextProperties Text { get; }
}