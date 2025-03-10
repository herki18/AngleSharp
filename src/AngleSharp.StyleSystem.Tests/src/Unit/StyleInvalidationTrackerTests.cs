namespace AngleSharp.StyleSystem.Tests.Unit;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Integration;

[TestFixture]
public class StyleInvalidationTrackerTests
{
    private IBrowsingContext _context;
    private StyleInvalidationTracker _tracker;
    private IHtmlParser _parser;
    private IDocument _document;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _tracker = new StyleInvalidationTracker();
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
    public void InvalidateElement_MarksElementAsInvalid()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        _tracker.InvalidateElement(element);

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.True);
    }

    [Test]
    public void MarkAsUpToDate_MarksElementAsValid()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        _tracker.InvalidateElement(element);
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.True, "Element should start invalid");

        // Act
        _tracker.MarkAsUpToDate(element);

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.False, "Element should now be valid");
    }

    [Test]
    public void InvalidateElement_AlsoInvalidatesChildren()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        var child = _document.CreateElement("span");
        parent.AppendChild(child);
        _document.Body!.AppendChild(parent);

        // Act
        _tracker.InvalidateElement(parent);

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(parent), Is.True, "Parent should be invalid");
        Assert.That(_tracker.NeedsStyleRecalculation(child), Is.True, "Child should also be invalid");
    }

    [Test]
    public void TrackDependency_InvalidatesDependentElements()
    {
        // Arrange
        var source = _document.CreateElement("div");
        var dependent = _document.CreateElement("span");
        _document.Body!.AppendChild(source);
        _document.Body!.AppendChild(dependent);

        // Track dependency
        _tracker.TrackDependency(dependent, source);

        // Mark dependent as up-to-date initially
        _tracker.MarkAsUpToDate(dependent);
        Assert.That(_tracker.NeedsStyleRecalculation(dependent), Is.False, "Dependent should start valid");

        // Act
        _tracker.InvalidateElement(source);

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(dependent), Is.True, "Dependent should be invalidated");
    }

    [Test]
    public void InvalidateProperties_InvalidatesElement()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Initially mark as up-to-date
        _tracker.MarkAsUpToDate(element);
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.False, "Element should start valid");

        // Act
        _tracker.InvalidateProperties(element, new[] { "color", "margin" });

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.True, "Element should be invalidated");
    }

    [Test]
    public void GetElementsToUpdate_ReturnsAllInvalidElements()
    {
        // Arrange
        var root = _document.CreateElement("div");
        var child1 = _document.CreateElement("span");
        var child2 = _document.CreateElement("p");
        root.AppendChild(child1);
        root.AppendChild(child2);
        _document.Body!.AppendChild(root);

        // Mark some elements as invalid
        _tracker.InvalidateElement(child1);

        // Mark other elements as valid
        _tracker.MarkAsUpToDate(root);
        _tracker.MarkAsUpToDate(child2);

        // Act
        var elementsToUpdate = _tracker.GetElementsToUpdate(root).ToList();

        // Assert
        Assert.That(elementsToUpdate, Is.Not.Null);
        Assert.That(elementsToUpdate.Count, Is.EqualTo(1));
        Assert.That(elementsToUpdate[0], Is.EqualTo(child1));
    }

    [Test]
    public void NeedsStyleRecalculation_ReturnsTrueForUntrackedElements()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act & Assert
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.True, "Untracked element should need calculation");
    }

    [Test]
    public void MarkAsDeviceDependent_MakesElementDependentOnDevice()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Mark as up-to-date initially
        _tracker.MarkAsUpToDate(element);
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.False, "Element should start valid");

        // Mark as device dependent
        _tracker.MarkAsDeviceDependent(element);

        // Act
        _tracker.InvalidateForDeviceChange();

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(element), Is.True, "Element should be invalidated by device change");
    }

    [Test]
    public void MarkAsDeviceDependent_OnlyAffectsMarkedElements()
    {
        // Arrange
        var dependent = _document.CreateElement("div");
        var notDependent = _document.CreateElement("span");
        _document.Body!.AppendChild(dependent);
        _document.Body!.AppendChild(notDependent);

        // Mark both as up-to-date initially
        _tracker.MarkAsUpToDate(dependent);
        _tracker.MarkAsUpToDate(notDependent);

        // Only mark one as device dependent
        _tracker.MarkAsDeviceDependent(dependent);

        // Act
        _tracker.InvalidateForDeviceChange();

        // Assert
        Assert.That(_tracker.NeedsStyleRecalculation(dependent), Is.True, "Dependent element should be invalidated");
        Assert.That(_tracker.NeedsStyleRecalculation(notDependent), Is.False, "Non-dependent element should remain valid");
    }
}