using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Interfaces
{
    /// <summary>
    /// Tracks style invalidation and dependencies.
    /// </summary>
    public interface IStyleInvalidationTracker
    {
        /// <summary>
        /// Marks an element as needing style recalculation.
        /// </summary>
        /// <param name="element">The element to invalidate.</param>
        void InvalidateElement(IElement element);

        /// <summary>
        /// Marks specific properties as needing recalculation.
        /// </summary>
        /// <param name="element">The element with properties to invalidate.</param>
        /// <param name="properties">The names of the properties to invalidate.</param>
        void InvalidateProperties(IElement element, IEnumerable<string> properties);

        /// <summary>
        /// Determines if an element needs style recalculation.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if the element needs recalculation; otherwise, false.</returns>
        bool NeedsStyleRecalculation(IElement element);

        /// <summary>
        /// Tracks a style dependency between elements.
        /// </summary>
        /// <param name="dependent">The element that depends on the source.</param>
        /// <param name="source">The element that affects the dependent.</param>
        void TrackDependency(IElement dependent, IElement source);

        /// <summary>
        /// Processes DOM changes and updates invalidation accordingly.
        /// </summary>
        /// <param name="changes">The DOM changes to process.</param>
        void ProcessDomChanges(IEnumerable<DomChange> changes);

        /// <summary>
        /// Gets or sets the style recalculation scheduler.
        /// </summary>
        IStyleRecalcScheduler? StyleRecalcScheduler { get; set; }

        /// <summary>
        /// Gets the elements that need style updating within a subtree.
        /// </summary>
        /// <param name="root">The root element to start from.</param>
        /// <returns>A collection of elements needing style update.</returns>
        IEnumerable<IElement> GetElementsToUpdate(IElement root);

        /// <summary>
        /// Marks an element as having up-to-date styles.
        /// </summary>
        /// <param name="element">The element to mark.</param>
        void MarkAsUpToDate(IElement element);

        /// <summary>
        /// Determines if an element has up-to-date styles.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if styles are up-to-date; otherwise, false.</returns>
        bool IsUpToDate(IElement element);

        /// <summary>
        /// Marks an element as dependent on device characteristics.
        /// </summary>
        /// <param name="element">The element to mark.</param>
        void MarkAsDeviceDependent(IElement element);

        /// <summary>
        /// Invalidates elements that depend on device characteristics.
        /// </summary>
        void InvalidateForDeviceChange();
    }
}