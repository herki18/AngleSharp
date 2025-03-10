namespace AngleSharp.StyleSystem.Core.Interfaces;

using Css.Dom;
using Css.Values;

/// <summary>
/// Text-related computed properties.
/// </summary>
public interface ITextProperties
{
    /// <summary>
    /// Gets the computed font family.
    /// </summary>
    string FontFamily { get; }

    /// <summary>
    /// Gets the computed font weight.
    /// </summary>
    int FontWeight { get; }

    /// <summary>
    /// Gets whether the font style is italic.
    /// </summary>
    bool IsItalic { get; }

    /// <summary>
    /// Gets the computed line height.
    /// </summary>
    CssLengthValue LineHeight { get; }

    /// <summary>
    /// Gets the computed text align property.
    /// </summary>
    TextAlign TextAlign { get; }

    /// <summary>
    /// Gets the computed color.
    /// </summary>
    CssColorValue Color { get; }
}