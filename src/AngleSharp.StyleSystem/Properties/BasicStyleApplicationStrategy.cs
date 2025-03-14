namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;

/// <summary>
/// Provides a basic strategy for style application with optimizations.
/// </summary>
public class BasicStyleApplicationStrategy : IStyleApplicationStrategy
{
    private readonly IStyleEngine _styleEngine;
    private readonly ConcurrentDictionary<string, bool> _displayNoneCache = new();

    /// <summary>
    /// List of element types that can be skipped for styling in most scenarios.
    /// </summary>
    private static readonly HashSet<string> _nonstyledElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "SCRIPT", "NOSCRIPT", "STYLE", "LINK", "META", "HEAD", "TITLE",
        "TEMPLATE", "IFRAME", "OBJECT", "PARAM", "EMBED"
    };

    /// <summary>
    /// List of attributes that affect styling.
    /// </summary>
    private static readonly HashSet<string> _styleAffectingAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "style", "class", "id", "align", "valign", "width", "height",
        "bgcolor", "color", "border", "cellspacing", "cellpadding",
        "colspan", "rowspan", "disabled", "readonly", "checked", "selected",
        "hidden", "data-theme", "aria-hidden", "aria-disabled", "title"
    };

    /// <summary>
    /// CSS properties that inhibit style sharing.
    /// </summary>
    private static readonly HashSet<string> _nonSharableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "position", "float", "display", "z-index", "opacity", "transform",
        "animation", "transition", "visibility", "box-shadow", "filter"
    };

    /// <summary>
    /// Creates a new BasicStyleApplicationStrategy.
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

        // Skip non-styled elements like script, style, etc.
        if (_nonstyledElements.Contains(element.NodeName))
            return true;

        // Skip elements with hidden attribute
        if (element.HasAttribute("hidden") ||
            element.HasAttribute("aria-hidden") && element.GetAttribute("aria-hidden") == "true")
            return true;

        // Check inline style for display:none
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(styleAttr))
        {
            var cacheKey = $"{element.GetHashCode()}:{styleAttr}";

            if (!_displayNoneCache.TryGetValue(cacheKey, out bool hasDisplayNone))
            {
                hasDisplayNone = ContainsDisplayNone(styleAttr);
                _displayNoneCache[cacheKey] = hasDisplayNone;
            }

            if (hasDisplayNone)
                return true;
        }

        // Check computed style for display:none
        try
        {
            var existingStyle = GetExistingComputedStyle(element);
            if (existingStyle != null && existingStyle.Display == DisplayMode.None)
                return true;
        }
        catch
        {
            // Couldn't get computed style, continue with traversal
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

        // Basic identity checks
        if (target.NodeName != donor.NodeName)
            return false;

        if (target.Id != donor.Id)
            return false;

        if (target.ClassName != donor.ClassName)
            return false;

        // Check all style-affecting attributes
        foreach (var attr in target.Attributes)
        {
            if (_styleAffectingAttributes.Contains(attr.Name))
            {
                var donorValue = donor.GetAttribute(attr.Name);
                if (attr.Value != donorValue)
                    return false;
            }
        }

        // Check if there are any donor attributes not in target
        foreach (var attr in donor.Attributes)
        {
            if (_styleAffectingAttributes.Contains(attr.Name))
            {
                if (!target.HasAttribute(attr.Name))
                    return false;
            }
        }

        // Ensure they have the same parent
        if (target.ParentElement != donor.ParentElement)
            return false;

        // Check structural position
        if (!HaveSameStructuralPosition(target, donor))
            return false;

        // Check computed style for non-sharable properties
        var donorStyle = GetExistingComputedStyle(donor);
        if (donorStyle != null)
        {
            foreach (var property in _nonSharableProperties)
            {
                var value = donorStyle.GetPropertyValue(property);
                if (!string.IsNullOrEmpty(value) && value != "initial" && value != "normal")
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<IElement> GetElementTraversalOrder(IElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        // Use level-order traversal (breadth-first) since it's typically more efficient
        // for style computation due to the likelihood of similar elements being at the same level
        var result = new List<IElement>();
        var queue = new Queue<IElement>();

        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (!ShouldSkipSubtree(current))
            {
                foreach (var child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public StyleContext CreateStyleContext(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var context = new StyleContext
        {
            Element = element,
            IsInDocumentFlow = true,
            IsVisible = true,
            HasContent = HasContent(element),
            IsContained = false
        };

        // Get parent style and inheritance information
        if (element.ParentElement != null)
        {
            try
            {
                IComputedStyle? parentStyle = null;

                // Try to get parent's existing computed style first to avoid recomputation
                parentStyle = GetExistingComputedStyle(element.ParentElement);

                // If we couldn't get an existing style, compute it
                if (parentStyle == null)
                {
                    parentStyle = _styleEngine.ComputeElementStyle(element.ParentElement);
                }

                context.ParentStyle = parentStyle;

                // Check if element is visible based on parent styles
                if (parentStyle.Display == DisplayMode.None)
                {
                    context.IsVisible = false;
                }
                else if (parentStyle.GetPropertyValue("visibility") == "hidden")
                {
                    context.IsVisible = false;
                }

                // Check if element is in normal flow based on parent styles
                var parentPosition = parentStyle.Position;
                if (parentPosition == PositionMode.Absolute || parentPosition == PositionMode.Fixed)
                {
                    context.IsInDocumentFlow = false;
                }

                // Check for containment
                var contain = parentStyle.GetPropertyValue("contain");
                if (contain.Contains("style") || contain == "strict" || contain == "content")
                {
                    context.IsContained = true;
                    context.ContainmentRoot = element.ParentElement;
                }
            }
            catch
            {
                // If we can't compute the parent style, continue without it
            }
        }

        // Look for style sharing opportunities
        FindStyleDonor(element, context);

        return context;
    }

    #region Helper Methods

    /// <summary>
    /// Checks if the given CSS rule contains display:none.
    /// </summary>
    /// <param name="styleText">The CSS text to check.</param>
    /// <returns>True if display:none is found; otherwise, false.</returns>
    private bool ContainsDisplayNone(string styleText)
    {
        if (string.IsNullOrEmpty(styleText))
            return false;

        // Simple string check for display:none without parsing
        return styleText.Contains("display", StringComparison.OrdinalIgnoreCase) &&
               styleText.Contains("none", StringComparison.OrdinalIgnoreCase) &&
               // Check for the typical format
               (styleText.Contains("display:none", StringComparison.OrdinalIgnoreCase) ||
                styleText.Contains("display: none", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the given elements have the same structural position in the DOM.
    /// </summary>
    /// <param name="element1">The first element.</param>
    /// <param name="element2">The second element.</param>
    /// <returns>True if the elements have the same structural position; otherwise, false.</returns>
    private bool HaveSameStructuralPosition(IElement element1, IElement element2)
    {
        if (element1.ParentElement != element2.ParentElement)
            return false;

        var parent = element1.ParentElement;
        if (parent == null)
            return true; // Both are top-level elements

        // Check sibling position
        bool isFirstChild1 = parent.FirstElementChild == element1;
        bool isFirstChild2 = parent.FirstElementChild == element2;
        if (isFirstChild1 != isFirstChild2)
            return false;

        bool isLastChild1 = parent.LastElementChild == element1;
        bool isLastChild2 = parent.LastElementChild == element2;
        if (isLastChild1 != isLastChild2)
            return false;

        // Check for nth-child pseudo-class matches
        int index1 = GetChildIndex(parent, element1);
        int index2 = GetChildIndex(parent, element2);

        // Check for odd/even positions, which can affect nth-child selectors
        bool isOdd1 = index1 % 2 == 1;
        bool isOdd2 = index2 % 2 == 1;
        if (isOdd1 != isOdd2)
            return false;

        // Check for common pseudo-class patterns
        if (IsSpecialPosition(index1, parent.ChildElementCount) !=
            IsSpecialPosition(index2, parent.ChildElementCount))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the index of a child element within its parent.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="child">The child element.</param>
    /// <returns>The index of the child element, or -1 if not found.</returns>
    private int GetChildIndex(IElement parent, IElement child)
    {
        int index = 0;
        foreach (var element in parent.Children)
        {
            if (element == child)
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Checks if the given index represents a special position in the element sequence.
    /// </summary>
    /// <param name="index">The element index.</param>
    /// <param name="count">The total count of elements.</param>
    /// <returns>A bit mask of special positions.</returns>
    private int IsSpecialPosition(int index, int count)
    {
        int result = 0;

        if (index == 0)
            result |= 1; // First

        if (index == count - 1)
            result |= 2; // Last

        if (index < 3)
            result |= 4; // Among first three

        if (index >= count - 3)
            result |= 8; // Among last three

        return result;
    }

    /// <summary>
    /// Checks if the given element has content that needs styling.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element has content; otherwise, false.</returns>
    private bool HasContent(IElement element)
    {
        // Check for text content
        if (!string.IsNullOrWhiteSpace(element.TextContent))
            return true;

        // Check for children
        if (element.ChildElementCount > 0)
            return true;

        // Check for background or border
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(styleAttr))
        {
            if (styleAttr.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                styleAttr.Contains("border", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check dimension attributes
        if (element.HasAttribute("width") || element.HasAttribute("height"))
            return true;

        return false;
    }

    /// <summary>
    /// Gets the existing computed style for an element, if available.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The computed style, or null if not available.</returns>
    private IComputedStyle? GetExistingComputedStyle(IElement element)
    {
        // Check if the element has a cached computed style
        if (element.TryGetExtension<IComputedStyle>("_computedStyle", out var style))
        {
            return style;
        }

        return null;
    }

    /// <summary>
    /// Finds a suitable style donor for the given element and updates the context.
    /// </summary>
    /// <param name="element">The element to find a donor for.</param>
    /// <param name="context">The style context to update.</param>
    private void FindStyleDonor(IElement element, StyleContext context)
    {
        var parent = element.ParentElement;
        if (parent == null)
            return;

        // Look for similar siblings that might be style donors
        foreach (var sibling in parent.Children)
        {
            if (sibling == element)
                continue;

            if (CanShareStyleWith(element, sibling))
            {
                context.StyleDonor = sibling;

                // Check if the donor already has a computed style
                var donorStyle = GetExistingComputedStyle(sibling);
                if (donorStyle != null)
                {
                    // Store the donor's style directly on the element for quick access
                    // This is an optimization to avoid recalculating styles
                    element.SetExtension("_computedStyle", donorStyle);
                }

                return;
            }
        }

        // If we didn't find a suitable sibling, look for cousins (children of parent's siblings)
        // that might be good style donors (especially useful for table cells, list items, etc.)
        if (parent.ParentElement != null)
        {
            foreach (var parentSibling in parent.ParentElement.Children)
            {
                if (parentSibling == parent || parentSibling.NodeName != parent.NodeName)
                    continue;

                foreach (var cousin in parentSibling.Children)
                {
                    if (cousin.NodeName == element.NodeName &&
                        CanShareStyleWith(element, cousin))
                    {
                        context.StyleDonor = cousin;
                        return;
                    }
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for DOM elements.
/// </summary>
internal static class ElementExtensions
{
    private static readonly ConditionalWeakTable<IElement, Dictionary<string, object>> _extensions =
        new ConditionalWeakTable<IElement, Dictionary<string, object>>();

    /// <summary>
    /// Sets an extension value on an element.
    /// </summary>
    /// <param name="element">The element to set the extension on.</param>
    /// <param name="key">The extension key.</param>
    /// <param name="value">The extension value.</param>
    public static void SetExtension(this IElement element, string key, object value)
    {
        var extensions = _extensions.GetOrCreateValue(element);
        extensions[key] = value;
    }

    /// <summary>
    /// Tries to get an extension value from an element.
    /// </summary>
    /// <typeparam name="T">The expected type of the extension value.</typeparam>
    /// <param name="element">The element to get the extension from.</param>
    /// <param name="key">The extension key.</param>
    /// <param name="value">The extension value, if found.</param>
    /// <returns>True if the extension was found; otherwise, false.</returns>
    public static bool TryGetExtension<T>(this IElement element, string key, out T? value)
        where T : class
    {
        if (_extensions.TryGetValue(element, out var extensions) &&
            extensions.TryGetValue(key, out var obj) &&
            obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Removes an extension from an element.
    /// </summary>
    /// <param name="element">The element to remove the extension from.</param>
    /// <param name="key">The extension key.</param>
    /// <returns>True if the extension was removed; otherwise, false.</returns>
    public static bool RemoveExtension(this IElement element, string key)
    {
        if (_extensions.TryGetValue(element, out var extensions))
        {
            return extensions.Remove(key);
        }

        return false;
    }
}