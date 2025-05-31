namespace LayoutEngine.Core.LayoutStyle.Public;

using LayoutEngine.Core.LayoutNG.Public;
using AngleSharp.Css.Dom;

/// <summary>
/// Computed style associated with a layout object.
/// Handles both element-backed and anonymous layout objects.
/// </summary>
public interface ILayoutComputedStyle
{
    /// <summary>
    /// The layout object this style belongs to.
    /// </summary>
    ILayoutObject LayoutObject { get; }

    /// <summary>
    /// The underlying CSS declaration.
    /// </summary>
    ICssStyleDeclaration Declaration { get; }

    /// <summary>
    /// Gets a computed property value.
    /// </summary>
    string GetPropertyValue(string propertyName);

    /// <summary>
    /// Sets a property value (used during style synthesis).
    /// </summary>
    void SetProperty(string propertyName, string value);

    /// <summary>
    /// Whether this is a synthesized style for an anonymous box.
    /// </summary>
    bool IsSynthesized { get; }

    /// <summary>
    /// The display type for quick access.
    /// </summary>
    string Display { get; }

    /// <summary>
    /// The position type for quick access.
    /// </summary>
    string Position { get; }

    /// <summary>
    /// Whether this style creates a block formatting context.
    /// </summary>
    bool CreatesBlockFormattingContext { get; }

    /// <summary>
    /// Whether this style creates a stacking context.
    /// </summary>
    bool CreatesStackingContext { get; }

    /// <summary>
    /// Creates a copy of this style for modification.
    /// </summary>
    ILayoutComputedStyle Clone();
}