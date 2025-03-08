using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Core;

namespace AngleSharp.StyleSystem.Tests
{
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
        public void PropertyTreeNode_SetAndGetProperty_StoredCorrectly()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);
            var color = CssColorValue.FromRgba(255, 0, 0, 1);

            // Act
            node.SetProperty("color", color);
            var retrievedValue = node.GetPropertyRawValue("color");

            // Assert - CssColorValue is a struct, so it can't be null
            Assert.That(retrievedValue, Is.EqualTo(color), "GetPropertyRawValue should return the exact same value");
        }

        [Test]
        public void PropertyTreeNode_SetAndGetPropertyValue_ReturnsCorrectString()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);
            var color = CssColorValue.FromRgba(255, 0, 0, 1);

            // Act
            node.SetProperty("color", color);
            var valueText = node.GetPropertyValue("color");

            // Assert
            Assert.That(valueText, Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")  // Allow for different formats
                .Or.EqualTo("#ff0000"),
                "GetPropertyValue should return the correct string representation");
        }

        [Test]
        public void PropertyTreeNode_GetPropertyValue_InheritsFromParent()
        {
            // Arrange
            var parentElement = _document.CreateElement("div");
            var childElement = _document.CreateElement("span");
            _document.Body!.AppendChild(parentElement);
            parentElement.AppendChild(childElement);

            var parentNode = _propertyTreeManager.CreateNode(parentElement);
            var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

            // Set property on parent only
            parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

            // Act
            var childValue = childNode.GetPropertyValue("color");

            // Assert
            Assert.That(childValue, Is.EqualTo("rgba(255, 0, 0, 1)")
                .Or.EqualTo("rgb(255, 0, 0)")  // Allow for different formats
                .Or.EqualTo("#ff0000"),
                "Child node should inherit property from parent");
        }

        [Test]
        public void PropertyTreeNode_GetPropertyValue_ChildOverridesParent()
        {
            // Arrange
            var parentElement = _document.CreateElement("div");
            var childElement = _document.CreateElement("span");
            _document.Body!.AppendChild(parentElement);
            parentElement.AppendChild(childElement);

            var parentNode = _propertyTreeManager.CreateNode(parentElement);
            var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

            // Set property on both parent and child
            parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red
            childNode.SetProperty("color", CssColorValue.FromRgba(0, 0, 255, 1));  // Blue

            // Act
            var childValue = childNode.GetPropertyValue("color");

            // Assert
            Assert.That(childValue, Is.EqualTo("rgba(0, 0, 255, 1)")
                .Or.EqualTo("rgb(0, 0, 255)")  // Allow for different formats
                .Or.EqualTo("#0000ff"),
                "Child property should override parent property");
        }

        [Test]
        public void PropertyTreeNode_HasProperty_ReturnsTrueForOwnProperty()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);
            node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

            // Act
            var hasProperty = node.HasProperty("color");

            // Assert
            Assert.That(hasProperty, Is.True, "HasProperty should return true for directly set properties");
        }

        [Test]
        public void PropertyTreeNode_HasProperty_ReturnsTrueForParentProperty()
        {
            // Arrange
            var parentElement = _document.CreateElement("div");
            var childElement = _document.CreateElement("span");
            _document.Body!.AppendChild(parentElement);
            parentElement.AppendChild(childElement);

            var parentNode = _propertyTreeManager.CreateNode(parentElement);
            var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

            // Set property on parent only
            parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

            // Act
            var hasProperty = childNode.HasProperty("color");

            // Assert
            Assert.That(hasProperty, Is.True, "HasProperty should return true for inherited properties");
        }

        [Test]
        public void PropertyTreeNode_HasProperty_ReturnsFalseForNonExistentProperty()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);

            // Act
            var hasProperty = node.HasProperty("non-existent-property");

            // Assert
            Assert.That(hasProperty, Is.False, "HasProperty should return false when property doesn't exist");
        }

        [Test]
        public void PropertyTreeNode_GetPropertyCachedValue_CachesComputedValues()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);
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
        public void PropertyTreeNode_ReplaceSubtree_ReplacesPropertyWithNode()
        {
            // Arrange
            var element1 = _document.CreateElement("div");
            var element2 = _document.CreateElement("span");
            _document.Body!.AppendChild(element1);
            _document.Body!.AppendChild(element2);

            var node1 = _propertyTreeManager.CreateNode(element1);
            var node2 = _propertyTreeManager.CreateNode(element2);

            node1.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

            // Act
            node1.ReplaceSubtree("color", node2);
            var hasDirectProperty = node1.GetAllProperties().ContainsKey("color");

            // Assert
            Assert.That(hasDirectProperty, Is.False, "Property should be removed from direct properties after replacement");
        }

        [Test]
        public void PropertyTreeNode_SetProperty_OverwritesExistingProperty()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);

            // Set initial property
            node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1)); // Red

            // Act
            node.SetProperty("color", CssColorValue.FromRgba(0, 255, 0, 1)); // Green
            var value = node.GetPropertyRawValue("color");

            // Assert
            Assert.That(value, Is.EqualTo(CssColorValue.FromRgba(0, 255, 0, 1)),
                "SetProperty should overwrite existing property with new value");
        }

        [Test]
        public void PropertyTreeNode_SetProperty_WithNull_RemovesProperty()
        {
            // Arrange
            var element = _document.CreateElement("div");
            _document.Body!.AppendChild(element);
            var node = _propertyTreeManager.CreateNode(element);

            // Set initial property
            node.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));

            // Act
            string? value = null;
            node.SetProperty("color", value);
            var hasProperty = node.HasProperty("color");

            // Assert
            Assert.That(hasProperty, Is.False, "SetProperty with null should remove the property");
        }

        [Test]
        public void GetAllProperties_ReturnsOnlyDirectProperties()
        {
            // Arrange
            var parentElement = _document.CreateElement("div");
            var childElement = _document.CreateElement("span");
            _document.Body!.AppendChild(parentElement);
            parentElement.AppendChild(childElement);

            var parentNode = _propertyTreeManager.CreateNode(parentElement);
            var childNode = _propertyTreeManager.CreateNode(childElement, parentNode);

            parentNode.SetProperty("color", CssColorValue.FromRgba(255, 0, 0, 1));
            childNode.SetProperty("background-color", CssColorValue.FromRgba(0, 0, 255, 1));

            // Act
            var childProperties = childNode.GetAllProperties();

            // Assert
            Assert.That(childProperties.ContainsKey("background-color"), Is.True,
                "GetAllProperties should include direct properties");
            Assert.That(childProperties.ContainsKey("color"), Is.False,
                "GetAllProperties should not include inherited properties");
        }
    }
}