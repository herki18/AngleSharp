using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Resource;

namespace LayoutEngine.Contracts.Platform.Resource;

/// <summary>
/// Provides font metrics information for text layout calculations.
/// </summary>
public interface IFontMetricsProvider
{
    /// <summary>
    /// Gets metrics for a specified font.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (normal, italic, etc.).</param>
    /// <returns>The font metrics.</returns>
    Task<IFontMetrics> GetFontMetricsAsync(string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Measures text using the specified font.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (normal, italic, etc.).</param>
    /// <returns>The text metrics.</returns>
    Task<ITextMetrics> MeasureTextAsync(string text, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Checks if a font is available.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <returns>True if the font is available, false otherwise.</returns>
    Task<bool> IsFontAvailableAsync(string fontFamily);

    /// <summary>
    /// Gets a list of fallback fonts for a specified font.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <returns>A list of fallback font family names.</returns>
    Task<IReadOnlyList<string>> GetFallbackFontsAsync(string fontFamily);

    /// <summary>
    /// Attempts to get glyph metrics for a character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (normal, italic, etc.).</param>
    /// <returns>The glyph metrics.</returns>
    Task<IGlyphMetrics> GetGlyphMetricsAsync(char character, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");
}

/// <summary>
/// Represents metrics for a font.
/// </summary>
public interface IFontMetrics
{
    /// <summary>
    /// Gets the font family name.
    /// </summary>
    string FontFamily { get; }

    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    float FontSize { get; }

    /// <summary>
    /// Gets the font weight.
    /// </summary>
    int FontWeight { get; }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    string FontStyle { get; }

    /// <summary>
    /// Gets the ascent of the font in pixels.
    /// </summary>
    float Ascent { get; }

    /// <summary>
    /// Gets the descent of the font in pixels.
    /// </summary>
    float Descent { get; }

    /// <summary>
    /// Gets the line gap of the font in pixels.
    /// </summary>
    float LineGap { get; }

    /// <summary>
    /// Gets the em square size of the font in pixels.
    /// </summary>
    float EmSquare { get; }

    /// <summary>
    /// Gets the cap height of the font in pixels.
    /// </summary>
    float CapHeight { get; }

    /// <summary>
    /// Gets the x-height of the font in pixels.
    /// </summary>
    float XHeight { get; }

    /// <summary>
    /// Gets a value indicating whether the font is a monospace font.
    /// </summary>
    bool IsMonospace { get; }

    /// <summary>
    /// Gets the average character width of the font in pixels.
    /// </summary>
    float AverageCharWidth { get; }

    /// <summary>
    /// Gets the maximum character width of the font in pixels.
    /// </summary>
    float MaxCharWidth { get; }
}

/// <summary>
/// Represents metrics for measured text.
/// </summary>
public interface ITextMetrics
{
    /// <summary>
    /// Gets the width of the text in pixels.
    /// </summary>
    float Width { get; }

    /// <summary>
    /// Gets the height of the text in pixels.
    /// </summary>
    float Height { get; }

    /// <summary>
    /// Gets the baseline of the text in pixels.
    /// </summary>
    float Baseline { get; }

    /// <summary>
    /// Gets the bounding box of the text.
    /// </summary>
    Rectangle BoundingBox { get; }

    /// <summary>
    /// Gets the actual text that was measured.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets character positions within the text.
    /// </summary>
    IReadOnlyList<CharacterPosition> CharacterPositions { get; }
}

/// <summary>
/// Represents metrics for a glyph.
/// </summary>
public interface IGlyphMetrics
{
    /// <summary>
    /// Gets the character the glyph represents.
    /// </summary>
    char Character { get; }

    /// <summary>
    /// Gets the width of the glyph in pixels.
    /// </summary>
    float Width { get; }

    /// <summary>
    /// Gets the height of the glyph in pixels.
    /// </summary>
    float Height { get; }

    /// <summary>
    /// Gets the horizontal bearing X of the glyph in pixels.
    /// </summary>
    float BearingX { get; }

    /// <summary>
    /// Gets the horizontal bearing Y of the glyph in pixels.
    /// </summary>
    float BearingY { get; }

    /// <summary>
    /// Gets the horizontal advance of the glyph in pixels.
    /// </summary>
    float Advance { get; }

    /// <summary>
    /// Gets the bounding box of the glyph.
    /// </summary>
    Rectangle BoundingBox { get; }
}

/// <summary>
/// Represents a position of a character in text.
/// </summary>
public struct CharacterPosition
{
    /// <summary>
    /// Gets the character.
    /// </summary>
    public char Character { get; }

    /// <summary>
    /// Gets the X position of the character in pixels.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the width of the character in pixels.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterPosition"/> struct.
    /// </summary>
    public CharacterPosition(char character, float x, float width)
    {
        Character = character;
        X = x;
        Width = width;
    }
}

/// <summary>
/// Represents a rectangle.
/// </summary>
public struct Rectangle
{
    /// <summary>
    /// Gets the X coordinate of the rectangle.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y coordinate of the rectangle.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}