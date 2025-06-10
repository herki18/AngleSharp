// Main implementation of the TreeNode class and traversal logic.

/*
 * Copyright (c) 2018-2022, Andreas Kling <andreas@ladybird.org>
 *
 * SPDX-License-Identifier: BSD-2-Clause
 */

namespace LadyBird.Libraries.LibWeb;

using System;
using System.Diagnostics;
using AK;

/// <summary>
/// Provides a non-recursive pre-order traversal method for a tree of TreeNode objects.
/// </summary>
public static class TreeTraversal
{
    // C++ function: traverse_preorder
    public static TraversalDecision TraversePreorder<T>(
        T? root,
        Func<T, TraversalDecision> callback
    )
        where T : TreeNode<T>
    {
        var current = root;
        while (current != null)
        {
            var decision = callback(current);
            if (decision == TraversalDecision.Break)
                return TraversalDecision.Break;

            if (
                decision != TraversalDecision.SkipChildrenAndContinue
                && current.FirstChild != null
            )
            {
                current = current.FirstChild;
                continue;
            }
            if (current == root)
                break;

            if (current.NextSibling != null)
            {
                current = current.NextSibling;
                continue;
            }

            while (current != root && current is { NextSibling: null })
            {
                current = current.Parent;
            }

            if (current == root)
            {
                break;
            }

            if (current == null || current == root)
            {
                break;
            }

            current = current.NextSibling;
        }
        return TraversalDecision.Continue;
    }
}

