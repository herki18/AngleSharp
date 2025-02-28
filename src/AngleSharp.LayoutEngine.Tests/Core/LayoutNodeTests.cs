namespace AngleSharp.LayoutEngine.Tests.Core;

using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using AngleSharp.LayoutEngine.FormattingContexts.Enums;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class LayoutNodeTests
{
    [Test]
    public void Constructor_InitializesProperties_Correctly()
    {
        // Arrange
        var mockDomNode = new Mock<IRenderNode>();

        // Act
        var node = new LayoutNode(mockDomNode.Object);

        // Assert
        Assert.That(node.DomNode, Is.SameAs(mockDomNode.Object));
        Assert.That(node.Children, Is.Empty);
        Assert.That(node.Box, Is.Not.Null);
        Assert.That(node.FormattingContext, Is.Null);
        Assert.That(node.CreatesFormattingContext, Is.False);
        Assert.That(node.IsDirty, Is.False);
        Assert.That(node.Display, Is.EqualTo(DisplayType.Inline)); // Default
        Assert.That(node.Position, Is.EqualTo(PositionType.Static)); // Default
        Assert.That(node.Float, Is.EqualTo(FloatType.None)); // Default
    }

    [Test]
    public void GetDescendants_ReturnsAllDescendants_InCorrectOrder()
    {
        // Arrange - Create a tree structure
        var rootNode = CreateNode("root");
        var child1 = CreateNode("child1");
        var child2 = CreateNode("child2");
        var grandchild1 = CreateNode("grandchild1");
        var grandchild2 = CreateNode("grandchild2");

        // Setup hierarchy
        rootNode.Children.Add(child1);
        rootNode.Children.Add(child2);
        child1.Parent = rootNode;
        child2.Parent = rootNode;

        child1.Children.Add(grandchild1);
        grandchild1.Parent = child1;

        child2.Children.Add(grandchild2);
        grandchild2.Parent = child2;

        // Act
        var descendants = rootNode.GetDescendants().ToList();

        // Assert
        Assert.That(descendants.Count, Is.EqualTo(4));
        Assert.That(descendants[0], Is.SameAs(child1));
        Assert.That(descendants[1], Is.SameAs(grandchild1));
        Assert.That(descendants[2], Is.SameAs(child2));
        Assert.That(descendants[3], Is.SameAs(grandchild2));
    }

    [Test]
    public void GetPreviousSiblings_ReturnsCorrectSiblings()
    {
        // Arrange - Create siblings
        var parent = CreateNode("parent");
        var child1 = CreateNode("child1");
        var child2 = CreateNode("child2");
        var child3 = CreateNode("child3");

        // Setup hierarchy
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);
        child1.Parent = parent;
        child2.Parent = parent;
        child3.Parent = parent;

        // Act & Assert

        // First child has no previous siblings
        var child1Siblings = child1.GetPreviousSiblings().ToList();
        Assert.That(child1Siblings, Is.Empty);

        // Child2's only previous sibling is child1
        var child2Siblings = child2.GetPreviousSiblings().ToList();
        Assert.That(child2Siblings.Count, Is.EqualTo(1));
        Assert.That(child2Siblings[0], Is.SameAs(child1));

        // Child3's previous siblings are child1 and child2
        var child3Siblings = child3.GetPreviousSiblings().ToList();
        Assert.That(child3Siblings.Count, Is.EqualTo(2));
        Assert.That(child3Siblings[0], Is.SameAs(child1));
        Assert.That(child3Siblings[1], Is.SameAs(child2));

        // Node without parent should return empty
        var orphanNode = CreateNode("orphan");
        Assert.That(orphanNode.GetPreviousSiblings(), Is.Empty);
    }

    [Test]
    public void GetPreviousSibling_ReturnsCorrectSibling()
    {
        // Arrange - Create siblings
        var parent = CreateNode("parent");
        var child1 = CreateNode("child1");
        var child2 = CreateNode("child2");
        var child3 = CreateNode("child3");

        // Setup hierarchy
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);
        child1.Parent = parent;
        child2.Parent = parent;
        child3.Parent = parent;

        // Act & Assert

        // First child has no previous sibling
        Assert.That(child1.GetPreviousSibling(), Is.Null);

        // Child2's previous sibling is child1
        Assert.That(child2.GetPreviousSibling(), Is.SameAs(child1));

        // Child3's previous sibling is child2
        Assert.That(child3.GetPreviousSibling(), Is.SameAs(child2));

        // Node without parent should return null
        var orphanNode = CreateNode("orphan");
        Assert.That(orphanNode.GetPreviousSibling(), Is.Null);
    }

    [Test]
    public void GetNextSiblings_ReturnsCorrectSiblings()
    {
        // Arrange - Create siblings
        var parent = CreateNode("parent");
        var child1 = CreateNode("child1");
        var child2 = CreateNode("child2");
        var child3 = CreateNode("child3");

        // Setup hierarchy
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);
        child1.Parent = parent;
        child2.Parent = parent;
        child3.Parent = parent;

        // Act & Assert

        // Child1's next siblings are child2 and child3
        var child1Siblings = child1.GetNextSiblings().ToList();
        Assert.That(child1Siblings.Count, Is.EqualTo(2));
        Assert.That(child1Siblings[0], Is.SameAs(child2));
        Assert.That(child1Siblings[1], Is.SameAs(child3));

        // Child2's only next sibling is child3
        var child2Siblings = child2.GetNextSiblings().ToList();
        Assert.That(child2Siblings.Count, Is.EqualTo(1));
        Assert.That(child2Siblings[0], Is.SameAs(child3));

        // Last child has no next siblings
        var child3Siblings = child3.GetNextSiblings().ToList();
        Assert.That(child3Siblings, Is.Empty);

        // Node without parent should return empty
        var orphanNode = CreateNode("orphan");
        Assert.That(orphanNode.GetNextSiblings(), Is.Empty);
    }

    [Test]
    public void GetNextSibling_ReturnsCorrectSibling()
    {
        // Arrange - Create siblings
        var parent = CreateNode("parent");
        var child1 = CreateNode("child1");
        var child2 = CreateNode("child2");
        var child3 = CreateNode("child3");

        // Setup hierarchy
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);
        child1.Parent = parent;
        child2.Parent = parent;
        child3.Parent = parent;

        // Act & Assert

        // Child1's next sibling is child2
        Assert.That(child1.GetNextSibling(), Is.SameAs(child2));

        // Child2's next sibling is child3
        Assert.That(child2.GetNextSibling(), Is.SameAs(child3));

        // Last child has no next sibling
        Assert.That(child3.GetNextSibling(), Is.Null);

        // Node without parent should return null
        var orphanNode = CreateNode("orphan");
        Assert.That(orphanNode.GetNextSibling(), Is.Null);
    }

    [Test]
    public void IsEmptyBlock_DetectsEmptyBlocks_Correctly()
    {
        // Arrange - Create an empty block node
        var emptyBlock = CreateNode("empty");
        emptyBlock.Display = DisplayType.Block;

        // An empty block has no rendered children, no height, and no padding/border

        // Act & Assert
        Assert.That(emptyBlock.IsEmptyBlock(), Is.True, "Node with no children should be empty block");

        // Add a child that doesn't render (comment node)
        var mockCommentNode = new Mock<IRenderNode>();
        mockCommentNode.Setup(n => n.GetType()).Returns(typeof(NonRenderableNode));
        var commentChild = new LayoutNode(mockCommentNode.Object);
        emptyBlock.Children.Add(commentChild);

        Assert.That(emptyBlock.IsEmptyBlock(), Is.True, "Node with only non-renderable children should be empty block");

        // Now add a renderable child
        var renderableChild = CreateNode("child");
        emptyBlock.Children.Add(renderableChild);

        Assert.That(emptyBlock.IsEmptyBlock(), Is.False, "Node with renderable children should not be empty block");

        // Create a block with explicit height
        var blockWithHeight = CreateNode("withHeight");
        blockWithHeight.Display = DisplayType.Block;
        blockWithHeight.Height = new StyleLengthValue(50, StyleUnit.Px);

        Assert.That(blockWithHeight.IsEmptyBlock(), Is.False, "Node with explicit height should not be empty block");

        // Create a block with padding
        var blockWithPadding = CreateNode("withPadding");
        blockWithPadding.Display = DisplayType.Block;
        blockWithPadding.Box.PaddingTop = 10;

        Assert.That(blockWithPadding.IsEmptyBlock(), Is.False, "Node with padding should not be empty block");

        // Create a block with border
        var blockWithBorder = CreateNode("withBorder");
        blockWithBorder.Display = DisplayType.Block;
        blockWithBorder.Box.BorderTop = 5;

        Assert.That(blockWithBorder.IsEmptyBlock(), Is.False, "Node with border should not be empty block");

        // Test non-block element
        var inlineNode = CreateNode("inline");
        inlineNode.Display = DisplayType.Inline;

        Assert.That(inlineNode.IsEmptyBlock(), Is.False, "Inline node should not be considered an empty block");
    }

    #region Helper Methods

    /// <summary>
    /// Creates a layout node with the specified ID.
    /// </summary>
    private LayoutNode CreateNode(string id)
    {
        var mockDomNode = new Mock<IRenderNode>();
        var mockElement = new Mock<ElementNode>();

        mockElement.Setup(e => e.Id).Returns(id);
        mockDomNode.Setup(d => d.Id).Returns(id);
        mockDomNode.As<ElementNode>().Setup(e => e.Id).Returns(id);

        var node = new LayoutNode(mockDomNode.Object);

        return node;
    }

    #endregion
}