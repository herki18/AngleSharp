namespace LayoutEngine.NG.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Dom;
using LayoutEngine.NG.Layout;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Style engine implementation following BlinkNG patterns.
/// Manages stylesheets, rule indexing, and style invalidation for a document.
/// Works with AngleSharp DOM nodes through LayoutDataManager.
/// </summary>
public class StyleEngine
{
    private readonly DocumentEngineData _documentEngineData;
    private readonly LayoutDataManager _layoutDataManager;
    private readonly IStyleResolver _styleResolver;
    private readonly LayoutTreeBuilder _layoutTreeBuilder;

    // Stylesheet collections by origin
    private readonly List<StyleSheetContents> _userAgentStyleSheets = new();
    private readonly List<StyleSheetContents> _userStyleSheets = new();
    private readonly List<StyleSheetContents> _authorStyleSheets = new();

    // Maps ICssStyleSheet to our wrapper for quick lookup
    private readonly Dictionary<ICssStyleSheet, StyleSheetContents> _styleSheetMap = new();

    // Element rule collector for style resolution
    private readonly ElementRuleCollector _elementRuleCollector;

    // Existing recalc roots
    internal readonly StyleRecalcRoot StyleRecalcRoot;
    internal readonly StyleRecalcRoot LayoutTreeRebuildRoot;

    /// <summary>
    /// Creates a StyleEngine for the given document layout.
    /// In BlinkNG: StyleEngine::StyleEngine(Document& document)
    /// </summary>
    public StyleEngine(
        DocumentEngineData documentEngineData,
        LayoutDataManager layoutDataManager,
        IServiceProvider serviceProvider)
    {
        _documentEngineData = documentEngineData ?? throw new ArgumentNullException(nameof(documentEngineData));
        _layoutDataManager = layoutDataManager ?? throw new ArgumentNullException(nameof(layoutDataManager));

        // Get the style resolver from DI
        _styleResolver = serviceProvider.GetService(typeof(IStyleResolver)) as IStyleResolver
            ?? throw new InvalidOperationException("IStyleResolver not registered");

        // Create layout tree builder
        _layoutTreeBuilder = new LayoutTreeBuilder(_documentEngineData.Document, _layoutDataManager);

        // Create element rule collector
        _elementRuleCollector = new ElementRuleCollector(this, _layoutDataManager);

        StyleRecalcRoot = new StyleRecalcRoot();
        LayoutTreeRebuildRoot = new StyleRecalcRoot();

        // Initialize user agent stylesheets
        InitializeUserAgentStyleSheets(serviceProvider);

        // Watch for document stylesheets
        WatchDocumentStyleSheets();
    }

    #region Stylesheet Management

    /// <summary>
    /// Called when a stylesheet is added to the document.
    /// In BlinkNG: StyleEngine::AddStyleSheet
    /// </summary>
    internal void DidAddStyleSheet(ICssStyleSheet styleSheet, StylesheetOrigin origin)
    {
        // Check if already tracked
        if (_styleSheetMap.ContainsKey(styleSheet))
            return;

        // Create wrapper
        var contents = new StyleSheetContents(styleSheet, origin);
        _styleSheetMap[styleSheet] = contents;

        // Add to appropriate collection
        switch (origin)
        {
            case StylesheetOrigin.UserAgent:
                _userAgentStyleSheets.Add(contents);
                break;
            case StylesheetOrigin.User:
                _userStyleSheets.Add(contents);
                break;
            case StylesheetOrigin.Author:
                _authorStyleSheets.Add(contents);
                break;
        }

        // Mark entire document for style recalc
        MarkAllElementsForStyleRecalc();
    }

    /// <summary>
    /// Called when a stylesheet is removed from the document.
    /// In BlinkNG: StyleEngine::RemoveStyleSheet
    /// </summary>
    internal void DidRemoveStyleSheet(ICssStyleSheet styleSheet)
    {
        if (!_styleSheetMap.TryGetValue(styleSheet, out var contents))
            return;

        // Remove from collections
        _userAgentStyleSheets.Remove(contents);
        _userStyleSheets.Remove(contents);
        _authorStyleSheets.Remove(contents);
        _styleSheetMap.Remove(styleSheet);

        // Mark entire document for style recalc
        MarkAllElementsForStyleRecalc();
    }

    /// <summary>
    /// Called when a stylesheet's content changes.
    /// In BlinkNG: StyleEngine::SetNeedsStyleRecalc
    /// </summary>
    internal void DidModifyStyleSheet(ICssStyleSheet styleSheet)
    {
        if (_styleSheetMap.TryGetValue(styleSheet, out var contents))
        {
            // Invalidate parsed rules
            contents.InvalidateRules();

            // TODO: Use invalidation analysis to mark only affected elements
            MarkAllElementsForStyleRecalc();
        }
    }

