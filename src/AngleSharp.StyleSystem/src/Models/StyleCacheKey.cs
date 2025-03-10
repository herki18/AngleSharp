namespace AngleSharp.StyleSystem.Models;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// A key for the style cache.
/// </summary>
public readonly struct StyleCacheKey : IEquatable<StyleCacheKey>
{
    public readonly IElement Element;
    public readonly string? PseudoElement;

    public StyleCacheKey(IElement element, string? pseudoElement)
    {
        Element = element;
        PseudoElement = pseudoElement;
    }

    public override bool Equals(object obj)
    {
        return obj is StyleCacheKey key && Equals(key);
    }

    public bool Equals(StyleCacheKey other)
    {
        return EqualityComparer<IElement>.Default.Equals(Element, other.Element) &&
               PseudoElement == other.PseudoElement;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Element, PseudoElement);
    }
}