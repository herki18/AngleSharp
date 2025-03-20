using System;
using System.Collections.Generic;

namespace LayoutEngine.Platform.DOM;

using Contracts.Platform.Dom;

/// <summary>
/// Adapts DOM elements for the rendering pipeline.
/// Provides a unified interface for element operations.
/// </summary>
public sealed class ElementAdapter : IElementAdapter
{
    /// <summary>
    /// Gets the element ID.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element ID.</returns>
    public string GetId(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.Id;
    }

    /// <summary>
    /// Gets the element tag name.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element tag name in lowercase.</returns>
    public string GetTagName(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.TagName.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the value of an attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value, or null if the attribute doesn't exist.</returns>
    public string? GetAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        return element.GetAttribute(name);
    }

    /// <summary>
    /// Sets the value of an attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public void SetAttribute(IElement element, string name, string? value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        element.SetAttribute(name, value);
    }

    /// <summary>
    /// Gets the children of an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element children.</returns>
    public IReadOnlyList<IElement> GetChildren(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var children = new List<IElement>();

        for (var i = 0; i < element.ChildNodes.Length; i++)
        {
            var child = element.ChildNodes[i];
            if (child is IElement childElement)
            {
                children.Add(childElement);
            }
        }

        return children;
    }

    /// <summary>
    /// Gets the parent of an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The parent element, or null if the element has no parent.</returns>
    public IElement? GetParent(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.ParentElement;
    }

    /// <summary>
    /// Checks if an element has the specified attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <returns>True if the element has the attribute, otherwise false.</returns>
    public bool HasAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        return element.HasAttribute(name);
    }

    /// <summary>
    /// Removes an attribute from an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    public void RemoveAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        element.RemoveAttribute(name);
    }

    /// <summary>
    /// Finds an element by ID.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="id">The element ID.</param>
    /// <returns>The element with the specified ID, or null if no such element exists.</returns>
    public IElement? GetElementById(IDocument document, string id)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        return document.GetElementById(id);
    }

    /// <summary>
    /// Gets elements by tag name.
    /// </summary>
    /// <param name="element">The root element to search from.</param>
    /// <param name="tagName">The tag name to search for.</param>
    /// <returns>A collection of elements with the specified tag name.</returns>
    public IReadOnlyList<IElement> GetElementsByTagName(IElement element, string tagName)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(tagName))
            throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

        var elements = element.GetElementsByTagName(tagName.ToLowerInvariant());
        var result = new List<IElement>(elements.Length);

        for (var i = 0; i < elements.Length; i++)
        {
            result.Add(elements[i]);
        }

        return result;
    }
}