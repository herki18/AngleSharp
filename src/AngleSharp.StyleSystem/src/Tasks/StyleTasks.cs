namespace AngleSharp.StyleSystem.Tasks;

using System;
using Dom;
using Interfaces;
using Models;

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