namespace AngleSharp.StyleSystem.Properties;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;

/// <summary>
/// Represents less-commonly used computed properties.
/// Uses a more flexible storage system to minimize memory impact.
/// </summary>
public class RareProperties
{
    // Store frequently accessed primitive values as direct properties
    private float _opacity = 1.0f;
    private int _zIndex = 0;
    private CssColorValue _backgroundColor = CssColorValue.Transparent;
    private CssColorValue _borderColor = CssColorValue.Black;

    // Store other rare properties in a dictionary to minimize memory footprint
    // (only allocate as needed)
    private Dictionary<string, ICssValue> _otherProperties = new();

    // Standard properties with direct access
    public float Opacity
    {
        get => _opacity;
        set => _opacity = value;
    }

    public int ZIndex
    {
        get => _zIndex;
        set => _zIndex = value;
    }

    public CssColorValue? BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value ?? CssColorValue.Transparent;
    }

    public CssColorValue? BorderColor
    {
        get => _borderColor;
        set => _borderColor = value ?? CssColorValue.Black;
    }

    // Additional properties with generic access
    public ICssValue? GetValue(string propertyName)
    {
        // Check for directly stored properties first
        if (propertyName == "opacity")
            return new CssNumberValue(_opacity);

        if (propertyName == "z-index")
            return new CssIntegerValue(_zIndex);

        if (propertyName == "background-color")
            return _backgroundColor;

        if (propertyName == "border-color")
            return _borderColor;

        // Look in the dictionary for other properties
        if (_otherProperties != null && _otherProperties.TryGetValue(propertyName, out var value))
            return value;

        return null;
    }

    public void SetValue(string propertyName, ICssValue value)
    {
        // Handle directly stored properties
        if (propertyName == "opacity" && value is CssNumberValue number)
        {
            _opacity = (float)number.Value;
            return;
        }

        if (propertyName == "z-index" && value is CssIntegerValue integer)
        {
            _zIndex = integer.IntValue;
            return;
        }

        if (propertyName == "background-color" && value is CssColorValue bgColor)
        {
            _backgroundColor = bgColor;
            return;
        }

        if (propertyName == "border-color" && value is CssColorValue borderColor)
        {
            _borderColor = borderColor;
            return;
        }

        // Store other properties in the dictionary
        if (value != null)
        {
            _otherProperties ??= new Dictionary<string, ICssValue>();
            _otherProperties[propertyName] = value;
        }
        else if (_otherProperties != null)
        {
            _otherProperties.Remove(propertyName);
        }
    }

    // Specialized getters for performance-critical properties

    /// <summary>
    /// Gets background color RGBA components for fast access.
    /// </summary>
    public (byte R, byte G, byte B, float A) GetBackgroundColorComponents()
    {
        return (_backgroundColor.R, _backgroundColor.G, _backgroundColor.B, _backgroundColor.A);
    }

    /// <summary>
    /// Gets border color RGBA components for fast access.
    /// </summary>
    public (byte R, byte G, byte B, float A) GetBorderColorComponents()
    {
        return (_borderColor.R, _borderColor.G, _borderColor.B, _borderColor.A);
    }

    /// <summary>
    /// Checks if the element has a background color.
    /// </summary>
    public bool HasBackgroundColor => !_backgroundColor.Equals(CssColorValue.Transparent);

    /// <summary>
    /// Checks if the element is visible (opacity > 0).
    /// </summary>
    public bool IsVisible => _opacity > 0;

    /// <summary>
    /// Checks if the element is fully opaque (opacity = 1).
    /// </summary>
    public bool IsFullyOpaque => _opacity >= 1.0f;
}