using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Xunit;

namespace LayoutEngine.Core.Tests;

/// <summary>
/// Tests the integration with AngleSharp Element class to ensure our custom
/// invalidation flag methods work correctly with real DOM elements.
/// CRITICAL: These tests validate our extension of external library.
/// </summary>
public class AngleSharpElementIntegrationTests
{
    private readonly IConfiguration _config;
    private readonly IBrowsingContext _context;

    public AngleSharpElementIntegrationTests()
    {
        _config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(_config);
    }

    [Fact]
    public async Task RealElement_HasInvalidationFlagMethods()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='test'>Test</div>"));
        var element = document.QuerySelector("#test") as IElement;

        // Assert - Element should have all invalidation flag methods
        Assert.NotNull(element);

        // These methods should exist and not throw
        Assert.False(element.NeedsStyleRecalc());
        Assert.False(element.ChildNeedsStyleRecalc());
        Assert.False(element.NeedsLayout());
        Assert.False(element.ChildNeedsLayout());
        Assert.False(element.NeedsPaintInvalidation());
        Assert.False(element.HasAnyInvalidation());
    }

    [Fact]
    public async Task RealElement_CanSetAndClearFlags()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='test'>Test</div>"));
        var element = document.QuerySelector("#test") as IElement;
        Assert.NotNull(element);

        // Act & Assert - Style flags
        Assert.False(element.NeedsStyleRecalc());
        element.SetNeedsStyleRecalc();
        Assert.True(element.NeedsStyleRecalc());
        element.ClearNeedsStyleRecalc();
        Assert.False(element.NeedsStyleRecalc());

        // Act & Assert - Layout flags
        Assert.False(element.NeedsLayout());
        element.SetNeedsLayout();
        Assert.True(element.NeedsLayout());
        Assert.True(element.NeedsPaintInvalidation()); // Should also set paint flag
        element.ClearNeedsLayout();
        Assert.False(element.NeedsLayout());

        // Act & Assert - Paint flags
        element.ClearNeedsPaintInvalidation(); // Clear the auto-set paint flag
        Assert.False(element.NeedsPaintInvalidation());
        element.SetNeedsPaintInvalidation();
        Assert.True(element.NeedsPaintInvalidation());
        element.ClearNeedsPaintInvalidation();
        Assert.False(element.NeedsPaintInvalidation());
    }

    [Fact]
    public async Task RealElement_ParentChildPropagation()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content(
            "<div id='parent'><div id='child'>Child</div></div>"));
        var parent = document.QuerySelector("#parent") as IElement;
        var child = document.QuerySelector("#child") as IElement;

        Assert.NotNull(parent);
        Assert.NotNull(child);

        // Act - Set child flag
        child.SetNeedsStyleRecalc();

        // Assert - Parent should have child flag set
        Assert.True(child.NeedsStyleRecalc());
        Assert.True(parent.ChildNeedsStyleRecalc());
    }

    [Fact]
    public async Task RealElement_HasAnyInvalidation_WorksCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='test'>Test</div>"));
        var element = document.QuerySelector("#test") as IElement;
        Assert.NotNull(element);

        // Initially no invalidation
        Assert.False(element.HasAnyInvalidation());

        // Set style flag
        element.SetNeedsStyleRecalc();
        Assert.True(element.HasAnyInvalidation());

        // Clear and set layout flag
        element.ClearNeedsStyleRecalc();
        Assert.False(element.HasAnyInvalidation());
        element.SetNeedsLayout();
        Assert.True(element.HasAnyInvalidation());

        // Clear and set paint flag
        element.ClearNeedsLayout();
        element.ClearNeedsPaintInvalidation(); // Layout sets this too
        Assert.False(element.HasAnyInvalidation());
        element.SetNeedsPaintInvalidation();
        Assert.True(element.HasAnyInvalidation());
    }

    [Fact]
    public async Task RealElement_ClearAllInvalidation_WorksCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='test'>Test</div>"));
        var element = document.QuerySelector("#test") as IElement;
        Assert.NotNull(element);

        // Set all flags
        element.SetNeedsStyleRecalc();
        element.SetNeedsLayout();
        element.SetNeedsPaintInvalidation();
        Assert.True(element.HasAnyInvalidation());

        // Act - Clear all
        element.ClearAllInvalidation();

        // Assert - All flags should be clear
        Assert.False(element.NeedsStyleRecalc());
        Assert.False(element.NeedsLayout());
        Assert.False(element.NeedsPaintInvalidation());
        Assert.False(element.HasAnyInvalidation());
    }

    [Fact]
    public async Task RealElement_DeepHierarchy_PropagationWorks()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content(
            @"<div id='root'>
                <div id='level1'>
                  <div id='level2'>
                    <div id='level3'>
                      <span id='leaf'>Leaf</span>
                    </div>
                  </div>
                </div>
              </div>"));

        var root = document.QuerySelector("#root") as IElement;
        var level1 = document.QuerySelector("#level1") as IElement;
        var level2 = document.QuerySelector("#level2") as IElement;
        var level3 = document.QuerySelector("#level3") as IElement;
        var leaf = document.QuerySelector("#leaf") as IElement;

        Assert.NotNull(root);
        Assert.NotNull(level1);
        Assert.NotNull(level2);
        Assert.NotNull(level3);
        Assert.NotNull(leaf);

        // Act - Invalidate leaf
        leaf.SetNeedsStyleRecalc();

        // Assert - All parents should have ChildNeedsStyleRecalc
        Assert.True(leaf.NeedsStyleRecalc());
        Assert.True(level3.ChildNeedsStyleRecalc());
        Assert.True(level2.ChildNeedsStyleRecalc());
        Assert.True(level1.ChildNeedsStyleRecalc());
        Assert.True(root.ChildNeedsStyleRecalc());
    }

    [Fact]
    public async Task RealElement_MultipleSiblings_PropagationWorks()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content(
            @"<div id='parent'>
                <div id='child1'>Child 1</div>
                <div id='child2'>Child 2</div>
                <div id='child3'>Child 3</div>
              </div>"));

        var parent = document.QuerySelector("#parent") as IElement;
        var child1 = document.QuerySelector("#child1") as IElement;
        var child2 = document.QuerySelector("#child2") as IElement;
        var child3 = document.QuerySelector("#child3") as IElement;

        Assert.NotNull(parent);
        Assert.NotNull(child1);
        Assert.NotNull(child2);
        Assert.NotNull(child3);

        // Act - Invalidate middle child
        child2.SetNeedsLayout();

        // Assert - Parent should have child flag, siblings should be unaffected
        Assert.False(child1.NeedsLayout());
        Assert.True(child2.NeedsLayout());
        Assert.False(child3.NeedsLayout());
        Assert.True(parent.ChildNeedsLayout());
    }

    [Fact]
    public async Task RealElement_ClearingBehavior_WorksCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content(
            @"<div id='parent'>
            <div id='needyChild'>Needy</div>
            <div id='cleanChild'>Clean</div>
          </div>"));

        var parent = document.QuerySelector("#parent") as IElement;
        var needyChild = document.QuerySelector("#needyChild") as IElement;
        var cleanChild = document.QuerySelector("#cleanChild") as IElement;

        Assert.NotNull(parent);
        Assert.NotNull(needyChild);
        Assert.NotNull(cleanChild);

        // Set up - Both children need style, parent has child flag
        needyChild.SetNeedsStyleRecalc();
        cleanChild.SetNeedsStyleRecalc();
        Assert.True(parent.ChildNeedsStyleRecalc());

        // Act - Clear one child
        cleanChild.ClearNeedsStyleRecalc();

        // Assert - Parent should still have child flag because needyChild still needs recalc
        Assert.True(needyChild.NeedsStyleRecalc());
        Assert.False(cleanChild.NeedsStyleRecalc());
        Assert.True(parent.ChildNeedsStyleRecalc());

        // Act - Clear the other child
        needyChild.ClearNeedsStyleRecalc();

        // Assert - Now parent should not have child flag
        Assert.False(needyChild.NeedsStyleRecalc());
        Assert.False(cleanChild.NeedsStyleRecalc());

        // FIXED: Parent's ChildNeedsStyleRecalc remains true - this matches Blink's behavior
        // The flag is only cleared during style recalc tree traversal, not on individual clear calls
        Assert.True(parent.ChildNeedsStyleRecalc());
    }
    
    [Fact]
    public async Task RealElement_FlagIndependence_WorksCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='test'>Test</div>"));
        var element = document.QuerySelector("#test") as IElement;
        Assert.NotNull(element);

        // Act & Assert - Each flag should be independent
        element.SetNeedsStyleRecalc();
        Assert.True(element.NeedsStyleRecalc());
        Assert.False(element.NeedsLayout());
        Assert.False(element.NeedsPaintInvalidation());

        element.SetNeedsLayout(); // This should also set paint invalidation
        Assert.True(element.NeedsStyleRecalc());
        Assert.True(element.NeedsLayout());
        Assert.True(element.NeedsPaintInvalidation());

        element.ClearNeedsStyleRecalc();
        Assert.False(element.NeedsStyleRecalc());
        Assert.True(element.NeedsLayout());
        Assert.True(element.NeedsPaintInvalidation());

        element.ClearNeedsPaintInvalidation();
        Assert.False(element.NeedsStyleRecalc());
        Assert.True(element.NeedsLayout());
        Assert.False(element.NeedsPaintInvalidation());
    }

    [Fact]
    public async Task RealElement_WithActualDomMutations_WorksCorrectly()
    {
        // Arrange
        var document = await _context.OpenAsync(req => req.Content("<div id='parent'>Original</div>"));
        var parent = document.QuerySelector("#parent") as IElement;
        Assert.NotNull(parent);

        // Act - Make actual DOM mutations
        var newChild = document.CreateElement("span");
        newChild.TextContent = "New Child";
        parent.AppendChild(newChild);

        // The element should still support invalidation flags after DOM mutations
        parent.SetNeedsStyleRecalc();
        newChild.SetNeedsLayout();

        // Assert
        Assert.True(parent.NeedsStyleRecalc());
        Assert.True(newChild.NeedsLayout());
        Assert.True(newChild.NeedsPaintInvalidation()); // Layout should set paint
        Assert.True(parent.ChildNeedsLayout()); // Parent should get child flag
    }

    [Fact]
    public async Task RealElement_PerformanceWithLargeDOM_IsAcceptable()
    {
        // Arrange - Create a larger DOM structure
        var html = "<div id='root'>";
        for (int i = 0; i < 100; i++)
        {
            html += $"<div id='item-{i}'>Item {i}</div>";
        }
        html += "</div>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var root = document.QuerySelector("#root") as IElement;
        Assert.NotNull(root);

        var elements = new List<IElement>();
        for (int i = 0; i < 100; i++)
        {
            var element = document.QuerySelector($"#item-{i}") as IElement;
            Assert.NotNull(element);
            elements.Add(element);
        }

        // Act & Assert - Flag operations should be fast on real elements
        var startTime = DateTime.UtcNow;

        foreach (var element in elements)
        {
            element.SetNeedsStyleRecalc();
            element.SetNeedsLayout();
            element.SetNeedsPaintInvalidation();

            var hasAny = element.HasAnyInvalidation();
            Assert.True(hasAny);

            element.ClearAllInvalidation();
            Assert.False(element.HasAnyInvalidation());
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Should complete quickly even with real DOM elements
        Assert.True(duration.TotalMilliseconds < 500,
            $"Flag operations took {duration.TotalMilliseconds}ms for 100 real elements, expected < 500ms");
    }
}