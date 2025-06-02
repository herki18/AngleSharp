namespace LayoutEngine.NG.Layout;

using LayoutEngine.NG.Style;
using System;
using AngleSharp.Dom;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Manages layout tree building, attachment, and detachment following BlinkNG patterns.
/// In BlinkNG: layout_tree_builder.cc
/// </summary>
public class LayoutTreeBuilder
{
    private readonly IDocument _document;
    private readonly LayoutDataManager _layoutDataManager;

    public LayoutTreeBuilder(IDocument document, LayoutDataManager layoutDataManager)
    {
        _document = document;
        _layoutDataManager = layoutDataManager;
    }

    /// <summary>
    /// Rebuilds the layout tree for nodes marked for reattachment.
    /// In BlinkNG: LayoutTreeBuilder::Rebuild()
    /// </summary>
    public void Rebuild()
    {
        var docLayout = _layoutDataManager.GetOrCreate(_document);
        var rebuildRoot = docLayout.LayoutTreeRebuildRoot.GetRootNode();
        if (rebuildRoot == null)
            return;

        // Create attachment context
        var context = new AttachmentContext();

        // Rebuild from the root
        RebuildInternal(rebuildRoot, context);

        // Clear the rebuild root
        docLayout.LayoutTreeRebuildRoot.Clear();
    }

    private void RebuildInternal(INode node, AttachmentContext context)
    {
        if (node is IElement element)
        {
            var style = _layoutDataManager.GetOrCreate(element).GetComputedStyle();

            // Update layout object based on current style
            UpdateLayoutObject(element, style, context);

            // Process children if needed
            if (_layoutDataManager.GetOrCreate(element).ChildNeedsReattach())
            {
                RebuildChildrenInternal(element, context);
            }
        }
        else if (node is IText textNode)
        {
            // Handle text node attachment
            UpdateTextLayoutObject(textNode, context);
        }

        // Clear reattachment flags
        _layoutDataManager.GetOrCreate(node).ClearNeedsReattach();
    }

    private void RebuildChildrenInternal(IElement element, AttachmentContext context)
    {
        // Process all children
        foreach (var child in element.ChildNodes)
        {
            RebuildInternal(child, context);
        }

        _layoutDataManager.GetOrCreate(element).ClearChildNeedsReattach();
    }

    /// <summary>
    /// Updates the layout object for an element based on its style.
    /// </summary>
    private void UpdateLayoutObject(IElement element, ComputedStyle? style, AttachmentContext context)
    {
        var elementLayout = _layoutDataManager.GetOrCreate(element);
        var currentLayoutObject = elementLayout.LayoutObject;
        var shouldHaveLayoutObject = style != null && ShouldCreateLayoutObject(element, style, _layoutDataManager);

        if (shouldHaveLayoutObject && currentLayoutObject == null)
        {
            // Need to create layout object
            AttachLayoutObject(element, style!, context);
        }
        else if (!shouldHaveLayoutObject && currentLayoutObject != null)
        {
            // Need to destroy layout object
            DetachLayoutObject(element, context);
        }
        else if (currentLayoutObject != null && style != null)
        {
            // Update existing layout object's style
            currentLayoutObject.Style = style;
        }
    }

    /// <summary>
    /// Updates the layout object for a text node.
    /// </summary>
    private void UpdateTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var textLayout = _layoutDataManager.GetOrCreate(textNode);
        var currentLayoutObject = textLayout.LayoutObject;
        var shouldHaveLayoutObject = ShouldCreateLayoutObjectForText(textNode);

