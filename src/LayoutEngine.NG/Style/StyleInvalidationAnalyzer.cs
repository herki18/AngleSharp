namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;

internal class StyleInvalidationAnalyzer
{
    internal StyleInvalidationSet AnalyzeStyleSheetChange(
        StyleSheetContents oldContents,
        StyleSheetContents newContents) { }

    internal void InvalidateStyle(StyleInvalidationSet invalidationSet, IElement root) { }
}