namespace LayoutEngine.Contracts.Platform.Viewport
{
    /// <summary>
    /// Detects viewport dimensions and changes.
    /// </summary>
    public interface IViewportDetector
    {
        /// <summary>
        /// Gets the current viewport dimensions.
        /// </summary>
        /// <returns>The viewport dimensions.</returns>
        ViewportDimensions GetViewportDimensions();
    }
}