namespace LayoutEngine.NG.Style;

/// <summary>
/// Types of style changes, following BlinkNG's style change classification.
/// </summary>
public enum StyleChangeType
{
    /// <summary>
    /// No style change needed.
    /// </summary>
    NoChange,

    /// <summary>
    /// Only the element's own style needs recalculation.
    /// In BlinkNG, this is kLocalStyleChange.
    /// </summary>
    LocalStyleChange,

    /// <summary>
    /// The entire subtree needs style recalculation.
    /// In BlinkNG, this is kSubtreeStyleChange.
    /// </summary>
    SubtreeStyleChange
}