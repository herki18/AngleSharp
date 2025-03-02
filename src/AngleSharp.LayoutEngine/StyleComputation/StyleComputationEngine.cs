namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Collections.Generic;
using System.Linq;
using Css;
using Css.Dom;
using Dom;

public class StyleComputationEngine
{
    public ICssStyleDeclaration ComputeElementStyle(IElement element,
        ICssStyleDeclaration? parentStyle = null,
        string? pseudoElement = null)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var window = element.OwnerDocument?.DefaultView;
        if (window is null)
            throw new InvalidOperationException("Element must be part of a document with a default view");

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
    private readonly IBrowsingContext _context;

    public StyleSheetManager(IBrowsingContext context)
    {
        _context = context;

        var defaultStyleSheetProviders = _context.GetServices<ICssDefaultStyleSheetProvider>();
        foreach (var provider in defaultStyleSheetProviders)
        {
            if (provider.Default is not null)
            {
                RegisterStylesheet(provider.Default, StylesheetOrigin.UserAgent);
            }
        }
    }

    public void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        if(stylesheet is null) throw new ArgumentNullException(nameof(stylesheet));
        _stylesheets.Add(new StylesheetEntry(stylesheet, origin));
    }

    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        _stylesheets.RemoveAll(e => e.Stylesheet == stylesheet);
    }

    private class StylesheetEntry
    {
        public ICssStyleSheet Stylesheet { get; }
        public StylesheetOrigin Origin { get; }

        public StylesheetEntry(ICssStyleSheet stylesheet, StylesheetOrigin origin)
        {
            Stylesheet = stylesheet;
            Origin = origin;
        }
    }
}

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