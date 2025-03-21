namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Test window provider for testing
/// </summary>
public class TestWindowProvider : IWindowProvider
{
    private TestWindow? _window;

    public TestWindowProvider()
    {
        _window = new TestWindow();
    }

    public IWindow? GetWindow()
    {
        return _window;
    }

    /// <summary>
    /// Gets the test window
    /// </summary>
    public TestWindow? TestWindow => _window;

    /// <summary>
    /// Sets the window to return
    /// </summary>
    public void SetWindow(TestWindow? window)
    {
        _window = window;
    }
}