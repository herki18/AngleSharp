using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Interfaces;

namespace AngleSharp.StyleSystem.Integration
{
    public partial class StyleInvalidationTracker : IStyleInvalidationTracker
    {
        private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new Dictionary<IElement, InvalidationState>();
        private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new Dictionary<IElement, HashSet<IElement>>();
        private HashSet<IElement> _deviceDependentElements = new HashSet<IElement>();
        private IStyleRecalcScheduler? _recalcScheduler;

        /// <summary>
        /// Gets or sets the style recalculation scheduler.
        /// </summary>
        public IStyleRecalcScheduler? StyleRecalcScheduler
        {
            get => _recalcScheduler;
            set => _recalcScheduler = value;
        }

        /// <summary>
        /// Processes a collection of DOM changes and invalidates styles accordingly.
        /// </summary>
        /// <param name="changes">The DOM changes to process.</param>
        public void ProcessDomChanges(IEnumerable<DomChange> changes)
        {
            if (changes == null) return;

            var elementsToInvalidate = new Dictionary<IElement, HashSet<string>>();
            var subtreesToInvalidate = new HashSet<IElement>();

            foreach (var change in changes)
            {
                ProcessDomChange(change, elementsToInvalidate, subtreesToInvalidate);
            }

            // Apply all invalidations
            ApplyInvalidations(elementsToInvalidate, subtreesToInvalidate);
        }

        /// <summary>
        /// Processes a single DOM change and determines what needs to be invalidated.
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
                    // A stylesheet change potentially affects the entire document
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
                // Always invalidate the element itself
                AddElementInvalidation(element, null, elementsToInvalidate);

