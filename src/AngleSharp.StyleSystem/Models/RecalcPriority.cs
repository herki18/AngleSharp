namespace AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Defines the priority of style recalculation work.
/// </summary>
public enum RecalcPriority : byte
{
    /// <summary>
    /// Lowest priority, for elements far from viewport or not visible.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority, for elements that will need styles but are not immediately visible.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority, for elements that are in or near the viewport.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority, for elements that are in the viewport and require immediate styling.
    /// </summary>
    Critical = 3
}