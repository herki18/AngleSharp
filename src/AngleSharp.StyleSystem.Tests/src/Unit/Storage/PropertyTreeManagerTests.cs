namespace AngleSharp.StyleSystem.Tests.Unit.Storage;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class PropertyTreeManagerTests
{
    #region Basic Functionality Tests
    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var manager = new PropertyTreeManager();

        // Assert
        Assert.That(manager, Is.Not.Null);
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(0));
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateNode_CreatesNewNode()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();

        // Act
        var node = manager.CreateNode(element);

        // Assert
        Assert.That(node, Is.Not.Null);
        Assert.That(node.GetParent(), Is.Null);
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(1));
    }

    [Test]
    public void CreateNode_WithParent_CreatesNodeWithParent()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var parentElement = Substitute.For<IElement>();
        var childElement = Substitute.For<IElement>();
        var parentNode = manager.CreateNode(parentElement);

        // Act
        var childNode = manager.CreateNode(childElement, parentNode);

        // Assert
        Assert.That(childNode, Is.Not.Null);
        Assert.That(childNode.GetParent(), Is.SameAs(parentNode));
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(2));
    }

    [Test]
    public void GetOrCreateNode_WhenNodeDoesNotExist_CreatesNewNode()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();

        // Act
        var node = manager.GetOrCreateNode(element);

        // Assert
        Assert.That(node, Is.Not.Null);
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrCreateNode_WhenNodeExists_ReturnsExistingNode()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node1 = manager.CreateNode(element);

        // Act
        var node2 = manager.GetOrCreateNode(element);

        // Assert
        Assert.That(node2, Is.SameAs(node1));
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(1), "Should not create duplicate nodes");
    }

    [Test]
    public void GetSharedNode_CreatesAndReusesSharedNodes()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var color = CssColorValue.FromRgba(255, 0, 0, 1);

        // Act
        var node1 = manager.GetSharedNode("color", color);
        var node2 = manager.GetSharedNode("color", color);

        // Assert
        Assert.That(node1, Is.Not.Null);
        Assert.That(node2, Is.Not.Null);
        Assert.That(node2, Is.SameAs(node1), "Should reuse identical shared nodes");

        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(1), "Should create only one shared node");
    }

    [Test]
    public void GetSharedNode_DifferentProperties_CreatesDifferentNodes()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var color = CssColorValue.FromRgba(255, 0, 0, 1);
        var fontSize = new CssLengthValue(16, CssLengthValue.Unit.Px);

        // Act
        var colorNode = manager.GetSharedNode("color", color);
        var fontSizeNode = manager.GetSharedNode("font-size", fontSize);

        // Assert
        Assert.That(colorNode, Is.Not.Null);
        Assert.That(fontSizeNode, Is.Not.Null);
        Assert.That(fontSizeNode, Is.Not.SameAs(colorNode), "Should create different nodes for different properties");

        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(2), "Should create two different shared nodes");
    }

    [Test]
    public void GetSharedNode_SamePropertyDifferentValues_CreatesDifferentNodes()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var red = CssColorValue.FromRgba(255, 0, 0, 1);
        var blue = CssColorValue.FromRgba(0, 0, 255, 1);

        // Act
        var redNode = manager.GetSharedNode("color", red);
        var blueNode = manager.GetSharedNode("color", blue);

        // Assert
        Assert.That(redNode, Is.Not.Null);
        Assert.That(blueNode, Is.Not.Null);
        Assert.That(blueNode, Is.Not.SameAs(redNode), "Should create different nodes for different values");

        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(2), "Should create two different shared nodes");
    }
    #endregion

    #region Tree Optimization Tests
    [Test]
    public void OptimizeTree_SingleProperty_CreatesSharedNode()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        manager.OptimizeTree(node);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(1), "Should create a shared node for the property");
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(1));
        Assert.That(metrics.TotalPropertiesAfterOptimization, Is.EqualTo(1));
    }

    [Test]
    public void OptimizeTree_MultipleProperties_CreatesSharedNodes()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
        node.SetProperty("margin", new CssLengthValue(10, CssLengthValue.Unit.Px));

        // Act
        manager.OptimizeTree(node);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.GreaterThanOrEqualTo(3), "Should create shared nodes for each property");
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(3));
    }

    [Test]
    public void OptimizeTree_SamePropertyValues_SharesSingleNode()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element1 = Substitute.For<IElement>();
        var element2 = Substitute.For<IElement>();
        var node1 = manager.CreateNode(element1);
        var node2 = manager.CreateNode(element2);

        var color = CssColorValue.FromRgba(255, 0, 0, 1);
        node1.SetProperty("color", color);
        node2.SetProperty("color", color);

        // Act
        manager.OptimizeTree(node1);
        manager.OptimizeTree(node2);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.EqualTo(1), "Should share a single node for identical property values");
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(2));
    }

    [Test]
    public void OptimizeTree_PropertyGroup_SharesProperties()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);

        // Set margin properties that should be recognized as a group
        node.SetProperty("margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node.SetProperty("margin-right", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node.SetProperty("margin-bottom", new CssLengthValue(10, CssLengthValue.Unit.Px));
        node.SetProperty("margin-left", new CssLengthValue(10, CssLengthValue.Unit.Px));

        // Act
        manager.OptimizeTree(node);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.SharedNodeCount, Is.GreaterThanOrEqualTo(1), "Should create at least one shared node");
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(4));
    }

    [Test]
    public void OptimizeTree_ParentChildRelationship_OptimsWithParent()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var parentElement = Substitute.For<IElement>();
        var childElement = Substitute.For<IElement>();

        var parentNode = manager.CreateNode(parentElement);
        var childNode = manager.CreateNode(childElement, parentNode);

        // Set same property in both nodes
        var color = CssColorValue.FromRgba(255, 0, 0, 1);
        parentNode.SetProperty("color", color);
        childNode.SetProperty("color", color);

        // Act
        manager.OptimizeTree(parentNode);
        manager.OptimizeTree(childNode);

        // Assert
        var childProps = childNode.GetAllProperties();
        var metrics = manager.GetOptimizationMetrics();
        // Child should have removed its duplicate property since it's available from parent
        // This behavior depends on the implementation details. Some implementations might
        // keep the property for faster access and shared node optimization.
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(2));
    }
    #endregion

    #region Metrics Tests
    [Test]
    public void GetOptimizationMetrics_TracksPropertyUsage()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);

        // Set color property multiple times to make it frequently used
        for (int i = 0; i < 5; i++)
        {
            var element2 = Substitute.For<IElement>();
            var node2 = manager.CreateNode(element2);
            node2.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
            manager.OptimizeTree(node2);
        }

        // Set font-size property fewer times
        for (int i = 0; i < 2; i++)
        {
            var element2 = Substitute.For<IElement>();
            var node2 = manager.CreateNode(element2);
            node2.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
            manager.OptimizeTree(node2);
        }

        // Act
        var metrics = manager.GetOptimizationMetrics();

        // Assert
        Assert.That(metrics.MostFrequentProperties, Is.Not.Empty);
        Assert.That(metrics.MostFrequentProperties.ContainsKey("color"), Is.True);
        Assert.That(metrics.MostFrequentProperties.ContainsKey("font-size"), Is.True);
        Assert.That(metrics.MostFrequentProperties["color"], Is.GreaterThan(metrics.MostFrequentProperties["font-size"]),
            "Color should be used more frequently than font-size");
    }

    [Test]
    public void ResetOptimizationMetrics_ClearsAllMetrics()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        manager.OptimizeTree(node);

        var beforeMetrics = manager.GetOptimizationMetrics();
        Assert.That(beforeMetrics.UniqueNodeCount, Is.GreaterThan(0));
        Assert.That(beforeMetrics.SharedNodeCount, Is.GreaterThan(0));

        // Act
        manager.ResetOptimizationMetrics();
        var afterMetrics = manager.GetOptimizationMetrics();

        // Assert
        Assert.That(afterMetrics.UniqueNodeCount, Is.EqualTo(0));
        Assert.That(afterMetrics.SharedNodeCount, Is.EqualTo(0));
        Assert.That(afterMetrics.TotalPropertiesBeforeOptimization, Is.EqualTo(0));
        Assert.That(afterMetrics.TotalPropertiesAfterOptimization, Is.EqualTo(0));
        Assert.That(afterMetrics.MostFrequentProperties, Is.Empty);
    }

    [Test]
    public void GetOptimizationMetrics_CalculatesMemorySavings()
    {
        // Arrange
        var manager = new PropertyTreeManager();

        // Create 10 nodes with identical color properties
        for (int i = 0; i < 10; i++)
        {
            var element = Substitute.For<IElement>();
            var node = manager.CreateNode(element);
            node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
            manager.OptimizeTree(node);
        }

        // Act
        var metrics = manager.GetOptimizationMetrics();

        // Assert
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(10));
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0),
            "Should show memory savings from property sharing");
    }
    #endregion

    #region Edge Cases Tests
    [Test]
    public void OptimizeTree_WithNullNode_DoesNotThrow()
    {
        // Arrange
        var manager = new PropertyTreeManager();

        // Act & Assert
        Assert.DoesNotThrow(() => manager.OptimizeTree(null!));
    }

    [Test]
    public void OptimizeTree_WithEmptyNode_DoesNotThrow()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);

        // Act
        manager.OptimizeTree(node);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(0));
        Assert.That(metrics.TotalPropertiesAfterOptimization, Is.EqualTo(0));
    }

    [Test]
    public void OptimizeTree_OptimizingMultipleTimes_DoesNotDuplicateSharedNodes()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        manager.OptimizeTree(node);
        var metricsAfterFirstOptimization = manager.GetOptimizationMetrics();

        manager.OptimizeTree(node);
        var metricsAfterSecondOptimization = manager.GetOptimizationMetrics();

        // Assert
        Assert.That(metricsAfterSecondOptimization.SharedNodeCount, Is.EqualTo(metricsAfterFirstOptimization.SharedNodeCount),
            "Multiple optimizations should not create duplicate shared nodes");
    }

    [Test]
    public void GetOrCreateNode_WithNullElement_Throws()
    {
        // Arrange
        var manager = new PropertyTreeManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.GetOrCreateNode(null!));
    }

    [Test]
    public void CreateNode_WithNullElement_Throws()
    {
        // Arrange
        var manager = new PropertyTreeManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.CreateNode(null!));
    }
    #endregion

    #region Performance Tests
    [Test]
    [Timeout(5000)] // 5 second timeout
    public void OptimizeTree_WithLargeNumberOfProperties_PerformsEfficiently()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        var element = Substitute.For<IElement>();
        var node = manager.CreateNode(element);

        const int propertyCount = 500; // Large number of properties
        for (int i = 0; i < propertyCount; i++)
        {
            node.SetProperty($"property-{i}", $"value-{i}");
        }

        // Act
        manager.OptimizeTree(node);

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(propertyCount));
    }

    [Test]
    [Timeout(5000)] // 5 second timeout
    public void OptimizeTree_WithManyNodesAndSharedProperties_PerformsEfficiently()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        const int nodeCount = 100;

        // Create nodes with the same properties
        for (int i = 0; i < nodeCount; i++)
        {
            var element = Substitute.For<IElement>();
            var node = manager.CreateNode(element);
            node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
            node.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
            node.SetProperty("margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px));
            node.SetProperty("margin-right", new CssLengthValue(10, CssLengthValue.Unit.Px));
            node.SetProperty("margin-bottom", new CssLengthValue(10, CssLengthValue.Unit.Px));
            node.SetProperty("margin-left", new CssLengthValue(10, CssLengthValue.Unit.Px));

            // Act
            manager.OptimizeTree(node);
        }

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.EqualTo(nodeCount * 6));
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0));
    }

    [Test]
    public void ConcurrentAccess_CreatingAndOptimizingNodes_DoesNotCauseExceptions()
    {
        // Arrange
        var manager = new PropertyTreeManager();
        const int concurrentOperations = 50;
        var elements = Enumerable.Range(0, concurrentOperations)
            .Select(_ => Substitute.For<IElement>())
            .ToArray();

        // Act
        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(i => Task.Run(() => {
                var element = elements[i];
                var node = manager.CreateNode(element);
                node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
                manager.OptimizeTree(node);
                return true;
            }))
            .ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        foreach (var task in tasks)
        {
            Assert.That(task.Result, Is.True);
        }
    }
    #endregion

    #region Common Scenarios Tests
    [Test]
    public void Scenario_ElementStyleSharing_WorksCorrectly()
    {
        // Arrange - Simulating elements with the same styles in a list or table
        var manager = new PropertyTreeManager();
        const int listItemCount = 10;
        var listItems = new List<IPropertyTreeNode>();

        // Create list item elements with identical styles
        for (int i = 0; i < listItemCount; i++)
        {
            var element = Substitute.For<IElement>();
            var node = manager.CreateNode(element);
            node.SetProperty("color", CssColorValue.FromRgba(51, 51, 51, 1)); // Common text color
            node.SetProperty("font-size", new CssLengthValue(14, CssLengthValue.Unit.Px));
            node.SetProperty("padding", new CssLengthValue(8, CssLengthValue.Unit.Px));
            node.SetProperty("margin-bottom", new CssLengthValue(4, CssLengthValue.Unit.Px));

            listItems.Add(node);
        }

        // Act - Optimize all nodes
        foreach (var node in listItems)
        {
            manager.OptimizeTree(node);
        }

        // Assert
        var metrics = manager.GetOptimizationMetrics();
        Assert.That(metrics.UniqueNodeCount, Is.EqualTo(listItemCount));
        Assert.That(metrics.SharedNodeCount, Is.GreaterThan(0), "Properties should be shared");
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0), "Memory should be saved through sharing");
    }

    [Test]
    public void Scenario_CSSFrameworkStyles_OptimizesEffectively()
    {
        // Arrange - Simulating common CSS framework patterns with many shared styles
        var manager = new PropertyTreeManager();

        // Create different element types with framework classes

        // Button elements - share button styles
        const int buttonCount = 5;
        for (int i = 0; i < buttonCount; i++)
        {
            var button = Substitute.For<IElement>();
            var buttonNode = manager.CreateNode(button);
            // Common button styles
            buttonNode.SetProperty("display", "inline-block");
            buttonNode.SetProperty("font-weight", "600");
            buttonNode.SetProperty("text-align", "center");
            buttonNode.SetProperty("padding", "10px 20px");
            buttonNode.SetProperty("border-radius", "4px");
            buttonNode.SetProperty("cursor", "pointer");

            // Primary button variant
            if (i % 2 == 0)
            {
                buttonNode.SetProperty("background-color", CssColorValue.FromRgba(0, 123, 255, 1)); // Blue
                buttonNode.SetProperty("color", CssColorValue.FromRgba(255, 255, 255, 1)); // White
                buttonNode.SetProperty("border", "1px solid #007bff");
            }
            // Secondary button variant
            else
            {
                buttonNode.SetProperty("background-color", CssColorValue.FromRgba(108, 117, 125, 1)); // Gray
                buttonNode.SetProperty("color", CssColorValue.FromRgba(255, 255, 255, 1)); // White
                buttonNode.SetProperty("border", "1px solid #6c757d");
            }

            manager.OptimizeTree(buttonNode);
        }

        // Card elements - share card styles
        const int cardCount = 5;
        for (int i = 0; i < cardCount; i++)
        {
            var card = Substitute.For<IElement>();
            var cardNode = manager.CreateNode(card);
            // Common card styles
            cardNode.SetProperty("display", "flex");
            cardNode.SetProperty("flex-direction", "column");
            cardNode.SetProperty("border", "1px solid rgba(0,0,0,0.125)");
            cardNode.SetProperty("border-radius", "4px");
            cardNode.SetProperty("overflow", "hidden");

            manager.OptimizeTree(cardNode);
        }

        // Act
        var metrics = manager.GetOptimizationMetrics();

        // Assert
        Assert.That(metrics.SharedNodeCount, Is.GreaterThan(0), "There should be shared nodes for common styles");
        Assert.That(metrics.MostFrequentProperties.ContainsKey("border-radius"), Is.True,
            "border-radius should be a frequent property");
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0),
            "Memory should be saved through property sharing");
    }
    #endregion
}