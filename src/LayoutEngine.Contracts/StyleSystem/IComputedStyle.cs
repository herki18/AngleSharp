namespace LayoutEngine.Contracts.StyleSystem;

using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Represents a computed style object for an element.
/// </summary>
public interface IComputedStyle
{
    /// <summary>
    /// Gets the element this style belongs to.
    /// </summary>
    IElement Element { get; }

    /// <summary>
    /// Gets a computed style property value.
    /// </summary>
    /// <param name="propertyName">The CSS property name.</param>
    /// <returns>The computed value as a string.</returns>
    string GetValue(string propertyName);

    /// <summary>
    /// Gets all computed style properties.
    /// </summary>
    IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>
    /// Checks if the style has a specific property.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    bool HasProperty(string propertyName);
}