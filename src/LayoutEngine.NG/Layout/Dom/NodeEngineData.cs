using AngleSharp.Dom;
using LayoutEngine.NG.Style;

namespace LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Per-node engine state container for DOM nodes (elements, text, document, etc).
/// This class stores all engine-specific data needed for style, layout, and rendering,
/// such as dirty flags, computed style, and other internal state.
///
/// This data is kept separate from the DOM node itself (e.g., AngleSharp's INode)
/// to avoid polluting the DOM implementation and to keep engine concerns decoupled.
///
/// This is NOT a layout object and NOT a DOM node.
/// It is purely an internal data holder for the engine.
/// </summary>
public class NodeEngineData
{
    protected readonly INode _node;
    protected readonly LayoutDataManager _manager;
    private StyleChangeType _styleChangeType = StyleChangeType.NoChange;
    private bool _childNeedsStyleRecalc = false;
    private bool _childNeedsReattach = false;
    private bool _forceReattachLayoutTree = false;
    private bool _needsReattachLayoutTree = false;
    private LayoutEngine.NG.Layout.LayoutObject? _layoutObject;

    public NodeEngineData(INode node, LayoutDataManager manager)
    {
        _node = node;
        _manager = manager;
    }

    public INode Node => _node;

    public StyleChangeType StyleChangeType
    {
        get => _styleChangeType;
        set => _styleChangeType = value;
    }

    public virtual LayoutEngine.NG.Layout.LayoutObject? LayoutObject
    {
        get => _layoutObject;
        set => _layoutObject = value;
    }

    #region Style Recalc State

    public bool ChildNeedsStyleRecalc() => _childNeedsStyleRecalc;

    public void SetChildNeedsStyleRecalc()
    {
        _childNeedsStyleRecalc = true;
        if (_node.Parent != null)
        {
            var parentLayout = _manager.GetOrCreate(_node.Parent);
            parentLayout.SetChildNeedsStyleRecalc();
        }
    }

    public void ClearChildNeedsStyleRecalc()
    {
        _childNeedsStyleRecalc = false;
    }

    public bool NeedsStyleRecalc() => _styleChangeType != StyleChangeType.NoChange;

    /// <summary>
    /// Notifies the style engine when this node needs style recalc.
    /// This is the integration point between layout data and style engine.
    /// </summary>
    public void SetNeedsStyleRecalc(StyleChangeType changeType)
    {
        _styleChangeType = changeType;
        if (_node.OwnerDocument is IDocument doc && changeType != StyleChangeType.NoChange)
        {
            var docLayout = _manager.GetOrCreate(doc);
            docLayout.StyleEngine.SetNeedsStyleRecalc(_node, changeType);
        }
        if (_node.Parent != null)
        {
            var parentLayout = _manager.GetOrCreate(_node.Parent);
            parentLayout.SetChildNeedsStyleRecalc();
        }
    }

    public void ClearNeedsStyleRecalc()
    {
        _styleChangeType = StyleChangeType.NoChange;
    }

    #endregion

    #region Layout Tree Attachment State

    public bool NeedsReattachLayoutTree() => _needsReattachLayoutTree;

    public void SetNeedsReattachLayoutTree()
    {
        _needsReattachLayoutTree = true;
        if (_node.OwnerDocument is IDocument doc)
        {
            var docLayout = _manager.GetOrCreate(doc);
            docLayout.SetNeedsLayoutTreeRebuild(_node);
        }
        if (_node.Parent != null)
        {
            var parentLayout = _manager.GetOrCreate(_node.Parent);
            parentLayout.SetChildNeedsReattach();
        }
    }

    public void ClearNeedsReattach()
    {
        _needsReattachLayoutTree = false;
        _forceReattachLayoutTree = false;
    }

    public bool ChildNeedsReattach() => _childNeedsReattach;

    public void SetChildNeedsReattach()
    {
        _childNeedsReattach = true;
        if (_node.Parent != null)
        {
            var parentLayout = _manager.GetOrCreate(_node.Parent);
            parentLayout.SetChildNeedsReattach();
        }
    }

    public void ClearChildNeedsReattach()
    {
        _childNeedsReattach = false;
    }

    public bool GetForceReattachLayoutTree() => _forceReattachLayoutTree;

    public void SetForceReattachLayoutTree()
    {
        _forceReattachLayoutTree = true;
        SetNeedsReattachLayoutTree();
    }

    #endregion
}