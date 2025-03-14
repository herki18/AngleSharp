namespace AngleSharp.StyleSystem.Properties;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;

/// <summary>
/// Basic implementation of IStyleApplicationStrategy that determines how styles
/// are applied to elements.
/// </summary>
public class BasicStyleApplicationStrategy : IStyleApplicationStrategy
{
    private readonly IStyleEngine _styleEngine;

    /// <summary>
    /// Creates a new BasicStyleApplicationStrategy with the specified style engine.
    /// </summary>
    /// <param name="styleEngine">The style engine to use.</param>
    public BasicStyleApplicationStrategy(IStyleEngine styleEngine)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
    }

    /// <inheritdoc />
    public bool ShouldSkipSubtree(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Skip SCRIPT, STYLE, NOSCRIPT elements
        if (element.NodeName.Equals("SCRIPT", StringComparison.OrdinalIgnoreCase) ||
            element.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
            element.NodeName.Equals("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Skip elements that are not displayed (display: none)
        var displayAttr = element.GetAttribute("style");
        if (displayAttr != null && displayAttr.Contains("display: none"))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool CanShareStyleWith(IElement target, IElement donor)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (donor == null)
            throw new ArgumentNullException(nameof(donor));

        // Check basic identity criteria for style sharing

        // 1. Must be the same element type
        if (target.NodeName != donor.NodeName)
            return false;

        // 2. Must have same ID, class, and style attributes
        if (target.Id != donor.Id)
            return false;

        if (target.ClassName != donor.ClassName)
            return false;

        if (target.GetAttribute("style") != donor.GetAttribute("style"))
            return false;

        // 3. Must have same parent structure for inheritance
        if (target.ParentElement?.NodeName != donor.ParentElement?.NodeName)
            return false;

        // 4. Must have the same context-dependent attributes
        var contextAttrs = new[] { "disabled", "checked", "required", "selected" };
        foreach (var attr in contextAttrs)
        {
            if (target.HasAttribute(attr) != donor.HasAttribute(attr))
                return false;
        }

        // 5. Must have same structural position
        if (GetElementPosition(target) != GetElementPosition(donor))
            return false;

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<IElement> GetElementTraversalOrder(IElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        // Implement breadth-first traversal
        var queue = new Queue<IElement>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            // Skip traversing into subtrees we should skip
            if (ShouldSkipSubtree(current))
                continue;

            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    /// <inheritdoc />
    public StyleContext CreateStyleContext(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var context = new StyleContext
        {
            Element = element,
            IsInDocumentFlow = IsInDocumentFlow(element),
            IsVisible = IsVisible(element),
            HasContent = HasContent(element)
        };

        // Get parent style
        if (element.ParentElement != null)
        {
            try
            {
                context.ParentStyle = _styleEngine.ComputeElementStyle(element.ParentElement);
            }
            catch (Exception)
            {
                // If parent style can't be computed, proceed without it
                context.ParentStyle = null;
            }
        }

        // Check for style donor
        var siblings = element.ParentElement?.Children
            .Where(e => e != element && _styleEngine.InvalidationTracker.IsUpToDate(e))
            .ToList();

        if (siblings != null)
        {
            foreach (var sibling in siblings)
            {
                if (CanShareStyleWith(element, sibling))
                {
                    context.StyleDonor = sibling;
                    break;
                }
            }
        }

        // Check for containment
        context.IsContained = HasStyleContainment(element);
        if (context.IsContained)
        {
            context.ContainmentRoot = element;
        }
        else
        {
            context.ContainmentRoot = FindContainmentRoot(element);
        }

        return context;
    }

    private bool IsInDocumentFlow(IElement element)
    {
        if (element.GetAttribute("style")?.Contains("position: absolute") == true ||
            element.GetAttribute("style")?.Contains("position: fixed") == true)
        {
            return false;
        }

        return true;
    }

    private bool IsVisible(IElement element)
    {
        if (element.GetAttribute("style")?.Contains("display: none") == true ||
            element.GetAttribute("style")?.Contains("visibility: hidden") == true ||
            element.GetAttribute("hidden") != null)
        {
            return false;
        }

        return true;
    }

    private bool HasContent(IElement element)
    {
        return element.ChildNodes.Length > 0 || !string.IsNullOrEmpty(element.TextContent);
    }

    private int GetElementPosition(IElement element)
    {
        if (element.ParentElement == null)
            return 0;

        int index = 0;
        foreach (var child in element.ParentElement.Children)
        {
            if (child == element)
                return index;
            index++;
        }

        return -1;
    }

    private bool HasStyleContainment(IElement element)
    {
        var styleAttr = element.GetAttribute("style");
        return styleAttr != null && (
            styleAttr.Contains("contain: style") ||
            styleAttr.Contains("contain: layout style") ||
            styleAttr.Contains("contain: strict") ||
            styleAttr.Contains("contain: content")
        );
    }

    private IElement? FindContainmentRoot(IElement element)
    {
        var current = element.ParentElement;
        while (current != null)
        {
            if (HasStyleContainment(current))
                return current;

            current = current.ParentElement;
        }

        return null;
    }
}