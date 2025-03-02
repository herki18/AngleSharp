namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Collections.Generic;
using System.Linq;
using Css;
using Css.Dom;
using Dom;

public class StyleComputationEngine
{
    private StyleSheetManager _stylesheetManager;

    public StyleComputationEngine(IBrowsingContext? context = null, IDocument? document = null)
    {
        _stylesheetManager = new StyleSheetManager(context, document);
    }

    public ICssStyleDeclaration ComputeElementStyle(IElement element,
        ICssStyleDeclaration? parentStyle = null,
        string? pseudoElement = null)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var window = element.OwnerDocument?.DefaultView;
        if (window is null)
            throw new InvalidOperationException("Element must be part of a document with a default view");

        // 1. Get all stylesheets in the correct cascade order
        var stylesheets = _stylesheetManager.GetStylesheets();

        // Get the style collection from the window
        var styles = window.GetStyleCollection();

        // If parent style is provided, we could enhance this to use it directly
        // rather than re-computing ancestor styles, but for now we'll use
        // the standard computation method which handles the cascade

        // Compute the style declarations for the element
        var computedStyle = styles.ComputeDeclarations(element, pseudoElement);

        return computedStyle;
    }
}

public class StyleSheetManager
{
    private readonly List<StylesheetEntry> _stylesheets = new();
    private readonly IBrowsingContext? _context;
    private IDocument? _document;

    public StyleSheetManager(IBrowsingContext? context = null, IDocument? document = null)
    {
        _context = context;
        _document = document;

        // Load default stylesheets if context is provided
        if (context != null)
        {
            LoadDefaultStylesheets();
        }

        // Load document stylesheets if document is provided
        if (document != null)
        {
            LoadDocumentStylesheets();
        }
    }

    private void LoadDefaultStylesheets()
    {
        if(_context == null) return;

        var defaultStyleSheetProviders = _context.GetServices<ICssDefaultStyleSheetProvider>();

        foreach (var provider in defaultStyleSheetProviders)
        {
            if (provider.Default != null)
            {
                RegisterStylesheet(provider.Default, StylesheetOrigin.UserAgent);
            }
        }
    }

    private void LoadDocumentStylesheets()
    {
        if (_document == null) return;

        var documentStylesheets = _document.GetStyleSheets().OfType<ICssStyleSheet>();
        foreach (var stylesheet in documentStylesheets)
        {
            RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        }
    }

    public void SetDocument(IDocument document)
    {
        // Clear existing document stylesheets
        _stylesheets.RemoveAll(e => e.Origin == StylesheetOrigin.Author);

        _document = document;

        LoadDocumentStylesheets();
    }

    public void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        _stylesheets.Add(new StylesheetEntry(stylesheet, origin));
    }

    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        _stylesheets.RemoveAll(e => e.Stylesheet == stylesheet);
    }

    public IEnumerable<StylesheetEntry> GetStylesheets()
    {
        return _stylesheets.OrderBy(e => e.Origin);
    }
}

public record StylesheetEntry(ICssStyleSheet Stylesheet, StylesheetOrigin Origin);

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