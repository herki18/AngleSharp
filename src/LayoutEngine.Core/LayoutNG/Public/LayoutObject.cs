namespace LayoutEngine.Core.LayoutNG.Public;

using AngleSharp.Dom;
using LayoutEngine.Core.Style.Public;

/// <summary>
/// Base abstract class for all layout objects, implementing common functionality.
/// </summary>
public abstract class LayoutObject : ILayoutObject
{
    private ILayoutObject? _parent;
    private IComputedStyle? _style;

    // Invalidation flags
    private bool _needsStyleRecalc;
    private bool _childNeedsStyleRecalc;
    private bool _needsLayout;
    private bool _childNeedsLayout;
    private bool _needsPaintInvalidation;

    protected LayoutObject(INode? node)
    {
        Node = node;
        Element = node as IElement;
    }

    public abstract LayoutObjectType Type { get; }

    public INode? Node { get; }

    public IElement? Element { get; }

    public ILayoutObject? Parent => _parent;

    public IComputedStyle? Style
    {
        get => _style;
        set => _style = value;
    }

    public virtual bool IsAnonymous => Node == null;

    public virtual bool IsText => false;

    public virtual bool IsInline => false;

    public virtual bool IsBlock => false;

    public virtual bool IsBox => false;

    public virtual bool IsPositioned => false;

    public virtual bool IsReplaced => false;

    public virtual bool EstablishesBlockFormattingContext => false;

    // Invalidation flag implementations

    public bool NeedsStyleRecalc() => _needsStyleRecalc;

    public bool ChildNeedsStyleRecalc() => _childNeedsStyleRecalc;

    public void SetNeedsStyleRecalc()
    {
        if (_needsStyleRecalc) return;

        _needsStyleRecalc = true;

        // Propagate up the tree
        _parent?.SetChildNeedsStyleRecalc();
    }

    public void ClearNeedsStyleRecalc()
    {
        _needsStyleRecalc = false;
    }

    public void SetChildNeedsStyleRecalc()
    {
        if (_childNeedsStyleRecalc) return;

        _childNeedsStyleRecalc = true;

        // Propagate up the tree
        _parent?.SetChildNeedsStyleRecalc();
    }

    public void ClearChildNeedsStyleRecalc()
    {
        _childNeedsStyleRecalc = false;
    }

    public bool NeedsLayout() => _needsLayout;

    public bool ChildNeedsLayout() => _childNeedsLayout;

    public void SetNeedsLayout()
    {
        if (_needsLayout) return;

        _needsLayout = true;

        // Also mark for paint invalidation
        SetNeedsPaintInvalidation();

        // Propagate up the tree
        _parent?.SetChildNeedsLayout();
    }

    public void ClearNeedsLayout()
    {
        _needsLayout = false;
    }

    public void SetChildNeedsLayout()
    {
        if (_childNeedsLayout) return;

        _childNeedsLayout = true;

        // Propagate up the tree
        _parent?.SetChildNeedsLayout();
    }

    public void ClearChildNeedsLayout()
    {
        _childNeedsLayout = false;
    }

    public bool NeedsPaintInvalidation() => _needsPaintInvalidation;

    public void SetNeedsPaintInvalidation()
    {
        _needsPaintInvalidation = true;
    }

    public void ClearNeedsPaintInvalidation()
    {
        _needsPaintInvalidation = false;
    }

    // Tree management

    public void SetParent(ILayoutObject? parent)
    {
        _parent = parent;
    }

    public virtual void Remove()
    {
        if (_parent is ILayoutContainer container)
        {
            container.RemoveChild(this);
        }
    }

    public virtual void Destroy()
    {
        Remove();
        _style = null;
    }

    public virtual void UpdateStyle(IComputedStyle? oldStyle, IComputedStyle newStyle)
    {
        _style = newStyle;

        // Check if layout is needed based on style changes
        if (StyleRequiresLayout(oldStyle, newStyle))
        {
            SetNeedsLayout();
        }
        else if (StyleRequiresPaint(oldStyle, newStyle))
        {
            SetNeedsPaintInvalidation();
        }
    }

    public virtual void ChildAdded(ILayoutObject child)
    {
        // Update invalidation flags based on child's state
        if (child.NeedsStyleRecalc() || child.ChildNeedsStyleRecalc())
        {
            SetChildNeedsStyleRecalc();
        }

        if (child.NeedsLayout() || child.ChildNeedsLayout())
        {
            SetChildNeedsLayout();
        }
    }

    public virtual void ChildRemoved(ILayoutObject child)
    {
        // May need to recalculate child flags if this was the only dirty child
        // This is an optimization that can be implemented later
    }

    // Helper methods

    protected virtual bool StyleRequiresLayout(IComputedStyle? oldStyle, IComputedStyle newStyle)
    {
        if (oldStyle == null) return true;

        // Check properties that affect layout
        return oldStyle.GetPropertyValue("display") != newStyle.GetPropertyValue("display") ||
               oldStyle.GetPropertyValue("position") != newStyle.GetPropertyValue("position") ||
               oldStyle.GetPropertyValue("width") != newStyle.GetPropertyValue("width") ||
               oldStyle.GetPropertyValue("height") != newStyle.GetPropertyValue("height") ||
               oldStyle.GetPropertyValue("margin") != newStyle.GetPropertyValue("margin") ||
               oldStyle.GetPropertyValue("padding") != newStyle.GetPropertyValue("padding") ||
               oldStyle.GetPropertyValue("border-width") != newStyle.GetPropertyValue("border-width");
    }

    protected virtual bool StyleRequiresPaint(IComputedStyle? oldStyle, IComputedStyle newStyle)
    {
        if (oldStyle == null) return true;

        // Check properties that only affect paint
        return oldStyle.GetPropertyValue("color") != newStyle.GetPropertyValue("color") ||
               oldStyle.GetPropertyValue("background-color") != newStyle.GetPropertyValue("background-color") ||
               oldStyle.GetPropertyValue("border-color") != newStyle.GetPropertyValue("border-color") ||
               oldStyle.GetPropertyValue("opacity") != newStyle.GetPropertyValue("opacity");
    }
}