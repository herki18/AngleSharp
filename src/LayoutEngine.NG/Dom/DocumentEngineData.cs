namespace LayoutEngine.NG.Dom;

using System;
using AngleSharp.Dom;

/// <summary>
/// Layout data specific to documents.
/// </summary>
public class DocumentEngineData : NodeEngineData
{
    private readonly Style.StyleEngine _styleEngine;

    public DocumentEngineData(IDocument document, LayoutDataManager manager, IServiceProvider serviceProvider)
        : base(document, manager)
    {
        // In BlinkNG, Document creates and owns StyleEngine
        _styleEngine = new Style.StyleEngine(this, manager, serviceProvider);
    }

    public IDocument Document => (IDocument)base._node;

    /// <summary>
    /// The style engine for this document.
    /// </summary>
    public Style.StyleEngine StyleEngine => _styleEngine;

    /// <summary>
    /// Style recalc root tracking.
    /// </summary>
    public Style.StyleRecalcRoot StyleRecalcRoot => _styleEngine.StyleRecalcRoot;

    /// <summary>
    /// Layout tree rebuild root tracking.
    /// </summary>
    public Style.StyleRecalcRoot LayoutTreeRebuildRoot => _styleEngine.LayoutTreeRebuildRoot;

    /// <summary>
    /// Updates style and layout tree.
    /// </summary>
    public void UpdateStyleAndLayoutTree()
    {
        _styleEngine.UpdateStyleAndLayoutTree();
    }

    /// <summary>
    /// Checks if any nodes need style recalc.
    /// </summary>
    public new bool NeedsStyleRecalc() => _styleEngine.NeedsStyleRecalc();

    /// <summary>
    /// Checks if layout tree needs rebuild.
    /// </summary>
    public bool NeedsLayoutTreeRebuild() => _styleEngine.NeedsLayoutTreeRebuild();

    /// <summary>
    /// Sets that a node needs layout tree rebuild.
    /// </summary>
    public void SetNeedsLayoutTreeRebuild(INode node)
    {
        _styleEngine.SetNeedsLayoutTreeRebuild(node);
    }
}