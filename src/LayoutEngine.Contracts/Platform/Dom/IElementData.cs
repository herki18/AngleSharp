using System.Collections.Generic;

namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Provides data and operations for an element.
/// </summary>
public interface IElementData
{
    /// <summary>
    /// Gets the ID of the element.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the tag name of the element.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Gets the class list of the element.
    /// </summary>
    IEnumerable<string> ClassList { get; }

    /// <summary>
    /// Gets the inner text of the element.
    /// </summary>
    string InnerText { get; }

    /// <summary>
    /// Gets a value indicating whether the element is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the value of the specified attribute.
    /// </summary>
    /// <param name="name">The name of the attribute.</param>
    /// <returns>The value of the attribute, or an empty string if the attribute does not exist.</returns>
    string GetAttribute(string name);

    /// <summary>
    /// Determines whether the element has the specified attribute.
    /// </summary>
    /// <param name="name">The name of the attribute to check.</param>
    /// <returns>true if the element has the attribute; otherwise, false.</returns>
    bool HasAttribute(string name);

    /// <summary>
    /// Gets the parent element data.
    /// </summary>
    IElementData Parent { get; }

    /// <summary>
    /// Gets the child element data.
    /// </summary>
    IEnumerable<IElementData> Children { get; }
}