        if (shouldHaveLayoutObject && currentLayoutObject == null)
        {
            // Create text layout object
            AttachTextLayoutObject(textNode, context);
        }
        else if (!shouldHaveLayoutObject && currentLayoutObject != null)
        {
            // Destroy text layout object
            DetachTextLayoutObject(textNode, context);
        }
    }

    /// <summary>
    /// Determines if an element needs a layout object.
    /// In BlinkNG: Element::LayoutObjectIsNeeded()
    /// </summary>
    public static bool ShouldCreateLayoutObject(IElement element, ComputedStyle style, LayoutDataManager layoutDataManager)
    {
        // display:none elements don't get layout objects
        if (style.Display == DisplayType.None)
            return false;

        // display:contents elements don't get layout objects
        if (style.Display == DisplayType.Contents)
            return false;

        // Check if we're in a context that allows layout objects
        if (!CanAttachLayoutObject(element, layoutDataManager))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if we can attach a layout object in the current context.
    /// </summary>
    private static bool CanAttachLayoutObject(IElement element, LayoutDataManager layoutDataManager)
    {
        // Check if we have a parent layout object to attach to
        var parent = element.ParentElement;
        while (parent != null)
        {
            var parentLayout = layoutDataManager.GetOrCreate(parent);
            if (parentLayout.LayoutObject != null)
                return true;

            // display:contents parents are transparent to layout tree
            var parentStyle = parentLayout.GetComputedStyle();
            if (parentStyle?.Display != DisplayType.Contents)
                return false;

            parent = parent.ParentElement;
        }

        // Document element can always attach
        return element == element.OwnerDocument?.DocumentElement;
    }

    /// <summary>
    /// Creates and attaches a layout object for the element.
    /// </summary>
    public void AttachLayoutObject(IElement element, ComputedStyle style, AttachmentContext context)
    {
        // Create appropriate layout object based on display type
        var layoutObject = CreateLayoutObject(element, style);

        // Set the style
        layoutObject.Style = style;

        // Store in element
        _layoutDataManager.GetOrCreate(element).LayoutObject = layoutObject;

        // Attach to parent
        var parentLayoutObject = FindParentLayoutObject(element);
        if (parentLayoutObject != null)
        {
            var beforeChild = FindNextLayoutObject(element);
            AddChild(parentLayoutObject, layoutObject, beforeChild);
        }

        // Attach children
        AttachChildLayoutObjects(element, context);
    }

    /// <summary>
    /// Creates the appropriate layout object type for the element.
    /// </summary>
    private LayoutObject CreateLayoutObject(IElement element, ComputedStyle style)
    {
        // Simplified - in real implementation would create proper subclasses
        switch (style.Display)
        {
            case DisplayType.Block:
                return new LayoutBlockFlow { Node = element };
            case DisplayType.Inline:
                return new LayoutInline { Node = element };
            case DisplayType.InlineBlock:
                return new LayoutInlineBlock { Node = element };
            case DisplayType.Flex:
            case DisplayType.Grid: // Simplified
            case DisplayType.Table: // Simplified
            case DisplayType.ListItem: // Simplified
            default:
                return new LayoutBlockFlow { Node = element }; // Simplified
        }
    }

    /// <summary>
    /// Detaches and destroys the layout object for an element.
    /// </summary>
    private void DetachLayoutObject(IElement element, AttachmentContext context)
    {
        var elementLayout = _layoutDataManager.GetOrCreate(element);
        var layoutObject = elementLayout.LayoutObject;
        if (layoutObject == null)
            return;

        // First detach all children
        DetachChildLayoutObjects(element, context);

        // Remove from parent
        RemoveChild(layoutObject.Parent, layoutObject);

        // Clear the layout object
        elementLayout.LayoutObject = null;
    }

    /// <summary>
    /// Attaches layout objects for all children.
    /// </summary>
    private void AttachChildLayoutObjects(IElement element, AttachmentContext context)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child is IElement childElement)
            {
                var childLayout = _layoutDataManager.GetOrCreate(childElement);
                var childStyle = childLayout.GetComputedStyle();
                UpdateLayoutObject(childElement, childStyle, context);
            }
            else if (child is IText textNode)
            {
                UpdateTextLayoutObject(textNode, context);
            }
        }
    }

    /// <summary>
    /// Detaches layout objects for all children.
    /// </summary>
    private void DetachChildLayoutObjects(IElement element, AttachmentContext context)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child is IElement childElement)
            {
                var childLayout = _layoutDataManager.GetOrCreate(childElement);
                if (childLayout.LayoutObject != null)
                {
                    DetachLayoutObject(childElement, context);
                }
            }
            else if (child is IText textNode)
            {
                var textLayout = _layoutDataManager.GetOrCreate(textNode);
                if (textLayout.LayoutObject != null)
                {
                    DetachTextLayoutObject(textNode, context);
                }
            }
        }
    }

    /// <summary>
    /// Determines if a text node needs a layout object.
    /// In BlinkNG: Text::TextLayoutObjectIsNeeded()
    /// </summary>
    private bool ShouldCreateLayoutObjectForText(IText textNode)
    {
        // Empty text nodes don't need layout objects
        if (string.IsNullOrEmpty(textNode.TextContent))
            return false;

        // Check if it's all whitespace and can be collapsed
        if (IsCollapsibleWhitespace(textNode))
            return false;

        // Check if parent can contain text
        var parent = FindParentLayoutObject(textNode);
        if (parent == null)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if text node contains only collapsible whitespace.
    /// </summary>
    private bool IsCollapsibleWhitespace(IText textNode)
    {
        if (!string.IsNullOrWhiteSpace(textNode.TextContent))
            return false;

        // Check parent's white-space style
        var parent = textNode.ParentElement;
        if (parent != null)
        {
            var parentLayout = _layoutDataManager.GetOrCreate(parent);
            var style = parentLayout.GetComputedStyle();

            // Preserve whitespace for pre/pre-wrap
            if (style?.GetPropertyValue("white-space") is string ws &&
                (ws == "pre" || ws == "pre-wrap"))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Attaches a layout object for a text node.
    /// </summary>
    private void AttachTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var layoutText = new LayoutText
        {
            Node = textNode,
            Text = textNode.TextContent
        };

        _layoutDataManager.GetOrCreate(textNode).LayoutObject = layoutText;

        // Attach to parent
        var parentLayout = FindParentLayoutObject(textNode);
        if (parentLayout != null)
        {
            var beforeChild = FindNextLayoutObject(textNode);
            AddChild(parentLayout, layoutText, beforeChild);
        }
    }

    /// <summary>
    /// Detaches a layout object for a text node.
    /// </summary>
    private void DetachTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var textLayout = _layoutDataManager.GetOrCreate(textNode);
        var layoutObject = textLayout.LayoutObject;
        if (layoutObject == null)
            return;

        RemoveChild(layoutObject.Parent, layoutObject);
        textLayout.LayoutObject = null;
    }

    /// <summary>
    /// Finds the parent layout object, handling display:contents.
    /// </summary>
    private LayoutObject? FindParentLayoutObject(INode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is IElement parentElement)
            {
                var parentLayout = _layoutDataManager.GetOrCreate(parentElement);
                var parentLayoutObject = parentLayout.LayoutObject;
                if (parentLayoutObject != null)
                    return parentLayoutObject;

                // Skip display:contents parents
                var parentStyle = parentLayout.GetComputedStyle();
                if (parentStyle?.Display != DisplayType.Contents)
                    return null;
            }
            parent = parent.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds the next sibling's layout object for insertion order.
    /// </summary>
    private LayoutObject? FindNextLayoutObject(INode node)
    {
        var sibling = node.NextSibling;
        while (sibling != null)
        {
            var siblingLayout = _layoutDataManager.GetOrCreate(sibling);
            var siblingLayoutObject = siblingLayout.LayoutObject;
            if (siblingLayoutObject != null)
                return siblingLayoutObject;
            sibling = sibling.NextSibling;
        }
        return null;
    }

    /// <summary>
    /// Adds a child to a parent layout object.
    /// </summary>
    private void AddChild(LayoutObject parent, LayoutObject child, LayoutObject? beforeChild)
    {
        child.Parent = parent;

        if (beforeChild != null)
        {
            // Insert before the specified child
            child.NextSibling = beforeChild;
            child.PreviousSibling = beforeChild.PreviousSibling;

            if (beforeChild.PreviousSibling != null)
                beforeChild.PreviousSibling.NextSibling = child;
            else
                parent.FirstChild = child;

            beforeChild.PreviousSibling = child;
        }
        else
        {
            // Append to the end
            if (parent.FirstChild == null)
            {
                parent.FirstChild = child;
            }
            else
            {
                var lastChild = parent.FirstChild;
                while (lastChild.NextSibling != null)
                    lastChild = lastChild.NextSibling;

                lastChild.NextSibling = child;
                child.PreviousSibling = lastChild;
            }
        }
    }

    /// <summary>
    /// Removes a child from its parent.
    /// </summary>
    private void RemoveChild(LayoutObject? parent, LayoutObject child)
    {
        if (parent == null)
            return;

        if (child.PreviousSibling != null)
            child.PreviousSibling.NextSibling = child.NextSibling;
        else if (parent.FirstChild == child)
            parent.FirstChild = child.NextSibling;

        if (child.NextSibling != null)
            child.NextSibling.PreviousSibling = child.PreviousSibling;

        child.Parent = null;
        child.NextSibling = null;
        child.PreviousSibling = null;
    }
}

/// <summary>
/// Context for layout tree attachment operations.
/// In BlinkNG: AttachContext
/// </summary>
public class AttachmentContext
{
    /// <summary>
    /// Whether we're doing a full document attachment.
    /// </summary>
    public bool IsDocumentAttachment { get; set; }

    /// <summary>
    /// Whether we're in a reattachment operation.
    /// </summary>
    public bool IsReattachment { get; set; }

    /// <summary>
    /// Style recalc context if we're attaching during style recalc.
    /// </summary>
    public StyleRecalcContext? StyleRecalcContext { get; set; }
}