namespace LayoutEngine.NG.Core;

using LayoutEngine.NG.Layout;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Responsible for advancing the lifecycle of its associated Frame.
/// Follows BlinkNG's per-frame scheduling architecture.
/// </summary>
public class FrameScheduler
{
    private readonly Frame _frame;

    public FrameScheduler(Frame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Advances the frame's lifecycle by one phase step.
    /// Decides which subsystem to call based on the current phase.
    /// </summary>
    public void Update()
    {
        var coordinator = _frame.LifecycleCoordinator;
        var currentPhase = coordinator.CurrentPhase;

        // LocalFrame is required to access LayoutView
        // In BlinkNG, only LocalFrame has a layout tree
        var localFrame = _frame as LocalFrame;

        // Get the layout data manager from LocalFrame
        var layoutDataManager = localFrame?.LayoutDataManager;
        if (layoutDataManager == null)
            return;

        // Get the document's StyleEngine through DocumentEngineData
        var docLayout = layoutDataManager.GetOrCreate(_frame.Document);
        var styleEngine = docLayout.StyleEngine;

        // Advance through phases following BlinkNG's lifecycle model
        switch (currentPhase)
        {
            case DocumentLifecyclePhase.Inactive:
                // Initial state - advance to style calculation
                if (styleEngine.NeedsStyleRecalc())
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.InStyleRecalc);
                }
                else
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.StyleClean);
                }
                break;

            case DocumentLifecyclePhase.InStyleRecalc:
                // Perform style calculation
                styleEngine.UpdateStyleAndLayoutTree();
                coordinator.TransitionTo(DocumentLifecyclePhase.StyleClean);
                break;

            case DocumentLifecyclePhase.StyleClean:
                // Check if layout is needed by inspecting LayoutView
                // In BlinkNG, layout need is tracked on LayoutObject/LayoutView, not a separate system
                if (localFrame?.LayoutView?.NeedsLayout ?? false)
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.InLayout);
                }
                else
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.LayoutClean);
                }
                break;

            case DocumentLifecyclePhase.InLayout:
                // Perform layout calculation
                // In BlinkNG, layout is triggered on the LayoutView
                if (localFrame?.LayoutView != null)
                {
                    // Perform the layout using the enhanced Layout method
                    localFrame.LayoutView.Layout();
                }
                coordinator.TransitionTo(DocumentLifecyclePhase.LayoutClean);
                break;

            case DocumentLifecyclePhase.LayoutClean:
                // Check if paint is needed
                if (_frame.PaintSystem.HasDirtyNodes(_frame.Document))
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.RenderReady);
                }
                else
                {
                    // No paint needed, check if we need to restart the cycle
                    if (styleEngine.NeedsStyleRecalc())
                    {
                        coordinator.TransitionTo(DocumentLifecyclePhase.InStyleRecalc);
                    }
                }
                break;

            case DocumentLifecyclePhase.RenderReady:
                // Ready for external renderer
                coordinator.TransitionTo(DocumentLifecyclePhase.InRender);
                break;

            case DocumentLifecyclePhase.InRender:
                // Perform rendering
                _frame.PaintSystem.Render(_frame.Document);

                // After render, check if we need to restart the cycle
                if (styleEngine.NeedsStyleRecalc())
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.InStyleRecalc);
                }
                else
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.StyleClean);
                }
                break;

            case DocumentLifecyclePhase.Disposed:
                // Frame is disposed, do nothing
                break;
        }
    }
}