namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using System.Collections.Generic;

/// <summary>
/// Interface for style resolution, following BlinkNG's StyleResolver.
/// Responsible for computing styles from CSS rules and the cascade.
/// </summary>
public interface IStyleResolver
{
    /// <summary>
    /// Resolves the computed style for an element.
    /// In BlinkNG, this is ResolveStyle().
    /// </summary>
    ComputedStyle ResolveStyle(IElement element, ComputedStyle? parentStyle = null);

    /// <summary>
    /// Resolves style with a specific recalc context.
    /// In BlinkNG, style resolution often needs context about the recalc operation.
    /// </summary>
    ComputedStyle ResolveStyle(IElement element, StyleRecalcContext context);

    /// <summary>
    /// Computes font properties separately (for special cases).
    /// In BlinkNG, this is ComputeFont().
    /// </summary>
    void ComputeFont(IElement element, ComputedStyle fontStyle, IEnumerable<ICssProperty> fontProperties);

    /// <summary>
    /// Gets or sets the cascade resolver used for CSS cascade resolution.
    /// </summary>
    ICascadeResolver CascadeResolver { get; set; }

    /// <summary>
    /// Invalidates any cached data.
    /// In BlinkNG, StyleResolver maintains various caches that need invalidation.
    /// </summary>
    void InvalidateMatchedPropertiesCache();
}

/// <summary>
/// Interface for CSS property representation.
/// This is a placeholder as the actual type would come from AngleSharp.
/// </summary>
public interface ICssProperty
{
    string Name { get; }
    string Value { get; }
    bool IsImportant { get; }
}