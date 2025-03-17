using System.Collections.Generic;
using AngleSharp.Dom;

namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Adapts DOM elements for the rendering engine.
/// </summary>
public interface IElementAdapter
{
    /// <summary>
    /// Gets the ID of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The ID of the element, or an empty string if the element has no ID.</returns>
    string GetId(IElement element);

    /// <summary>
    /// Gets the tag name of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The tag name of the element.</returns>
    string GetTagName(IElement element);

    /// <summary>
    /// Gets the class list of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The class list of the element.</returns>
    IEnumerable<string> GetClassList(IElement element);

    /// <summary>
    /// Determines whether the specified element has the specified attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>true if the element has the attribute; otherwise, false.</returns>
    bool HasAttribute(IElement element, string attributeName);

    /// <summary>
    /// Gets the value of the specified attribute for the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The value of the attribute, or an empty string if the attribute does not exist.</returns>
    string GetAttribute(IElement element, string attributeName);

    /// <summary>
    /// Gets the names of all attributes for the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The names of all attributes.</returns>
    IEnumerable<string> GetAttributeNames(IElement element);

    /// <summary>
    /// Gets the inner text of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The inner text of the element.</returns>
    string GetInnerText(IElement element);

    /// <summary>
    /// Gets the parent element of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The parent element, or null if the element has no parent.</returns>
    IElement GetParentElement(IElement element);

    /// <summary>
    /// Gets the child elements of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The child elements.</returns>
    IEnumerable<IElement> GetChildElements(IElement element);

    /// <summary>
    /// Gets the number of child elements of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The number of child elements.</returns>
    int GetChildCount(IElement element);

    /// <summary>
    /// Gets the first child element of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The first child element, or null if the element has no children.</returns>
    IElement GetFirstChild(IElement element);

    /// <summary>
    /// Gets the last child element of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The last child element, or null if the element has no children.</returns>
    IElement GetLastChild(IElement element);

    /// <summary>
    /// Gets the next sibling element of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The next sibling element, or null if the element has no next sibling.</returns>
    IElement GetNextSibling(IElement element);

    /// <summary>
    /// Gets the previous sibling element of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The previous sibling element, or null if the element has no previous sibling.</returns>
    IElement GetPreviousSibling(IElement element);

    /// <summary>
    /// Determines whether the specified element is visible.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>true if the element is visible; otherwise, false.</returns>
    bool IsVisible(IElement element);

    /// <summary>
    /// Determines whether the specified element matches the specified selector.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="selector">The selector.</param>
    /// <returns>true if the element matches the selector; otherwise, false.</returns>
    bool MatchesSelector(IElement element, string selector);

    /// <summary>
    /// Gets the element data for the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element data.</returns>
    IElementData GetElementData(IElement element);

    /// <summary>
    /// Gets the bounding client rectangle for the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The bounding client rectangle.</returns>
    IBoundingClientRect GetBoundingClientRect(IElement element);

    /// <summary>
    /// Determines whether the specified element has child nodes.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>true if the element has child nodes; otherwise, false.</returns>
    bool HasChildNodes(IElement element);
}