                // Check if we have complex selectors that might be affected by this class change
                if (DocumentUsesComplexSelectors(element.OwnerDocument))
                {
                    // A class change could affect parent elements with :has() selectors
                    // or other elements with complex selectors that match this class
                    if (element.ParentElement != null)
                    {
                        AddElementInvalidation(element.ParentElement, null, elementsToInvalidate);
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
                // Always invalidate the element itself
                AddElementInvalidation(element, null, elementsToInvalidate);

                // Since ID changes could affect any part of the document through ID selectors,
                // we need broader invalidation
                if (element.OwnerDocument?.DocumentElement != null)
                {
                    // Consider finding specific ID selectors in stylesheets for more targeted invalidation
                    // For now, be conservative with a document-wide approach if the ID was actually in use
                    if (!string.IsNullOrEmpty(change.OldValue) || !string.IsNullOrEmpty(change.NewValue))
                    {
                        // We could limit scope here by analyzing stylesheets for ID usage
                        if (element.OwnerDocument?.DocumentElement != null)
                        {
                            subtreesToInvalidate.Add(element.OwnerDocument.DocumentElement);
                        }
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
                // Invalidate the added element itself
                AddElementInvalidation(addedElement, null, elementsToInvalidate);

                // Adding an element can affect sibling selectors and parent states
                if (change.Target is IElement parentElement)
                {
                    // Check for :empty, :only-child, etc.
                    AddElementInvalidation(parentElement, null, elementsToInvalidate);

                    // Invalidate siblings for :nth-child, etc.
                    foreach (var sibling in parentElement.Children)
                    {
                        if (sibling != addedElement && sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }

                // Special handling for style/link elements
                if (addedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                    (addedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                     addedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
                {
                    // This potentially affects the entire document
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
                // For removed elements, we need to invalidate the parent and siblings
                if (change.Target is IElement parentElement)
                {
                    // Check for :empty, :only-child, etc.
                    AddElementInvalidation(parentElement, null, elementsToInvalidate);

                    // Invalidate siblings for :nth-child, etc.
                    foreach (var sibling in parentElement.Children)
                    {
                        if (sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }

                // Special handling for style/link elements
                if (removedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                    (removedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                     removedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
                {
                    // This potentially affects the entire document
                    if (removedElement.OwnerDocument?.DocumentElement != null)
                    {
                        subtreesToInvalidate.Add(removedElement.OwnerDocument.DocumentElement);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a text content change.
        /// </summary>
        private void ProcessTextChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            // Text changes generally only affect the parent element
            if (change.Target?.ParentElement is IElement parentElement)
            {
                AddElementInvalidation(parentElement, null, elementsToInvalidate);

                // For certain elements like <style>, we need special handling
                if (parentElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase))
                {
                    // This potentially affects the entire document
                    if (parentElement.OwnerDocument?.DocumentElement != null)
                    {
                        // Instead of invalidating here, we'd expect the StyleSheetManager
                        // to handle this by properly tracking the stylesheet change
                    }
                }
            }
        }

        /// <summary>
        /// Processes an element structure change.
        /// </summary>
        private void ProcessStructureChange(
            DomChange change,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
            if (change.Target is IElement element)
            {
                // Structure changes can affect :empty, :has(), etc.
                AddElementInvalidation(element, null, elementsToInvalidate);

                // May also affect siblings
                if (element.ParentElement != null)
                {
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
        /// Applies all the collected invalidations.
        /// </summary>
        private void ApplyInvalidations(
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            // First apply containment optimizations
            ApplyContainmentOptimizations(elementsToInvalidate, subtreesToInvalidate);

            // Process subtree invalidations (these are broader)
            foreach (var root in subtreesToInvalidate)
            {
                InvalidateElement(root);
                InvalidateSubtree(root);
            }

            // Process element-specific invalidations
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

            // Schedule recalculation if we have a scheduler
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
        /// Applies containment optimizations to limit the scope of invalidations.
        /// </summary>
        private void ApplyContainmentOptimizations(
            Dictionary<IElement, HashSet<string>> elementsToInvalidate,
            HashSet<IElement> subtreesToInvalidate)
        {
            // Find elements with style containment that can limit invalidation scope
            var containmentBoundaries = new HashSet<IElement>();
            var elementsToRemove = new HashSet<IElement>();
            var subtreesToRemove = new HashSet<IElement>();

            foreach (var element in elementsToInvalidate.Keys.Concat(subtreesToInvalidate))
            {
                var containmentBoundary = FindContainmentBoundary(element);
                if (containmentBoundary != null)
                {
                    containmentBoundaries.Add(containmentBoundary);

                    // If we're invalidating something inside a containment boundary,
                    // we can remove any document-wide invalidations
                    if (element.OwnerDocument?.DocumentElement != null &&
                        subtreesToInvalidate.Contains(element.OwnerDocument.DocumentElement))
                    {
                        subtreesToRemove.Add(element.OwnerDocument.DocumentElement);
                    }
                }
            }

            // Apply the optimizations
            foreach (var element in elementsToRemove)
            {
                elementsToInvalidate.Remove(element);
            }

            foreach (var element in subtreesToRemove)
            {
                subtreesToInvalidate.Remove(element);
            }

            // Add the containment boundaries for subtree invalidation if needed
            foreach (var boundary in containmentBoundaries)
            {
                subtreesToInvalidate.Add(boundary);
            }
        }

        /// <summary>
        /// Finds the nearest containment boundary for an element, if any.
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
        /// Checks if an element has style containment.
        /// </summary>
        private bool HasStyleContainment(IElement element)
        {
            var styleAttribute = element.GetAttribute("style");
            if (string.IsNullOrEmpty(styleAttribute))
                return false;

            // Simple check for containment properties
            return styleAttribute.Contains("contain: style") ||
                   styleAttribute.Contains("contain: layout style") ||
                   styleAttribute.Contains("contain: strict") ||
                   styleAttribute.Contains("contain: content");
        }

        /// <summary>
        /// Helper method to add an element invalidation with optional properties.
        /// </summary>
        private void AddElementInvalidation(
            IElement element,
            HashSet<string>? properties,
            Dictionary<IElement, HashSet<string>> elementsToInvalidate)
        {
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

            // Global attributes that affect styling
            var globalStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "class", "style", "id", "lang", "dir", "hidden", "tabindex", "draggable", "contenteditable"
            };

            // Element-specific attributes that affect presentation
            var elementSpecificStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Form elements
                "disabled", "readonly", "checked", "required", "placeholder", "multiple",

                // Table elements
                "colspan", "rowspan", "headers", "scope", "border",

                // Image elements
                "width", "height", "align", "valign",

                // Link elements
                "href", "target", "rel",

                // Other
                "src", "alt", "title"
            };

            return globalStyleAttributes.Contains(attributeName) ||
                   elementSpecificStyleAttributes.Contains(attributeName);
        }

        /// <summary>
        /// Determines if a document uses complex selectors that might require more conservative invalidation.
        /// </summary>
        private bool DocumentUsesComplexSelectors(IDocument? document)
        {
            // In a real implementation, this would analyze the document's stylesheets
            // to check for complex selectors like :has(), :is(), :where(), etc.

            // For now, assume any document might have complex selectors
            return true;
        }

        // Existing methods that need to be implemented for IStyleInvalidationTracker
        public void InvalidateElement(IElement element)
        {
            MarkAsInvalid(element);
            if (_dependencies.TryGetValue(element, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    MarkAsInvalid(dependent);
                }
            }
        }

        public void InvalidateProperties(IElement element, IEnumerable<string> properties)
        {
            InvalidateElement(element);
        }

        public bool NeedsStyleRecalculation(IElement element)
        {
            if (_invalidationStates.TryGetValue(element, out var state))
            {
                return state == InvalidationState.Invalid;
            }
            return true;
        }

        public void TrackDependency(IElement dependent, IElement source)
        {
            if (!_dependencies.TryGetValue(source, out var dependents))
            {
                dependents = new HashSet<IElement>();
                _dependencies[source] = dependents;
            }
            dependents.Add(dependent);
        }

        public IEnumerable<IElement> GetElementsToUpdate(IElement root)
        {
            var result = new List<IElement>();
            CollectInvalidElements(root, result);
            return result;
        }

        public void MarkAsUpToDate(IElement element)
        {
            _invalidationStates[element] = InvalidationState.Valid;
        }

        public bool IsUpToDate(IElement element)
        {
            return _invalidationStates.TryGetValue(element, out var state) &&
                   state == InvalidationState.Valid;
        }

        private void InvalidateSubtree(IElement element)
        {
            foreach (var child in element.Children)
            {
                MarkAsInvalid(child);
                InvalidateSubtree(child);
            }
        }

        private void MarkAsInvalid(IElement element)
        {
            _invalidationStates[element] = InvalidationState.Invalid;
        }

        private void CollectInvalidElements(IElement element, List<IElement> result)
        {
            if (NeedsStyleRecalculation(element))
            {
                result.Add(element);
            }
            foreach (var child in element.Children)
            {
                CollectInvalidElements(child, result);
            }
        }

        private enum InvalidationState
        {
            Valid,
            Invalid
        }

        public void MarkAsDeviceDependent(IElement element)
        {
            _deviceDependentElements.Add(element);
        }

        public void InvalidateForDeviceChange()
        {
            foreach (var element in _deviceDependentElements)
            {
                MarkAsInvalid(element);
            }
        }
    }
}