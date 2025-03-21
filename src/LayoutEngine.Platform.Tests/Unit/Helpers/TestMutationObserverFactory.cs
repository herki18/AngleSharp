namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Test mutation observer factory for testing
/// </summary>
public class TestMutationObserverFactory : IMutationObserverFactory
{
    public IMutationObserver Create(Action<MutationRecord[]> callback)
    {
        return new TestMutationObserver(callback);
    }
}