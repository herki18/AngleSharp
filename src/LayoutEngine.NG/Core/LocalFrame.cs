namespace LayoutEngine.NG.Core;

using AngleSharp.Dom;
using LayoutEngine.NG.Layout;
using LayoutEngine.NG.Paint;
using LayoutEngine.NG.Style;

/// <summary>
/// Represents a local browsing context (e.g., top-level frame or iframe) with a local document.
/// In BlinkNG, LocalFrame is a concrete implementation of Frame that owns a Document and its layout tree.
/// This follows BlinkNG's frame hierarchy where LocalFrame inherits from Frame.
/// </summary>
public class LocalFrame : Frame
{
    private LayoutView? _layoutView;

    public LocalFrame(
        IDocument document,
        IPaintSystem paintSystem)
        : base(document, paintSystem)
    {
        // In BlinkNG, the LocalFrame creates and owns the LayoutView
        InitializeLayoutView();
    }

    /// <summary>
    /// Gets the layout view (root of the layout tree) for this frame.
    /// In BlinkNG, this is GetLayoutView() and represents the root LayoutObject.
    /// </summary>
    public LayoutView? LayoutView => _layoutView;

    /// <summary>
    /// Triggers a lifecycle update for this frame.
    /// In BlinkNG, this corresponds to View::UpdateLifecyclePhases() or similar.
    /// </summary>
    public void Update()
    {
        // Delegate to the frame scheduler to advance the lifecycle
        Scheduler.Update();
    }

    /// <summary>
    /// Initializes the layout view for this frame.
    /// In BlinkNG, the LayoutView is created when the frame is attached to a document.
    /// </summary>
    private void InitializeLayoutView()
    {
        // Create the root layout object
        _layoutView = new LayoutView
        {
            Document = Document,
            OwnerFrame = this,
            Node = Document.DocumentElement,
            // In BlinkNG, the LayoutView starts with NeedsLayout = true
            NeedsLayout = true
        };

        // In real BlinkNG, this would also:
        // - Set up the initial containing block
        // - Connect to the document's style sheets
        // - Initialize viewport properties
    }

    /// <summary>
    /// Sets the viewport size for this frame.
    /// In BlinkNG, this is SetViewportSize() or similar.
    /// </summary>
    public void SetViewportSize(float width, float height)
    {
        _layoutView?.UpdateViewportSize(width, height);
    }

    /// <summary>
    /// Checks if this frame needs any lifecycle updates.
    /// In BlinkNG, this checks various dirty flags across subsystems.
    /// </summary>
    public bool NeedsLifecycleUpdate()
    {
        // Check style recalc needs
        if (StyleEngine.NeedsStyleRecalc(Document))
            return true;

        // Check layout needs directly on LayoutView
        // In BlinkNG, this replaces the LayoutSystem abstraction
        if (_layoutView?.NeedsLayout ?? false)
            return true;

        // Check paint needs
        if (PaintSystem.HasDirtyNodes(Document))
            return true;

        return false;
    }

    /// <summary>
    /// Gets the visible content rect for this frame.
    /// In BlinkNG, this is VisibleContentRect() and represents the scrolled viewport.
    /// </summary>
    public (float x, float y, float width, float height) GetVisibleContentRect()
    {
        if (_layoutView == null)
            return (0, 0, 0, 0);

        return (_layoutView.ScrollOffset.Left, _layoutView.ScrollOffset.Top,
                _layoutView.ViewportWidth, _layoutView.ViewportHeight);
    }
}