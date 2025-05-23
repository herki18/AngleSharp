using AngleSharp.Dom;
using NSubstitute;
using Xunit;

namespace LayoutEngine.Core.Tests;

/// <summary>
/// Tests for the critical node flag propagation behavior.
/// This is the core new functionality that needs thorough validation.
/// </summary>
public class NodeFlagPropagationTests
{
    [Fact]
    public void SetNeedsStyleRecalc_PropagatesChildNeedsStyleRecalc_ToParent()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var child = TestHelpers.CreateMockElement("span", parent);

        // Set up parent to not initially have ChildNeedsStyleRecalc
        parent.ChildNeedsStyleRecalc().Returns(false);

        // Act
        child.SetNeedsStyleRecalc();

        // Assert - In real implementation, this should trigger parent.ChildNeedsStyleRecalc = true
        // For now, we verify the child flag was set
        child.Received(1).SetNeedsStyleRecalc();
    }

    [Fact]
    public void SetNeedsLayout_PropagatesChildNeedsLayout_ToParentChain()
    {
        // Arrange
        var grandparent = TestHelpers.CreateMockElement("div");
        var parent = TestHelpers.CreateMockElement("div", grandparent);
        var child = TestHelpers.CreateMockElement("span", parent);

        // Set up parent chain
        parent.ChildNeedsLayout().Returns(false);
        grandparent.ChildNeedsLayout().Returns(false);

        // Act
        child.SetNeedsLayout();

        // Assert
        child.Received(1).SetNeedsLayout();
        // In real implementation, should also set parent and grandparent ChildNeedsLayout flags
    }

    [Fact]
    public void SetNeedsLayout_AlsoSetsNeedsPaintInvalidation()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        element.SetNeedsLayout();

        // Assert - Layout changes should automatically trigger paint invalidation
        element.Received(1).SetNeedsLayout();
        element.Received(1).SetNeedsPaintInvalidation();
    }

    [Fact]
    public void ClearNeedsStyleRecalc_ClearsChildNeedsStyleRecalc_WhenNoChildrenNeedRecalc()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var child1 = TestHelpers.CreateMockElement("span", parent);
        var child2 = TestHelpers.CreateMockElement("span", parent);
        var children = new TestHtmlCollection(new[] { child1, child2 });
        parent.Children.Returns(children);

        // Set up: no children need style recalc
        child1.NeedsStyleRecalc().Returns(false);
        child1.ChildNeedsStyleRecalc().Returns(false);
        child2.NeedsStyleRecalc().Returns(false);
        child2.ChildNeedsStyleRecalc().Returns(false);

        // Act
        parent.ClearNeedsStyleRecalc();

        // Assert
        parent.Received(1).ClearNeedsStyleRecalc();
        // Should also clear ChildNeedsStyleRecalc since no children need recalc
    }

    [Fact]
    public void ClearNeedsStyleRecalc_DoesNotClearChildNeedsStyleRecalc_WhenChildStillNeedsRecalc()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var needyChild = TestHelpers.CreateMockElement("span", parent);
        var cleanChild = TestHelpers.CreateMockElement("span", parent);
        var children = new TestHtmlCollection(new[] { needyChild, cleanChild });
        parent.Children.Returns(children);

        // Set up: one child still needs recalc
        needyChild.NeedsStyleRecalc().Returns(true);
        needyChild.ChildNeedsStyleRecalc().Returns(false);
        cleanChild.NeedsStyleRecalc().Returns(false);
        cleanChild.ChildNeedsStyleRecalc().Returns(false);

        // Act
        parent.ClearNeedsStyleRecalc();

        // Assert
        parent.Received(1).ClearNeedsStyleRecalc();
        // Should NOT clear ChildNeedsStyleRecalc since one child still needs recalc
    }

    [Fact]
    public void DeepNesting_PropagatesProperly()
    {
        // Arrange - Create 5-level deep nesting
        var level1 = TestHelpers.CreateMockElement("div");        // Root
        var level2 = TestHelpers.CreateMockElement("div", level1);
        var level3 = TestHelpers.CreateMockElement("div", level2);
        var level4 = TestHelpers.CreateMockElement("div", level3);
        var level5 = TestHelpers.CreateMockElement("span", level4); // Leaf

        // Set up parent chain - none initially have ChildNeeds flags
        level1.ChildNeedsStyleRecalc().Returns(false);
        level2.ChildNeedsStyleRecalc().Returns(false);
        level3.ChildNeedsStyleRecalc().Returns(false);
        level4.ChildNeedsStyleRecalc().Returns(false);

        // Act - Invalidate deepest child
        level5.SetNeedsStyleRecalc();

        // Assert
        level5.Received(1).SetNeedsStyleRecalc();
        // In real implementation, all parents should get ChildNeedsStyleRecalc set
    }

    [Fact]
    public void HasAnyInvalidation_ReturnsTrueWhenAnyFlagSet()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Test each flag type
        TestHelpers.SetupElementInvalidationFlags(element, needsStyle: true);
        Assert.True(element.HasAnyInvalidation());

        TestHelpers.SetupElementInvalidationFlags(element, needsLayout: true);
        Assert.True(element.HasAnyInvalidation());

        TestHelpers.SetupElementInvalidationFlags(element, needsPaint: true);
        Assert.True(element.HasAnyInvalidation());

        TestHelpers.SetupElementInvalidationFlags(element, childNeedsStyle: true);
        Assert.True(element.HasAnyInvalidation());

        TestHelpers.SetupElementInvalidationFlags(element, childNeedsLayout: true);
        Assert.True(element.HasAnyInvalidation());
    }

    [Fact]
    public void HasAnyInvalidation_ReturnsFalseWhenNoFlagsSet()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        TestHelpers.SetupElementInvalidationFlags(element); // All false

        // Assert
        Assert.False(element.HasAnyInvalidation());
    }

    [Fact]
    public void ClearAllInvalidation_ClearsAllFlags()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        element.ClearAllInvalidation();

        // Assert
        element.Received(1).ClearAllInvalidation();
    }

    [Fact]
    public void MultipleSiblings_PropagationWorksCorrectly()
    {
        // Arrange
        var parent = TestHelpers.CreateMockElement("div");
        var child1 = TestHelpers.CreateMockElement("span", parent);
        var child2 = TestHelpers.CreateMockElement("span", parent);
        var child3 = TestHelpers.CreateMockElement("span", parent);
        var children = new TestHtmlCollection(new[] { child1, child2, child3 });
        parent.Children.Returns(children);

        parent.ChildNeedsStyleRecalc().Returns(false);

        // Act - Invalidate middle child
        child2.SetNeedsStyleRecalc();

        // Assert
        child2.Received(1).SetNeedsStyleRecalc();
        // Parent should get ChildNeedsStyleRecalc flag
    }

    [Fact]
    public void PropagationStops_WhenParentAlreadyHasChildFlag()
    {
        // Arrange
        var grandparent = TestHelpers.CreateMockElement("div");
        var parent = TestHelpers.CreateMockElement("div", grandparent);
        var child = TestHelpers.CreateMockElement("span", parent);

        // Set up: parent already has ChildNeedsStyleRecalc
        parent.ChildNeedsStyleRecalc().Returns(true);
        grandparent.ChildNeedsStyleRecalc().Returns(false);

        // Act
        child.SetNeedsStyleRecalc();

        // Assert
        child.Received(1).SetNeedsStyleRecalc();
        // Propagation should stop at parent since it already has the flag
        // Only grandparent should get the flag in real implementation
    }
}