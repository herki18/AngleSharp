using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render.Commands;

namespace LayoutEngine.Core.Render;

using System.Linq;

public class RenderSystem : IRenderSystem
{
    private readonly IEventAggregator _eventAggregator;
    private readonly RenderTreeWalker _treeWalker;
    private IRenderer? _renderer;
    private readonly HashSet<IElement> _elementsNeedingRender = new();

    public RenderSystem(
        IEventAggregator eventAggregator,
        RenderTreeWalker treeWalker)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _treeWalker = treeWalker ?? throw new ArgumentNullException(nameof(treeWalker));
        _eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated);
    }

    public IReadOnlyList<IRenderCommand> ProcessFragmentTree(IFragmentTree fragmentTree)
    {
        var commands = _treeWalker.Walk(fragmentTree);
        _renderer?.Execute(commands);
        _eventAggregator.Publish(new RenderCompletedEvent());
        return commands;
    }

    public void InvalidateRender(IElement element, bool recursive = true)
    {
        var affectedElements = new List<IElement>();
        _elementsNeedingRender.Add(element);
        affectedElements.Add(element);
        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
                InvalidateRenderRecursive(child, affectedElements);
        }
        _eventAggregator.Publish(new RenderInvalidatedEvent(affectedElements));
    }

    private void InvalidateRenderRecursive(IElement element, List<IElement> affectedElements)
    {
        _elementsNeedingRender.Add(element);
        affectedElements.Add(element);
        foreach (var child in element.Children.OfType<IElement>())
            InvalidateRenderRecursive(child, affectedElements);
    }

    public bool NeedsRender(IElement element) => _elementsNeedingRender.Contains(element);

    public void AttachRenderer(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public IRenderer GetRenderer()
    {
        return _renderer ?? throw new InvalidOperationException("No renderer attached to the render system.");
    }

    private void OnLayoutInvalidated(LayoutInvalidatedEvent @event)
    {
        foreach (var element in @event.Elements)
            InvalidateRender(element, false);
    }
}