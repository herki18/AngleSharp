namespace LayoutEngine.Contracts.StyleSystem;

/// <summary>
/// Represents the origin of a stylesheet.
/// </summary>
public enum StyleSheetOrigin
{
    UserAgent = 0,
    User = 1,
    Author = 2,
    Inline = 3
}