namespace LayoutEngine.NG.Layout.Dom;

using AngleSharp.Dom;
using Style;

/// <summary>
/// Base layout data for all nodes.
/// </summary>
public class NodeLayout
{
    protected readonly INode _node;
    protected readonly LayoutDataManager _manager;
    private StyleChangeType _styleChangeType = StyleChangeType.NoChange;
    private bool _childNeedsStyleRecalc = false;
    private bool _childNeedsReattach = false;
    private bool _forceReattachLayoutTree = false;
    private bool _needsReattachLayoutTree = false;
    private LayoutEngine.NG.Layout.LayoutObject? _layoutObject;

    public NodeLayout(INode node, LayoutDataManager manager)
    {
        _node = node;
        _manager = manager;
    }

    public INode Node => _node;

    /// <summary>
    /// The style change type for this node.
    /// </summary>
    public StyleChangeType StyleChangeType
    {
        get => _styleChangeType;
        set => _styleChangeType = value;
    }

    /// <summary>
    /// Layout object for this node.
    /// </summary>
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

        // Propagate up the tree
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

    public void SetNeedsStyleRecalc(StyleChangeType changeType)
    {
        _styleChangeType = changeType;

        // Notify document's style engine about the dirty node
        if (_node.OwnerDocument is IDocument doc && changeType != StyleChangeType.NoChange)
        {
            var docLayout = _manager.GetOrCreate(doc);
            docLayout.StyleEngine.SetNeedsStyleRecalc(_node, changeType);
        }

        // Mark parent as having dirty children
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

        // Notify document's rebuild root
        if (_node.OwnerDocument is IDocument doc)
        {
            var docLayout = _manager.GetOrCreate(doc);
            docLayout.SetNeedsLayoutTreeRebuild(_node);
        }

        // Mark parent as having children that need reattachment
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

        // Propagate up
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