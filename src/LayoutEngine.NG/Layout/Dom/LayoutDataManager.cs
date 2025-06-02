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
    private readonly ConditionalWeakTable<INode, NodeEngineData> _nodeLayoutData = new();
    private readonly IServiceProvider _serviceProvider;

    public LayoutDataManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets or creates layout data for a node.
    /// </summary>
    public NodeEngineData GetOrCreate(INode node)
    {
        return _nodeLayoutData.GetValue(node, key => CreateLayoutData(key));
    }

    /// <summary>
    /// Gets element-specific layout data.
    /// </summary>
    public ElementEngineData GetOrCreate(IElement element)
    {
        return (ElementEngineData)GetOrCreate((INode)element);
    }

    /// <summary>
    /// Gets document-specific layout data.
    /// </summary>
    public DocumentEngineData GetOrCreate(IDocument document)
    {
        return (DocumentEngineData)GetOrCreate((INode)document);
    }

    /// <summary>
    /// Gets text node-specific layout data.
    /// </summary>
    public TextNodeEngineData GetOrCreate(IText textNode)
    {
        return (TextNodeEngineData)GetOrCreate((INode)textNode);
    }

    /// <summary>
    /// Gets layout data if it exists, null otherwise.
    /// </summary>
    public NodeEngineData? GetIfExists(INode node)
    {
        return _nodeLayoutData.TryGetValue(node, out var layout) ? layout : null;
    }

    /// <summary>
    /// Creates appropriate layout data based on node type.
    /// </summary>
    private NodeEngineData CreateLayoutData(INode node)
    {
        return node switch
        {
            IDocument document => new DocumentEngineData(document, this, _serviceProvider),
            IElement element => new ElementEngineData(element, this),
            IText text => new TextNodeEngineData(text, this),
            _ => new NodeEngineData(node, this)
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