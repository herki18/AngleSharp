using System.Collections.Generic;
using AngleSharp.Css.Dom;

namespace LayoutEngine.Contracts.Platform.StyleSheets;

using AngleSharp.Dom;

/// <summary>
/// Manages stylesheets for the document.
/// </summary>
public interface IStyleSheetManager
{
    /// <summary>
    /// Adds a stylesheet to the document.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to add.</param>
    void AddStyleSheet(IStyleSheet stylesheet);

    /// <summary>
    /// Removes a stylesheet from the document.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to remove.</param>
    void RemoveStyleSheet(IStyleSheet stylesheet);

    /// <summary>
    /// Gets all stylesheets for the document.
    /// </summary>
    /// <returns>The stylesheets.</returns>
    IEnumerable<IStyleSheet> GetStyleSheets();

    /// <summary>
    /// Gets a value indicating whether user agent stylesheets are loaded.
    /// </summary>
    bool AreUserAgentStylesheetsLoaded { get; }

    /// <summary>
    /// Loads the user agent stylesheets.
    /// </summary>
    void LoadUserAgentStylesheets();
}