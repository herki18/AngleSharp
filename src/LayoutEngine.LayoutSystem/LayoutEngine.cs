using System;

namespace LayoutEngine.LayoutSystem;

using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Contracts.LayoutSystem;
using Contracts.Platform.Lifecycle;
using Contracts.StyleSystem;

public class LayoutEngine : ILayoutEngine
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public DocumentLifecyclePhase CurrentPhase { get; }
    public bool HasPendingUpdates { get; }
    public Rect Viewport { get; }
    public Task InitializeAsync(IDocument document)
    {
        throw new NotImplementedException();
    }

    public Task ShutdownAsync()
    {
        throw new NotImplementedException();
    }

    public Task ProcessUpdatesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ILayoutBox> ComputeLayoutAsync(IElement element)
    {
        throw new NotImplementedException();
    }

    public void InvalidateLayout(IReadOnlyList<IElement> elements)
    {
        throw new NotImplementedException();
    }

    public void InvalidateAllLayout()
    {
        throw new NotImplementedException();
    }

    public ILayoutBox? GetCachedLayout(IElement element)
    {
        throw new NotImplementedException();
    }

    public ILayoutBox GetLayoutTree()
    {
        throw new NotImplementedException();
    }

    public void SetViewportSize(float width, float height)
    {
        throw new NotImplementedException();
    }

    public IElement? ElementFromPoint(float x, float y)
    {
        throw new NotImplementedException();
    }
}