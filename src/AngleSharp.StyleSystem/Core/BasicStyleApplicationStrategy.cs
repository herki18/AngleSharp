namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// A basic implementation of the style application strategy that follows a top-down traversal.
/// Implements simple optimizations for display:none elements and potential style sharing.
/// </summary>
public class BasicStyleApplicationStrategy : IStyleApplicationStrategy
{
    private readonly Dictionary<string, List<IElement>> _elementsByTagName = new();
    private readonly IStyleEngine _styleEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicStyleApplicationStrategy"/> class.
    /// </summary>
    /// <param name="styleEngine">The style engine instance.</param>
    public BasicStyleApplicationStrategy(IStyleEngine styleEngine)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
    }

    /// <inheritdoc/>
    public bool ShouldSkipSubtree(IElement element)
    {
        // Skip subtrees of display:none elements
        var style = _styleEngine.ComputeElementStyle(element);
        return style.Display == DisplayMode.None;
    }

    /// <inheritdoc/>
    public bool CanShareStyleWith(IElement target, IElement donor)
    {
        // Basic criteria for style sharing
        return target.NodeName == donor.NodeName &&
               target.ClassName == donor.ClassName &&
               target.Id == donor.Id &&
               target.ParentElement?.NodeName == donor.ParentElement?.NodeName &&
               !HasStyleAffectingAttributes(target) &&
               !HasStyleAffectingAttributes(donor);
    }

    /// <inheritdoc/>
    public IEnumerable<IElement> GetElementTraversalOrder(IElement root)
    {
        // Pre-order traversal (parent before children)
        var queue = new Queue<IElement>();
        queue.Enqueue(root);

        _elementsByTagName.Clear();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Track element by tag name for potential style sharing
            if (!_elementsByTagName.TryGetValue(current.NodeName, out var elements))
            {
                elements = new List<IElement>();
                _elementsByTagName[current.NodeName] = elements;
            }
            elements.Add(current);

            yield return current;

            // Skip traversing children if this subtree can be skipped
            if (ShouldSkipSubtree(current))
                continue;

            // Add all child elements to the queue
            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    /// <inheritdoc/>
    public StyleContext CreateStyleContext(IElement element)
    {
        var context = new StyleContext
        {
            Element = element
        };

        // Set parent style if available
        if (element.ParentElement != null)
        {
            context.ParentStyle = _styleEngine.ComputeElementStyle(element.ParentElement);
        }

        // Check visibility
        if (context.ParentStyle != null)
        {
            context.IsVisible = context.ParentStyle.Display != DisplayMode.None;
        }

        // Look for style donor
        if (_elementsByTagName.TryGetValue(element.NodeName, out var potentialDonors))
        {
            foreach (var donor in potentialDonors)
            {
                if (donor != element && CanShareStyleWith(element, donor))
                {
                    context.StyleDonor = donor;
                    break;
                }
            }
        }

        return context;
    }

    private bool HasStyleAffectingAttributes(IElement element)
    {
        // Attributes that commonly affect styling
        return element.HasAttribute("style") ||
               element.HasAttribute("align") ||
               element.HasAttribute("valign") ||
               element.HasAttribute("width") ||
               element.HasAttribute("height");
    }
}