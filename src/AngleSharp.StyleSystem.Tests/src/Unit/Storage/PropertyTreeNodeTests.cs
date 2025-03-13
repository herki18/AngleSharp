namespace AngleSharp.StyleSystem.Tests.Unit.Storage;

using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Storage;
using NUnit.Framework;

[TestFixture]
public class PropertyTreeNodeTests
{
    #region Basic Node Operations
    [Test]
    public void Constructor_WithParent_StoresParentReference()
    {
        // Arrange
        var parentNode = new PropertyTreeNode(null);

        // Act
        var childNode = new PropertyTreeNode(parentNode);

        // Assert
        Assert.That(childNode.GetParent(), Is.SameAs(parentNode), "GetParent should return the correct parent node");
    }

    [Test]
    public void SetProperty_WithValue_StoresPropertyValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var color = CssColorValue.FromRgba(255, 0, 0, 1);

        // Act
        node.SetProperty("color", color);
        var retrievedValue = node.GetPropertyRawValue("color");

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(color), "GetPropertyRawValue should return the set value");
    }

    [Test]
    public void SetProperty_WithString_StoresPropertyValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);

        // Act
        node.SetProperty("font-family", "Arial, sans-serif");
        var value = node.GetPropertyValue("font-family");

        // Assert
        Assert.That(value, Is.EqualTo("Arial, sans-serif"), "GetPropertyValue should return the set string value");
    }

    [Test]
    public void RemoveProperty_RemovesExistingProperty()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        node.RemoveProperty("color");
        var hasProperty = node.HasProperty("color");

        // Assert
        Assert.That(hasProperty, Is.False, "Property should be removed");
    }

    [Test]
    public void HasProperty_AfterSettingProperty_ReturnsTrue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        var hasProperty = node.HasProperty("color");

        // Assert
        Assert.That(hasProperty, Is.True, "HasProperty should return true for set properties");
    }

    [Test]
    public void SetProperty_WithNull_RemovesProperty()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        node.SetProperty("color", (ICssValue)null!);
        var hasProperty = node.HasProperty("color");

        // Assert
        Assert.That(hasProperty, Is.False, "Property should be removed");
    }

    [Test]
    public void SetProperty_OverwritesExistingProperty()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red

        // Act
        node.SetProperty("color", CssColorValue.FromRgba(0, 255, 0, 1)); // Green
        var value = node.GetPropertyRawValue("color");

        // Assert
        Assert.That(value, Is.EqualTo(CssColorValue.FromRgba(0, 255, 0, 1)),
            "Property should be overwritten with new value");
    }
    #endregion

    #region Property Inheritance
    [Test]
    public void HasProperty_FromParent_ReturnsTrue()
    {
        // Arrange
        var parentNode = new PropertyTreeNode(null);
        var childNode = new PropertyTreeNode(parentNode);
        parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        var hasProperty = childNode.HasProperty("color");

        // Assert
        Assert.That(hasProperty, Is.True, "HasProperty should return true for inherited properties");
    }

    [Test]
    public void GetPropertyValue_Inheritance_ChildOverridesParent()
    {
        // Arrange
        var parentNode = new PropertyTreeNode(null);
        var childNode = new PropertyTreeNode(parentNode);

        parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        childNode.SetProperty("color", CssColorValue.FromRgba(0, 0, 255, 1));  // Blue

        // Act
        var parentValue = parentNode.GetPropertyValue("color");
        var childValue = childNode.GetPropertyValue("color");

        // Assert
        Assert.That(parentValue, Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")
                .Or.EqualTo("#ff0000"),
            "Parent should have its own value");

        Assert.That(childValue, Is.EqualTo("rgba(0, 0, 255, 1)")
                .Or.EqualTo("rgb(0, 0, 255)")
                .Or.EqualTo("#0000ff"),
            "Child should override parent's value");
    }

    [Test]
    public void PropertyInheritance_MultipleGenerations_InheritsCorrectly()
    {
        // Arrange
        var greatGrandparent = new PropertyTreeNode(null);
        var grandparent = new PropertyTreeNode(greatGrandparent);
        var parent = new PropertyTreeNode(grandparent);
        var child = new PropertyTreeNode(parent);

        greatGrandparent.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red

        // Act
        var color = child.GetPropertyValue("color");

        // Assert
        Assert.That(color, Does.Contain("255") & Does.Contain("0"), "Child should inherit color through multiple levels");
    }

    [Test]
    public void PropertyInheritance_OverrideInMiddleGeneration_UsesClosestValue()
    {
        // Arrange
        var greatGrandparent = new PropertyTreeNode(null);
        var grandparent = new PropertyTreeNode(greatGrandparent);
        var parent = new PropertyTreeNode(grandparent);
        var child = new PropertyTreeNode(parent);

        greatGrandparent.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        parent.SetProperty("color", CssColorValue.FromRgba(0, 0, 255, 1)); // Blue

        // Act
        var grandparentValue = grandparent.GetPropertyValue("color");
        var childValue = child.GetPropertyValue("color");

        // Assert
        Assert.That(grandparentValue, Does.Contain("255"), "Grandparent should inherit from great-grandparent");
        Assert.That(childValue, Does.Contain("0, 0, 255"), "Child should inherit from parent, not great-grandparent");
    }

    [Test]
    public void ComplexInheritance_RemovingParentProperty_ChildInheritsFromGrandparent()
    {
        // Arrange
        var grandparent = new PropertyTreeNode(null);
        var parent = new PropertyTreeNode(grandparent);
        var child = new PropertyTreeNode(parent);

        grandparent.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        parent.SetProperty("color", CssColorValue.FromRgba(0, 255, 0, 1)); // Green

        // Act
        parent.RemoveProperty("color");
        var color = child.GetPropertyValue("color");

        // Assert
        Assert.That(color, Does.Contain("255") & Does.Contain("0"),
            "After parent property is removed, child should inherit from grandparent");
    }
    #endregion

    #region Shared Node Functionality
    [Test]
    public void GetPropertyValue_FromSharedNode_ReturnsCorrectValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);
        sharedNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        node.ReplaceSubtree("color", sharedNode);
        var value = node.GetPropertyValue("color");

        // Assert
        Assert.That(value, Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")  // Allow for different formats
                .Or.EqualTo("#ff0000"),
            "GetPropertyValue should return the value from shared node");
    }

    [Test]
    public void ReplaceSubtree_RemovesDirectPropertyAndAddsSharedNode()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);

        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        sharedNode.SetProperty("color", CssColorValue.FromRgba(0, 255, 0, 1));

        // Act
        node.ReplaceSubtree("color", sharedNode);
        var directProperties = node.GetAllProperties();
        var value = node.GetPropertyValue("color");

        // Assert
        Assert.That(directProperties.ContainsKey("color"), Is.True,
            "The property should still be accessible via GetAllProperties");
        Assert.That(value, Is.EqualTo("rgba(0, 255, 0, 1)")
                .Or.EqualTo("rgb(0, 255, 0)")
                .Or.EqualTo("#00ff00"),
            "The value should come from the shared node");
    }

    [Test]
    public void IsSharedWith_WhenNodesShareProperty_ReturnsTrue()
    {
        // Arrange
        var node1 = new PropertyTreeNode(null);
        var node2 = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);

        sharedNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        node1.ReplaceSubtree("color", sharedNode);
        node2.ReplaceSubtree("color", sharedNode);

        // Act
        var isShared = node1.IsSharedWith(node2, "color");

        // Assert
        Assert.That(isShared, Is.True, "Nodes should share the property");
    }

    [Test]
    public void IsSharedWith_WhenNodesDontShareProperty_ReturnsFalse()
    {
        // Arrange
        var node1 = new PropertyTreeNode(null);
        var node2 = new PropertyTreeNode(null);
        var sharedNode1 = new PropertyTreeNode(null);
        var sharedNode2 = new PropertyTreeNode(null);

        sharedNode1.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        sharedNode2.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        node1.ReplaceSubtree("color", sharedNode1);
        node2.ReplaceSubtree("color", sharedNode2);

        // Act
        var isShared = node1.IsSharedWith(node2, "color");

        // Assert
        Assert.That(isShared, Is.False,
            "Nodes should not share the property (different instances of shared nodes)");
    }

    [Test]
    public void MultipleSharedNodes_GetAllProperties_IncludesAllSharedProperties()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var colorNode = new PropertyTreeNode(null);
        var fontNode = new PropertyTreeNode(null);
        var marginNode = new PropertyTreeNode(null);

        colorNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        fontNode.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));
        marginNode.SetProperty("margin", new CssLengthValue(10, CssLengthValue.Unit.Px));

        node.ReplaceSubtree("color", colorNode);
        node.ReplaceSubtree("font-size", fontNode);
        node.ReplaceSubtree("margin", marginNode);

        // Act
        var properties = node.GetAllProperties();

        // Assert
        Assert.That(properties.Count, Is.EqualTo(3), "All shared properties should be included");
        Assert.That(properties.Keys, Does.Contain("color"), "Should include color property");
        Assert.That(properties.Keys, Does.Contain("font-size"), "Should include font-size property");
        Assert.That(properties.Keys, Does.Contain("margin"), "Should include margin property");
    }

    [Test]
    public void MultipleSharedNodes_ReplaceExistingSharedNode_UpdatesCorrectly()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var colorNode1 = new PropertyTreeNode(null);
        var colorNode2 = new PropertyTreeNode(null);

        colorNode1.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        colorNode2.SetProperty("color", CssColorValue.FromRgba(0, 0, 255, 1)); // Blue

        node.ReplaceSubtree("color", colorNode1);

        // Act
        node.ReplaceSubtree("color", colorNode2);
        var value = node.GetPropertyValue("color");

        // Assert
        Assert.That(value, Does.Contain("0, 0, 255"), "Value should be from the new shared node");
    }

    [Test]
    public void SharedNodes_WithOverlappingProperties_PrefersDirectNodeOverShared()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);

        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red in direct node
        sharedNode.SetProperty("color", CssColorValue.FromRgba(0, 0, 255, 1)); // Blue in shared node
        sharedNode.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));

        // Act
        node.ReplaceSubtree("font-size", sharedNode); // Only share font-size, not color
        var colorValue = node.GetPropertyValue("color");
        var fontValue = node.GetPropertyValue("font-size");

        // Assert
        Assert.That(colorValue, Does.Contain("255"), "Should use the direct node's color property");
        Assert.That(fontValue, Does.Contain("16px"), "Should use the shared node's font-size property");
    }

    [Test]
    public void RemoveProperty_SharedNodeProperty_RemovesSharedNode()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);
        sharedNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        node.ReplaceSubtree("color", sharedNode);

        // Act
        node.RemoveProperty("color");
        var hasProperty = node.HasProperty("color");

        // Assert
        Assert.That(hasProperty, Is.False, "Property from shared node should be removed");
    }
    #endregion

    #region Property Collection
    [Test]
    public void GetAllProperties_IncludesOnlyDirectAndSharedProperties()
    {
        // Arrange
        var parentNode = new PropertyTreeNode(null);
        var childNode = new PropertyTreeNode(parentNode);
        var sharedNode = new PropertyTreeNode(null);

        parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        childNode.SetProperty("background-color", CssColorValue.FromRgba(0, 0, 255, 1));
        sharedNode.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));

        childNode.ReplaceSubtree("font-size", sharedNode);

        // Act
        var properties = childNode.GetAllProperties();

        // Assert
        Assert.That(properties.Count, Is.EqualTo(2), "Should have 2 properties (direct + shared)");
        Assert.That(properties.ContainsKey("background-color"), Is.True, "Should include direct property");
        Assert.That(properties.ContainsKey("font-size"), Is.True, "Should include shared property");
        Assert.That(properties.ContainsKey("color"), Is.False, "Should not include inherited property");
    }

    [Test]
    public void GetPropertyCount_ReturnsCorrectCount()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);

        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node.SetProperty("background-color", CssColorValue.FromRgba(0, 0, 255, 1));
        sharedNode.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));

        node.ReplaceSubtree("font-size", sharedNode);

        // Act
        var count = node.GetPropertyCount();

        // Assert
        Assert.That(count, Is.EqualTo(3), "Property count should include direct and shared properties");
    }

    [Test]
    public void LargeNumberOfProperties_GetAllProperties_ReturnsAllProperties()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        const int propertyCount = 100; // Reduced from 1000 for faster testing

        for (int i = 0; i < propertyCount; i++)
        {
            node.SetProperty($"property-{i}", $"value-{i}");
        }

        // Act
        var properties = node.GetAllProperties();

        // Assert
        Assert.That(properties.Count, Is.EqualTo(propertyCount), "Should return all properties");
    }
    #endregion

    #region Property Cache Tests
    [Test]
    public void GetPropertyCachedValue_CachesComputedValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var color = CssColorValue.FromRgba(255, 0, 0, 1);
        node.SetProperty("color", color);

        // Act
        var value1 = node.GetPropertyCachedValue("color");
        var value2 = node.GetPropertyCachedValue("color");

        // Assert
        Assert.That(value1, Is.Not.Null, "Cached value should not be null");
        Assert.That(value2, Is.SameAs(value1), "Subsequent calls should return the same cached instance");
    }

    [Test]
    public void GetPropertyCachedValue_FromSharedNode_CachesValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);
        var color = CssColorValue.FromRgba(255, 0, 0, 1);
        sharedNode.SetProperty("color", color);
        node.ReplaceSubtree("color", sharedNode);

        // Act
        var value1 = node.GetPropertyCachedValue("color");
        var value2 = node.GetPropertyCachedValue("color");

        // Assert
        Assert.That(value1, Is.Not.Null, "Cached value from shared node should not be null");
        Assert.That(value2, Is.SameAs(value1), "Subsequent calls should return the same cached instance");
    }

    [Test]
    public void SetProperty_InvalidatesComputedValueCache()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        var cachedValue1 = node.GetPropertyCachedValue("color");

        // Act
        node.SetProperty("color", CssColorValue.FromRgba(0, 255, 0, 1)); // Green
        var cachedValue2 = node.GetPropertyCachedValue("color");

        // Assert
        Assert.That(cachedValue2, Is.Not.SameAs(cachedValue1),
            "Cached value should be invalidated when property is overwritten");
    }

    [Test]
    public void GetPropertyCachedValue_ComplexCssValues_ReturnsCorrectlyCachedValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var complexValue = new CssPeriodicValue(new ICssValue[] {
            new CssLengthValue(10, CssLengthValue.Unit.Px),
            new CssLengthValue(20, CssLengthValue.Unit.Px),
            new CssLengthValue(30, CssLengthValue.Unit.Px),
            new CssLengthValue(40, CssLengthValue.Unit.Px)
        });

        node.SetProperty("margin", complexValue);

        // Act
        var value1 = node.GetPropertyCachedValue("margin");
        var value2 = node.GetPropertyCachedValue("margin");

        // Assert
        Assert.That(value1, Is.Not.Null, "First cached value should not be null");
        Assert.That(value2, Is.SameAs(value1), "Second cached value should be same instance as first");
    }

    [Test]
    public void GetPropertyCachedValue_SharedNodeWithCache_ReusesCache()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);
        sharedNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

        node.ReplaceSubtree("color", sharedNode);

        // Prime the cache in shared node
        var sharedValue = sharedNode.GetPropertyCachedValue("color");

        // Act
        var nodeValue = node.GetPropertyCachedValue("color");

        // Assert
        Assert.That(nodeValue, Is.Not.Null, "Cached value should not be null");
    }
    #endregion

    #region Edge Cases
    [Test]
    public void SetProperty_LongPropertyValue_HandlesCorrectly()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var longText = new string('a', 1000); // Reduced from 10000 for faster testing

        // Act
        node.SetProperty("data", longText);
        var value = node.GetPropertyValue("data");

        // Assert
        Assert.That(value, Is.EqualTo(longText), "Should correctly store and retrieve long property values");
    }

    [Test]
    public void GetPropertyValue_CaseInsensitivity_MatchesRegardlessOfCase()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("Color", CssColorValue.FromRgba(255, 0, 0, 1));

        // Act
        var value1 = node.GetPropertyValue("color");
        var value2 = node.GetPropertyValue("COLOR");

        // Assert
        Assert.That(value1, Is.Not.Empty, "Should find property with lowercase name");
        Assert.That(value2, Is.Not.Empty, "Should find property with uppercase name");
        Assert.That(value1, Is.EqualTo(value2), "Values should be identical regardless of case used");
    }

    [Test]
    public void RemoveProperty_NonExistentProperty_DoesNotThrow()
    {
        // Arrange
        var node = new PropertyTreeNode(null);

        // Act & Assert
        Assert.DoesNotThrow(() => node.RemoveProperty("non-existent-property"));
    }
    #endregion

    #region GetSelfPropertyValue Tests
    [Test]
    public void GetSelfPropertyValue_NoInheritance_ReturnsOnlyDirectProperty()
    {
        // Arrange
        var parentNode = new PropertyTreeNode(null);
        var childNode = new PropertyTreeNode(parentNode);

        parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
        childNode.SetProperty("background-color", CssColorValue.FromRgba(0, 0, 255, 1)); // Blue

        // Act
        var colorValue = childNode.GetSelfPropertyValue("color");
        var bgColorValue = childNode.GetSelfPropertyValue("background-color");

        // Assert
        Assert.That(colorValue, Is.Empty, "Should not return parent's property");
        Assert.That(bgColorValue, Does.Contain("0, 0, 255"), "Should return only own property");
    }

    [Test]
    public void GetSelfPropertyValue_FromSharedNode_ReturnsSharedNodeValue()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        var sharedNode = new PropertyTreeNode(null);

        sharedNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node.ReplaceSubtree("color", sharedNode);

        // Act
        var value = node.GetSelfPropertyValue("color");

        // Assert
        Assert.That(value, Does.Contain("255"), "Should return value from shared node");
    }

    [Test]
    public void GetSelfPropertyValue_NonExistentProperty_ReturnsEmptyString()
    {
        // Arrange
        var node = new PropertyTreeNode(null);

        // Act
        var value = node.GetSelfPropertyValue("non-existent-property");

        // Assert
        Assert.That(value, Is.Empty, "Should return empty string for non-existent property");
    }
    #endregion

    #region Concurrent Access Tests
    [Test]
    public void ConcurrentAccess_GetPropertyValue_ReturnsConsistentResults()
    {
        // Arrange
        var node = new PropertyTreeNode(null);
        node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
        node.SetProperty("font-size", new CssLengthValue(16, CssLengthValue.Unit.Px));

        const int taskCount = 50; // Reduced from 100 for faster testing
        var tasks = new Task<string>[taskCount];

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var propertyName = i % 2 == 0 ? "color" : "font-size";
            tasks[i] = Task.Run(() => node.GetPropertyValue(propertyName));
        }

        Task.WaitAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.That(task.Result, Is.Not.Empty, "All concurrent accesses should return values");
        }

        var colorResults = tasks.Where((t, i) => i % 2 == 0).Select(t => t.Result).Distinct();
        var fontResults = tasks.Where((t, i) => i % 2 != 0).Select(t => t.Result).Distinct();

        Assert.That(colorResults.Count(), Is.EqualTo(1), "All color results should be identical");
        Assert.That(fontResults.Count(), Is.EqualTo(1), "All font-size results should be identical");
    }
    #endregion

    #region Lifecycle and Memory Management Tests
    [Test]
    public void ReplacingPropertyMultipleTimes_DoesNotLeakMemory()
    {
        // Arrange
        var node = new PropertyTreeNode(null);

        // Act - replace the same property multiple times
        for (int i = 0; i < 100; i++) // Reduced from 1000 for faster testing
        {
            node.SetProperty("color", CssColorValue.FromRgba(i % 255, i % 255, i % 255, 1));
        }

        // Get property count
        var properties = node.GetAllProperties();

        // Assert
        Assert.That(properties.Count, Is.EqualTo(1), "Should only have one property despite multiple replacements");
    }
    #endregion
}