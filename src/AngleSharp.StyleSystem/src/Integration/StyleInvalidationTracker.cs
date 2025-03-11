using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Integration
{
    using Interfaces;

    /// <summary>
    /// Tracks style invalidation and manages style recalculation needs for elements in the DOM.
    /// </summary>
    public class StyleInvalidationTracker : IStyleInvalidationTracker
    {
        private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new Dictionary<IElement, InvalidationState>();
        private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new Dictionary<IElement, HashSet<IElement>>();
        private readonly HashSet<IElement> _deviceDependentElements = new HashSet<IElement>();
        private IStyleRecalcScheduler? _recalcScheduler;

        /// <summary>
        /// Gets or sets the style recalculation scheduler used to schedule recalculations.
        /// </summary>
        public IStyleRecalcScheduler? StyleRecalcScheduler
        {
            get => _recalcScheduler;
            set => _recalcScheduler = value;
        }

        /// <summary>
        /// Processes a collection of DOM changes and invalidates affected elements.
        /// </summary>
        /// <param name="changes">The collection of DOM changes to process.</param>
        public void ProcessDomChanges(IEnumerable<DomChange> changes)
        {
            if (changes == null) return;

            var elementsToInvalidate = new Dictionary<IElement, HashSet<string>>();
            var subtreesToInvalidate = new HashSet<IElement>();

            foreach (var change in changes)
            {
                ProcessDomChange(change, elementsToInvalidate, subtreesToInvalidate);
            }

            ApplyInvalidations(elementsToInvalidate, subtreesToInvalidate);
        }

        /// <summary>
        /// Processes a single DOM change and collects elements and subtrees to invalidate.
        /// </summary>
        private void ProcessDomChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            switch (change.Type)
            {
                case DomChangeType.StyleAttributeChanged:
                    if (change.Target is IElement element)
                    {
                        AddElementInvalidation(element, null, elementsToInvalidate);
                    }
                    break;
                case DomChangeType.ClassAttributeChanged:
                    ProcessClassChange(change, elementsToInvalidate, subtreesToInvalidate);
                    break;
                case DomChangeType.IdAttributeChanged:
                    ProcessIdChange(change, elementsToInvalidate, subtreesToInvalidate);
                    break;
                case DomChangeType.AttributeChanged:
                    ProcessAttributeChange(change, elementsToInvalidate);
                    break;
                case DomChangeType.NodeAdded:
                    ProcessNodeAddition(change, elementsToInvalidate, subtreesToInvalidate);
                    break;
                case DomChangeType.NodeRemoved:
                    ProcessNodeRemoval(change, elementsToInvalidate, subtreesToInvalidate);
                    break;
                case DomChangeType.TextChanged:
                    ProcessTextChange(change, elementsToInvalidate);
                    break;
                case DomChangeType.ElementStructureChanged:
                    ProcessStructureChange(change, elementsToInvalidate);
                    break;
                case DomChangeType.StylesheetChanged:
                    if (change.Target is IElement targetElement && targetElement.OwnerDocument?.DocumentElement != null)
                    {
                        subtreesToInvalidate.Add(targetElement.OwnerDocument.DocumentElement);
                    }
                    break;
            }
        }

        /// <summary>
        /// Processes a class attribute change.
        /// </summary>
        private void ProcessClassChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            if (change.Target is IElement element)
            {
                AddElementInvalidation(element, null, elementsToInvalidate);

                // If document uses complex selectors (e.g., .class + .sibling),
                // we need to invalidate the parent's children as well
                if (DocumentUsesComplexSelectors(element.OwnerDocument))
                {
                    if (element.ParentElement != null)
                    {
                        AddElementInvalidation(element.ParentElement, null, elementsToInvalidate);

                        // For sibling selectors, we might need to invalidate siblings
                        foreach (var sibling in element.ParentElement.Children)
                        {
                            if (sibling != element)
                            {
                                AddElementInvalidation(sibling as IElement, null, elementsToInvalidate);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes an ID attribute change.
        /// </summary>
        private void ProcessIdChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            if (change.Target is IElement element)
            {
                AddElementInvalidation(element, null, elementsToInvalidate);

                // If the ID changed and it's used as a target for CSS rules,
                // we might need to invalidate the entire document
                if (!string.IsNullOrEmpty(change.OldValue) || !string.IsNullOrEmpty(change.NewValue))
                {
                    if (element.OwnerDocument?.DocumentElement != null)
                    {
                        subtreesToInvalidate.Add(element.OwnerDocument.DocumentElement);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a generic attribute change.
        /// </summary>
        private void ProcessAttributeChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            if (change.Target is IElement element && IsStyleAffectingAttribute(change.AttributeName))
            {
                AddElementInvalidation(element, null, elementsToInvalidate);
            }
        }

        /// <summary>
        /// Processes a node addition.
        /// </summary>
        private void ProcessNodeAddition(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            if (change.Node is IElement addedElement)
            {
                AddElementInvalidation(addedElement, null, elementsToInvalidate);

                if (change.Target is IElement parentElement)
                {
                    AddElementInvalidation(parentElement, null, elementsToInvalidate);

                    // Sibling selectors might be affected
                    foreach (var sibling in parentElement.Children)
                    {
                        if (sibling != addedElement && sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }

                // If a style or link element was added, we might need to invalidate the entire document
                if (addedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                    (addedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                     addedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
                {
                    if (addedElement.OwnerDocument?.DocumentElement != null)
                    {
                        subtreesToInvalidate.Add(addedElement.OwnerDocument.DocumentElement);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a node removal.
        /// </summary>
        private void ProcessNodeRemoval(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            if (change.Node is IElement removedElement)
            {
                if (change.Target is IElement parentElement)
                {
                    AddElementInvalidation(parentElement, null, elementsToInvalidate);

                    // Sibling selectors might be affected
                    foreach (var sibling in parentElement.Children)
                    {
                        if (sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }

                // If a style or link element was removed, we might need to invalidate the entire document
                if (removedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                    (removedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                     removedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
                {
                    if (removedElement.OwnerDocument?.DocumentElement != null)
                    {
                        subtreesToInvalidate.Add(removedElement.OwnerDocument.DocumentElement);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a text change.
        /// </summary>
        private void ProcessTextChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            if (change.Target?.ParentElement is IElement parentElement)
            {
                AddElementInvalidation(parentElement, null, elementsToInvalidate);

                // If the text in a style element changed, we might need to invalidate the document
                if (parentElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase))
                {
                    if (parentElement.OwnerDocument?.DocumentElement != null)
                    {
                        // This will be handled by the StylesheetManager
                    }
                }
            }
        }

        /// <summary>
        /// Processes a structure change.
        /// </summary>
        private void ProcessStructureChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            if (change.Target is IElement element)
            {
                AddElementInvalidation(element, null, elementsToInvalidate);

                if (element.ParentElement != null)
                {
                    // Sibling selectors might be affected
                    foreach (var sibling in element.ParentElement.Children)
                    {
                        if (sibling != element && sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies the collected invalidations to elements and subtrees.
        /// </summary>
        private void ApplyInvalidations(
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            // Apply containment optimizations
            ApplyContainmentOptimizations(elementsToInvalidate, subtreesToInvalidate);

            // Invalidate entire subtrees
            foreach (var root in subtreesToInvalidate)
            {
                InvalidateElement(root);
                InvalidateSubtree(root);
            }

            // Invalidate individual elements
            foreach (var kvp in elementsToInvalidate)
            {
                var element = kvp.Key;
                var properties = kvp.Value;

                if (properties == null || properties.Count == 0)
                {
                    InvalidateElement(element);
                }
                else
                {
                    InvalidateProperties(element, properties);
                }
            }

            // Schedule recalculations if a scheduler is available
            if (_recalcScheduler != null && (subtreesToInvalidate.Count > 0 || elementsToInvalidate.Count > 0))
            {
                foreach (var root in subtreesToInvalidate)
                {
                    _recalcScheduler.ScheduleSubtreeRecalc(root);
                }

                foreach (var element in elementsToInvalidate.Keys)
                {
                    _recalcScheduler.ScheduleElementRecalc(element);
                }
            }
        }

        /// <summary>
        /// Optimizes invalidations based on CSS containment rules.
        /// </summary>
        private void ApplyContainmentOptimizations(
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            var containmentBoundaries = new HashSet<IElement>();
            var elementsToRemove = new HashSet<IElement>();
            var subtreesToRemove = new HashSet<IElement>();

            // Find containment boundaries
            foreach (var element in elementsToInvalidate.Keys.Concat(subtreesToInvalidate))
            {
                var containmentBoundary = FindContainmentBoundary(element);
                if (containmentBoundary != null)
                {
                    containmentBoundaries.Add(containmentBoundary);

                    // If we have a containment boundary and the entire document is already
                    // scheduled for invalidation, we can optimize by removing it
                    if (element.OwnerDocument?.DocumentElement != null &&
                        subtreesToInvalidate.Contains(element.OwnerDocument.DocumentElement))
                    {
                        subtreesToRemove.Add(element.OwnerDocument.DocumentElement);
                    }
                }
            }

            // Apply optimizations
            foreach (var element in elementsToRemove)
            {
                elementsToInvalidate.Remove(element);
            }

            foreach (var element in subtreesToRemove)
            {
                subtreesToInvalidate.Remove(element);
            }

            // Add containment boundaries to subtrees to invalidate
            foreach (var boundary in containmentBoundaries)
            {
                subtreesToInvalidate.Add(boundary);
            }
        }

        /// <summary>
        /// Finds the nearest containment boundary for an element.
        /// </summary>
        private IElement? FindContainmentBoundary(IElement element)
        {
            var current = element;
            while (current.ParentElement != null)
            {
                current = current.ParentElement;
                if (HasStyleContainment(current))
                {
                    return current;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines if an element has style containment.
        /// </summary>
        private bool HasStyleContainment(IElement element)
        {
            var styleAttribute = element.GetAttribute("style");
            if (string.IsNullOrEmpty(styleAttribute))
                return false;

            return styleAttribute.Contains("contain: style") ||
                   styleAttribute.Contains("contain: layout style") ||
                   styleAttribute.Contains("contain: strict") ||
                   styleAttribute.Contains("contain: content");
        }

        /// <summary>
        /// Adds an element to the invalidation collection with optional specific properties.
        /// </summary>
        private void AddElementInvalidation(
            IElement? element,
            HashSet<string>? properties,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            if (element == null)
                return;

            if (!elementsToInvalidate.TryGetValue(element, out var existingProps))
            {
                existingProps = new HashSet<string>();
                elementsToInvalidate[element] = existingProps;
            }

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    existingProps.Add(prop);
                }
            }
        }

        /// <summary>
        /// Determines if an attribute affects styling.
        /// </summary>
        private bool IsStyleAffectingAttribute(string? attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            var globalStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "class", "style", "id", "lang", "dir", "hidden", "tabindex", "draggable", "contenteditable"
            };

            var elementSpecificStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "disabled", "readonly", "checked", "required", "placeholder", "multiple",
                "colspan", "rowspan", "headers", "scope", "border",
                "width", "height", "align", "valign",
                "href", "target", "rel",
                "src", "alt", "title"
            };

            return globalStyleAttributes.Contains(attributeName) ||
                   elementSpecificStyleAttributes.Contains(attributeName);
        }

        /// <summary>
        /// Determines if a document uses complex selectors.
        /// </summary>
        private bool DocumentUsesComplexSelectors(IDocument? document)
        {
            // In a real implementation, we would check the document's stylesheets
            // for complex selectors. For now, we assume all documents use complex selectors.
            return true;
        }

        /// <summary>
        /// Invalidates an element, marking it for style recalculation.
        /// </summary>
        public void InvalidateElement(IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            MarkAsInvalid(element);

            // Also invalidate dependent elements
            if (_dependencies.TryGetValue(element, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    MarkAsInvalid(dependent);
                }
            }
        }

        /// <summary>
        /// Invalidates specific properties of an element.
        /// </summary>
        public void InvalidateProperties(IElement element, IEnumerable<string> properties)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            // For simplicity, we just invalidate the whole element
            InvalidateElement(element);
        }

        /// <summary>
        /// Determines if an element needs style recalculation.
        /// </summary>
        public bool NeedsStyleRecalculation(IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (_invalidationStates.TryGetValue(element, out var state))
            {
                return state == InvalidationState.Invalid;
            }

            // If we don't have a state for the element, assume it needs calculation
            return true;
        }

        /// <summary>
        /// Tracks a style dependency between elements.
        /// </summary>
        public void TrackDependency(IElement dependent, IElement source)
        {
            if (dependent == null)
                throw new ArgumentNullException(nameof(dependent));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!_dependencies.TryGetValue(source, out var dependents))
            {
                dependents = new HashSet<IElement>();
                _dependencies[source] = dependents;
            }

            dependents.Add(dependent);
        }

        /// <summary>
        /// Gets all elements that need style recalculation in a subtree.
        /// </summary>
        public IEnumerable<IElement> GetElementsToUpdate(IElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = new List<IElement>();
            CollectInvalidElements(root, result);
            return result;
        }

        /// <summary>
        /// Marks an element as having up-to-date styles.
        /// </summary>
        public void MarkAsUpToDate(IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            _invalidationStates[element] = InvalidationState.Valid;
        }

        /// <summary>
        /// Determines if an element has up-to-date styles.
        /// </summary>
        public bool IsUpToDate(IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return _invalidationStates.TryGetValue(element, out var state) &&
                   state == InvalidationState.Valid;
        }

        /// <summary>
        /// Marks an element as having device-dependent styles (e.g., viewport units).
        /// </summary>
        public void MarkAsDeviceDependent(IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            _deviceDependentElements.Add(element);
        }

        /// <summary>
        /// Invalidates all device-dependent elements (e.g., after viewport resize).
        /// </summary>
        public void InvalidateForDeviceChange()
        {
            foreach (var element in _deviceDependentElements)
            {
                MarkAsInvalid(element);
            }
        }

        /// <summary>
        /// Invalidates all elements in a subtree.
        /// </summary>
        private void InvalidateSubtree(IElement element)
        {
            if (element == null)
                return;

            foreach (var child in element.Children)
            {
                MarkAsInvalid(child);
                InvalidateSubtree(child);
            }
        }

        /// <summary>
        /// Marks an element as invalid (needing style recalculation).
        /// </summary>
        private void MarkAsInvalid(IElement element)
        {
            if (element == null)
                return;

            _invalidationStates[element] = InvalidationState.Invalid;
        }

        /// <summary>
        /// Collects all invalid elements in a subtree.
        /// </summary>
        private void CollectInvalidElements(IElement element, List<IElement> result)
        {
            if (element == null)
                return;

            if (NeedsStyleRecalculation(element))
            {
                result.Add(element);
            }

            foreach (var child in element.Children)
            {
                CollectInvalidElements(child, result);
            }
        }

        /// <summary>
        /// The state of an element's style validity.
        /// </summary>
        private enum InvalidationState
        {
            Valid,
            Invalid
        }
    }
}