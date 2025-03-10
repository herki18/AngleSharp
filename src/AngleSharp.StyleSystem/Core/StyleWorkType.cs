namespace AngleSharp.StyleSystem.Core;

/// <summary>
/// The type of style recalculation work.
/// </summary>
internal enum StyleWorkType
{
    /// <summary>
    /// Recalculate styles for a single element.
    /// </summary>
    Element,

    /// <summary>
    /// Recalculate styles for an element and all its descendants.
    /// </summary>
    Subtree
}