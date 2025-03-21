namespace LayoutEngine.Platform.Tests.Helpers;

using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

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