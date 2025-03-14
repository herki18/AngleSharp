namespace AngleSharp.StyleSystem.Tests.Unit;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Properties;
using Moq;
using StyleSystem.Computation;
using StyleSystem.Integration;

[TestFixture]
public class BasicStyleApplicationStrategyTests
{
    private IBrowsingContext _context;
    private BasicStyleApplicationStrategy _strategy;
    private Mock<IStyleEngine> _mockStyleEngine;
    private IHtmlParser _parser;
    private IDocument _document;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");

        // Mock IStyleEngine interface directly
        _mockStyleEngine = new Mock<IStyleEngine>();
        _strategy = new BasicStyleApplicationStrategy(_mockStyleEngine.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _document?.Dispose();
    }

    [Test]
    public void ShouldSkipSubtree_WithDisplayNone_ReturnsTrue()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var mockStyle = new Mock<IComputedStyle>();
        mockStyle.Setup(s => s.Display).Returns(DisplayMode.None);

        _mockStyleEngine.Setup(e => e.ComputeElementStyle(element, null))
            .Returns(mockStyle.Object);

        // Act
        var result = _strategy.ShouldSkipSubtree(element);

        // Assert
        Assert.That(result, Is.True, "Should skip subtree for display:none elements");
    }

    [Test]
    public void ShouldSkipSubtree_WithDisplayBlock_ReturnsFalse()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var mockStyle = new Mock<IComputedStyle>();
        mockStyle.Setup(s => s.Display).Returns(DisplayMode.Block);

        _mockStyleEngine.Setup(e => e.ComputeElementStyle(element, null))
            .Returns(mockStyle.Object);

        // Act
        var result = _strategy.ShouldSkipSubtree(element);

        // Assert
        Assert.That(result, Is.False, "Should not skip subtree for display:block elements");
    }

    [Test]
    public void CanShareStyleWith_IdenticalElements_ReturnsTrue()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        element1.ClassName = "test";

        var element2 = _document.CreateElement("div");
        element2.ClassName = "test";

        var parent = _document.CreateElement("section");
        parent.AppendChild(element1);
        parent.AppendChild(element2);
        _document.Body!.AppendChild(parent);

        // Act
        var result = _strategy.CanShareStyleWith(element1, element2);

        // Assert
        Assert.That(result, Is.True, "Should be able to share style between identical elements");
    }

    [Test]
    public void CanShareStyleWith_DifferentTagNames_ReturnsFalse()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span");

        var parent = _document.CreateElement("section");
        parent.AppendChild(element1);
        parent.AppendChild(element2);
        _document.Body!.AppendChild(parent);

        // Act
        var result = _strategy.CanShareStyleWith(element1, element2);

        // Assert
        Assert.That(result, Is.False, "Should not share style between elements with different tag names");
    }

    [Test]
    public void CanShareStyleWith_DifferentClasses_ReturnsFalse()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        element1.ClassName = "test1";

        var element2 = _document.CreateElement("div");
        element2.ClassName = "test2";

        var parent = _document.CreateElement("section");
        parent.AppendChild(element1);
        parent.AppendChild(element2);
        _document.Body!.AppendChild(parent);

        // Act
        var result = _strategy.CanShareStyleWith(element1, element2);

        // Assert
        Assert.That(result, Is.False, "Should not share style between elements with different classes");
    }

    [Test]
    public void CanShareStyleWith_WithStyleAttribute_ReturnsFalse()
    {
        // Arrange
        var element1 = _document.CreateElement("div");

        var element2 = _document.CreateElement("div");
        element2.SetAttribute("style", "color: red;");

        var parent = _document.CreateElement("section");
        parent.AppendChild(element1);
        parent.AppendChild(element2);
        _document.Body!.AppendChild(parent);

        // Act
        var result = _strategy.CanShareStyleWith(element1, element2);

        // Assert
        Assert.That(result, Is.False, "Should not share style when one element has a style attribute");
    }

    [Test]
    public void GetElementTraversalOrder_TraversesParentBeforeChildren()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child1 = _document.CreateElement("span");
        var child2 = _document.CreateElement("p");
        var grandchild = _document.CreateElement("a");

        child1.AppendChild(grandchild);
        parent.AppendChild(child1);
        parent.AppendChild(child2);
        _document.Body!.AppendChild(parent);

        // Mock style to avoid skipping any subtrees
        var mockStyle = new Mock<IComputedStyle>();
        mockStyle.Setup(s => s.Display).Returns(DisplayMode.Block);

        // Setup for any element
        _mockStyleEngine.Setup(e => e.ComputeElementStyle(It.IsAny<IElement>(), It.IsAny<string>()))
            .Returns(mockStyle.Object);

        // Act
        var traversalOrder = _strategy.GetElementTraversalOrder(parent).ToList();

        // Assert
        Assert.That(traversalOrder.Count, Is.EqualTo(4), "Should traverse all elements");
        Assert.That(traversalOrder[0], Is.EqualTo(parent), "Parent should be first");

        // Children should come after parent
        Assert.That(traversalOrder.IndexOf(parent), Is.LessThan(traversalOrder.IndexOf(child1)));
        Assert.That(traversalOrder.IndexOf(parent), Is.LessThan(traversalOrder.IndexOf(child2)));

        // Grandchild should come after its parent
        Assert.That(traversalOrder.IndexOf(child1), Is.LessThan(traversalOrder.IndexOf(grandchild)));
    }

    [Test]
    public void GetElementTraversalOrder_SkipsSubtreeWithDisplayNone()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child1 = _document.CreateElement("span");
        var child2 = _document.CreateElement("p");
        var grandchild = _document.CreateElement("a");

        child1.AppendChild(grandchild);
        parent.AppendChild(child1);
        parent.AppendChild(child2);
        _document.Body!.AppendChild(parent);

        var normalStyle = new Mock<IComputedStyle>();
        normalStyle.Setup(s => s.Display).Returns(DisplayMode.Block);

        var noneStyle = new Mock<IComputedStyle>();
        noneStyle.Setup(s => s.Display).Returns(DisplayMode.None);

        // Default to normal style
        _mockStyleEngine.Setup(e => e.ComputeElementStyle(It.IsAny<IElement>(), It.IsAny<string>()))
            .Returns(normalStyle.Object);

        // Only child1 has display:none
        _mockStyleEngine.Setup(e => e.ComputeElementStyle(child1, It.IsAny<string>()))
            .Returns(noneStyle.Object);

        // Act
        var traversalOrder = _strategy.GetElementTraversalOrder(parent).ToList();

        // Assert
        Assert.That(traversalOrder.Count, Is.EqualTo(3), "Should skip child1's subtree");
        Assert.That(traversalOrder[0], Is.EqualTo(parent), "Parent should be first");
        Assert.That(traversalOrder[1], Is.EqualTo(child1), "Child1 should be second");
        Assert.That(traversalOrder[2], Is.EqualTo(child2), "Child2 should be third");
        Assert.That(traversalOrder.Contains(grandchild), Is.False, "Grandchild should be skipped");
    }

    [Test]
    public void CreateStyleContext_SetsParentStyle()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        var parentStyle = new Mock<IComputedStyle>();
        _mockStyleEngine.Setup(e => e.ComputeElementStyle(parent, It.IsAny<string>()))
            .Returns(parentStyle.Object);

        // Act
        var context = _strategy.CreateStyleContext(child);

        // Assert
        Assert.That(context.Element, Is.EqualTo(child), "Context should reference the child element");
        Assert.That(context.ParentStyle, Is.EqualTo(parentStyle.Object), "ParentStyle should be set");
    }

    [Test]
    public void CreateStyleContext_IdentifiesStyleDonor()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child1 = _document.CreateElement("span");
        child1.ClassName = "test";
        var child2 = _document.CreateElement("span");
        child2.ClassName = "test";

        parent.AppendChild(child1);
        parent.AppendChild(child2);
        _document.Body!.AppendChild(parent);

        // Mock styles for traversal
        var mockStyle = new Mock<IComputedStyle>();
        mockStyle.Setup(s => s.Display).Returns(DisplayMode.Block);
        _mockStyleEngine.Setup(e => e.ComputeElementStyle(It.IsAny<IElement>(), It.IsAny<string>()))
            .Returns(mockStyle.Object);

        // Need to visit child1 first so it's in the elements-by-tag-name cache
        _strategy.GetElementTraversalOrder(parent).ToList();

        // Act
        var context = _strategy.CreateStyleContext(child2);

        // Assert
        Assert.That(context.StyleDonor, Is.EqualTo(child1),
            "Should identify child1 as style donor for child2");
    }

    [Test]
    public void CreateStyleContext_SetsVisibilityBasedOnParent()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        var invisibleParentStyle = new Mock<IComputedStyle>();
        invisibleParentStyle.Setup(s => s.Display).Returns(DisplayMode.None);

        _mockStyleEngine.Setup(e => e.ComputeElementStyle(parent, It.IsAny<string>()))
            .Returns(invisibleParentStyle.Object);

        // Act
        var context = _strategy.CreateStyleContext(child);

        // Assert
        Assert.That(context.IsVisible, Is.False,
            "Child of display:none parent should be marked as not visible");
    }
}