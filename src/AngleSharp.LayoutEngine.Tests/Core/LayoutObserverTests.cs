namespace AngleSharp.LayoutEngine.Tests.Core;

using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class LayoutObserverTests
{
    private LayoutObserver _observer;

    [SetUp]
    public void Setup()
    {
        _observer = new LayoutObserver();
    }

    [Test]
    public void HasDirtyNodes_WithNoDirtyNodes_ReturnsFalse()
    {
        // Assert
        Assert.That(_observer.HasDirtyNodes, Is.False);
    }

    [Test]
    public void MarkDirty_SetsIsDirtyFlag_AndAddsToDirtyNodes()
    {
        // Arrange
        var mockDomNode = new Mock<IRenderNode>();
        var node = new LayoutNode(mockDomNode.Object);

        // Act
        _observer.MarkDirty(node);

        // Assert
        Assert.That(node.IsDirty, Is.True);
        Assert.That(_observer.HasDirtyNodes, Is.True);

        // Check if the node is in the dirty nodes collection
        var dirtyNodes = _observer.GetDirtyNodes();
        Assert.That(dirtyNodes, Does.Contain(node));
    }

    [Test]
    public void MarkSubtreeDirty_SetsIsDirtyFlag_ForAllDescendants()
    {
        // Arrange - Create a tree structure
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var mockDomNode3 = new Mock<IRenderNode>();

        var parent = new LayoutNode(mockDomNode1.Object);
        var child1 = new LayoutNode(mockDomNode2.Object);
        var child2 = new LayoutNode(mockDomNode3.Object);

        // Setup hierarchy
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.Parent = parent;
        child2.Parent = parent;

        // Act
        _observer.MarkSubtreeDirty(parent);

        // Assert
        Assert.That(parent.IsDirty, Is.True);
        Assert.That(child1.IsDirty, Is.True);
        Assert.That(child2.IsDirty, Is.True);

        // Check if all nodes are in the dirty nodes collection
        var dirtyNodes = _observer.GetDirtyNodes();
        Assert.That(dirtyNodes, Does.Contain(parent));
        Assert.That(dirtyNodes, Does.Contain(child1));
        Assert.That(dirtyNodes, Does.Contain(child2));
    }

    [Test]
    public void MarkDirtyNodes_MarksAllNodes_AndPropagatesToAncestors()
    {
        // Arrange - Create a tree structure
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var mockDomNode3 = new Mock<IRenderNode>();
        var mockDomNode4 = new Mock<IRenderNode>();

        var root = new LayoutNode(mockDomNode1.Object);
        var parent = new LayoutNode(mockDomNode2.Object);
        var child1 = new LayoutNode(mockDomNode3.Object);
        var child2 = new LayoutNode(mockDomNode4.Object);

        // Setup hierarchy
        root.Children.Add(parent);
        parent.Parent = root;
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.Parent = parent;
        child2.Parent = parent;

        // Act - Mark only child1 as dirty
        _observer.MarkDirtyNodes(new[] { child1 });

        // Assert
        Assert.That(child1.IsDirty, Is.True, "Direct node should be marked dirty");
        Assert.That(parent.IsDirty, Is.True, "Parent should be marked dirty through propagation");
        Assert.That(root.IsDirty, Is.True, "Root should be marked dirty through propagation");
        Assert.That(child2.IsDirty, Is.False, "Sibling should not be marked dirty");

        // Check the dirty nodes collection
        var dirtyNodes = _observer.GetDirtyNodes();
        Assert.That(dirtyNodes, Does.Contain(child1));
        Assert.That(dirtyNodes, Does.Contain(parent));
        Assert.That(dirtyNodes, Does.Contain(root));
        Assert.That(dirtyNodes, Does.Not.Contain(child2));
    }

    [Test]
    public void PropagateToAncestors_MarksAncestors_Correctly()
    {
        // Arrange - Create a tree structure
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var mockDomNode3 = new Mock<IRenderNode>();

        var root = new LayoutNode(mockDomNode1.Object);
        var parent = new LayoutNode(mockDomNode2.Object);
        var child = new LayoutNode(mockDomNode3.Object);

        // Setup hierarchy
        root.Children.Add(parent);
        parent.Parent = root;
        parent.Children.Add(child);
        child.Parent = parent;

        // Act
        _observer.PropagateToAncestors(child);

        // Assert
        Assert.That(child.IsDirty, Is.False, "Original node should not be marked directly");
        Assert.That(parent.IsDirty, Is.True, "Parent should be marked dirty");
        Assert.That(root.IsDirty, Is.True, "Root should be marked dirty");

        // Check the dirty ancestors collection
        var dirtyNodes = _observer.GetDirtyNodes();
        Assert.That(dirtyNodes, Does.Not.Contain(child));
        Assert.That(dirtyNodes, Does.Contain(parent));
        Assert.That(dirtyNodes, Does.Contain(root));
    }

    [Test]
    public void ClearDirty_ResetsAllDirtyFlags_AndClearsCollections()
    {
        // Arrange - Mark some nodes as dirty
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();

        var node1 = new LayoutNode(mockDomNode1.Object);
        var node2 = new LayoutNode(mockDomNode2.Object);

        _observer.MarkDirty(node1);
        _observer.MarkDirty(node2);

        // Verify setup worked
        Assert.That(node1.IsDirty, Is.True);
        Assert.That(node2.IsDirty, Is.True);
        Assert.That(_observer.HasDirtyNodes, Is.True);

        // Act
        _observer.ClearDirty();

        // Assert
        Assert.That(node1.IsDirty, Is.False, "Node1 dirty flag should be reset");
        Assert.That(node2.IsDirty, Is.False, "Node2 dirty flag should be reset");
        Assert.That(_observer.HasDirtyNodes, Is.False, "Observer should no longer have dirty nodes");
        Assert.That(_observer.GetDirtyNodes(), Is.Empty, "Dirty nodes collection should be empty");
    }

    [Test]
    public void GetDirtyNodes_ReturnsAllDirtyNodes_FromAllCollections()
    {
        // Arrange
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var mockDomNode3 = new Mock<IRenderNode>();
        var mockDomNode4 = new Mock<IRenderNode>();

        var node1 = new LayoutNode(mockDomNode1.Object);
        var node2 = new LayoutNode(mockDomNode2.Object);
        var root = new LayoutNode(mockDomNode3.Object);
        var subtreeRoot = new LayoutNode(mockDomNode4.Object);
        var subtreeChild = new LayoutNode(new Mock<IRenderNode>().Object);

        // Setup hierarchy for subtree
        subtreeRoot.Children.Add(subtreeChild);
        subtreeChild.Parent = subtreeRoot;

        // Setup hierarchy for propagation
        node1.Parent = root;

        // Mark nodes in different ways
        _observer.MarkDirty(node1); // Direct marking
        _observer.MarkDirty(node2); // Direct marking
        _observer.PropagateToAncestors(node1); // Mark ancestors
        _observer.MarkSubtreeDirty(subtreeRoot); // Mark subtree

        // Act
        var dirtyNodes = _observer.GetDirtyNodes();

        // Assert
        Assert.That(dirtyNodes.Count, Is.EqualTo(5), "Should include directly marked, ancestors, and subtree nodes");
        Assert.That(dirtyNodes, Does.Contain(node1));
        Assert.That(dirtyNodes, Does.Contain(node2));
        Assert.That(dirtyNodes, Does.Contain(root));
        Assert.That(dirtyNodes, Does.Contain(subtreeRoot));
        Assert.That(dirtyNodes, Does.Contain(subtreeChild));
    }

    [Test]
    public void IsDirectlyDirty_DetectsDirectlyMarkedNodes()
    {
        // Arrange
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();

        var node1 = new LayoutNode(mockDomNode1.Object);
        var node2 = new LayoutNode(mockDomNode2.Object);

        // Mark only node1
        _observer.MarkDirty(node1);

        // Act & Assert
        Assert.That(_observer.IsDirectlyDirty(node1), Is.True);
        Assert.That(_observer.IsDirectlyDirty(node2), Is.False);
    }

    [Test]
    public void IsInDirtySubtree_DetectsNodesInMarkedSubtrees()
    {
        // Arrange
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();

        var parent = new LayoutNode(mockDomNode1.Object);
        var child = new LayoutNode(mockDomNode2.Object);

        // Setup hierarchy
        parent.Children.Add(child);
        child.Parent = parent;

        // Mark only the parent's subtree
        _observer.MarkSubtreeDirty(parent);

        // Act & Assert
        Assert.That(_observer.IsInDirtySubtree(parent), Is.True);
        Assert.That(_observer.IsInDirtySubtree(child), Is.True);

        // Create an unrelated node
        var unrelatedNode = new LayoutNode(new Mock<IRenderNode>().Object);
        Assert.That(_observer.IsInDirtySubtree(unrelatedNode), Is.False);
    }

    [Test]
    public void IsDirtyAncestor_DetectsMarkedAncestors()
    {
        // Arrange
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();

        var parent = new LayoutNode(mockDomNode1.Object);
        var child = new LayoutNode(mockDomNode2.Object);

        // Setup hierarchy
        parent.Children.Add(child);
        child.Parent = parent;

        // Mark only through propagation
        _observer.PropagateToAncestors(child);

        // Act & Assert
        Assert.That(_observer.IsDirtyAncestor(parent), Is.True);
        Assert.That(_observer.IsDirtyAncestor(child), Is.False);
    }
}