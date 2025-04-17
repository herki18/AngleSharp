namespace LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// Defines the priority of a visual update.
/// </summary>
public enum UpdatePriority
{
    /// <summary>
    /// Low priority updates.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority updates.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority updates.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority updates.
    /// </summary>
    Critical = 30
}