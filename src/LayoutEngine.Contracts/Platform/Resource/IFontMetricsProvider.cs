namespace LayoutEngine.Contracts.Platform.Resource;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Provides font metrics and text measurement capabilities.
/// This interface is platform-agnostic and can be implemented for different rendering systems.
/// </summary>
public interface IFontMetricsProvider
{
    /// <summary>
    /// Gets the metrics for a specified font asynchronously.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>A task that resolves to the font metrics.</returns>
    Task<IFontMetrics> GetFontMetricsAsync(string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Measures text dimensions with the specified font settings asynchronously.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>A task that resolves to the text metrics.</returns>
    Task<ITextMetrics> MeasureTextAsync(string text, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Checks if a font is available in the system asynchronously.
    /// </summary>
    /// <param name="fontFamily">The font family name to check.</param>
    /// <returns>A task that resolves to true if the font is available, false otherwise.</returns>
    Task<bool> IsFontAvailableAsync(string fontFamily);

    /// <summary>
    /// Gets a list of fallback fonts for a specified font family asynchronously.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <returns>A task that resolves to a list of fallback font family names.</returns>
    Task<IReadOnlyList<string>> GetFallbackFontsAsync(string fontFamily);

    /// <summary>
    /// Gets metrics for a specific character (glyph) asynchronously.
    /// </summary>
    /// <param name="character">The character to get metrics for.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>A task that resolves to the glyph metrics.</returns>
    Task<IGlyphMetrics> GetGlyphMetricsAsync(char character, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Gets the metrics for a specified font (synchronous version).
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>The font metrics.</returns>
    IFontMetrics GetFontMetrics(string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Measures text dimensions with the specified font settings (synchronous version).
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>The text metrics.</returns>
    ITextMetrics MeasureText(string text, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Checks if a font is available in the system (synchronous version).
    /// </summary>
    /// <param name="fontFamily">The font family name to check.</param>
    /// <returns>True if the font is available, false otherwise.</returns>
    bool IsFontAvailable(string fontFamily);

    /// <summary>
    /// Gets a list of fallback fonts for a specified font family (synchronous version).
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <returns>A list of fallback font family names.</returns>
    IReadOnlyList<string> GetFallbackFonts(string fontFamily);

    /// <summary>
    /// Gets metrics for a specific character (glyph) (synchronous version).
    /// </summary>
    /// <param name="character">The character to get metrics for.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <param name="fontWeight">The font weight (400 is normal, 700 is bold).</param>
    /// <param name="fontStyle">The font style (e.g., "normal", "italic").</param>
    /// <returns>The glyph metrics.</returns>
    IGlyphMetrics GetGlyphMetrics(char character, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal");

    /// <summary>
    /// Registers a font resource by its path.
    /// This allows specifying a font by a resource path specific to the platform.
    /// </summary>
    /// <param name="fontFamily">The font family name to use for this font.</param>
    /// <param name="resourcePath">The platform-specific resource path to the font.</param>
    void RegisterFontResource(string fontFamily, string resourcePath);
}

/// <summary>
/// Extended font metrics provider with additional platform capabilities.
/// </summary>
public interface IExtendedFontMetricsProvider : IFontMetricsProvider
{
    /// <summary>
    /// Measures text with a platform-native font asset.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="nativeFontAsset">The platform-native font asset (cast to appropriate type in implementation).</param>
    /// <param name="fontSize">The font size in points or pixels.</param>
    /// <returns>The text metrics.</returns>
    ITextMetrics MeasureTextWithNativeAsset(string text, object nativeFontAsset, float fontSize);

    /// <summary>
    /// Registers custom font data as a font family.
    /// </summary>
    /// <param name="fontData">The raw font data (e.g., TTF or OTF file content).</param>
    /// <param name="fontFamily">The font family name to use.</param>
    /// <returns>True if registration succeeded, false otherwise.</returns>
    bool RegisterCustomFont(byte[] fontData, string fontFamily);

    /// <summary>
    /// Registers a fallback chain for a primary font.
    /// </summary>
    /// <param name="primaryFontFamily">The primary font family.</param>
    /// <param name="fallbackFontFamilies">A list of fallback font families in priority order.</param>
    void RegisterFallbackChain(string primaryFontFamily, IEnumerable<string> fallbackFontFamilies);

    /// <summary>
    /// Checks if a specific platform is supported by this provider.
    /// </summary>
    /// <param name="platformName">The platform name (e.g., "Unity", "Browser").</param>
    /// <returns>True if the platform is supported, false otherwise.</returns>
    bool IsPlatformSupported(string platformName);
}

/// <summary>
/// Represents metrics for a font at a specific size.
/// </summary>
public interface IFontMetrics
{
    /// <summary>The font family name.</summary>
    string FontFamily { get; }

    /// <summary>The font size in points or pixels.</summary>
    float FontSize { get; }

    /// <summary>The font weight (400 is normal, 700 is bold).</summary>
    int FontWeight { get; }

    /// <summary>The font style (e.g., "normal", "italic").</summary>
    string FontStyle { get; }

    /// <summary>The distance from the baseline to the top of capital letters.</summary>
    float Ascent { get; }

    /// <summary>The distance from the baseline to the bottom of descending letters (positive value).</summary>
    float Descent { get; }

    /// <summary>The recommended spacing between lines of text.</summary>
    float LineGap { get; }

    /// <summary>The em square size of the font.</summary>
    float EmSquare { get; }

    /// <summary>The height of capital letters from the baseline.</summary>
    float CapHeight { get; }

    /// <summary>The height of lowercase 'x' from the baseline.</summary>
    float XHeight { get; }

    /// <summary>Whether the font is monospace (all characters have the same width).</summary>
    bool IsMonospace { get; }

    /// <summary>The average width of characters in the font.</summary>
    float AverageCharWidth { get; }

    /// <summary>The maximum width of characters in the font.</summary>
    float MaxCharWidth { get; }
}

/// <summary>
/// Represents metrics for a text string at a specific font and size.
/// </summary>
public interface ITextMetrics
{
    /// <summary>The width of the text.</summary>
    float Width { get; }

    /// <summary>The height of the text.</summary>
    float Height { get; }

    /// <summary>The baseline position relative to the top of the text.</summary>
    float Baseline { get; }

    /// <summary>The bounding box of the text.</summary>
    Rectangle BoundingBox { get; }

    /// <summary>The text string that was measured.</summary>
    string Text { get; }

    /// <summary>Position information for each character in the text.</summary>
    IReadOnlyList<CharacterPosition> CharacterPositions { get; }
}

/// <summary>
/// Represents metrics for a single character (glyph).
/// </summary>
public interface IGlyphMetrics
{
    /// <summary>The character.</summary>
    char Character { get; }

    /// <summary>The width of the character.</summary>
    float Width { get; }

    /// <summary>The height of the character.</summary>
    float Height { get; }

    /// <summary>The horizontal bearing (offset from origin to left edge).</summary>
    float BearingX { get; }

    /// <summary>The vertical bearing (offset from baseline to top edge).</summary>
    float BearingY { get; }

    /// <summary>The horizontal advance (distance to move for next character).</summary>
    float Advance { get; }

    /// <summary>The bounding box of the character.</summary>
    Rectangle BoundingBox { get; }
}

/// <summary>
/// Represents the position of a character within text.
/// </summary>
public struct CharacterPosition
{
    /// <summary>The character.</summary>
    public char Character { get; }

    /// <summary>The X position of the character.</summary>
    public float X { get; }

    /// <summary>The width of the character.</summary>
    public float Width { get; }

    /// <summary>
    /// Creates a new CharacterPosition.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="x">The X position of the character.</param>
    /// <param name="width">The width of the character.</param>
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
    /// <summary>The X coordinate of the rectangle.</summary>
    public float X { get; }

    /// <summary>The Y coordinate of the rectangle.</summary>
    public float Y { get; }

    /// <summary>The width of the rectangle.</summary>
    public float Width { get; }

    /// <summary>The height of the rectangle.</summary>
    public float Height { get; }

    /// <summary>
    /// Creates a new Rectangle.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}