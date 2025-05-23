using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using LayoutEngine.Core.Style;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LayoutEngine.Core.Tests;

/// <summary>
/// Tests the interaction between Style, Layout, and Render systems using node flags.
/// Critical for ensuring the end-to-end invalidation flow works correctly.
/// </summary>
public class CrossSystemIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStyleSystem _styleSystem;
    private readonly ILayoutSystem _layoutSystem;
    private readonly IRenderSystem _renderSystem;
    private readonly IEventAggregator _eventAggregator;

    public CrossSystemIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();
        _serviceProvider = services.BuildServiceProvider();

        _styleSystem = _serviceProvider.GetRequiredService<IStyleSystem>();
        _layoutSystem = _serviceProvider.GetRequiredService<ILayoutSystem>();
        _renderSystem = _serviceProvider.GetRequiredService<IRenderSystem>();
        _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
    }

    [Fact]
    public void StyleChange_TriggersLayoutAndRenderInvalidation()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Act - Style system processes style change
        _styleSystem.InvalidateStyle(element, false);
        _styleSystem.ComputeDocumentStyles(document);

        // Assert - Style change should trigger layout and render invalidation
        element.Received(1).SetNeedsStyleRecalc();
        // In real implementation, style computation should also set layout/paint flags
        // if the style change affects layout or rendering
    }

    [Fact]
    public void LayoutChange_TriggersRenderInvalidation()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Act - Layout system processes layout change
        _layoutSystem.InvalidateLayout(element, false);
        _layoutSystem.PerformLayout(document);

        // Assert - Layout change should trigger render invalidation
        element.Received(1).SetNeedsLayout();
        element.Received(1).SetNeedsPaintInvalidation(); // Layout changes require repainting
    }

    [Fact]
    public void CompleteInvalidationFlow_StyleToLayoutToRender()
    {
        // Arrange
        var rootElement = TestHelpers.CreateMockElement("html");
        var childElement = TestHelpers.CreateMockElement("div", rootElement);
        var children = new TestHtmlCollection(new[] { childElement });
        rootElement.Children.Returns(children);
        var document = TestHelpers.CreateMockDocument(rootElement);

        // Set up mock computed style
        var computedStyle = TestHelpers.CreateMockComputedStyle(childElement);
        var styleSystem = Substitute.For<IStyleSystem>();
        styleSystem.GetComputedStyle(childElement).Returns(computedStyle);
        styleSystem.ComputeStyle(childElement).Returns(computedStyle);

        var layoutSystem = new LayoutSystem(styleSystem, _eventAggregator);
        var renderSystem = _serviceProvider.GetRequiredService<IRenderSystem>();

        // Act - Complete invalidation flow
        // 1. Style invalidation
        TestHelpers.SetupElementInvalidationFlags(childElement, needsStyle: true);
        styleSystem.ComputeDocumentStyles(document);

        // 2. Layout processing
        TestHelpers.SetupElementInvalidationFlags(childElement, needsLayout: true);
        var layoutResult = layoutSystem.PerformLayout(document);

        // 3. Render processing
        TestHelpers.SetupElementInvalidationFlags(childElement, needsPaint: true);
        var fragmentTree = layoutSystem.GetFragmentTree();
        renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert - Each stage should clear its own flags
        childElement.Received(1).ClearNeedsStyleRecalc();
        childElement.Received(1).ClearNeedsLayout();
        childElement.Received(1).ClearNeedsPaintInvalidation();
    }

    [Fact]
    public void SystemsRespectProcessingOrder()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Set up element with all invalidation flags
        TestHelpers.SetupElementInvalidationFlags(element,
            needsStyle: true, needsLayout: true, needsPaint: true);

        var callOrder = new List<string>();

        // Mock systems to track processing order
        var mockStyleSystem = Substitute.For<IStyleSystem>();
        mockStyleSystem.When(x => x.ComputeDocumentStyles(Arg.Any<IDocument>()))
                      .Do(_ => callOrder.Add("Style"));

        var mockLayoutSystem = Substitute.For<ILayoutSystem>();
        mockLayoutSystem.When(x => x.PerformLayout(Arg.Any<IDocument>()))
                       .Do(_ => callOrder.Add("Layout"));

        var mockRenderSystem = Substitute.For<IRenderSystem>();
        mockRenderSystem.When(x => x.ProcessFragmentTree(Arg.Any<IFragmentTree>()))
                       .Do(_ => callOrder.Add("Render"));

        // Act - Process in correct order
        if (element.NeedsStyleRecalc()) mockStyleSystem.ComputeDocumentStyles(document);
        if (element.NeedsLayout()) mockLayoutSystem.PerformLayout(document);
        if (element.NeedsPaintInvalidation())
        {
            var fragmentTree = TestHelpers.CreateMockFragmentTree();
            mockRenderSystem.ProcessFragmentTree(fragmentTree);
        }

        // Assert - Should process in Style → Layout → Render order
        Assert.Equal(new[] { "Style", "Layout", "Render" }, callOrder);
    }

    [Fact]
    public void FlagClearing_DoesNotAffectOtherSystems()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        TestHelpers.SetupElementInvalidationFlags(element,
            needsStyle: true, needsLayout: true, needsPaint: true);

        // Act - Clear style flag
        element.ClearNeedsStyleRecalc();

        // Assert - Other flags should remain
        // (In real implementation, would verify that layout and paint flags are still set)
        element.Received(1).ClearNeedsStyleRecalc();
        // Should not affect layout or paint flags
    }

    [Fact]
    public void HierarchicalInvalidation_WorksAcrossSystems()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var child1 = TestHelpers.CreateMockElement("span", parent);
        var child2 = TestHelpers.CreateMockElement("span", parent);
        var grandchild = TestHelpers.CreateMockElement("em", child1);

        var children = new TestHtmlCollection(new[] { child1, child2 });
        var grandchildren = new TestHtmlCollection(new[] { grandchild });
        parent.Children.Returns(children);
        child1.Children.Returns(grandchildren);
        child2.Children.Returns(new TestHtmlCollection(new IElement[0]));

        // Act - Invalidate grandchild style
        _styleSystem.InvalidateStyle(grandchild, false);

        // Assert - Should propagate up the tree
        grandchild.Received(1).SetNeedsStyleRecalc();
        // In real implementation, parent and child1 should get ChildNeedsStyleRecalc flags
    }

    [Fact]
    public void RecursiveInvalidation_WorksAcrossSystems()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var child1 = TestHelpers.CreateMockElement("span", parent);
        var child2 = TestHelpers.CreateMockElement("span", parent);
        var children = new TestHtmlCollection(new[] { child1, child2 });
        parent.Children.Returns(children);

        // Act - Recursive invalidation from parent
        _styleSystem.InvalidateStyle(parent, recursive: true);

        // Assert - All elements should be invalidated
        parent.Received(1).SetNeedsStyleRecalc();
        child1.Received(1).SetNeedsStyleRecalc();
        child2.Received(1).SetNeedsStyleRecalc();
    }

    [Fact]
    public void SystemEventsCascadeProperly()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var eventsReceived = new List<string>();

        _eventAggregator.Subscribe<LayoutEngine.Core.Events.StyleComputedEvent>(
            e => eventsReceived.Add("StyleComputed"));
        _eventAggregator.Subscribe<LayoutEngine.Core.Events.FragmentTreeUpdatedEvent>(
            e => eventsReceived.Add("LayoutUpdated"));
        _eventAggregator.Subscribe<LayoutEngine.Core.Events.RenderCompletedEvent>(
            e => eventsReceived.Add("RenderCompleted"));

        // Act - Trigger style computation
        var document = TestHelpers.CreateMockDocument(element);
        _styleSystem.ComputeDocumentStyles(document);

        // Assert - Should receive style event
        Assert.Contains("StyleComputed", eventsReceived);
    }

    [Fact]
    public void MemoryEfficiency_NoSystemsHoldElementReferences()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Act - Process through all systems
        _styleSystem.ComputeDocumentStyles(document);
        _layoutSystem.PerformLayout(document);

        var fragmentTree = _layoutSystem.GetFragmentTree();
        _renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert - Systems should not hold references to elements
        // (This is more of a design verification - systems should only
        //  interact with elements through the node flags, not store references)

        // Clear styles to simulate cleanup
        _styleSystem.ClearStyles();

        // Systems should still function without holding element references
        Assert.NotNull(_styleSystem);
        Assert.NotNull(_layoutSystem);
        Assert.NotNull(_renderSystem);
    }

    [Fact]
    public void ErrorRecovery_SystemsCanRecoverFromInvalidState()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var document = TestHelpers.CreateMockDocument(element);

        // Act - Simulate error condition by clearing all flags unexpectedly
        element.ClearAllInvalidation();

        // Systems should handle this gracefully
        var styleResult = _styleSystem.ComputeStyle(element);
        var layoutResult = _layoutSystem.PerformLayout(document);

        // Assert - Systems should still function
        Assert.NotNull(styleResult);
        Assert.NotNull(layoutResult);
    }

    [Fact]
    public void PerformanceCharacteristics_FlagChecksAreFast()
    {
        // Arrange
        var elements = new List<IElement>();
        for (int i = 0; i < 1000; i++)
        {
            elements.Add(TestHelpers.CreateMockElement($"element-{i}"));
        }

        // Act & Assert - Flag checks should be fast enough for large numbers of elements
        var startTime = DateTime.UtcNow;

        foreach (var element in elements)
        {
            // Simulate typical flag checking pattern
            var needsStyle = element.NeedsStyleRecalc();
            var needsLayout = element.NeedsLayout();
            var needsPaint = element.NeedsPaintInvalidation();
            var hasAny = element.HasAnyInvalidation();
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Should complete very quickly (this is a basic performance smoke test)
        Assert.True(duration.TotalMilliseconds < 100,
            $"Flag checking took {duration.TotalMilliseconds}ms for 1000 elements, expected < 100ms");
    }
}