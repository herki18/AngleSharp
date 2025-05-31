namespace LayoutEngine.NG.Core;

using AngleSharp.Dom;
using LayoutEngine.NG.Paint;
using LayoutEngine.NG.Style;

/// <summary>
/// Represents a single document context (e.g., a page or iframe).
/// Follows BlinkNG's Frame architecture, which owns the document and its associated subsystems.
/// In BlinkNG, Frame is the base class for LocalFrame and RemoteFrame.
/// </summary>
public class Frame
{
    private readonly IDocument _document;
    private readonly IStyleEngine _styleEngine;
    private readonly IPaintSystem _paintSystem;
    private readonly DocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly FrameScheduler _frameScheduler;

    public Frame(
        IDocument document,
        IStyleEngine styleEngine,
        IPaintSystem paintSystem)
    {
        _document = document;
        _styleEngine = styleEngine;
        _paintSystem = paintSystem;
        _lifecycleCoordinator = new DocumentLifecycleCoordinator();
        _frameScheduler = new FrameScheduler(this);
    }

    /// <summary>
    /// Gets the DOM document associated with this frame.
    /// </summary>
    public IDocument Document => _document;

    /// <summary>
    /// Gets the style calculation subsystem.
    /// </summary>
    public IStyleEngine StyleEngine => _styleEngine;

    /// <summary>
    /// Gets the paint/rendering subsystem.
    /// </summary>
    public IPaintSystem PaintSystem => _paintSystem;

    /// <summary>
    /// Gets the document lifecycle coordinator that tracks the current phase.
    /// </summary>
    public DocumentLifecycleCoordinator LifecycleCoordinator => _lifecycleCoordinator;

    /// <summary>
    /// Gets the frame scheduler responsible for advancing this frame's lifecycle.
    /// </summary>
    public FrameScheduler Scheduler => _frameScheduler;
}