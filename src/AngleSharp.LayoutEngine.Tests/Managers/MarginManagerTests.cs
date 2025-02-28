namespace AngleSharp.LayoutEngine.Tests.Managers;

using AngleSharp.LayoutEngine.Box;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using AngleSharp.LayoutEngine.FormattingContexts;
using AngleSharp.LayoutEngine.FormattingContexts.Enums;
using AngleSharp.LayoutEngine.Managers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class MarginManagerTests
{
    private MarginManager _marginManager;

    [SetUp]
    public void Setup()
    {
        _marginManager = new MarginManager();
    }

    [Test]
    public void CollapseMargins_WithBothPositive_ReturnsLargest()
    {
        // Act
        float result = _marginManager.CollapseMargins(10, 20);

        // Assert
        Assert.That(result, Is.EqualTo(20));

        // Test symmetry
        result = _marginManager.CollapseMargins(20, 10);
        Assert.That(result, Is.EqualTo(20));

        // Test with same values
        result = _marginManager.CollapseMargins(15, 15);
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CollapseMargins_WithBothNegative_ReturnsMostNegative()
    {
        // Act
        float result = _marginManager.CollapseMargins(-10, -20);

        // Assert
        Assert.That(result, Is.EqualTo(-20));

        // Test symmetry
        result = _marginManager.CollapseMargins(-20, -10);
        Assert.That(result, Is.EqualTo(-20));
    }

    [Test]
    public void CollapseMargins_WithMixedSigns_ReturnsSum()
    {
        // Act
        float result = _marginManager.CollapseMargins(10, -5);

        // Assert
        Assert.That(result, Is.EqualTo(5));

        // Test symmetry
        result = _marginManager.CollapseMargins(-5, 10);
        Assert.That(result, Is.EqualTo(5));

        // Test with larger negative
        result = _marginManager.CollapseMargins(10, -15);
        Assert.That(result, Is.EqualTo(-5));
    }

    [Test]
    public void CollapseMargins_WithZero_HandlesCorrectly()
    {
        // Act & Assert
        Assert.That(_marginManager.CollapseMargins(0, 10), Is.EqualTo(10));
        Assert.That(_marginManager.CollapseMargins(10, 0), Is.EqualTo(10));
        Assert.That(_marginManager.CollapseMargins(0, -10), Is.EqualTo(-10));
        Assert.That(_marginManager.CollapseMargins(-10, 0), Is.EqualTo(-10));
        Assert.That(_marginManager.CollapseMargins(0, 0), Is.EqualTo(0));
    }

    [Test]
    public void ResolveMarginChain_WithSiblingMargins_AppliesCorrectMargins()
    {
        // Arrange - Create mock nodes with margins
        var node1 = CreateNodeWithMargins(marginTop: 0, marginBottom: 20);
        var node2 = CreateNodeWithMargins(marginTop: 30, marginBottom: 0);

        var chain = new MarginCollapseChain
        {
            IsTopMargin = false, // Bottom margin of node1 with top margin of node2
            Nodes = new List<LayoutNode> { node1, node2 },
            IsSiblingChain = true
        };

        // Act
        _marginManager.ResolveMarginChain(chain);

        // Assert
        Assert.That(chain.CollapsedMargin, Is.EqualTo(30)); // Larger of the two margins
        Assert.That(node1.Box.HasBottomMarginCollapsed, Is.True);
        Assert.That(node2.Box.HasTopMarginCollapsed, Is.True);
        Assert.That(node1.Box.EffectiveBottomMargin, Is.EqualTo(30));
        Assert.That(node2.Box.EffectiveTopMargin, Is.EqualTo(30));
    }

    [Test]
    public void ResolveMarginChain_WithEmptyBlock_CollapsesTopAndBottomMargins()
    {
        // Arrange - Create a mock empty block node
        var node = CreateNodeWithMargins(marginTop: 20, marginBottom: 30);

        var chain = new MarginCollapseChain
        {
            IsTopMargin = true,
            Nodes = new List<LayoutNode> { node },
            IsEmptyBlockChain = true
        };

        // Act
        _marginManager.ResolveMarginChain(chain);

        // Assert - Should use the larger of top and bottom margins
        Assert.That(chain.CollapsedMargin, Is.EqualTo(30));
        Assert.That(node.Box.HasTopMarginCollapsed, Is.True);
        Assert.That(node.Box.HasBottomMarginCollapsed, Is.True);
        Assert.That(node.Box.EffectiveTopMargin, Is.EqualTo(30));
        Assert.That(node.Box.EffectiveBottomMargin, Is.EqualTo(30));
    }

    [Test]
    public void FindAffectedMarginChains_WithChangedNodes_IncludesDirectlyAffectedChains()
    {
        // Arrange
        var parent = CreateNodeWithMargins(10, 10);
        var child1 = CreateNodeWithMargins(20, 20);
        var child2 = CreateNodeWithMargins(30, 30);
        var child3 = CreateNodeWithMargins(40, 40);

        // Setup hierarchy
        child1.Parent = parent;
        child2.Parent = parent;
        child3.Parent = parent;
        parent.Children.AddRange(new[] { child1, child2, child3 });

        // Act - Find chains affected by changing child2
        var changedNodes = new List<LayoutNode> { child2 };
        var allNodes = new List<LayoutNode> { parent, child1, child2, child3 };

        var affectedChains = _marginManager.FindAffectedMarginChains(changedNodes, allNodes).ToList();

        // Assert - Should include chains involving child2 and possibly adjacent siblings
        Assert.That(affectedChains, Is.Not.Empty);

        // Check if chains include the changed node
        var hasChangedNode = affectedChains.Any(chain => chain.Nodes.Contains(child2));
        Assert.That(hasChangedNode, Is.True, "Affected chains should include the changed node");
    }

    [Test]
    public void ProcessFormattingContext_WithBlockElements_CreatesAndResolvesMarginChains()
    {
        // Arrange
        var parent = CreateNodeWithMargins(0, 0);
        var child1 = CreateNodeWithMargins(10, 15);
        var child2 = CreateNodeWithMargins(20, 25);

        // Setup hierarchy
        child1.Parent = parent;
        child2.Parent = parent;
        parent.Children.AddRange(new[] { child1, child2 });

        var participants = new List<LayoutNode> { parent, child1, child2 };

        // Create mock formatting context
        var mockContext = new Mock<IFormattingContext>();
        mockContext.Setup(c => c.EstablishingNode).Returns(parent);
        mockContext.Setup(c => c.GetParticipants()).Returns(participants);

        // Act
        _marginManager.ProcessFormattingContext(mockContext.Object, participants);

        // Assert
        // Check that margin collapsing was applied
        Assert.That(child1.Box.HasBottomMarginCollapsed || child2.Box.HasTopMarginCollapsed,
            Is.True, "Margins between siblings should be collapsed");

        // If margins collapsed correctly, effective margins should be the larger of the two
        if (child1.Box.HasBottomMarginCollapsed && child2.Box.HasTopMarginCollapsed)
        {
            var expectedMargin = System.Math.Max(child1.Box.MarginBottom, child2.Box.MarginTop);
            Assert.That(child1.Box.EffectiveBottomMargin, Is.EqualTo(expectedMargin));
            Assert.That(child2.Box.EffectiveTopMargin, Is.EqualTo(expectedMargin));
        }
    }

    #region Helper Methods

    /// <summary>
    /// Creates a mock layout node with the specified margins.
    /// </summary>
    private LayoutNode CreateNodeWithMargins(float marginTop, float marginBottom)
    {
        var mockDomNode = new Mock<IRenderNode>();
        var node = new LayoutNode(mockDomNode.Object);

        // Set up the box with margins
        node.Box.MarginTop = marginTop;
        node.Box.MarginBottom = marginBottom;
        node.Box.EffectiveTopMargin = marginTop;  // Initially, effective = specified
        node.Box.EffectiveBottomMargin = marginBottom;

        // Set display to block by default
        node.Display = DisplayType.Block;

        return node;
    }

    #endregion
}