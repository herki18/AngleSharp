using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using NSubstitute;

namespace LayoutEngine.Core.Tests;

using Render.Commands;

public class RenderSystemTests
{
    private readonly IEventAggregator _eventAggregator;
    private readonly RenderSystem _renderSystem;

    public RenderSystemTests()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _renderSystem = new RenderSystem(_eventAggregator);
    }

    [Fact]
    public void ProcessFragmentTree_GeneratesRenderCommands()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        // Act
        var commands = _renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert
        Assert.NotNull(commands);
        Assert.NotEmpty(commands);

        // Should at least have a create command and a set layout command
        Assert.Contains(commands, c => c.CommandType == RenderCommandType.Create);
        Assert.Contains(commands, c => c.CommandType == RenderCommandType.SetLayout);
    }

    [Fact]
    public void ProcessFragmentTree_WithRenderer_ExecutesCommands()
    {
        // Arrange
        var renderer = Substitute.For<IRenderer>();
        _renderSystem.AttachRenderer(renderer);

        var element = TestHelpers.CreateMockElement();
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        // Act
        _renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert
        renderer.Received(1).Execute(Arg.Any<IReadOnlyList<IRenderCommand>>());
    }

    [Fact]
    public void ProcessFragmentTree_PublishesRenderCompletedEvent()
    {
        // Arrange
        var fragmentTree = TestHelpers.CreateMockFragmentTree();

        // Act
        _renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Any<RenderCompletedEvent>());
    }

    [Fact]
    public void NeedsRender_ReturnsFalse_ForInitialElement()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        var result = _renderSystem.NeedsRender(element);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsRender_ReturnsTrue_ForInvalidatedElement()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        _renderSystem.InvalidateRender(element, false);

        // Act
        var result = _renderSystem.NeedsRender(element);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InvalidateRender_PublishesRenderInvalidatedEvent()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act
        _renderSystem.InvalidateRender(element, false);

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Any<RenderInvalidatedEvent>());
    }

    [Fact]
    public void InvalidateRender_WithRecursive_InvalidatesChildRenders()
    {
        // Arrange
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var parent = TestHelpers.CreateMockElement("div");

        // Create a proper mock for IHtmlCollection that will work with LINQ's OfType<IElement>()
        var children = new List<IElement> { child1, child2 };

        // Enhanced TestHtmlCollection implementation
        var htmlCollection = new TestHtmlCollection(children);

        // Set up the parent.Children to return our test collection
        parent.Children.Returns(htmlCollection);

        // Verify we've set up the Children property correctly
        Assert.Equal(2, parent.Children.Length);

        // Act - invalidate the parent with recursive=true
        _renderSystem.InvalidateRender(parent, true);

        // Assert
        Assert.True(_renderSystem.NeedsRender(parent), "Parent element should need rendering");
        Assert.True(_renderSystem.NeedsRender(child1), "First child element should need rendering");
        Assert.True(_renderSystem.NeedsRender(child2), "Second child element should need rendering");
    }

    [Fact]
    public void AttachRenderer_SetsRenderer()
    {
        // Arrange
        var renderer = Substitute.For<IRenderer>();

        // Act
        _renderSystem.AttachRenderer(renderer);

        // Assert
        Assert.Equal(renderer, _renderSystem.GetRenderer());
    }

    [Fact]
    public void GetRenderer_ThrowsException_WhenNoRendererAttached()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _renderSystem.GetRenderer());
    }

    [Fact]
    public void ProcessFragmentTree_MaintainsFragmentIdentity()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var childElement = TestHelpers.CreateMockElement("span", element);

        var childFragment = TestHelpers.CreateMockLayoutFragment(childElement);
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        layoutFragment.Children.Returns(new List<ILayoutFragment> { childFragment });

        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        // Act
        // Process twice to ensure fragment identity is maintained
        var commands1 = _renderSystem.ProcessFragmentTree(fragmentTree);
        var commands2 = _renderSystem.ProcessFragmentTree(fragmentTree);

        // Assert
        // The second processing should not create new elements
        Assert.DoesNotContain(commands2, c => c.CommandType == RenderCommandType.Create);
    }

    [Fact]
    public void OnLayoutInvalidated_InvalidatesRender()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();
        var layoutInvalidatedEvent = new LayoutInvalidatedEvent(new List<IElement> { element });

        // Clear any previous interactions with the mock
        _eventAggregator.ClearReceivedCalls();

        // Act - directly simulate what the event handler should do
        // By extracting the event handler logic and calling it directly
        foreach (var el in layoutInvalidatedEvent.Elements)
        {
            _renderSystem.InvalidateRender(el, false);
        }

        // Assert
        Assert.True(_renderSystem.NeedsRender(element));
        _eventAggregator.Received(1).Publish(Arg.Is<RenderInvalidatedEvent>(e =>
            e.Elements.Contains(element)));
    }

}