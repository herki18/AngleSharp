namespace AngleSharp.StyleSystem.Interfaces;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Manages stylesheets from different origins and provides centralized access to them.
/// </summary>
public interface IStyleSheetManager : IDisposable
{
    /// <summary>
    /// Event raised when stylesheets are added, removed, or modified.
    /// </summary>
    event EventHandler<StylesheetChangedEventArgs>? StylesheetChanged;

    /// <summary>
    /// Attaches to a document and loads its stylesheets.
    /// </summary>
    /// <param name="document">The document to attach to.</param>
    void AttachToDocument(IDocument document);

    /// <summary>
    /// Detaches from the specified document.
    /// </summary>
    /// <param name="document">The document to detach from.</param>
    void DetachFromDocument(IDocument document);

    /// <summary>
    /// Registers a stylesheet with the manager.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to register.</param>
    /// <param name="origin">The origin of the stylesheet.</param>
    void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);

    /// <summary>
    /// Unregisters a stylesheet from the manager.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to unregister.</param>
    void UnregisterStylesheet(ICssStyleSheet stylesheet);

    /// <summary>
    /// Gets all registered stylesheets in priority order.
    /// </summary>
    /// <returns>Stylesheets ordered by cascade priority.</returns>
    IEnumerable<StylesheetEntry> GetStylesheets();

    /// <summary>
    /// Gets stylesheets from a specific origin.
    /// </summary>
    /// <param name="origin">The origin to filter by.</param>
    /// <returns>Stylesheets from the specified origin.</returns>
    IEnumerable<ICssStyleSheet> GetStylesheetsByOrigin(StylesheetOrigin origin);

    /// <summary>
    /// Gets the origin of a stylesheet.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to check.</param>
    /// <returns>The origin of the stylesheet.</returns>
    StylesheetOrigin GetStylesheetOrigin(ICssStyleSheet stylesheet);

    /// <summary>
    /// Gets all rules from all stylesheets in cascade order.
    /// </summary>
    /// <returns>All CSS rules.</returns>
    IEnumerable<ICssRule> GetAllRules();

    /// <summary>
    /// Gets all style rules from all stylesheets in cascade order.
    /// </summary>
    /// <returns>All CSS style rules.</returns>
    IEnumerable<ICssStyleRule> GetAllStyleRules();

    /// <summary>
    /// Checks for changes in the document's stylesheets and updates if needed.
    /// </summary>
    void RefreshDocumentStylesheets();
}