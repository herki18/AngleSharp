// Path: LayoutEngine.Contracts/StyleSystem/StyleSheetOrigin.cs (or wherever you place your contracts)
namespace LayoutEngine.Contracts.StyleSystem;

/// <summary>
/// Defines the origin of a stylesheet, influencing its precedence in the cascade.
/// </summary>
public enum StyleSheetOrigin
{
    /// <summary>
    /// Styles originating from the browser or user agent default styles. Lowest precedence.
    /// </summary>
    UserAgent = 0,

    /// <summary>
    /// Styles originating from user-defined stylesheets (e.g., via browser settings).
    /// </summary>
    User = 1,

    /// <summary>
    /// Styles originating from the document's author (linked or embedded stylesheets).
    /// </summary>
    Author = 2,

    /// <summary>
    /// Styles originating from inline style attributes on elements. Highest precedence for non-important rules.
    /// </summary>
    Inline = 3 // Added to match the new contract
}