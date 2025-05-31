namespace LayoutEngine.Core.LayoutNG.Internal;

using System;
using AngleSharp.Dom;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using Public;

/// <summary>
/// Builds the layout tree from the DOM tree.
/// </summary>
public class LayoutTreeBuilder
{
    private readonly IStyleSystem _styleSystem;
    private readonly ILogger<LayoutTreeBuilder> _logger;

    public LayoutTreeBuilder(IStyleSystem styleSystem, ILogger<LayoutTreeBuilder> logger)
    {
        _styleSystem = styleSystem ?? throw new ArgumentNullException(nameof(styleSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a layout tree from a DOM element.
    /// </summary>
    public ILayoutObject? BuildLayoutTree(IElement element)
    {
        var style = _styleSystem.GetComputedStyle(element);
        if (style == null)
        {
            _logger.LogWarning("No computed style for element {TagName}", element.TagName);
            return null;
        }

        // Check if element generates a layout object
        var display = style.GetPropertyValue("display");
        if (display == "none")
        {
            return null; // No layout object for display:none
        }

        // Create appropriate layout object based on display type
        var layoutObject = CreateLayoutObject(element, style, display);
        if (layoutObject == null)
        {
            return null;
        }

        // Set the style
        layoutObject.Style = style;

        // Process children if this is a container
        if (layoutObject is ILayoutContainer container)
        {
            BuildChildrenForContainer(element, container);

            // Create anonymous wrappers if needed
            container.CreateAnonymousWrappersIfNeeded();
        }

        return layoutObject;
    }

    /// <summary>
    /// Creates the appropriate layout object type based on display value.
    /// </summary>
    private ILayoutObject? CreateLayoutObject(IElement element, IComputedStyle style, string display)
    {
        switch (display)
        {
            case "block":
            case "list-item":
            case "table":
            case "table-caption":
                return new LayoutBlock(element);

            case "inline":
                return new LayoutInline(element);

            case "inline-block":
            case "inline-table":
                // These create LayoutBlock with special inline-level behavior
                var inlineBlock = new LayoutBlock(element);
                // Mark as inline-level (would need a property for this)
                return inlineBlock;

            case "flex":
            case "inline-flex":
                return new LayoutFlex(element);

            case "table-row":
            case "table-cell":
            case "table-row-group":
            case "table-header-group":
            case "table-footer-group":
                // Table layout objects - simplified for now
                return new LayoutBlock(element);

            case "none":
                return null;

            default:
                _logger.LogWarning("Unknown display value: {Display}, defaulting to block", display);
                return new LayoutBlock(element);
        }
    }

    /// <summary>
    /// Builds children for a container layout object.
    /// </summary>
    private void BuildChildrenForContainer(INode node, ILayoutContainer container)
    {
        foreach (var childNode in node.ChildNodes)
        {
            ILayoutObject? childLayout = null;

            switch ((int)childNode.NodeType)
            {
                case (int)NodeType.Element:
                    if (childNode is IElement childElement)
                    {
                        childLayout = BuildLayoutTree(childElement);
                    }
                    break;

                case (int)NodeType.Text:
                    if (childNode is IText textNode)
                    {
                        // Only create layout object for non-empty text
                        var text = textNode.TextContent;
                        if (!string.IsNullOrWhiteSpace(text) || !ShouldIgnoreWhitespace(container))
                        {
                            childLayout = new LayoutText(textNode);

                            // Text nodes inherit style from parent
                            if (container.Style != null)
                            {
                                childLayout.Style = container.Style;
                            }
                        }
                    }
                    break;

                case (int)NodeType.Comment:
                case (int)NodeType.ProcessingInstruction:
                    // Skip these node types
                    continue;
            }

            if (childLayout != null)
            {
                container.AddChild(childLayout);
            }
        }
    }

    /// <summary>
    /// Determines if whitespace-only text nodes should be ignored.
    /// </summary>
    private bool ShouldIgnoreWhitespace(ILayoutContainer container)
    {
        if (container.Style == null)
            return true;

        var whiteSpace = container.Style.GetPropertyValue("white-space");
        return whiteSpace != "pre" && whiteSpace != "pre-wrap";
    }

    /// <summary>
    /// Updates the layout tree when the DOM changes.
    /// </summary>
    public void UpdateLayoutTree(ILayoutObject layoutObject, INode node)
    {
        if (layoutObject.Node != node)
        {
            throw new InvalidOperationException("Layout object does not match DOM node");
        }

        // Re-compute style
        if (node is IElement element)
        {
            var oldStyle = layoutObject.Style;
            var newStyle = _styleSystem.GetComputedStyle(element);

            if (newStyle != null)
            {
                layoutObject.UpdateStyle(oldStyle, newStyle);

                // Check if we need to rebuild the subtree due to display change
                var oldDisplay = oldStyle?.GetPropertyValue("display");
                var newDisplay = newStyle.GetPropertyValue("display");

                if (oldDisplay != newDisplay)
                {
                    // Display change requires rebuilding
                    RebuildLayoutObject(layoutObject, element);
                }
            }
        }

        // Update children if this is a container
        if (layoutObject is ILayoutContainer container)
        {
            UpdateContainerChildren(container, node);
        }
    }

    /// <summary>
    /// Rebuilds a layout object when significant style changes occur.
    /// </summary>
    private void RebuildLayoutObject(ILayoutObject oldObject, IElement element)
    {
        var parent = oldObject.Parent as ILayoutContainer;
        if (parent == null)
        {
            _logger.LogWarning("Cannot rebuild root layout object");
            return;
        }

        // Build new layout object
        var newObject = BuildLayoutTree(element);
        if (newObject == null)
        {
            // Element is now display:none
            oldObject.Remove();
            return;
        }

        // Replace old with new
        var nextSibling = GetNextSibling(parent, oldObject);
        parent.RemoveChild(oldObject);
        parent.InsertChild(newObject, nextSibling);

        // Destroy old object
        oldObject.Destroy();
    }

    /// <summary>
    /// Updates children of a container when DOM changes.
    /// </summary>
    private void UpdateContainerChildren(ILayoutContainer container, INode node)
    {
        // This is a simplified version
        // A full implementation would diff the children and only update what changed

        // For now, rebuild all children
        container.RemoveAllChildren();
        BuildChildrenForContainer(node, container);
        container.CreateAnonymousWrappersIfNeeded();
    }

    private ILayoutObject? GetNextSibling(ILayoutContainer container, ILayoutObject child)
    {
        bool foundChild = false;
        foreach (var sibling in container.Children)
        {
            if (foundChild)
                return sibling;
            if (sibling == child)
                foundChild = true;
        }
        return null;
    }
}