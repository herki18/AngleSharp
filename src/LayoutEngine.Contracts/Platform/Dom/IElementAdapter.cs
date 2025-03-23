using AngleSharp.Dom;
using System.Collections.Generic;

namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Provides adapter methods for working with DOM elements.
/// </summary>
public interface IElementAdapter
{
    /// <summary>
    /// Gets the ID of the specified element.
    /// </summary>
    /// <param name="element">The element to get the ID from.</param>
    /// <returns>The ID of the element.</returns>
    string GetId(IElement element);

    /// <summary>
    /// Gets the tag name of the specified element.
    /// </summary>
    /// <param name="element">The element to get the tag name from.</param>
    /// <returns>The tag name of the element.</returns>
    string GetTagName(IElement element);

    /// <summary>
    /// Gets the value of the specified attribute from the element.
    /// </summary>
    /// <param name="element">The element to get the attribute from.</param>
    /// <param name="name">The name of the attribute to get.</param>
    /// <returns>The value of the attribute, or null if the attribute does not exist.</returns>
    string? GetAttribute(IElement element, string name);

    /// <summary>
    /// Sets the value of the specified attribute on the element.
    /// </summary>
    /// <param name="element">The element to set the attribute on.</param>
    /// <param name="name">The name of the attribute to set.</param>
    /// <param name="value">The value of the attribute to set.</param>
    void SetAttribute(IElement element, string name, string? value);

    /// <summary>
    /// Gets all child elements of the specified element.
    /// </summary>
    /// <param name="element">The element to get the children of.</param>
    /// <returns>The child elements of the element.</returns>
    IReadOnlyList<IElement> GetChildren(IElement element);

    /// <summary>
    /// Gets the parent element of the specified element.
    /// </summary>
    /// <param name="element">The element to get the parent of.</param>
    /// <returns>The parent element, or null if the element has no parent.</returns>
    IElement? GetParent(IElement element);

    /// <summary>
    /// Determines whether the element has the specified attribute.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <param name="name">The name of the attribute to check for.</param>
    /// <returns>True if the element has the attribute, false otherwise.</returns>
    bool HasAttribute(IElement element, string name);

    /// <summary>
    /// Removes the specified attribute from the element.
    /// </summary>
    /// <param name="element">The element to remove the attribute from.</param>
    /// <param name="name">The name of the attribute to remove.</param>
    /// <returns>True if the attribute was removed, false if it did not exist.</returns>
    bool RemoveAttribute(IElement element, string name);

    /// <summary>
    /// Gets an element by its ID within the specified document.
    /// </summary>
    /// <param name="document">The document to search in.</param>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The element with the specified ID, or null if no such element exists.</returns>
    IElement? GetElementById(IDocument document, string id);

    /// <summary>
    /// Gets all elements with the specified tag name within the specified element.
    /// </summary>
    /// <param name="element">The element to search within.</param>
    /// <param name="tagName">The tag name to search for.</param>
    /// <returns>All elements with the specified tag name.</returns>
    IReadOnlyList<IElement> GetElementsByTagName(IElement element, string tagName);
}