namespace LayoutEngine.NG.Layout.Process;

using LayoutEngine.NG.Layout.Core;
using LayoutEngine.NG.Style;

/// <summary>
/// Context for attaching layout objects to the tree.
/// In LayoutNG, this carries state during attachment operations.
/// </summary>
public class AttachContext
{
    public LayoutObject? Parent { get; set; }
    public bool IsDocumentAttachment { get; set; }
    public bool IsReattachment { get; set; }
    public StyleRecalcContext? StyleRecalcContext { get; set; }
    public bool WasDestroyed { get; set; }
}