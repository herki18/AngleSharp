namespace LayoutEngine.NG.Layout;

using LayoutEngine.NG.Style;
using System;
using System.Net.Mime;
using AngleSharp.Dom;

/// <summary>
/// Manages layout tree building, attachment, and detachment following BlinkNG patterns.
/// In BlinkNG: layout_tree_builder.cc
/// </summary>
internal class LayoutTreeBuilder
{
    private readonly IDocument _document;

    public LayoutTreeBuilder(IDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Rebuilds the layout tree for nodes marked for reattachment.
    /// In BlinkNG: LayoutTreeBuilder::Rebuild()
    /// </summary>
    public void Rebuild()
    {
        var rebuildRoot = _document.LayoutData.LayoutTreeRebuildRoot.GetRootNode();
        if (rebuildRoot == null)
            return;

        // Create attachment context
        var context = new AttachmentContext();

        // Rebuild from the root
        RebuildInternal(rebuildRoot, context);

        // Clear the rebuild root
        _document.LayoutData.LayoutTreeRebuildRoot.Clear();
    }

    private void RebuildInternal(Node node, AttachmentContext context)
    {
        if (node is IElement element)
        {
            var style = element.LayoutData.GetComputedStyle();

            // Update layout object based on current style
            UpdateLayoutObject(element, style, context);

            // Process children if needed
            if (element.LayoutData.ChildNeedsReattach())
            {
                RebuildChildrenInternal(element, context);
            }
        }
        else if (node is MediaTypeNames.Text textNode)
        {
            // Handle text node attachment
            UpdateTextLayoutObject(textNode, context);
        }

        // Clear reattachment flags
        node.LayoutData.ClearNeedsReattach();
    }

    private void RebuildChildrenInternal(IElement element, AttachmentContext context)
    {
        // Process all children
        foreach (var child in element.ChildNodes)
        {
            RebuildInternal(child, context);
        }

        element.LayoutData.ClearChildNeedsReattach();
    }

    /// <summary>
    /// Updates the layout object for an element based on its style.
    /// </summary>
    private void UpdateLayoutObject(IElement element, ComputedStyle? style, AttachmentContext context)
    {
        var currentLayoutObject = element.LayoutData.LayoutObject;
        var shouldHaveLayoutObject = style != null && ShouldCreateLayoutObject(element, style);

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
            currentLayoutObject.SetStyle(style);
        }
    }

