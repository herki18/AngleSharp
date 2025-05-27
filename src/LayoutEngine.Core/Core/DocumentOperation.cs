namespace LayoutEngine.Core.Core;

/// <summary>
/// Defines operations that can be performed on a document.
/// </summary>
public enum DocumentOperation
{
    /// <summary>
    /// Reading styles.
    /// </summary>
    StyleReading,

    /// <summary>
    /// Modifying styles.
    /// </summary>
    StyleModification,

    /// <summary>
    /// Reading layout.
    /// </summary>
    LayoutReading,

    /// <summary>
    /// Calculating layout.
    /// </summary>
    LayoutCalculation,

    /// <summary>
    /// Reading render information.
    /// </summary>
    RenderReading,

    /// <summary>
    /// Performing rendering.
    /// </summary>
    Rendering,

    /// <summary>
    /// Reading DOM.
    /// </summary>
    DomReading,

    /// <summary>
    /// Modifying DOM.
    /// </summary>
    DomModification
}