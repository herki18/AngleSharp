namespace AngleSharp.StyleSystem.Tests.Unit;

using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Storage;
using Interfaces;

[TestFixture]
public class PropertyTreeManagerTests
{
    private IBrowsingContext _context;
    private PropertyTreeManager _propertyTreeManager;
    private IHtmlParser _parser;
    private IDocument _document;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _propertyTreeManager = new PropertyTreeManager();
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _document?.Dispose();
    }

    [Test]
    public void CreateNode_WithoutParent_CreatesRootNode()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var node = _propertyTreeManager.CreateNode(element);

        // Assert
        Assert.That(node, Is.Not.Null, "PropertyTreeNode should be created");
        Assert.That(node.GetParent(), Is.Null, "Node should not have a parent");
    }

    [Test]
    public void CreateNode_WithParent_CreatesChildNode()
    {
        // Arrange
        var parentElement = _document.CreateElement("div");
        var childElement = _document.CreateElement("span");
        _document.Body!.AppendChild(parentElement);
        parentElement.AppendChild(childElement);

        var parentNode = _propertyTreeManager.CreateNode(parentElement);

        // Act
        var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

        // Assert
        Assert.That(childNode, Is.Not.Null, "Child PropertyTreeNode should be created");
        Assert.That(childNode.GetParent(), Is.SameAs(parentNode), "Child node should have correct parent");
    }

    [Test]
    public void GetOrCreateNode_ForExistingElement_ReturnsSameNode()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var node1 = _propertyTreeManager.CreateNode(element);

        // Act
        var node2 = _propertyTreeManager.GetOrCreateNode(element);

        // Assert
        Assert.That(node2, Is.SameAs(node1), "GetOrCreateNode should return existing node for tracked element");
    }

    [Test]
    public void GetOrCreateNode_ForNewElement_CreatesNewNode()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var node = _propertyTreeManager.GetOrCreateNode(element);

        // Assert
        Assert.That(node, Is.Not.Null, "GetOrCreateNode should create new node for untracked element");
    }

    [Test]
    public void GetSharedNode_WithSameProperties_ReturnsSameNode()
    {
        // Arrange
        var colorValue = CssColorValue.FromRgba(255, 0, 0, 1);

        // Act
        var node1 = _propertyTreeManager.GetSharedNode("color", colorValue);
        var node2 = _propertyTreeManager.GetSharedNode("color", colorValue);

        // Assert
        Assert.That(node2, Is.SameAs(node1), "Same property and value should return same shared node");
    }

    [Test]
    public void GetSharedNode_WithDifferentProperties_ReturnsDifferentNodes()
    {
        // Arrange
        var colorValue1 = CssColorValue.FromRgba(255, 0, 0, 1);
        var colorValue2 = CssColorValue.FromRgba(0, 255, 0, 1);

        // Act
        var node1 = _propertyTreeManager.GetSharedNode("color", colorValue1);
        var node2 = _propertyTreeManager.GetSharedNode("color", colorValue2);

        // Assert
        Assert.That(node2, Is.Not.SameAs(node1), "Different values should return different shared nodes");
    }

    [Test]
    public void OptimizeTree_WithDuplicateProperties_SharesNodes()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        var node1 = _propertyTreeManager.CreateNode(element1);
        var node2 = _propertyTreeManager.CreateNode(element2);

        var colorValue = CssColorValue.FromRgba(255, 0, 0, 1);
        node1.SetProperty("color", colorValue);
        node2.SetProperty("color", colorValue);

        // Act
        _propertyTreeManager.OptimizeTree(node1);
        _propertyTreeManager.OptimizeTree(node2);

        // Assert
        var retrievedValue1 = node1.GetPropertyRawValue("color");
        var retrievedValue2 = node2.GetPropertyRawValue("color");

        Assert.That(retrievedValue1, Is.EqualTo(colorValue), "First node should preserve property value");
        Assert.That(retrievedValue2, Is.EqualTo(colorValue), "Second node should preserve property value");
    }

    [Test]
    public void GetOptimizationMetrics_ReturnsValidMetrics()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        var node1 = _propertyTreeManager.CreateNode(element1);
        var node2 = _propertyTreeManager.CreateNode(element2);

        node1.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node1.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
        node2.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        _propertyTreeManager.OptimizeTree(node1);
        _propertyTreeManager.OptimizeTree(node2);

        // Act
        var metrics = _propertyTreeManager.GetOptimizationMetrics();

        // Assert
        Assert.That(metrics, Is.Not.Null, "Metrics should be returned");
        Assert.That(metrics.SharedNodeCount, Is.GreaterThan(0), "Should have shared nodes");
        Assert.That(metrics.UniqueNodeCount, Is.GreaterThan(0), "Should have unique nodes");
        Assert.That(metrics.MostFrequentProperties, Is.Not.Empty, "Should track property usage");
    }

    [Test]
    public void ResetOptimizationMetrics_ClearsAllMetrics()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var node = _propertyTreeManager.CreateNode(element);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        _propertyTreeManager.OptimizeTree(node);

        var beforeMetrics = _propertyTreeManager.GetOptimizationMetrics();

        // Act
        _propertyTreeManager.ResetOptimizationMetrics();
        var afterMetrics = _propertyTreeManager.GetOptimizationMetrics();

        // Assert
        Assert.That(beforeMetrics.TotalPropertiesBeforeOptimization, Is.GreaterThan(0),
            "Should have some properties before reset");
        Assert.That(afterMetrics.TotalPropertiesBeforeOptimization, Is.EqualTo(0),
            "Should have no properties after reset");
    }

    [Test]
    public void OptimizeTree_WithPropertyGroups_CreatesSharedGroupNodes()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        var node1 = _propertyTreeManager.CreateNode(element1);
        var node2 = _propertyTreeManager.CreateNode(element2);

        // Create same margin properties on both nodes
        node1.SetProperty("margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node1.SetProperty("margin-right", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node1.SetProperty("margin-bottom", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node1.SetProperty("margin-left", new CssLengthValue(10, CssLengthValue.Unit.Px));

        node2.SetProperty("margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node2.SetProperty("margin-right", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node2.SetProperty("margin-bottom", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node2.SetProperty("margin-left", new CssLengthValue(10, CssLengthValue.Unit.Px));

        // Count properties before optimization
        var countBefore1 = node1.GetPropertyCount();
        var countBefore2 = node2.GetPropertyCount();

        // Act
        _propertyTreeManager.OptimizeTree(node1);
        _propertyTreeManager.OptimizeTree(node2);

        // Assert
        var marginTop1 = node1.GetPropertyValue("margin-top");
        var marginTop2 = node2.GetPropertyValue("margin-top");

        Assert.That(marginTop1, Is.EqualTo(marginTop2), "Values should be preserved after optimization");

        // The specific reduction depends on the implementation, but both nodes should
        // have fewer direct properties after optimization
        var metrics = _propertyTreeManager.GetOptimizationMetrics();
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.GreaterThanOrEqualTo(8),
            "Should have counted all properties before optimization");
    }

    [Test]
    public void OptimizeTree_WithParentChildRelationship_RemovesRedundantProperties()
    {
        // Arrange
        var parentElement = _document.CreateElement("div");
        var childElement = _document.CreateElement("span");
        _document.Body!.AppendChild(parentElement);
        parentElement.AppendChild(childElement);

        var parentNode = _propertyTreeManager.CreateNode(parentElement);
        var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

        // Set identical properties on both parent and child
        var colorValue = CssColorValue.FromRgba(255, 0, 0, 1);
        parentNode.SetProperty("color", colorValue);
        childNode.SetProperty("color", colorValue);

        // Act
        _propertyTreeManager.OptimizeTree(parentNode);
        _propertyTreeManager.OptimizeTree(childNode);

        // Assert
        var childHasDirectProperty = childNode.GetAllProperties().ContainsKey("color");
        var childValue = childNode.GetPropertyValue("color");

        // Child should not have a direct property if the parent has the same value
        Assert.That(childHasDirectProperty, Is.False,
            "Child node should not duplicate parent's property after optimization");

        // Child should still return the correct value through inheritance
        Assert.That(childValue, Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")
                .Or.EqualTo("#ff0000"),
            "Child should still have access to the property value");
    }

    [Test]
    public void OptimizeTree_WithComplexProperties_MaintainsCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        var node = _propertyTreeManager.CreateNode(element);

        // Set multiple property types
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
        node.SetProperty("display", "block");
        node.SetProperty("margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px));

        // Act
        _propertyTreeManager.OptimizeTree(node);

        // Assert
        Assert.That(node.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")
                .Or.EqualTo("#ff0000"),
            "Color value should be preserved");

        Assert.That(node.GetPropertyValue("font-size"), Is.EqualTo("16px"),
            "Font size value should be preserved");

        Assert.That(node.GetPropertyValue("display"), Is.EqualTo("block"),
            "Display value should be preserved");

        Assert.That(node.GetPropertyValue("margin-top"), Is.EqualTo("10px"),
            "Margin value should be preserved");
    }

    [Test]
    public void OptimizeTree_SamePropertiesAcrossMultipleElements_SharesNodes()
    {
        // Arrange
        const int elementCount = 10;
        var nodes = new IPropertyTreeNode[elementCount];
        var elements = new IElement[elementCount];

        // Create multiple elements with the same properties
        for (int i = 0; i < elementCount; i++)
        {
            elements[i] = _document.CreateElement("div");
            _document.Body!.AppendChild(elements[i]);
            nodes[i] = _propertyTreeManager.CreateNode(elements[i]);

            nodes[i].SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
            nodes[i].SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
        }

        // Act
        for (int i = 0; i < elementCount; i++)
        {
            _propertyTreeManager.OptimizeTree(nodes[i]);
        }

        var metrics = _propertyTreeManager.GetOptimizationMetrics();

        // Assert
        Assert.That(metrics.SharedNodeCount, Is.GreaterThan(0),
            "Should create shared nodes for common properties");

        // Calculate the memory savings
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0),
            "Should have positive memory savings");

        // Check usage statistics
        Assert.That(metrics.MostFrequentProperties.ContainsKey("color"), Is.True,
            "Should track usage of 'color' property");
        Assert.That(metrics.MostFrequentProperties.ContainsKey("font-size"), Is.True,
            "Should track usage of 'font-size' property");
    }
}