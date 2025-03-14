namespace AngleSharp.StyleSystem.Tasks;

using System;
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

/// <summary>
/// Schedules and prioritizes style tasks.
/// </summary>
public interface IStyleTaskScheduler : IDisposable
{
    /// <summary>
    /// Enqueues a task for execution.
    /// </summary>
    /// <param name="task">The task to enqueue.</param>
    void EnqueueTask(IStyleTask task);

    /// <summary>
    /// Processes pending tasks.
    /// </summary>
    void ProcessTasks();

    /// <summary>
    /// Processes pending tasks asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    System.Threading.Tasks.Task ProcessTasksAsync(System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether there are pending tasks to process.
    /// </summary>
    bool HasPendingTasks { get; }

    /// <summary>
    /// Cancels all pending tasks.
    /// </summary>
    void CancelPendingTasks();
}

/// <summary>
/// Basic implementations of style tasks for different scenarios.
/// </summary>
public static class StyleTasks
{
    /// <summary>
    /// Creates a task to compute style for a single element.
    /// </summary>
    /// <param name="element">The element to compute style for.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>A style task.</returns>
    public static IStyleTask ComputeElementStyle(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        return new ComputeElementStyleTask(element, priority);
    }

    /// <summary>
    /// Creates a task to update styles for an entire subtree.
    /// </summary>
    /// <param name="rootElement">The root element of the subtree.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>A style task.</returns>
    public static IStyleTask UpdateSubtreeStyles(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal)
    {
        return new UpdateSubtreeStylesTask(rootElement, priority);
    }

    /// <summary>
    /// Task that computes style for a single element.
    /// </summary>
    private class ComputeElementStyleTask : IStyleTask
    {
        private readonly IElement _element;
        private readonly RecalcPriority _priority;

        public ComputeElementStyleTask(IElement element, RecalcPriority priority)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _priority = priority;
        }

        public RecalcPriority Priority => _priority;

        public IElement Element => _element;

        public void Execute(IStyleEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            engine.ComputeElementStyle(_element);
        }
    }

    /// <summary>
    /// Task that updates styles for an entire subtree.
    /// </summary>
    private class UpdateSubtreeStylesTask : IStyleTask
    {
        private readonly IElement _rootElement;
        private readonly RecalcPriority _priority;

        public UpdateSubtreeStylesTask(IElement rootElement, RecalcPriority priority)
        {
            _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
            _priority = priority;
        }

        public RecalcPriority Priority => _priority;

        public IElement Element => _rootElement;

        public void Execute(IStyleEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            engine.UpdateStyles(_rootElement);
        }
    }
}