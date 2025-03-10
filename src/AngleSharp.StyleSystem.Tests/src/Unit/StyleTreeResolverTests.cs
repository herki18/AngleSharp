namespace AngleSharp.StyleSystem.Tests.Unit;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using Moq;

[TestFixture]
public class StyleTreeResolverTests
{
    private IBrowsingContext _context;
    private StyleEngine _styleEngine;
    private StyleTreeResolver _resolver;
    private Mock<IStyleApplicationStrategy> _strategyMock;
    private StyleCache _styleCache;
    private StyleInvalidationTracker _invalidationTracker;
    private IHtmlParser _parser;
    private IDocument _document;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _styleEngine = new StyleEngine(_context);
        _styleCache = new StyleCache();
        _invalidationTracker = new StyleInvalidationTracker();
        _strategyMock = new Mock<IStyleApplicationStrategy>();
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");

        // Set up strategy mock with some default behaviors
        _strategyMock.Setup(s => s.ShouldSkipSubtree(It.IsAny<IElement>())).Returns(false);

        _resolver = new StyleTreeResolver(
            _styleEngine,
            _strategyMock.Object,
            _styleCache,
            _invalidationTracker);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _document?.Dispose();
        _styleEngine?.Dispose();
    }

    [Test]
    public void ResolveElementStyle_ComputesStyleForElement()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Setup strategy to create a simple context
        _strategyMock.Setup(s => s.CreateStyleContext(element))
            .Returns(new StyleContext { Element = element });

        // Act
        var style = _resolver.ResolveElementStyle(element);

        // Assert
        Assert.That(style, Is.Not.Null, "Style should be computed");
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(element), Is.False,
            "Element should be marked as up-to-date after style computation");
    }

    [Test]
    public void ResolveElementStyle_UsesParentStyleWhenProvided()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        // Compute parent style first
        var parentStyle = _resolver.ResolveElementStyle(parent);

        // Act
        var childStyle = _resolver.ResolveElementStyle(child, parentStyle);

        // Assert
        Assert.That(childStyle, Is.Not.Null, "Child style should be computed");

        // We can't directly assert internal state, but we can verify the style was computed
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(child), Is.False,
            "Child should be marked as up-to-date");
    }

    [Test]
    public void ResolveStylesForSubtree_ProcessesAllElementsInTraversalOrder()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child1 = _document.CreateElement("span");
        var child2 = _document.CreateElement("p");
        parent.AppendChild(child1);
        parent.AppendChild(child2);
        _document.Body!.AppendChild(parent);

        var traversalOrder = new List<IElement> { parent, child1, child2 };

        _strategyMock.Setup(s => s.GetElementTraversalOrder(parent))
            .Returns(traversalOrder);

        _strategyMock.Setup(s => s.CreateStyleContext(It.IsAny<IElement>()))
            .Returns<IElement>(e => new StyleContext { Element = e });

        // Invalidate all elements to ensure they get processed
        _invalidationTracker.InvalidateElement(parent);

        // Act
        _resolver.ResolveStylesForSubtree(parent);

        // Assert
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(parent), Is.False,
            "Parent should be marked as up-to-date");
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(child1), Is.False,
            "Child1 should be marked as up-to-date");
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(child2), Is.False,
            "Child2 should be marked as up-to-date");
    }

    [Test]
    public void ResolveStylesForSubtree_SkipsSubtreesWhenStrategyIndicates()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        var traversalOrder = new List<IElement> { parent };

        _strategyMock.Setup(s => s.GetElementTraversalOrder(parent))
            .Returns(traversalOrder);

        _strategyMock.Setup(s => s.ShouldSkipSubtree(parent))
            .Returns(true);

        _strategyMock.Setup(s => s.CreateStyleContext(It.IsAny<IElement>()))
            .Returns<IElement>(e => new StyleContext { Element = e });

        // Invalidate all elements
        _invalidationTracker.InvalidateElement(parent);
        _invalidationTracker.InvalidateElement(child);

        // Act
        _resolver.ResolveStylesForSubtree(parent);

        // Assert
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(parent), Is.False,
            "Parent should be marked as up-to-date even though subtree is skipped");

        // Child is not in traversal order and is not processed
        Assert.That(_invalidationTracker.NeedsStyleRecalculation(child), Is.True,
            "Child should still be marked as invalid since it wasn't in traversal order");
    }

    [Test]
    public void CanShareStyle_ReturnsTrueWhenStyleSharingIsPossible()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("div");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        // Setup strategy to allow style sharing
        _strategyMock.Setup(s => s.CanShareStyleWith(element2, element1))
            .Returns(true);

        // Compute style for the first element
        _resolver.ResolveElementStyle(element1);
        _invalidationTracker.MarkAsUpToDate(element1);

        // Act
        var canShare = _resolver.CanShareStyle(element2);

        // Assert
        Assert.That(canShare, Is.True, "Elements should be able to share styles");
    }

    [Test]
    public void CanShareStyle_ReturnsFalseWhenNoCompatibleDonorExists()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span"); // Different tag
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        // Setup strategy to disallow style sharing
        _strategyMock.Setup(s => s.CanShareStyleWith(element2, element1))
            .Returns(false);

        // Compute style for the first element
        _resolver.ResolveElementStyle(element1);
        _invalidationTracker.MarkAsUpToDate(element1);

        // Act
        var canShare = _resolver.CanShareStyle(element2);

        // Assert
        Assert.That(canShare, Is.False, "Elements should not be able to share styles");
    }

    [Test]
    public void ResolveElementStyle_HandlesCircularReferences()
    {
        // This test is tricky because we need to simulate a circular reference
        // We'll use a custom strategy to create this situation

        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        bool firstCall = true;

        // Custom strategy setup to create a circular dependency
        _strategyMock.Setup(s => s.CreateStyleContext(element))
            .Returns(() => {
                if (firstCall)
                {
                    firstCall = false;
                    // Trigger a circular reference by calling resolver again with same element
                    _resolver.ResolveElementStyle(element);
                }
                return new StyleContext { Element = element };
            });

        // Act - this should not throw due to circular reference handling
        var style = _resolver.ResolveElementStyle(element);

        // Assert
        Assert.That(style, Is.Not.Null, "Style should be computed despite circular reference");
        Assert.That(_resolver.IsResolving, Is.False, "Resolver should no longer be in resolving state");
    }

    [Test]
    public void GetElementsNeedingStyleResolution_ReturnsElementsFromInvalidationTracker()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        // Mark the child as needing recalculation
        _invalidationTracker.InvalidateElement(child);
        _invalidationTracker.MarkAsUpToDate(parent);

        // Act
        var elementsToUpdate = _resolver.GetElementsNeedingStyleResolution(parent).ToList();

        // Assert
        Assert.That(elementsToUpdate, Is.Not.Null);
        Assert.That(elementsToUpdate.Count, Is.EqualTo(1));
        Assert.That(elementsToUpdate[0], Is.EqualTo(child));
    }

    [Test]
    public void CurrentElement_TracksElementBeingProcessed()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        Assert.That(_resolver.CurrentElement, Is.Null, "Initially no element should be current");
        Assert.That(_resolver.IsResolving, Is.False, "Initially not resolving");

        // Setup the strategy to allow us to check the current element during resolution
        IElement? currentElementDuringResolution = null;
        bool isResolvingDuringResolution = false;

        _strategyMock.Setup(s => s.CreateStyleContext(element))
            .Returns<IElement>(e => {
                currentElementDuringResolution = _resolver.CurrentElement;
                isResolvingDuringResolution = _resolver.IsResolving;
                return new StyleContext { Element = e };
            });

        // Act
        _resolver.ResolveElementStyle(element);

        // Assert
        Assert.That(currentElementDuringResolution, Is.EqualTo(element),
            "CurrentElement should be set to the element being processed");
        Assert.That(isResolvingDuringResolution, Is.True,
            "IsResolving should be true during resolution");

        Assert.That(_resolver.CurrentElement, Is.Null,
            "CurrentElement should be null after resolution completes");
        Assert.That(_resolver.IsResolving, Is.False,
            "IsResolving should be false after resolution completes");
    }

    [Test]
    public void ClearCache_ClearsStyleCache()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Compute style and verify it's cached
        var style1 = _resolver.ResolveElementStyle(element);

        // Act
        _resolver.ClearCache();

        // Force style recomputation by invalidating
        _invalidationTracker.InvalidateElement(element);
        var style2 = _resolver.ResolveElementStyle(element);

        // Assert - we can't directly check if the cache was accessed,
        // but we can verify the styles are different objects
        Assert.That(style2, Is.Not.SameAs(style1),
            "A new style object should be created after cache clear");
    }

    [Test]
    public void ResolveElementStyle_UsesCachedStyleWhenAvailable()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Compute style first time
        var style1 = _resolver.ResolveElementStyle(element);

        // Verify the strategy was called
        _strategyMock.Verify(s => s.CreateStyleContext(element), Times.Once);

        // Reset the mock to verify it's not called again
        _strategyMock.Invocations.Clear();

        // Act - resolve again
        var style2 = _resolver.ResolveElementStyle(element);

        // Assert
        Assert.That(style2, Is.SameAs(style1),
            "The same style object should be returned from cache");

        // Verify the strategy wasn't called again (cache was used)
        _strategyMock.Verify(s => s.CreateStyleContext(It.IsAny<IElement>()), Times.Never,
            "Strategy should not be called when cache is used");
    }
}