    /// <summary>
    /// Updates the layout object for a text node.
    /// </summary>
    private void UpdateTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var currentLayoutObject = textNode.LayoutData.LayoutObject;
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
    private bool ShouldCreateLayoutObject(IElement element, ComputedStyle style)
    {
        // display:none elements don't get layout objects
        if (style.Display == DisplayType.None)
            return false;

        // display:contents elements don't get layout objects
        if (style.Display == DisplayType.Contents)
            return false;

        // Check if we're in a context that allows layout objects
        if (!CanAttachLayoutObject(element))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if we can attach a layout object in the current context.
    /// </summary>
    private bool CanAttachLayoutObject(IElement element)
    {
        // Check if we have a parent layout object to attach to
        var parent = element.ParentNode as IElement;
        while (parent != null)
        {
            if (parent.LayoutData.LayoutObject != null)
                return true;

            // display:contents parents are transparent to layout tree
            var parentStyle = parent.LayoutData.GetComputedStyle();
            if (parentStyle?.Display != DisplayType.Contents)
                return false;

            parent = parent.ParentNode as IElement;
        }

        // Document element can always attach
        return element == _document.DocumentElement;
    }

    /// <summary>
    /// Creates and attaches a layout object for the element.
    /// </summary>
    private void AttachLayoutObject(IElement element, ComputedStyle style, AttachmentContext context)
    {
        // Create appropriate layout object based on display type
        var layoutObject = CreateLayoutObject(element, style);

        // Set the style
        layoutObject.SetStyle(style);

        // Store in element
        element.LayoutData.LayoutObject = layoutObject;

        // Attach to parent
        var parentLayoutObject = FindParentLayoutObject(element);
        if (parentLayoutObject != null)
        {
            var beforeChild = FindNextLayoutObject(element);
            parentLayoutObject.AddChild(layoutObject, beforeChild);
        }

        // Attach children
        AttachChildLayoutObjects(element, context);
    }

    /// <summary>
    /// Creates the appropriate layout object type for the element.
    /// </summary>
    private LayoutObject CreateLayoutObject(IElement element, ComputedStyle style)
    {
        return style.Display switch
        {
            DisplayType.Block => new LayoutBlock(element),
            DisplayType.Inline => new LayoutInline(element),
            DisplayType.InlineBlock => new LayoutBlock(element) { IsInlineBlock = true },
            DisplayType.Flex => new LayoutBlock(element), // Simplified
            DisplayType.Grid => new LayoutBlock(element), // Simplified
            DisplayType.Table => new LayoutBlock(element), // Simplified
            DisplayType.ListItem => new LayoutBlock(element), // Simplified
            _ => new LayoutBlock(element)
        };
    }

    /// <summary>
    /// Detaches and destroys the layout object for an element.
    /// </summary>
    private void DetachLayoutObject(IElement element, AttachmentContext context)
    {
        var layoutObject = element.LayoutData.LayoutObject;
        if (layoutObject == null)
            return;

        // First detach all children
        DetachChildLayoutObjects(element, context);

        // Remove from parent
        layoutObject.Parent?.RemoveChild(layoutObject);

        // Destroy the layout object
        layoutObject.Destroy();
        element.LayoutData.LayoutObject = null;
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
                var childStyle = childElement.LayoutData.GetComputedStyle();
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
            if (child is IElement childElement && childElement.LayoutData.LayoutObject != null)
            {
                DetachLayoutObject(childElement, context);
            }
            else if (child is IText textNode && textNode.LayoutData.LayoutObject != null)
            {
                DetachTextLayoutObject(textNode, context);
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
        if (textNode.Length == 0)
            return false;

        // Check if it's all whitespace and can be collapsed
        if (IsCollapsibleWhitespace(textNode))
            return false;

        // Check if parent can contain text
        var parent = FindParentLayoutObject(textNode);
        if (parent == null)
            return false;

        // Some layout objects can't have text children
        if (!parent.CanHaveChildren())
            return false;

        return true;
    }

    /// <summary>
    /// Checks if text node contains only collapsible whitespace.
    /// </summary>
    private bool IsCollapsibleWhitespace(IText textNode)
    {
        if (!textNode.IsWhitespaceOnly)
            return false;

        // Check parent's white-space style
        var parent = textNode.ParentNode as IElement;
        var style = parent?.LayoutData.GetComputedStyle();

        // Preserve whitespace for pre/pre-wrap
        if (style?.WhiteSpace == WhiteSpaceType.Pre ||
            style?.WhiteSpace == WhiteSpaceType.PreWrap)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attaches a layout object for a text node.
    /// </summary>
    private void AttachTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var layoutText = new LayoutText(textNode);
        textNode.LayoutData.LayoutObject = layoutText;

        // Attach to parent
        var parentLayout = FindParentLayoutObject(textNode);
        if (parentLayout != null)
        {
            var beforeChild = FindNextLayoutObject(textNode);
            parentLayout.AddChild(layoutText, beforeChild);
        }
    }

    /// <summary>
    /// Detaches a layout object for a text node.
    /// </summary>
    private void DetachTextLayoutObject(IText textNode, AttachmentContext context)
    {
        var layoutObject = textNode.LayoutData.LayoutObject;
        if (layoutObject == null)
            return;

        layoutObject.Parent?.RemoveChild(layoutObject);
        layoutObject.Destroy();
        textNode.LayoutData.LayoutObject = null;
    }

    /// <summary>
    /// Finds the parent layout object, handling display:contents.
    /// </summary>
    private LayoutObject? FindParentLayoutObject(INode node)
    {
        var parent = node.ParentNode;
        while (parent != null)
        {
            if (parent is IElement parentElement)
            {
                var parentLayoutObject = parentElement.LayoutData.LayoutObject;
                if (parentLayoutObject != null)
                    return parentLayoutObject;

                // Skip display:contents parents
                var parentStyle = parentElement.LayoutData.GetComputedStyle();
                if (parentStyle?.Display != DisplayType.Contents)
                    return null;
            }

            parent = parent.ParentNode;
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
            var siblingLayout = sibling.LayoutData.LayoutObject;
            if (siblingLayout != null)
                return siblingLayout;

            sibling = sibling.NextSibling;
        }

        return null;
    }
}

/// <summary>
/// Context for layout tree attachment operations.
/// In BlinkNG: AttachContext
/// </summary>
internal class AttachmentContext
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