namespace LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// Defines the type of visual update.
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// Style-only update.
    /// </summary>
    Style,

    /// <summary>
    /// Layout update (may include style changes).
    /// </summary>
    Layout,

    /// <summary>
    /// Render update (may include style and layout changes).
    /// </summary>
    Render,

    /// <summary>
    /// Resource-related update.
    /// </summary>
    Resource,

    /// <summary>
    /// Full update of all aspects.
    /// </summary>
    Full
}