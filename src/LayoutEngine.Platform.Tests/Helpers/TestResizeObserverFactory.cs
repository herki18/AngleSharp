namespace LayoutEngine.Platform.Tests.Helpers;

using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Test resize observer factory for testing
/// </summary>
public class TestResizeObserverFactory : IResizeObserverFactory
{
    public IResizeObserver Create(Action<ResizeObserverEntry[]> callback)
    {
        return new TestResizeObserver(callback);
    }
}