    /// <summary>
    /// Gets active author stylesheets for the current document state.
    /// In BlinkNG: StyleEngine::GetActiveAuthorStyleSheets
    /// </summary>
    internal IEnumerable<StyleSheetContents> GetActiveAuthorStyleSheets()
    {
        return _authorStyleSheets.Where(s => !s.IsDisabled);
    }

    /// <summary>
    /// Gets user agent stylesheets.
    /// </summary>
    internal IEnumerable<StyleSheetContents> GetUserAgentStyleSheets()
    {
        return _userAgentStyleSheets;
    }

    /// <summary>
    /// Gets user stylesheets.
    /// </summary>
    internal IEnumerable<StyleSheetContents> GetUserStyleSheets()
    {
        return _userStyleSheets;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes user agent stylesheets from AngleSharp's defaults.
    /// </summary>
    private void InitializeUserAgentStyleSheets(IServiceProvider serviceProvider)
    {
        // Get AngleSharp's default stylesheets
        var context = _documentEngineData.Document.Context;
        if (context != null)
        {
            var defaultProviders = context.GetServices<ICssDefaultStyleSheetProvider>();
            foreach (var provider in defaultProviders)
            {
                if (provider.Default != null)
                {
                    DidAddStyleSheet(provider.Default, StylesheetOrigin.UserAgent);
                }
            }
        }
    }

    /// <summary>
    /// Sets up watching for document stylesheet changes.
    /// </summary>
    private void WatchDocumentStyleSheets()
    {
        // Process existing stylesheets
        foreach (var sheet in _documentEngineData.Document.StyleSheets.OfType<ICssStyleSheet>())
        {
            DidAddStyleSheet(sheet, StylesheetOrigin.Author);
        }

        // TODO: Set up mutation observer for stylesheet changes
    }

    #endregion

    #region Style Resolution Support

    /// <summary>
    /// Gets the element rule collector for style resolution.
    /// Used by StyleResolver to collect matching rules.
    /// </summary>
    public ElementRuleCollector ElementRuleCollector => _elementRuleCollector;

    #endregion

    #region Element Style Data (using ElementLayout)

    /// <summary>
    /// Notifies that an element's inline style changed.
    /// In BlinkNG, this is handled by Element::StyleAttributeChanged
    /// </summary>
    public void ElementInlineStyleChanged(IElement element, string? oldValue, string? newValue)
    {
        var elementLayout = _layoutDataManager.GetOrCreate(element);

        // Mark element for style recalc
        elementLayout.SetNeedsStyleRecalc(StyleChangeType.LocalStyleChange);

        // Clear any cached inline style in ElementLayout
        // TODO: Implement inline style caching in ElementLayout
    }

    #endregion

    #region Style and Layout Tree Update Logic

    /// <summary>
    /// Gets the style resolver.
    /// </summary>
    public IStyleResolver StyleResolver => _styleResolver;

    /// <summary>
    /// Checks if style recalc is needed.
    /// </summary>
    public bool NeedsStyleRecalc()
    {
        return StyleRecalcRoot.GetRootNode() != null;
    }

    /// <summary>
    /// Checks if layout tree rebuild is needed.
    /// </summary>
    public bool NeedsLayoutTreeRebuild()
    {
        return LayoutTreeRebuildRoot.GetRootNode() != null;
    }

    /// <summary>
    /// Updates style and layout tree.
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
        var docElement = _documentEngineData.Document.DocumentElement;
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
        StyleRecalcRoot.Update(node.Parent, node);
    }

    /// <summary>
    /// Sets that a node needs layout tree rebuild.
    /// Called from NodeLayout when marked for reattachment.
    /// </summary>
    public void SetNeedsLayoutTreeRebuild(INode node)
    {
        // Update the rebuild root
        LayoutTreeRebuildRoot.Update(node.Parent, node);
    }

    /// <summary>
    /// Container query style recalc.
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
    /// </summary>
    public void UpdateViewport()
    {
        // In BlinkNG, this would:
        // 1. Re-evaluate media queries
        // 2. Update viewport units
        // 3. Mark affected elements for style recalc

        // For now, mark document element for recalc if it exists
        var docElement = _documentEngineData.Document.DocumentElement;
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
        var docElement = _documentEngineData.Document.DocumentElement;
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

    #endregion
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
        var current = node.Parent;
        while (current != null)
        {
            if (current == possibleAncestor)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private INode? FindCommonAncestor(INode node1, INode node2)
    {
        // Build path from node1 to root
        var path1 = new List<INode>();
        var current = node1;
        while (current != null)
        {
            path1.Add(current);
            current = current.Parent;
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