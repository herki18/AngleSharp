namespace LayoutEngine.NG.Core;

using LayoutEngine.NG.Layout;

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

        // Advance through phases following BlinkNG's lifecycle model
        switch (currentPhase)
        {
            case DocumentLifecyclePhase.Inactive:
                // Initial state - advance to style calculation
                if (_frame.StyleEngine.NeedsStyleRecalc(_frame.Document))
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
                _frame.StyleEngine.RecalcStyle(_frame.Document);
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
                    // Mark that we're in layout to prevent re-entrancy
                    localFrame.LayoutView.IsInLayout = true;

                    // In real BlinkNG, this would call LayoutView::UpdateLayout()
                    // or similar method that performs the actual layout algorithm
                    PerformLayout(localFrame.LayoutView);

                    localFrame.LayoutView.IsInLayout = false;
                }
                coordinator.TransitionTo(DocumentLifecyclePhase.LayoutClean);
                break;

            case DocumentLifecyclePhase.LayoutClean:
                // Check if paint is needed
                if (_frame.PaintSystem.HasDirtyNodes(_frame.Document))
                {
                    coordinator.TransitionTo(DocumentLifecyclePhase.RenderReady);
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
                if (_frame.StyleEngine.NeedsStyleRecalc(_frame.Document))
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

    /// <summary>
    /// Performs layout starting from the LayoutView.
    /// In BlinkNG, this would be LayoutView::UpdateLayout() or Document::UpdateLayout().
    /// </summary>
    private void PerformLayout(LayoutView layoutView)
    {
        // Skeleton implementation
        // In real BlinkNG, this would:
        // 1. Traverse the layout tree from the root
        // 2. Call the appropriate layout algorithm for each LayoutObject
        // 3. Generate fragments
        // 4. Clear NeedsLayout flags

        // For now, just clear the needs layout flag
        layoutView.NeedsLayout = false;

        // Update overflow after layout
        layoutView.UpdateLayoutOverflow();
    }
}