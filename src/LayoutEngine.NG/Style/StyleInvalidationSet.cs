namespace LayoutEngine.NG.Style;

using System.Collections.Generic;

/// <summary>
/// Represents a set of invalidations for style recalculation.
/// In BlinkNG, InvalidationSets track which elements need style updates
/// based on selector matching and DOM changes.
/// </summary>
public class StyleInvalidationSet
{
    /// <summary>
    /// Classes that trigger invalidation.
    /// In BlinkNG, class changes are a common invalidation trigger.
    /// </summary>
    public HashSet<string> Classes { get; } = new();

    /// <summary>
    /// IDs that trigger invalidation.
    /// </summary>
    public HashSet<string> Ids { get; } = new();

    /// <summary>
    /// Tag names that trigger invalidation.
    /// </summary>
    public HashSet<string> TagNames { get; } = new();

    /// <summary>
    /// Attributes that trigger invalidation.
    /// </summary>
    public HashSet<string> Attributes { get; } = new();

    /// <summary>
    /// Whether this invalidation affects the entire subtree.
    /// In BlinkNG, some changes require descendant invalidation.
    /// </summary>
    public bool InvalidatesSelf { get; set; }

    /// <summary>
    /// Whether descendants need invalidation.
    /// </summary>
    public bool InvalidatesDescendants { get; set; }

    /// <summary>
    /// Whether siblings need invalidation.
    /// In BlinkNG, sibling selectors can trigger sibling invalidation.
    /// </summary>
    public bool InvalidatesSiblings { get; set; }

    /// <summary>
    /// Combines this invalidation set with another.
    /// In BlinkNG, multiple invalidation sets may be combined.
    /// </summary>
    public void Combine(StyleInvalidationSet other)
    {
        Classes.UnionWith(other.Classes);
        Ids.UnionWith(other.Ids);
        TagNames.UnionWith(other.TagNames);
        Attributes.UnionWith(other.Attributes);

        InvalidatesSelf |= other.InvalidatesSelf;
        InvalidatesDescendants |= other.InvalidatesDescendants;
        InvalidatesSiblings |= other.InvalidatesSiblings;
    }
}