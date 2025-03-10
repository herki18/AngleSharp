namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Maps between logical and physical properties.
/// </summary>
public interface IStylePropertyMapper
{
    /// <summary>
    /// Maps a logical property to physical properties.
    /// </summary>
    /// <param name="logicalProperty">The logical property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="writingMode">The writing mode context.</param>
    /// <returns>A dictionary of physical property names and values.</returns>
    IDictionary<string, ICssValue> MapLogicalToPhysical(string logicalProperty, ICssValue value, WritingMode writingMode);

    /// <summary>
    /// Maps physical properties to a logical property.
    /// </summary>
    /// <param name="physicalProperties">The physical property names and values.</param>
    /// <param name="logicalProperty">The logical property name.</param>
    /// <param name="writingMode">The writing mode context.</param>
    /// <returns>The logical property value.</returns>
    ICssValue? MapPhysicalToLogical(IDictionary<string, ICssValue> physicalProperties, string logicalProperty, WritingMode writingMode);

    /// <summary>
    /// Checks if a property is a logical property.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property is logical; otherwise, false.</returns>
    bool IsLogicalProperty(string propertyName);

    /// <summary>
    /// Gets the corresponding physical properties for a logical property.
    /// </summary>
    /// <param name="logicalProperty">The logical property name.</param>
    /// <returns>The physical property names.</returns>
    IEnumerable<string> GetPhysicalProperties(string logicalProperty);
}