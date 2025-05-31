namespace LayoutEngine.NG.Core;

/// <summary>
/// Defines the phases of the document lifecycle.
/// </summary>
public enum DocumentLifecyclePhase
{
    /// <summary>
    /// Initial state after creation before active processing.
    /// </summary>
    Inactive,

    /// <summary>
    /// Document with clean (up-to-date) styles.
    /// </summary>
    StyleClean,

    /// <summary>
    /// Actively calculating styles.
    /// </summary>
    InStyleRecalc,

    /// <summary>
    /// Document with clean (up-to-date) layout.
    /// </summary>
    LayoutClean,

    /// <summary>
    /// Actively calculating layout.
    /// </summary>
    InLayout,

    /// <summary>
    /// Document ready for rendering via external frameworks.
    /// </summary>
    RenderReady,

    /// <summary>
    /// Actively rendering via external framework.
    /// </summary>
    InRender,

    /// <summary>
    /// Document has been shut down.
    /// </summary>
    Disposed,

    /// <summary>
    /// Unknown phase (used for error conditions).
    /// </summary>
    Unknown
}