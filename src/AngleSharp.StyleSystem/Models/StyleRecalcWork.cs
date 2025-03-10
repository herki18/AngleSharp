namespace AngleSharp.StyleSystem.Core;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Represents a style recalculation work item.
/// </summary>
internal class StyleRecalcWork
{
    /// <summary>
    /// The element to recalculate styles for.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// The priority of the recalculation.
    /// </summary>
    public RecalcPriority Priority { get; }

    /// <summary>
    /// The type of style work to perform.
    /// </summary>
    public StyleWorkType WorkType { get; }

    /// <summary>
    /// Creates a new style recalculation work item.
    /// </summary>
    /// <param name="element">The element to recalculate styles for.</param>
    /// <param name="priority">The priority of the recalculation.</param>
    /// <param name="workType">The type of style work to perform.</param>
    public StyleRecalcWork(IElement element, RecalcPriority priority, StyleWorkType workType)
    {
        Element = element;
        Priority = priority;
        WorkType = workType;
    }
}