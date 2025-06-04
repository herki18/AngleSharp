namespace LayoutEngine.NG.Dom;

using AngleSharp.Dom;
using Layout;

/// <summary>
/// Extension methods for document-related layout operations.
/// </summary>
public static class DocumentExtensions
{
    /// <summary>
    /// Gets the layout view for a document.
    /// In LayoutNG, every rendered document has a LayoutView as its root.
    /// </summary>
    public static LayoutView? GetLayoutView(this IDocument document)
    {
        // In a real implementation, this would get the view from the document's layout data
        // For now, return null or implement proper view management

        // TODO: Implement proper LayoutView management
        // This would typically be stored in DocumentEngineData
        return null;
    }

    /// <summary>
    /// Gets whether the document is in quirks mode.
    /// </summary>
    public static bool InQuirksMode(this IDocument document)
    {
        // AngleSharp should provide quirks mode information
        // For now, return false as default
        return false;
    }
}