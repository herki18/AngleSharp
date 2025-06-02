namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using LayoutEngine.NG.Layout;
using System;
using Layout.Dom;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Style engine implementation following BlinkNG patterns.
/// Works with AngleSharp DOM nodes through LayoutDataManager.
/// </summary>
public class StyleEngine
{
    private readonly DocumentLayout _documentLayout;
    private readonly LayoutDataManager _layoutDataManager;
    private readonly IStyleResolver _styleResolver;
    private readonly LayoutTreeBuilder _layoutTreeBuilder;
    internal readonly StyleRecalcRoot StyleRecalcRoot;
    internal readonly StyleRecalcRoot LayoutTreeRebuildRoot;

    /// <summary>
    /// Creates a StyleEngine for the given document layout.
    /// In BlinkNG: StyleEngine::StyleEngine(Document& document)
    /// </summary>
    public StyleEngine(DocumentLayout documentLayout, LayoutDataManager layoutDataManager, IServiceProvider serviceProvider)
    {
        _documentLayout = documentLayout ?? throw new ArgumentNullException(nameof(documentLayout));
        _layoutDataManager = layoutDataManager ?? throw new ArgumentNullException(nameof(layoutDataManager));

        // Get the style resolver from DI
        _styleResolver = serviceProvider.GetService(typeof(IStyleResolver)) as IStyleResolver
            ?? throw new InvalidOperationException("IStyleResolver not registered");

        // Create layout tree builder
        _layoutTreeBuilder = new LayoutTreeBuilder(_documentLayout.Document, _layoutDataManager);

        StyleRecalcRoot = new StyleRecalcRoot();
        LayoutTreeRebuildRoot = new StyleRecalcRoot();
    }

    /// <summary>
    /// Gets the style resolver.
    /// </summary>
    public IStyleResolver StyleResolver => _styleResolver;

    /// <summary>
    /// Checks if style recalc is needed.
    /// In BlinkNG: bool NeedsStyleRecalc() const { return style_recalc_root_.GetRootNode(); }
    /// </summary>
    public bool NeedsStyleRecalc()
    {
        return StyleRecalcRoot.GetRootNode() != null;
    }

    /// <summary>
    /// Checks if layout tree rebuild is needed.
    /// In BlinkNG: bool NeedsLayoutTreeRebuild() const { return layout_tree_rebuild_root_.GetRootNode(); }
    /// </summary>
    public bool NeedsLayoutTreeRebuild()
    {
        return LayoutTreeRebuildRoot.GetRootNode() != null;
    }

    /// <summary>
    /// Updates style and layout tree.
    /// In BlinkNG: void UpdateStyleAndLayoutTree()
    /// </summary>
    public void UpdateStyleAndLayoutTree()
    {
        // Phase 1: Style recalculation
        if (NeedsStyleRecalc())
        {
            RecalcStyle();
        }

        // Phase 2: Layout tree rebuild for elements marked during style recalc
        if (NeedsLayoutTreeRebuild())
        {
            RebuildLayoutTree();
        }

        // Phase 3: Ensure document element has layout object if needed
        EnsureDocumentElementLayoutObject();
    }

    /// <summary>
    /// Performs style recalculation.
    /// In BlinkNG: void RecalcStyle()
    /// </summary>
    public void RecalcStyle()
    {
        var rootNode = StyleRecalcRoot.GetRootNode();
        if (rootNode == null)
            return;

        // Create initial context
        var context = StyleRecalcContext.FromAncestors(rootNode as IElement, _layoutDataManager);
        var change = new StyleRecalcChange(StyleRecalcPropagate.RecalcDescendants);

        // In BlinkNG, this starts the tree traversal from the recalc root
        RecalcStyleInternal(rootNode, change, context);

        // Clear the recalc root after processing
        StyleRecalcRoot.Clear();
    }

    /// <summary>
    /// Internal style recalc implementation.
    /// </summary>
    private void RecalcStyleInternal(INode node, StyleRecalcChange change, StyleRecalcContext context)
    {
        if (node is IElement element)
        {
            // Recalc the element's style
            // This may mark nodes for reattachment
            var elementLayout = _layoutDataManager.GetOrCreate(element);
            elementLayout.RecalcStyle(change, context);
        }
        else if (node is IText textNode)
        {
            // Text nodes might need reattachment based on parent style
            if (change.ShouldRecalcStyleFor(textNode, _layoutDataManager))
            {
                var textLayout = _layoutDataManager.GetOrCreate(textNode);
                textLayout.ClearNeedsStyleRecalc();
            }
        }
        else
        {
            // For other node types, just clear the flag
            var nodeLayout = _layoutDataManager.GetOrCreate(node);
            nodeLayout.ClearNeedsStyleRecalc();
        }
    }

    /// <summary>
    /// Rebuilds the layout tree.
    /// In BlinkNG: void RebuildLayoutTree()
    /// </summary>
    public void RebuildLayoutTree()
    {
        _layoutTreeBuilder.Rebuild();
    }

