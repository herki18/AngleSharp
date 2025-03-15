namespace AngleSharp.StyleSystem.Tasks;

using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Represents a unit of work in the style system.
/// </summary>
public interface IStyleTask
{
    /// <summary>
    /// Gets the priority of this task.
    /// </summary>
    RecalcPriority Priority { get; }

    /// <summary>
    /// Gets the element associated with this task.
    /// </summary>
    IElement Element { get; }

    /// <summary>
    /// Executes the task using the provided style engine.
    /// </summary>
    /// <param name="engine">The style engine to use for execution.</param>
    void Execute(IStyleEngine engine);
}