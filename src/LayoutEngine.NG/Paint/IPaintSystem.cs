namespace LayoutEngine.NG.Paint;

using AngleSharp.Dom;

public interface IPaintSystem
{
    bool HasDirtyNodes(IDocument document);
    void Render(IDocument document);
}