using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Dom;

namespace LayoutEngine.Platform.DOM;

/// <summary>
/// Implementation of IElementAdapter that works with AngleSharp DOM elements.
/// </summary>
public sealed class ElementAdapter : IElementAdapter
{
    /// <inheritdoc />
    public string GetId(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.Id ?? string.Empty;
    }

    /// <inheritdoc />
    public string GetTagName(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.TagName.ToLowerInvariant();
    }

    /// <inheritdoc />
    public string? GetAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        return element.GetAttribute(name);
    }

    /// <inheritdoc />
    public void SetAttribute(IElement element, string name, string? value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        if (value == null)
        {
            element.RemoveAttribute(name);
        }
        else
        {
            element.SetAttribute(name, value);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IElement> GetChildren(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var children = new List<IElement>();

        foreach (var child in element.Children)
        {
            children.Add(child);
        }

        return children;
    }

    /// <inheritdoc />
    public IElement? GetParent(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.ParentElement;
    }

    /// <inheritdoc />
    public bool HasAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        return element.HasAttribute(name);
    }

    /// <inheritdoc />
    public bool RemoveAttribute(IElement element, string name)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        if (element.HasAttribute(name))
        {
            element.RemoveAttribute(name);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IElement? GetElementById(IDocument document, string id)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        return document.GetElementById(id);
    }

    /// <inheritdoc />
    public IReadOnlyList<IElement> GetElementsByTagName(IElement element, string tagName)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrEmpty(tagName))
            throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

        var elements = element.GetElementsByTagName(tagName.ToLowerInvariant());
        var result = new List<IElement>(elements.Length);

        foreach (var el in elements)
        {
            result.Add(el);
        }

        return result;
    }
}