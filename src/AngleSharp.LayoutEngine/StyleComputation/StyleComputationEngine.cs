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

        throw new NotImplementedException();
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