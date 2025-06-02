namespace LayoutEngine.NG.Layout.Dom;

using System;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using LayoutEngine.NG.Style;

/// <summary>
/// Manages layout data for AngleSharp DOM nodes externally.
/// Uses ConditionalWeakTable to avoid memory leaks - when nodes are GC'd, their layout data is too.
/// </summary>
public class LayoutDataManager
{
    private readonly ConditionalWeakTable<INode, NodeLayout> _nodeLayoutData = new();
    private readonly IServiceProvider _serviceProvider;

    public LayoutDataManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets or creates layout data for a node.
    /// </summary>
    public NodeLayout GetOrCreate(INode node)
    {
        return _nodeLayoutData.GetValue(node, key => CreateLayoutData(key));
    }

    /// <summary>
    /// Gets element-specific layout data.
    /// </summary>
    public ElementLayout GetOrCreate(IElement element)
    {
        return (ElementLayout)GetOrCreate((INode)element);
    }

    /// <summary>
    /// Gets document-specific layout data.
    /// </summary>
    public DocumentLayout GetOrCreate(IDocument document)
    {
        return (DocumentLayout)GetOrCreate((INode)document);
    }

    /// <summary>
    /// Gets text node-specific layout data.
    /// </summary>
    public TextNodeLayout GetOrCreate(IText textNode)
    {
        return (TextNodeLayout)GetOrCreate((INode)textNode);
    }

    /// <summary>
    /// Gets layout data if it exists, null otherwise.
    /// </summary>
    public NodeLayout? GetIfExists(INode node)
    {
        return _nodeLayoutData.TryGetValue(node, out var layout) ? layout : null;
    }

    /// <summary>
    /// Creates appropriate layout data based on node type.
    /// </summary>
    private NodeLayout CreateLayoutData(INode node)
    {
        return node switch
        {
            IDocument document => new DocumentLayout(document, this, _serviceProvider),
            IElement element => new ElementLayout(element, this),
            IText text => new TextNodeLayout(text, this),
            _ => new NodeLayout(node, this)
        };
    }

    /// <summary>
    /// Clears layout data for a node (used in tests or when detaching).
    /// </summary>
    public bool Remove(INode node)
    {
        return _nodeLayoutData.Remove(node);
    }
}