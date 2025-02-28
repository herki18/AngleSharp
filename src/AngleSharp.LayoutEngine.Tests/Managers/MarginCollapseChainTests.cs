namespace AngleSharp.LayoutEngine.Tests.Managers;

using AngleSharp.LayoutEngine.Box;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using AngleSharp.LayoutEngine.FormattingContexts.Enums;
using AngleSharp.LayoutEngine.Managers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class MarginCollapseChainTests
{
    [Test]
    public void Constructor_WithDefaultValues_InitializesCorrectly()
    {
        // Act
        var chain = new MarginCollapseChain();

        // Assert
        Assert.That(chain.Nodes, Is.Empty);
        Assert.That(chain.CollapsedMargin, Is.EqualTo(0));
        Assert.That(chain.IsTopMargin, Is.False);
        Assert.That(chain.IsEmptyBlockChain, Is.False);
        Assert.That(chain.IsParentChildChain, Is.False);
        Assert.That(chain.IsSiblingChain, Is.False);
    }

    [Test]
    public void Constructor_WithParameters_InitializesCorrectly()
    {
        // Act
        var chain = new MarginCollapseChain(isTopMargin: true, chainIndex: 5);

        // Assert
        Assert.That(chain.Nodes, Is.Empty);
        Assert.That(chain.IsTopMargin, Is.True);
        Assert.That(chain.ChainIndex, Is.EqualTo(5));
    }

    [Test]
    public void AddNode_WithNewAndExistingNodes_AddsCorrectly()
    {
        // Arrange
        var chain = new MarginCollapseChain();
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var node1 = new LayoutNode(mockDomNode1.Object);
        var node2 = new LayoutNode(mockDomNode2.Object);

        // Act - Add a new node
        chain.AddNode(node1);

        // Assert
        Assert.That(chain.Nodes.Count, Is.EqualTo(1));
        Assert.That(chain.Nodes.First(), Is.SameAs(node1));

        // Act - Add the same node again
        chain.AddNode(node1);

        // Assert - Should not duplicate
        Assert.That(chain.Nodes.Count, Is.EqualTo(1));

        // Act - Add a different node
        chain.AddNode(node2);

        // Assert
        Assert.That(chain.Nodes.Count, Is.EqualTo(2));
        Assert.That(chain.Nodes.Contains(node2), Is.True);
    }

    [Test]
    public void CalculateCollapsedMargin_WithMixedMargins_FollowsCssRules()
    {
        // Arrange
        var chain = new MarginCollapseChain { IsTopMargin = true };

        // Create nodes with various margins
        var node1 = CreateNodeWithMargins(10, 5);  // Top=10, Bottom=5
        var node2 = CreateNodeWithMargins(20, 15); // Top=20, Bottom=15
        var node3 = CreateNodeWithMargins(-5, -8); // Top=-5, Bottom=-8

        chain.Nodes.Add(node1);
        chain.Nodes.Add(node2);
        chain.Nodes.Add(node3);

        // Act
        chain.CalculateCollapsedMargin();

        // Assert - For top margins, should pick max of positive (20) and min of negative (-5)
        Assert.That(chain.CollapsedMargin, Is.EqualTo(15)); // 20 + (-5) = 15

        // Test with empty block chain
        chain.IsEmptyBlockChain = true;
        chain.CalculateCollapsedMargin();

        // For empty blocks, should consider both top and bottom margins
        // Positive max: 20, Negative min: -8, result: 20 + (-8) = 12
        Assert.That(chain.CollapsedMargin, Is.EqualTo(12));

        // Test with only negative margins
        chain = new MarginCollapseChain { IsTopMargin = true };
        var negativeNode1 = CreateNodeWithMargins(-10, -15);
        var negativeNode2 = CreateNodeWithMargins(-20, -5);
        chain.Nodes.Add(negativeNode1);
        chain.Nodes.Add(negativeNode2);

        chain.CalculateCollapsedMargin();
        Assert.That(chain.CollapsedMargin, Is.EqualTo(-20)); // Most negative
    }

    [Test]
    public void ApplyCollapsedMargin_SetsEffectiveMargins_OnAllNodes()
    {
        // Arrange
        var chain = new MarginCollapseChain { IsTopMargin = true };
        var node1 = CreateNodeWithMargins(10, 20);
        var node2 = CreateNodeWithMargins(30, 40);

        chain.Nodes.Add(node1);
        chain.Nodes.Add(node2);

        // Calculate margin manually for this test (max of 10 and 30)
        chain.CollapsedMargin = 30;

        // Act
        chain.ApplyCollapsedMargin();

        // Assert
        Assert.That(node1.Box.EffectiveTopMargin, Is.EqualTo(30));
        Assert.That(node2.Box.EffectiveTopMargin, Is.EqualTo(30));
        Assert.That(node1.Box.HasTopMarginCollapsed, Is.True);
        Assert.That(node2.Box.HasTopMarginCollapsed, Is.True);
        Assert.That(node1.Box.IsInMarginCollapsedChain, Is.True);
        Assert.That(node2.Box.IsInMarginCollapsedChain, Is.True);

        // Test with bottom margin
        chain = new MarginCollapseChain { IsTopMargin = false };
        node1 = CreateNodeWithMargins(10, 20);
        node2 = CreateNodeWithMargins(30, 40);

        chain.Nodes.Add(node1);
        chain.Nodes.Add(node2);
        chain.CollapsedMargin = 40; // Max of 20 and 40

        chain.ApplyCollapsedMargin();

        Assert.That(node1.Box.EffectiveBottomMargin, Is.EqualTo(40));
        Assert.That(node2.Box.EffectiveBottomMargin, Is.EqualTo(40));
        Assert.That(node1.Box.HasBottomMarginCollapsed, Is.True);
        Assert.That(node2.Box.HasBottomMarginCollapsed, Is.True);
    }

    [Test]
    public void ContainsNode_ReturnsCorrectResult()
    {
        // Arrange
        var chain = new MarginCollapseChain();
        var mockDomNode1 = new Mock<IRenderNode>();
        var mockDomNode2 = new Mock<IRenderNode>();
        var node1 = new LayoutNode(mockDomNode1.Object);
        var node2 = new LayoutNode(mockDomNode2.Object);

        chain.Nodes.Add(node1);

        // Act & Assert
        Assert.That(chain.ContainsNode(node1), Is.True);
        Assert.That(chain.ContainsNode(node2), Is.False);
    }

    [Test]
    public void IsAffectedBy_WithVariousChanges_DetectsCorrectly()
    {
        // Arrange
        var parent = CreateNodeWithMargins(10, 10);
        var child1 = CreateNodeWithMargins(20, 20);
        var child2 = CreateNodeWithMargins(30, 30);

        // Setup hierarchy
        child1.Parent = parent;
        child2.Parent = parent;
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Create a parent-child chain
        var parentChildChain = new MarginCollapseChain
        {
            IsParentChildChain = true,
            ParentNode = parent
        };
        parentChildChain.Nodes.Add(parent);
        parentChildChain.Nodes.Add(child1);

        // Create a sibling chain
        var siblingChain = new MarginCollapseChain
        {
            IsSiblingChain = true
        };
        siblingChain.Nodes.Add(child1);
        siblingChain.Nodes.Add(child2);

        // Act & Assert

        // Test direct intersection
        Assert.That(parentChildChain.IsAffectedBy(new[] { child1 }), Is.True,
            "Chain should be affected by changes to a node in the chain");

        // Test parent-child relationship
        Assert.That(parentChildChain.IsAffectedBy(new[] { parent }), Is.True,
            "Parent-child chain should be affected by changes to parent");

        // Test sibling relationship
        var unrelatedNode = CreateNodeWithMargins(5, 5);
        Assert.That(siblingChain.IsAffectedBy(new[] { unrelatedNode }), Is.False,
            "Chain should not be affected by unrelated node changes");
    }

    [Test]
    public void ToString_ReturnsFormattedDescription()
    {
        // Arrange
        var chain = new MarginCollapseChain
        {
            IsEmptyBlockChain = true,
            IsTopMargin = true,
            CollapsedMargin = 15
        };
        chain.Nodes.Add(CreateNodeWithMargins(10, 10));
        chain.Nodes.Add(CreateNodeWithMargins(15, 15));

        // Act
        var result = chain.ToString();

        // Assert
        Assert.That(result, Does.Contain("Empty Block"));
        Assert.That(result, Does.Contain("Top"));
        Assert.That(result, Does.Contain("2 nodes"));
        Assert.That(result, Does.Contain("Margin=15"));
    }

    #region Helper Methods

    /// <summary>
    /// Creates a layout node with the specified margins.
    /// </summary>
    private LayoutNode CreateNodeWithMargins(float marginTop, float marginBottom)
    {
        var mockDomNode = new Mock<IRenderNode>();
        var node = new LayoutNode(mockDomNode.Object);

        node.Box.MarginTop = marginTop;
        node.Box.MarginBottom = marginBottom;

        // Default to block display
        node.Display = DisplayType.Block;

        return node;
    }

    #endregion
}