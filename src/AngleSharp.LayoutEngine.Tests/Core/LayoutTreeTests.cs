namespace AngleSharp.LayoutEngine.Tests.Core;

using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class LayoutTreeTests
{
    private LayoutTree _layoutTree;

    [SetUp]
    public void Setup()
    {
        _layoutTree = new LayoutTree();
    }

    [Test]
    public void BuildFromDOM_WithHtmlStructure_CreatesCorrectTree()
    {
        // Arrange - Create a mock DOM tree
        var mockRoot = new Mock<IRenderNode>();
        var mockChild1 = new Mock<IRenderNode>();
        var mockChild2 = new Mock<IRenderNode>();
        var mockGrandchild = new Mock<IRenderNode>();

        // Setup IDs for identification
        mockRoot.Setup(n => n.Id).Returns("root");
        mockChild1.Setup(n => n.Id).Returns("child1");
        mockChild2.Setup(n => n.Id).Returns("child2");
        mockGrandchild.Setup(n => n.Id).Returns("grandchild");

        // Setup DOM hierarchy
        var children = new List<IRenderNode> { mockChild1.Object, mockChild2.Object };
        mockRoot.Setup(n => n.Children).Returns(children);

        var grandchildren = new List<IRenderNode> { mockGrandchild.Object };
        mockChild1.Setup(n => n.Children).Returns(grandchildren);
        mockChild2.Setup(n => n.Children).Returns(new List<IRenderNode>());
        mockGrandchild.Setup(n => n.Children).Returns(new List<IRenderNode>());

        // Act
        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Assert
        Assert.That(_layoutTree.Root, Is.Not.Null);
        Assert.That(_layoutTree.Root.DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(_layoutTree.Root.Children.Count, Is.EqualTo(2));
        Assert.That(_layoutTree.Root.Children[0].DomNode, Is.SameAs(mockChild1.Object));
        Assert.That(_layoutTree.Root.Children[1].DomNode, Is.SameAs(mockChild2.Object));
        Assert.That(_layoutTree.Root.Children[0].Children.Count, Is.EqualTo(1));
        Assert.That(_layoutTree.Root.Children[0].Children[0].DomNode, Is.SameAs(mockGrandchild.Object));
    }

    [Test]
    public void BuildFromDOM_WithNonRenderableNodes_SkipsThemInTree()
    {
        // Arrange - Create a mock DOM tree with non-renderable nodes
        var mockRoot = new Mock<IRenderNode>();
        var mockChild1 = new Mock<IRenderNode>();
        var mockComment = new Mock<NonRenderableNode>(); // Non-renderable node

        // Setup IDs for identification
        mockRoot.Setup(n => n.Id).Returns("root");
        mockChild1.Setup(n => n.Id).Returns("child1");

        // Setup DOM hierarchy
        var children = new List<IRenderNode> { mockChild1.Object, mockComment.Object };
        mockRoot.Setup(n => n.Children).Returns(children);

        mockChild1.Setup(n => n.Children).Returns(new List<IRenderNode>());
        mockComment.Setup(n => n.Children).Returns(new List<IRenderNode>());

        // Act
        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Assert
        Assert.That(_layoutTree.Root, Is.Not.Null);
        Assert.That(_layoutTree.Root.DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(_layoutTree.Root.Children.Count, Is.EqualTo(1), "Non-renderable nodes should be skipped");
        Assert.That(_layoutTree.Root.Children[0].DomNode, Is.SameAs(mockChild1.Object));
    }

    [Test]
    public void FindNodeForDomNode_WithExistingAndNonExistingNodes_ReturnsCorrect()
    {
        // Arrange - Create a simple tree
        var mockRoot = new Mock<IRenderNode>();
        var mockChild = new Mock<IRenderNode>();

        mockRoot.Setup(n => n.Children).Returns(new List<IRenderNode> { mockChild.Object });
        mockChild.Setup(n => n.Children).Returns(new List<IRenderNode>());

        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Act & Assert
        var rootNode = _layoutTree.FindNodeForDomNode(mockRoot.Object);
        var childNode = _layoutTree.FindNodeForDomNode(mockChild.Object);

        Assert.That(rootNode, Is.Not.Null);
        Assert.That(childNode, Is.Not.Null);
        Assert.That(rootNode.DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(childNode.DomNode, Is.SameAs(mockChild.Object));

        // Test with non-existing node
        var mockNonExisting = new Mock<IRenderNode>();
        var nonExistingNode = _layoutTree.FindNodeForDomNode(mockNonExisting.Object);

        Assert.That(nonExistingNode, Is.Null);
    }

    [Test]
    public void FindNodeById_WithExistingAndNonExistingIds_ReturnsCorrect()
    {
        // Arrange - Create a tree with IDs
        var mockRoot = new Mock<IRenderNode>();
        var mockChild = new Mock<IRenderNode>();

        mockRoot.Setup(n => n.Id).Returns("root");
        mockChild.Setup(n => n.Id).Returns("child");

        mockRoot.Setup(n => n.Children).Returns(new List<IRenderNode> { mockChild.Object });
        mockChild.Setup(n => n.Children).Returns(new List<IRenderNode>());

        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Act & Assert
        var rootNode = _layoutTree.FindNodeById("root");
        var childNode = _layoutTree.FindNodeById("child");

        Assert.That(rootNode, Is.Not.Null);
        Assert.That(childNode, Is.Not.Null);
        Assert.That(rootNode.DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(childNode.DomNode, Is.SameAs(mockChild.Object));

        // Test with non-existing ID
        var nonExistingNode = _layoutTree.FindNodeById("nonexisting");

        Assert.That(nonExistingNode, Is.Null);
    }

    [Test]
    public void FindNodesForDomNodes_WithCollections_ReturnsCorrectNodes()
    {
        // Arrange - Create a tree
        var mockRoot = new Mock<IRenderNode>();
        var mockChild1 = new Mock<IRenderNode>();
        var mockChild2 = new Mock<IRenderNode>();

        mockRoot.Setup(n => n.Children).Returns(new List<IRenderNode> { mockChild1.Object, mockChild2.Object });
        mockChild1.Setup(n => n.Children).Returns(new List<IRenderNode>());
        mockChild2.Setup(n => n.Children).Returns(new List<IRenderNode>());

        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Act
        var nodes = _layoutTree.FindNodesForDomNodes(new[] { mockRoot.Object, mockChild2.Object });

        // Assert
        Assert.That(nodes.Count, Is.EqualTo(2));
        Assert.That(nodes[0].DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(nodes[1].DomNode, Is.SameAs(mockChild2.Object));

        // Test with mixed existing and non-existing nodes
        var mockNonExisting = new Mock<IRenderNode>();
        nodes = _layoutTree.FindNodesForDomNodes(new[] { mockRoot.Object, mockNonExisting.Object });

        Assert.That(nodes.Count, Is.EqualTo(1), "Should only return existing nodes");
        Assert.That(nodes[0].DomNode, Is.SameAs(mockRoot.Object));

        // Test with empty collection
        nodes = _layoutTree.FindNodesForDomNodes(new IRenderNode[0]);

        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public void GetAllNodes_ReturnsAllNodesInTree_InCorrectOrder()
    {
        // Arrange - Create a tree structure
        var mockRoot = new Mock<IRenderNode>();
        var mockChild1 = new Mock<IRenderNode>();
        var mockChild2 = new Mock<IRenderNode>();
        var mockGrandchild = new Mock<IRenderNode>();

        // Setup IDs for identification
        mockRoot.Setup(n => n.Id).Returns("root");
        mockChild1.Setup(n => n.Id).Returns("child1");
        mockChild2.Setup(n => n.Id).Returns("child2");
        mockGrandchild.Setup(n => n.Id).Returns("grandchild");

        // Setup DOM hierarchy
        var children = new List<IRenderNode> { mockChild1.Object, mockChild2.Object };
        mockRoot.Setup(n => n.Children).Returns(children);

        var grandchildren = new List<IRenderNode> { mockGrandchild.Object };
        mockChild1.Setup(n => n.Children).Returns(grandchildren);
        mockChild2.Setup(n => n.Children).Returns(new List<IRenderNode>());
        mockGrandchild.Setup(n => n.Children).Returns(new List<IRenderNode>());

        _layoutTree.BuildFromDOM(mockRoot.Object);

        // Act
        var allNodes = _layoutTree.GetAllNodes().ToList();

        // Assert
        Assert.That(allNodes.Count, Is.EqualTo(4));

        // Ensure pre-order traversal: root, child1, grandchild, child2
        Assert.That(allNodes[0].DomNode, Is.SameAs(mockRoot.Object));
        Assert.That(allNodes[1].DomNode, Is.SameAs(mockChild1.Object));
        Assert.That(allNodes[2].DomNode, Is.SameAs(mockGrandchild.Object));
        Assert.That(allNodes[3].DomNode, Is.SameAs(mockChild2.Object));
    }

    [Test]
    public void StyleValue_SubclassesWorkCorrectly()
    {
        // Test StyleAutoValue
        var autoValue = StyleValue.Auto;
        Assert.That(autoValue.ToString(), Is.EqualTo("auto"));

        // Test StyleLengthValue with pixels
        var pixelValue = StyleValue.FromPixels(100);
        Assert.That(pixelValue, Is.InstanceOf<StyleLengthValue>());
        var pixelLengthValue = pixelValue as StyleLengthValue;
        Assert.That(pixelLengthValue, Is.Not.Null);
        Assert.That(pixelLengthValue.Value, Is.EqualTo(100));
        Assert.That(pixelLengthValue.Unit, Is.EqualTo(StyleUnit.Px));

        // Test StyleLengthValue with percentage
        var percentageValue = StyleValue.FromPercentage(50);
        Assert.That(percentageValue, Is.InstanceOf<StyleLengthValue>());
        var percentageLengthValue = percentageValue as StyleLengthValue;
        Assert.That(percentageLengthValue, Is.Not.Null);
        Assert.That(percentageLengthValue.Value, Is.EqualTo(50));
        Assert.That(percentageLengthValue.Unit, Is.EqualTo(StyleUnit.Percentage));

        // Test ToString
        Assert.That(pixelLengthValue.ToString(), Is.EqualTo("100px"));

        // Test pixel conversion in context
        var context = new LayoutContext(1000, 800);
        float pixels = pixelLengthValue.ToPixels(context);
        Assert.That(pixels, Is.EqualTo(100));

        // Test percentage conversion in context
        pixels = percentageLengthValue.ToPixels(context, 200); // 200px container
        Assert.That(pixels, Is.EqualTo(100)); // 50% of 200px = 100px

        // Test viewport units
        var vwValue = new StyleLengthValue(50, StyleUnit.Vw);
        pixels = vwValue.ToPixels(context);
        Assert.That(pixels, Is.EqualTo(500)); // 50% of 1000px viewport width

        var vhValue = new StyleLengthValue(25, StyleUnit.Vh);
        pixels = vhValue.ToPixels(context);
        Assert.That(pixels, Is.EqualTo(200)); // 25% of 800px viewport height
    }
}