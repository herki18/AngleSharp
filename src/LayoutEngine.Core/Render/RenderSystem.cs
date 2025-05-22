using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render.Commands;

namespace LayoutEngine.Core.Render;

public class RenderSystem : IRenderSystem
{
    private readonly IEventAggregator _eventAggregator;
    private readonly RenderTreeWalker _treeWalker;
    private IRenderer? _renderer;

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

        // Clear paint flags after processing
        ClearPaintFlags(fragmentTree.RootFragment);

        _eventAggregator.Publish(new RenderCompletedEvent());
        return commands;
    }

    public void InvalidateRender(IElement element, bool recursive = true)
    {
        var affectedElements = new List<IElement>();

        // Use node flags instead of HashSet
        element.SetNeedsPaintInvalidation();
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
        element.SetNeedsPaintInvalidation();
        affectedElements.Add(element);

        foreach (var child in element.Children.OfType<IElement>())
            InvalidateRenderRecursive(child, affectedElements);
    }

    public bool NeedsRender(IElement element)
    {
        // Use node flags instead of HashSet
        return element.NeedsPaintInvalidation();
    }

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

    /// <summary>
    /// Recursively clears paint invalidation flags from all elements in the fragment tree.
    /// </summary>
    private void ClearPaintFlags(ILayoutFragment rootFragment)
    {
        if (rootFragment.Element != null)
        {
            rootFragment.Element.ClearNeedsPaintInvalidation();
        }

        foreach (var child in rootFragment.Children)
        {
            ClearPaintFlags(child);
        }
    }
}