namespace AngleSharp.LayoutEngine.StyleComputation;

/// <summary>
/// Represents the origin of a stylesheet, which affects cascade priority.
/// </summary>
public enum StylesheetOrigin
{
    /// <summary>Default browser styles</summary>
    UserAgent,

    /// <summary>User-specified styles</summary>
    User,

    /// <summary>Document/author styles</summary>
    Author
}