    /// <summary>
    /// Ensures the document element has a layout object if needed.
    /// Called after style/layout updates to handle initial attachment.
    /// </summary>
    private void EnsureDocumentElementLayoutObject()
    {
        var docElement = _documentLayout.Document.DocumentElement;
        if (docElement == null)
            return;

        var elementLayout = _layoutDataManager.GetOrCreate(docElement);
        var style = elementLayout.GetComputedStyle();
        if (style == null)
            return;

        // Check if document element needs a layout object
        if (elementLayout.LayoutObject == null &&
            LayoutTreeBuilder.ShouldCreateLayoutObject(docElement, style, _layoutDataManager))
        {
            // Attach layout tree for document element
            var context = new AttachmentContext
            {
                IsDocumentAttachment = true
            };
            _layoutTreeBuilder.AttachLayoutObject(docElement, style, context);
        }
    }

    /// <summary>
    /// Sets that a node needs style recalculation.
    /// Called from NodeLayout when a node is marked dirty.
    /// </summary>
    public void SetNeedsStyleRecalc(INode node, StyleChangeType changeType)
    {
        if (changeType == StyleChangeType.NoChange)
            return;

        // Update the recalc root
        // In BlinkNG: style_recalc_root_.Update(parent, node)
        StyleRecalcRoot.Update(node.ParentNode, node);
    }

    /// <summary>
    /// Sets that a node needs layout tree rebuild.
    /// Called from NodeLayout when marked for reattachment.
    /// </summary>
    public void SetNeedsLayoutTreeRebuild(INode node)
    {
        // Update the rebuild root
        LayoutTreeRebuildRoot.Update(node.ParentNode, node);
    }

    /// <summary>
    /// Container query style recalc.
    /// In BlinkNG: void UpdateStyleForContainer(Element& container, ...)
    /// </summary>
    public void UpdateStyleForContainer(IElement container)
    {
        // Set up for container query recalc
        var containerLayout = _layoutDataManager.GetOrCreate(container);
        containerLayout.SetChildNeedsStyleRecalc();
        StyleRecalcRoot.Update(null, container);

        var context = StyleRecalcContext.FromAncestors(container, _layoutDataManager);
        context.IsContainerQueryRecalc = true;

        var change = new StyleRecalcChange().SuppressRecalc();
        RecalcStyleInternal(container, change, context);
    }

    /// <summary>
    /// Updates viewport-dependent styles.
    /// In BlinkNG: void UpdateViewport()
    /// </summary>
    public void UpdateViewport()
    {
        // In BlinkNG, this would:
        // 1. Re-evaluate media queries
        // 2. Update viewport units
        // 3. Mark affected elements for style recalc

        // For now, mark document element for recalc if it exists
        var docElement = _documentLayout.Document.DocumentElement;
        if (docElement != null)
        {
            var elementLayout = _layoutDataManager.GetOrCreate(docElement);
            elementLayout.SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);
        }
    }

    /// <summary>
    /// Forces a full document style recalc and layout tree rebuild.
    /// Used for major changes like stylesheet additions.
    /// </summary>
    public void MarkAllElementsForStyleRecalc()
    {
        var docElement = _documentLayout.Document.DocumentElement;
        if (docElement != null)
        {
            // Mark entire document tree
            MarkSubtreeForStyleRecalc(docElement, StyleChangeType.LocalStyleChange);
        }
    }

    private void MarkSubtreeForStyleRecalc(IElement element, StyleChangeType changeType)
    {
        var elementLayout = _layoutDataManager.GetOrCreate(element);
        elementLayout.SetNeedsStyleRecalc(changeType);

        foreach (var child in element.ChildNodes)
        {
            if (child is IElement childElement)
            {
                MarkSubtreeForStyleRecalc(childElement, changeType);
            }
        }
    }
}

/// <summary>
/// Tracks the root of the subtree that needs style recalc or layout tree rebuild.
/// In BlinkNG, this is StyleRecalcRoot class.
/// </summary>
public class StyleRecalcRoot
{
    private INode? _rootNode;

    /// <summary>
    /// Gets the current recalc root node.
    /// In BlinkNG: Node* GetRootNode() const
    /// </summary>
    public INode? GetRootNode() => _rootNode;

    /// <summary>
    /// Updates the recalc root.
    /// In BlinkNG: void Update(Node* parent, Node* node)
    /// </summary>
    public void Update(INode? parent, INode node)
    {
        // If no root yet, this node is the root
        if (_rootNode == null)
        {
            _rootNode = node;
            return;
        }

        // If the new node is an ancestor of current root, it becomes the new root
        if (IsAncestor(node, _rootNode))
        {
            _rootNode = node;
            return;
        }

        // If current root is ancestor of new node, keep current root
        if (IsAncestor(_rootNode, node))
        {
            return;
        }

        // Otherwise, find common ancestor
        _rootNode = FindCommonAncestor(_rootNode, node);
    }

    /// <summary>
    /// Clears the recalc root.
    /// In BlinkNG: void Clear()
    /// </summary>
    public void Clear()
    {
        _rootNode = null;
    }

    private bool IsAncestor(INode possibleAncestor, INode node)
    {
        var current = node.ParentNode;
        while (current != null)
        {
            if (current == possibleAncestor)
                return true;
            current = current.ParentNode;
        }
        return false;
    }

    private INode? FindCommonAncestor(INode node1, INode node2)
    {
        // Build path from node1 to root
        var path1 = new System.Collections.Generic.List<INode>();
        var current = node1;
        while (current != null)
        {
            path1.Add(current);
            current = current.ParentNode;
        }

        // Find first node in path that is ancestor of node2
        foreach (var ancestor in path1)
        {
            if (IsAncestor(ancestor, node2) || ancestor == node2)
                return ancestor;
        }

        return null;
    }
}