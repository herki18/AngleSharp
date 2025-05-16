namespace LayoutEngine.Core;

/// <summary>
/// Defines triggers that cause state transitions in the document lifecycle state machine.
/// </summary>
public enum LifecycleTrigger
{
    // Initialization triggers
    EnterStyleClean,

    // Style phase triggers
    BeginStyleCalculation,
    StyleChanged,
    NoStyleChanges,
    StyleComplete,
    ExitInStyleRecalc,
    ExitStyleDirty,

    // Layout phase triggers
    EnterLayoutClean,
    BeginLayoutCalculation,
    LayoutChanged,
    NoLayoutChanges,
    LayoutComplete,
    ExitInLayout,
    ExitLayoutDirty,

    // Render phase triggers
    EnterRenderReady,
    BeginRendering,
    RenderChanged,
    NoRenderChanges,
    RenderComplete,
    ExitInRender,
    ExitRenderDirty,

    // Frame cycle trigger
    NextFrame,

    // Disposal trigger
    Dispose
}