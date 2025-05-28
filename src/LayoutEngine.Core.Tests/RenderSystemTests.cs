namespace LayoutEngine.Core.Tests;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Layout.Public;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using LayoutEngine.Core.Render.Commands;
using LayoutEngine.Core.Viewport;
using NSubstitute;
using Style.Public;
using Xunit;

public class RenderSystemTests
{
    private readonly IEventAggregator _eventAggregator;
    private readonly FragmentRegistry _fragmentRegistry;
    private readonly IStyleSystem _styleSystem;
    private readonly ViewportManager _viewportManager;
    private readonly RenderCommandGenerator _commandGenerator;
    private readonly RenderTreeWalker _treeWalker;
    private readonly RenderSystem _renderSystem;

    public RenderSystemTests()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _fragmentRegistry = new FragmentRegistry();
        _styleSystem = Substitute.For<IStyleSystem>();
        _viewportManager = new ViewportManager(_styleSystem);
        _commandGenerator = new RenderCommandGenerator();
        _treeWalker = new RenderTreeWalker(_fragmentRegistry, _viewportManager, _commandGenerator);
        _renderSystem = new RenderSystem(_eventAggregator, _treeWalker);
    }

    [Fact]
    public void ProcessFragmentTree_GeneratesRenderCommands()
    {
        var element = TestHelpers.CreateMockElement();
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        var commands = _renderSystem.ProcessFragmentTree(fragmentTree);

        Assert.NotNull(commands);
        Assert.NotEmpty(commands);
        Assert.Contains(commands, c => c.CommandType == RenderCommandType.Create);
        Assert.Contains(commands, c => c.CommandType == RenderCommandType.SetLayout);
    }

    [Fact]
    public void ProcessFragmentTree_WithRenderer_ExecutesCommands()
    {
        var renderer = Substitute.For<IRenderer>();
        _renderSystem.AttachRenderer(renderer);

        var element = TestHelpers.CreateMockElement();
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        _renderSystem.ProcessFragmentTree(fragmentTree);

        renderer.Received(1).Execute(Arg.Any<IReadOnlyList<IRenderCommand>>());
    }

    [Fact]
    public void ProcessFragmentTree_PublishesRenderCompletedEvent()
    {
        var fragmentTree = TestHelpers.CreateMockFragmentTree();

        _renderSystem.ProcessFragmentTree(fragmentTree);

        _eventAggregator.Received(1).Publish(Arg.Any<RenderCompletedEvent>());
    }

    [Fact]
    public void ProcessFragmentTree_ClearsPaintFlags()
    {
        var element = TestHelpers.CreateMockElement();
        // Set up the element to return true for NeedsPaintInvalidation initially
        element.NeedsPaintInvalidation().Returns(true);

        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        _renderSystem.ProcessFragmentTree(fragmentTree);

        // Verify that ClearNeedsPaintInvalidation was called
        element.Received(1).ClearNeedsPaintInvalidation();
    }

    [Fact]
    public void NeedsRender_UsesNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        // Set up mock to return false initially
        element.NeedsPaintInvalidation().Returns(false);
        Assert.False(_renderSystem.NeedsRender(element));

        // Set up mock to return true after invalidation
        element.NeedsPaintInvalidation().Returns(true);
        Assert.True(_renderSystem.NeedsRender(element));
    }

    [Fact]
    public void InvalidateRender_SetsNodeFlags()
    {
        var element = TestHelpers.CreateMockElement();

        _renderSystem.InvalidateRender(element, false);

        // Verify that SetNeedsPaintInvalidation was called
        element.Received(1).SetNeedsPaintInvalidation();
    }

    [Fact]
    public void InvalidateRender_PublishesRenderInvalidatedEvent()
    {
        var element = TestHelpers.CreateMockElement();

        _renderSystem.InvalidateRender(element, false);

        _eventAggregator.Received(1).Publish(Arg.Any<RenderInvalidatedEvent>());
    }

    [Fact]
    public void InvalidateRender_WithRecursive_InvalidatesChildRenders()
    {
        var child1 = TestHelpers.CreateMockElement("div");
        var child2 = TestHelpers.CreateMockElement("span");
        var parent = TestHelpers.CreateMockElement("div");
        var children = new List<IElement> { child1, child2 };
        var htmlCollection = new TestHtmlCollection(children);
        parent.Children.Returns(htmlCollection);

        Assert.Equal(2, parent.Children.Length);

        _renderSystem.InvalidateRender(parent, true);

        // Verify that SetNeedsPaintInvalidation was called on all elements
        parent.Received(1).SetNeedsPaintInvalidation();
        child1.Received(1).SetNeedsPaintInvalidation();
        child2.Received(1).SetNeedsPaintInvalidation();
    }

    [Fact]
    public void AttachRenderer_SetsRenderer()
    {
        var renderer = Substitute.For<IRenderer>();

        _renderSystem.AttachRenderer(renderer);

        Assert.Equal(renderer, _renderSystem.GetRenderer());
    }

    [Fact]
    public void GetRenderer_ThrowsException_WhenNoRendererAttached()
    {
        Assert.Throws<InvalidOperationException>(() => _renderSystem.GetRenderer());
    }

    [Fact]
    public void ProcessFragmentTree_MaintainsFragmentIdentity()
    {
        var element = TestHelpers.CreateMockElement();
        var childElement = TestHelpers.CreateMockElement("span", element);
        var childFragment = TestHelpers.CreateMockLayoutFragment(childElement);
        var layoutFragment = TestHelpers.CreateMockLayoutFragment(element);
        layoutFragment.Children.Returns(new List<ILayoutFragment> { childFragment });
        var fragmentTree = TestHelpers.CreateMockFragmentTree(layoutFragment);

        var commands1 = _renderSystem.ProcessFragmentTree(fragmentTree);
        var commands2 = _renderSystem.ProcessFragmentTree(fragmentTree);

        // On second run, elements shouldn't need to be created again
        Assert.DoesNotContain(commands2, c => c.CommandType == RenderCommandType.Create);
    }

    [Fact]
    public void OnLayoutInvalidated_InvalidatesRender()
    {
        var element = TestHelpers.CreateMockElement();
        var layoutInvalidatedEvent = new LayoutInvalidatedEvent(new List<IElement> { element });

        // Clear any previous calls to setup clean test
        _eventAggregator.ClearReceivedCalls();

        // Simulate what happens when layout invalidated event is received
        foreach (var el in layoutInvalidatedEvent.Elements)
        {
            _renderSystem.InvalidateRender(el, false);
        }

        // Verify SetNeedsPaintInvalidation was called
        element.Received(1).SetNeedsPaintInvalidation();

        _eventAggregator.Received(1).Publish(Arg.Is<RenderInvalidatedEvent>(e =>
            e.Elements.Contains(element)));
    }
}