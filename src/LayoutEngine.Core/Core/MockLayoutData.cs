namespace LayoutEngine.Core.Core;

using Layout.Internal;
using Layout.Public;
using LayoutEngine.Core.Layout;

public static class MockLayoutData
{
    public static bool UseMockData = true; // Toggle this to enable/disable mocks

    public static ILayoutResult GetMockLayoutResult()
    {
        // Create a mock root fragment
        var rootFragment = new LayoutFragment
        {
            Bounds = new Rect(0, 0, 300, 150),
            VisualProperties = new VisualProperties
            {
                BackgroundColor = "blue",
                FontSize = 24,
                Color = "white"
            }
        };

        // Optionally add a child fragment
        var childFragment = new LayoutFragment
        {
            Bounds = new Rect(20, 20, 100, 50),
            VisualProperties = new VisualProperties
            {
                BackgroundColor = "red",
                FontSize = 16,
                Color = "yellow"
            }
        };

        rootFragment.Children = new[] { childFragment };

        var result = new LayoutResult();
        result.SetRootFragment(rootFragment);
        // Optionally, set layout info for fragments if needed
        return result;
    }
}