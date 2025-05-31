namespace LayoutEngine.NG.Layout;

using AngleSharp.Dom;

public interface ILayoutSystem
{
    bool HasDirtyNodes(IDocument document);
    void PerformLayout(IDocument document);
}