namespace LayoutEngine.Core.Tests;

using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using LayoutEngine.Core.Render.Commands;
using LayoutEngine.Core.Viewport;
using NSubstitute;
using Style;
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
    public void NeedsRender_ReturnsFalse_ForInitialElement()
    {
        var element = TestHelpers.CreateMockElement();

        var result = _renderSystem.NeedsRender(element);

        Assert.False(result);
    }

    [Fact]
    public void NeedsRender_ReturnsTrue_ForInvalidatedElement()
    {
        var element = TestHelpers.CreateMockElement();
        _renderSystem.InvalidateRender(element, false);

        var result = _renderSystem.NeedsRender(element);

        Assert.True(result);
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

        Assert.True(_renderSystem.NeedsRender(parent), "Parent element should need rendering");
        Assert.True(_renderSystem.NeedsRender(child1), "First child element should need rendering");
        Assert.True(_renderSystem.NeedsRender(child2), "Second child element should need rendering");
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

        Assert.DoesNotContain(commands2, c => c.CommandType == RenderCommandType.Create);
    }

    [Fact]
    public void OnLayoutInvalidated_InvalidatesRender()
    {
        var element = TestHelpers.CreateMockElement();
        var layoutInvalidatedEvent = new LayoutInvalidatedEvent(new List<IElement> { element });

        _eventAggregator.ClearReceivedCalls();

        foreach (var el in layoutInvalidatedEvent.Elements)
        {
            _renderSystem.InvalidateRender(el, false);
        }

        Assert.True(_renderSystem.NeedsRender(element));
        _eventAggregator.Received(1).Publish(Arg.Is<RenderInvalidatedEvent>(e =>
            e.Elements.Contains(element)));
    }
}