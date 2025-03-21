namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Test mutation observer for testing
/// </summary>
public class TestMutationObserver : IMutationObserver
{
    private readonly Action<MutationRecord[]> _callback;
    private IDomNode? _target;
    private MutationObserverInit? _options;
    private bool _isConnected;

    public TestMutationObserver(Action<MutationRecord[]> callback)
    {
        _callback = callback;
    }

    public void Observe(IDomNode target, MutationObserverInit options)
    {
        _target = target;
        _options = options;
        _isConnected = true;
    }

    public void Disconnect()
    {
        _isConnected = false;
        _target = null;
        _options = null;
    }

    /// <summary>
    /// Simulates a mutation for testing
    /// </summary>
    public void SimulateMutation(MutationRecord mutation)
    {
        if (_isConnected)
        {
            _callback(new[] { mutation });
        }
    }

    /// <summary>
    /// Gets the target being observed
    /// </summary>
    public IDomNode? Target => _target;

    /// <summary>
    /// Gets whether the observer is connected
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Gets the options being used
    /// </summary>
    public MutationObserverInit? Options => _options;
}