/// <summary>
/// A generic base class for nodes in a tree structure.
/// It requires the generic type T to be a descendant of TreeNode<T> itself.
/// </summary>
/// <typeparam name="T">The type of the node, which must inherit from TreeNode<T></typeparam>
// The C++ code inherits from LibJS::Heap::Cell, which is a stubbed class here.
public abstract class TreeNode<T>
    where T : TreeNode<T>
{
    private T? _parent;
    private T? _firstChild;
    private T? _lastChild;
    private T? _nextSibling;
    private T? _previousSibling;

    public T? Parent => _parent;
    public bool HasChildren => _firstChild != null;
    public T? NextSibling => _nextSibling;
    public T? PreviousSibling => _previousSibling;
    public T? FirstChild => _firstChild;
    public T? LastChild => _lastChild;

    // https://dom.spec.whatwg.org/#concept-tree-index
    /// <summary>
    /// The index of a node is its number of preceding siblings, or 0 if it has none.
    /// </summary>
    public int Index()
    {
        int index = 0;
        for (
            var node = PreviousSibling;
            node != null;
            node = node.PreviousSibling
        )
            ++index;
        return index;
    }

    // C++ method: is_ancestor_of
    public bool IsAncestorOf(TreeNode<T> other)
    {
        for (
            var ancestor = other.Parent;
            ancestor != null;
            ancestor = ancestor.Parent
        )
        {
            if (ancestor == this)
                return true;
        }
        return false;
    }

    // C++ method: is_inclusive_ancestor_of
    public bool IsInclusiveAncestorOf(TreeNode<T> other) =>
        other == this || IsAncestorOf(other);

    // C++ method: append_child
    public void AppendChild(T node)
    {
        // C++: VERIFY(!node->m_parent);
        Debug.Assert(node._parent == null, "Node should not have a parent.");

        if (_lastChild != null)
            _lastChild._nextSibling = node;
        node._previousSibling = _lastChild;
        node._parent = (T)this;
        _lastChild = node;
        if (_firstChild == null)
            _firstChild = _lastChild;
    }

    // C++ method: prepend_child
    public void PrependChild(T node)
    {
        // C++: VERIFY(!node->m_parent);
        Debug.Assert(node._parent == null, "Node should not have a parent.");

        if (_firstChild != null)
            _firstChild._previousSibling = node;
        node._nextSibling = _firstChild;
        node._parent = (T)this;
        _firstChild = node;
        if (_lastChild == null)
            _lastChild = _firstChild;

        // C++: node->inserted_into(static_cast<T&>(*this));
        node.InsertedInto((T)this);

        // C++: static_cast<T*>(this)->children_changed();
        ChildrenChanged();
    }

    // C++ method: insert_before
    // C++ parameter 'child' was GC::Ptr<T>, which is nullable.
    public void InsertBefore(T node, T? child)
    {
        if (child == null)
        {
            AppendChild(node);
            return;
        }

        // C++: VERIFY(!node->m_parent);
        Debug.Assert(node._parent == null, "Node should not have a parent.");
        // C++: VERIFY(child->parent() == this);
        Debug.Assert(
            child.Parent == this,
            "Child to insert before must be a child of this node."
        );

        node._previousSibling = child._previousSibling;
        node._nextSibling = child;

        if (child._previousSibling != null)
            child._previousSibling._nextSibling = node;

        if (_firstChild == child)
            _firstChild = node;

        child._previousSibling = node;

        node._parent = (T)this;
    }

    // C++ method: remove_child
    public void RemoveChild(T node)
    {
        // C++: VERIFY(node->m_parent == this);
        Debug.Assert(
            node._parent == this,
            "Node to remove must be a child of this node."
        );

        if (_firstChild == node)
            _firstChild = node._nextSibling;

        if (_lastChild == node)
            _lastChild = node._previousSibling;

        if (node._nextSibling != null)
            node._nextSibling._previousSibling = node._previousSibling;

        if (node._previousSibling != null)
            node._previousSibling._nextSibling = node._nextSibling;

        node._nextSibling = null;
        node._previousSibling = null;
        node._parent = null;
    }

    // C++ method: replace_child
    public void ReplaceChild(T newChild, T oldChild)
    {
        // C++: VERIFY(old_child != new_child);
        Debug.Assert(oldChild != newChild);
        // C++: VERIFY(old_child->m_parent == this);
        Debug.Assert(oldChild.Parent == this);
        // C++: VERIFY(new_child->m_parent == nullptr);
        Debug.Assert(newChild.Parent == null);

        if (_firstChild == oldChild)
            _firstChild = newChild;
        if (_lastChild == oldChild)
            _lastChild = newChild;

        newChild._nextSibling = oldChild._nextSibling;
        if (newChild._nextSibling != null)
            newChild._nextSibling._previousSibling = newChild;

        newChild._previousSibling = oldChild._previousSibling;
        if (newChild._previousSibling != null)
            newChild._previousSibling._nextSibling = newChild;

        newChild._parent = oldChild._parent;

        oldChild._nextSibling = null;
        oldChild._previousSibling = null;
        oldChild._parent = null;
    }

    // C++ method: remove
    public void Remove()
    {
        // C++: VERIFY(m_parent);
        Debug.Assert(_parent != null, "Cannot remove a node with no parent.");
        _parent.RemoveChild((T)this);
    }

    // C++ method: next_in_pre_order
    public T? NextInPreOrder()
    {
        if (FirstChild != null)
            return FirstChild;

        T? node;
        if ((node = NextSibling) == null)
        {
            node = Parent;
            while (node != null && node.NextSibling == null)
                node = node.Parent;
            if (node != null)
                node = node.NextSibling;
        }
        return node;
    }

    // C++ method: next_in_pre_order (with stay_within)
    public T? NextInPreOrder(T? stayWithin)
    {
        if (FirstChild != null)
            return FirstChild;

        var node = (T)this;
        T? next;
        while ((next = node.NextSibling) == null)
        {
            node = node.Parent;
            if (node == null || node == stayWithin)
                return null;
        }
        return next;
    }

    // C++ method: previous_in_pre_order
    public T? PreviousInPreOrder()
    {
        var node = PreviousSibling;
        if (node != null)
        {
            while (node.LastChild != null)
                node = node.LastChild;

            return node;
        }

        return Parent;
    }

    // C++ method: for_each_in_inclusive_subtree
    public TraversalDecision ForEachInInclusiveSubtree(
        Func<T, TraversalDecision> callback
    ) => TreeTraversal.TraversePreorder((T)this, callback);

    // C++ method: for_each_in_inclusive_subtree_of_type
    public TraversalDecision ForEachInInclusiveSubtreeOfType<U>(
        Func<U, TraversalDecision> callback
    )
        where U : T
    {
        return ForEachInInclusiveSubtree(node =>
        {
            // C++: as_if<U>(node)
            if (node is U nodeOfType)
                return callback(nodeOfType);
            return TraversalDecision.Continue;
        });
    }

    // C++ method: for_each_in_subtree
    public TraversalDecision ForEachInSubtree(
        Func<T, TraversalDecision> callback
    )
    {
        for (var child = FirstChild; child != null; child = child.NextSibling)
        {
            if (
                child.ForEachInInclusiveSubtree(callback)
                == TraversalDecision.Break
            )
                return TraversalDecision.Break;
        }
        return TraversalDecision.Continue;
    }

    // C++ method: for_each_in_subtree_of_type
    public TraversalDecision ForEachInSubtreeOfType<U>(
        Func<U, TraversalDecision> callback
    )
        where U : T
    {
        for (var child = FirstChild; child != null; child = child.NextSibling)
        {
            if (
                child.ForEachInInclusiveSubtreeOfType(callback)
                == TraversalDecision.Break
            )
                return TraversalDecision.Break;
        }
        return TraversalDecision.Continue;
    }

    // C++ method: for_each_child
    public void ForEachChild(Func<T, IterationDecision> callback)
    {
        for (var node = FirstChild; node != null; node = node.NextSibling)
        {
            if (callback(node) == IterationDecision.Break)
                return;
        }
    }

    // C++ method: for_each_child_of_type
    public void ForEachChildOfType<U>(Func<U, IterationDecision> callback)
        where U : T
    {
        for (var node = FirstChild; node != null; node = node.NextSibling)
        {
            // C++: as_if<U>(node)
            if (node is U nodeOfType)
            {
                if (callback(nodeOfType) == IterationDecision.Break)
                    return;
            }
        }
    }

    // C++ method: next_sibling_of_type
    public U? NextSiblingOfType<U>()
        where U : class
    {
        for (
            var sibling = NextSibling;
            sibling != null;
            sibling = sibling.NextSibling
        )
        {
            // C++: as_if<U>(*sibling)
            if (sibling is U siblingOfType)
                return siblingOfType;
        }
        return null;
    }

    // C++ method: previous_sibling_of_type
    public U? PreviousSiblingOfType<U>()
        where U : class
    {
        for (
            var sibling = PreviousSibling;
            sibling != null;
            sibling = sibling.PreviousSibling
        )
        {
            // C++: as_if<U>(*sibling)
            if (sibling is U siblingOfType)
                return siblingOfType;
        }
        return null;
    }

    // C++ method: first_child_of_type
    public U? FirstChildOfType<U>()
        where U : T
    {
        for (var child = FirstChild; child != null; child = child.NextSibling)
        {
            // C++: as_if<U>(*child)
            if (child is U childOfType)
                return childOfType;
        }
        return null;
    }

    // C++ method: last_child_of_type
    public U? LastChildOfType<U>()
        where U : T
    {
        for (
            var child = LastChild;
            child != null;
            child = child.PreviousSibling
        )
        {
            // C++: as_if<U>(*child)
            if (child is U childOfType)
                return childOfType;
        }
        return null;
    }

    // C++ method: first_ancestor_of_type
    public U? FirstAncestorOfType<U>()
        where U : T
    {
        for (
            var ancestor = Parent;
            ancestor != null;
            ancestor = ancestor.Parent
        )
        {
            // C++: as_if<U>(*ancestor)
            if (ancestor is U ancestorOfType)
                return ancestorOfType;
        }
        return null;
    }

    protected TreeNode() { }

    /// <summary>
    /// Virtual method called when a node is inserted into the tree.
    /// Derived from the C++ code's call to `node->inserted_into()`.
    /// </summary>
    protected virtual void InsertedInto(T parent) { }

    /// <summary>
    /// Virtual method called when the children of a node change.
    /// Derived from the C++ code's call to `children_changed()`.
    /// </summary>
    protected virtual void ChildrenChanged() { }

    // The C++ destructor ~TreeNode() is not needed in C#.
    // The C++ method visit_edges is for a custom garbage collector and is not needed in C#.
}