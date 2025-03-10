using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Core;
using NUnit.Framework;
using System;

namespace AngleSharp.StyleSystem.Tests
{
    using Css.Dom;

    [TestFixture]
    public class PropertyTreeNodeTests
    {
        private IBrowsingContext _context;
        private IHtmlParser _parser;
        private IDocument _document;

        [SetUp]
        public void Setup()
        {
            var config = Configuration.Default.WithCss();
            _context = BrowsingContext.New(config);
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
    }
}