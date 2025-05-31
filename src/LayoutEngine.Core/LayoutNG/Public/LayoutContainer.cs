namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;

/// <summary>
/// Base class for layout objects that can contain children.
/// </summary>
public abstract class LayoutContainer : LayoutObject, ILayoutContainer
{
    private readonly LinkedList<ILayoutObject> _children = new();

    protected LayoutContainer(INode? node) : base(node)
    {
    }

    public ILayoutObject? FirstChild => _children.FirstOrDefault();

    public ILayoutObject? LastChild => _children.LastOrDefault();

    public IEnumerable<ILayoutObject> Children => _children;

    public int ChildCount => _children.Count;

    public virtual void AddChild(ILayoutObject child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent != null)
            throw new InvalidOperationException("Child already has a parent");

        _children.AddLast(child);
        child.SetParent(this);

        ChildAdded(child);

        // Mark layout as needed when children change
        SetNeedsLayout();
    }

    public virtual void InsertChild(ILayoutObject child, ILayoutObject? beforeChild)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent != null)
            throw new InvalidOperationException("Child already has a parent");

        if (beforeChild == null)
        {
            AddChild(child);
            return;
        }

        var node = _children.Find(beforeChild);
        if (node == null)
            throw new InvalidOperationException("beforeChild is not a child of this container");

        _children.AddBefore(node, child);
        child.SetParent(this);

        ChildAdded(child);

        // Mark layout as needed when children change
        SetNeedsLayout();
    }

    public virtual void RemoveChild(ILayoutObject child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (!_children.Remove(child))
            throw new InvalidOperationException("Child is not a child of this container");

        child.SetParent(null);

        ChildRemoved(child);

        // Mark layout as needed when children change
        SetNeedsLayout();

        // Remove unnecessary anonymous wrappers after child removal
        RemoveUnnecessaryAnonymousWrappers();
    }

    public virtual void RemoveAllChildren()
    {
        foreach (var child in _children.ToList())
        {
            RemoveChild(child);
        }
    }

    public virtual void MoveChild(ILayoutObject child, ILayoutObject? beforeChild)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (!_children.Contains(child))
            throw new InvalidOperationException("Child is not a child of this container");

        _children.Remove(child);

        if (beforeChild == null)
        {
            _children.AddLast(child);
        }
        else
        {
            var node = _children.Find(beforeChild);
            if (node == null)
                throw new InvalidOperationException("beforeChild is not a child of this container");

            _children.AddBefore(node, child);
        }

        // Mark layout as needed when children move
        SetNeedsLayout();
    }

    public virtual void CreateAnonymousWrappersIfNeeded()
    {
        // This will be overridden in specific layout object types
        // For example, LayoutBlock will create anonymous blocks to wrap inline content
        // when block-level children are inserted
    }

    public virtual void RemoveUnnecessaryAnonymousWrappers()
    {
        // Remove anonymous wrappers that are no longer needed
        var anonymousToRemove = new List<ILayoutObject>();

        foreach (var child in _children)
        {
            if (child.IsAnonymous && child is ILayoutContainer container)
            {
                // Check if anonymous wrapper is still needed
                if (!IsAnonymousWrapperNeeded(container))
                {
                    anonymousToRemove.Add(child);
                }
            }
        }

        foreach (var anonymous in anonymousToRemove)
        {
            RemoveAnonymousWrapper(anonymous as ILayoutContainer);
        }
    }

    protected virtual bool IsAnonymousWrapperNeeded(ILayoutContainer anonymous)
    {
        // Override in derived classes to implement specific rules
        return true;
    }

    protected virtual void RemoveAnonymousWrapper(ILayoutContainer? anonymous)
    {
        if (anonymous == null) return;

        // Move all children of the anonymous wrapper to this container
        var children = anonymous.Children.ToList();
        var beforeChild = GetNextSibling(anonymous);

        foreach (var child in children)
        {
            anonymous.RemoveChild(child);
            InsertChild(child, beforeChild);
        }

        RemoveChild(anonymous);
    }

    protected internal ILayoutObject? GetNextSibling(ILayoutObject child)
    {
        var node = _children.Find(child);
        return node?.Next?.Value;
    }

    protected ILayoutObject? GetPreviousSibling(ILayoutObject child)
    {
        var node = _children.Find(child);
        return node?.Previous?.Value;
    }

    public override void Destroy()
    {
        RemoveAllChildren();
        base.Destroy();
    }
}