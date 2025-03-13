namespace AngleSharp.StyleSystem.Properties;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using Css;
using Interfaces;

/// <summary>
/// Represents less-commonly used computed properties.
/// Uses a more flexible storage system to minimize memory impact.
/// </summary>
public class RareProperties : IRareProperties
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

    public CssColorValue BackgroundColor => _backgroundColor;

    public CssColorValue BorderColor => _borderColor;

    // Additional properties with generic access
    public ICssValue? GetValue(string propertyName)
    {
        return propertyName switch
        {
            PropertyNames.Opacity => new CssNumberValue(_opacity),
            PropertyNames.ZIndex => new CssIntegerValue(_zIndex),
            PropertyNames.BackgroundColor => _backgroundColor,
            PropertyNames.BorderColor => _borderColor,
            _ => _otherProperties.GetValueOrDefault(propertyName)
        };
    }

    public void SetValue(string propertyName, ICssValue value)
    {
        switch (propertyName)
        {
            case PropertyNames.Opacity when value is CssNumberValue number:
                _opacity = (float)number.Value;
                return;
            case PropertyNames.ZIndex when value is CssIntegerValue integer:
                _zIndex = integer.IntValue;
                return;
            case PropertyNames.BackgroundColor when value is CssColorValue bgColor:
                _backgroundColor = bgColor;
                return;
            case PropertyNames.BorderColor when value is CssColorValue borderColor:
                _borderColor = borderColor;
                return;
        }

        // Store other properties in the dictionary
        if (value != null)
        {
            _otherProperties[propertyName] = value;
        }
        else if (_otherProperties != null)
        {
            _otherProperties.Remove(propertyName);
        }
    }
}