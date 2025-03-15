namespace AngleSharp.StyleSystem.Interfaces;
using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Manages stylesheets within the style system.
/// </summary>
public interface IStyleSheetManager : IDisposable
{
    /// <summary>
    /// Attaches the manager to a document to track its stylesheets.
    /// </summary>
    /// <param name="document">The document to attach to.</param>
    void AttachToDocument(IDocument document);

    /// <summary>
    /// Detaches the manager from a document.
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
    /// Gets all registered stylesheets.
    /// </summary>
    /// <returns>The collection of stylesheet entries.</returns>
    IEnumerable<StylesheetEntry> GetStylesheets();

    /// <summary>
    /// Gets all stylesheets from the specified origin.
    /// </summary>
    /// <param name="origin">The origin to filter by.</param>
    /// <returns>The collection of stylesheets.</returns>
    IEnumerable<ICssStyleSheet> GetStylesheetsByOrigin(StylesheetOrigin origin);

    /// <summary>
    /// Gets the origin of a stylesheet.
    /// </summary>
    /// <param name="stylesheet">The stylesheet to check.</param>
    /// <returns>The origin of the stylesheet.</returns>
    StylesheetOrigin GetStylesheetOrigin(ICssStyleSheet stylesheet);

    /// <summary>
    /// Gets all rules from all registered stylesheets.
    /// </summary>
    /// <returns>The collection of rules.</returns>
    IEnumerable<ICssRule> GetAllRules();

    /// <summary>
    /// Gets all style rules from all registered stylesheets.
    /// </summary>
    /// <returns>The collection of style rules.</returns>
    IEnumerable<ICssStyleRule> GetAllStyleRules();

    /// <summary>
    /// Refreshes all document stylesheets.
    /// </summary>
    void RefreshDocumentStylesheets();
}