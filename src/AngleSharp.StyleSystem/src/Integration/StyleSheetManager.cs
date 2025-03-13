namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Interfaces;
using Models;

public sealed class StyleSheetManager : IStyleSheetManager
{
    private readonly List<StylesheetEntry> _stylesheets = new();
    private readonly IBrowsingContext _context;
    private readonly Dictionary<ICssStyleSheet, StylesheetOrigin> _originCache = new();
    private IDocument? _currentDocument;
    private MutationObserver? _observer;

    public event EventHandler<StylesheetChangedEventArgs>? StylesheetChanged;

    public StyleSheetManager(IBrowsingContext context, bool loadUserAgentStylesheets = true)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Create the mutation observer
        _observer = new MutationObserver(MutationCallback);

        // Register for document change events
        if (_context.Active != null)
        {
            AttachToDocument(_context.Active);
        }

        if(loadUserAgentStylesheets)
        {
            // Load user agent stylesheets
            LoadUserAgentStylesheets();
        }
    }

    public void AttachToDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        // Detach from current document if exists
        if (_currentDocument != null)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;

        // Clear previous document stylesheets
        ClearStylesheetsByOrigin(StylesheetOrigin.Author);

        // Load all stylesheets from the document
        LoadDocumentStylesheets(document);

        // Setup mutation observer for the document
        SetupMutationObserver(document);
    }

    public void DetachFromDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        // Disconnect mutation observer
        _observer?.Disconnect();

        // Clear document stylesheets
        ClearStylesheetsByOrigin(StylesheetOrigin.Author);

        if (_currentDocument == document)
        {
            _currentDocument = null;
        }
    }

    public void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        // Don't add duplicates
        if (_originCache.ContainsKey(stylesheet))
            return;

        var entry = new StylesheetEntry(stylesheet, origin);
        _stylesheets.Add(entry);
        _originCache[stylesheet] = origin;

        // Notify listeners
        OnStylesheetChanged(new StylesheetChangedEventArgs(stylesheet, StylesheetChangeType.Added));
    }

    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        // Remove from collections
        _stylesheets.RemoveAll(e => e.Stylesheet == stylesheet);
        _originCache.Remove(stylesheet);

        // Notify listeners
        OnStylesheetChanged(new StylesheetChangedEventArgs(stylesheet, StylesheetChangeType.Removed));
    }

    public IEnumerable<StylesheetEntry> GetStylesheets()
    {
        // Return in cascade order: user agent, user, author
        return _stylesheets.OrderBy(e => e.Origin);
    }

    public IEnumerable<ICssStyleSheet> GetStylesheetsByOrigin(StylesheetOrigin origin)
    {
        return _stylesheets
            .Where(e => e.Origin == origin)
            .Select(e => e.Stylesheet);
    }

    public StylesheetOrigin GetStylesheetOrigin(ICssStyleSheet stylesheet)
    {
        if (_originCache.TryGetValue(stylesheet, out var origin))
        {
            return origin;
        }

        return StylesheetOrigin.Author; // Default
    }

    public IEnumerable<ICssRule> GetAllRules()
    {
        var rules = new List<ICssRule>();

        foreach (var entry in GetStylesheets())
        {
            CollectRulesRecursively(entry.Stylesheet.Rules, rules);
        }

        return rules;
    }

    public IEnumerable<ICssStyleRule> GetAllStyleRules()
    {
        return GetAllRules().OfType<ICssStyleRule>();
    }

    private void CollectRulesRecursively(IEnumerable<ICssRule> rules, List<ICssRule> collectedRules)
    {
        foreach (var rule in rules)
        {
            // Add the current rule
            collectedRules.Add(rule);

            // If this is a container rule (like @media or @supports), collect its nested rules
            if (rule is ICssGroupingRule groupingRule)
            {
                CollectRulesRecursively(groupingRule.Rules, collectedRules);
            }
        }
    }

    public void RefreshDocumentStylesheets()
    {
        if (_currentDocument == null)
            return;
        // Clear existing author stylesheets
        ClearStylesheetsByOrigin(StylesheetOrigin.Author);

        // Re-load all stylesheets from the document
        LoadDocumentStylesheets(_currentDocument);
    }

    public void Dispose()
    {
        _observer?.Disconnect();
        _observer = null;
    }

    private void LoadUserAgentStylesheets()
    {
        var defaultStyleSheetProviders = _context.GetServices<ICssDefaultStyleSheetProvider>();

        foreach (var provider in defaultStyleSheetProviders)
        {
            if (provider.Default != null)
            {
                RegisterStylesheet(provider.Default, StylesheetOrigin.UserAgent);
            }
        }
    }

    private void LoadDocumentStylesheets(IDocument document)
    {
        foreach (var stylesheet in document.StyleSheets.OfType<ICssStyleSheet>())
        {
            RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        }

        foreach (var styleElement in document.QuerySelectorAll("style"))
        {
            // Only process style elements that don't already have associated stylesheets
            if (document.StyleSheets.All(sheet => sheet.OwnerNode != styleElement))
            {
                // Force AngleSharp to process this style element
                var cssParser = _context.GetService<ICssParser>();
                if (cssParser != null && !string.IsNullOrWhiteSpace(styleElement.TextContent))
                {
                    var sheet = cssParser.ParseStyleSheet(styleElement.TextContent);
                    sheet.SetOwner(styleElement);
                    RegisterStylesheet(sheet, StylesheetOrigin.Author);
                }
            }
        }
    }

    private void ClearStylesheetsByOrigin(StylesheetOrigin origin)
    {
        var sheetsToRemove = _stylesheets
            .Where(e => e.Origin == origin)
            .Select(e => e.Stylesheet)
            .ToList();

        foreach (var sheet in sheetsToRemove)
        {
            UnregisterStylesheet(sheet);
        }
    }

    private void SetupMutationObserver(IDocument document)
    {
        if (_observer == null)
            return;

        // Watch for changes in the head
        if (document.Head != null)
        {
            _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
        }

        // Also watch for changes in the body (in case styles are added there)
        if (document.Body != null)
        {
            _observer.Connect(document.Body, childList: true, subtree: true, attributes: true);
        }
    }

    private void MutationCallback(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
    {
        bool needsRefresh = false;

        foreach (var mutation in mutations)
        {
            // Check for added/removed style/link elements
            if (mutation.Type == "childList")
            {
                if (mutation.Added != null)
                {
                    // Check added nodes
                    foreach (var node in mutation.Added)
                    {
                        if (IsStyleElement(node))
                        {
                            needsRefresh = true;
                            break;
                        }
                    }
                }

                // Check removed nodes
                if (!needsRefresh && mutation.Removed != null)
                {
                    foreach (var node in mutation.Removed)
                    {
                        if (IsStyleElement(node))
                        {
                            needsRefresh = true;
                            break;
                        }
                    }
                }
            }

            // Check for attribute changes on link elements that might affect stylesheets
            else if (mutation.Type == "attributes" &&
                     IsLinkElement(mutation.Target) &&
                     (mutation.AttributeName == "href" || mutation.AttributeName == "rel"))
            {
                needsRefresh = true;
            }

            if (needsRefresh)
                break;
        }

        if (needsRefresh)
        {
            RefreshDocumentStylesheets();
        }
    }

    private bool IsStyleElement(INode node)
    {
        return node is IElement element &&
            (element.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
             IsLinkElement(element));
    }

    private bool IsLinkElement(INode node)
    {
        if (node is IElement element && element.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase))
        {
            var rel = element.GetAttribute("rel");
            return rel != null && rel.Contains("stylesheet", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private void OnStylesheetChanged(StylesheetChangedEventArgs e)
    {
        StylesheetChanged?.Invoke(this, e);
    }
}

/// <summary>
/// Represents a stylesheet with its origin.
/// </summary>
public class StylesheetEntry
{
    /// <summary>
    /// Gets the stylesheet.
    /// </summary>
    public ICssStyleSheet Stylesheet { get; }

    /// <summary>
    /// Gets the origin of the stylesheet.
    /// </summary>
    public StylesheetOrigin Origin { get; }

    /// <summary>
    /// Creates a new StylesheetEntry.
    /// </summary>
    public StylesheetEntry(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        Stylesheet = stylesheet ?? throw new ArgumentNullException(nameof(stylesheet));
        Origin = origin;
    }
}

/// <summary>
/// Arguments for the StylesheetChanged event.
/// </summary>
public class StylesheetChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the stylesheet that changed.
    /// </summary>
    public ICssStyleSheet Stylesheet { get; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public StylesheetChangeType ChangeType { get; }

    /// <summary>
    /// Creates new StylesheetChangedEventArgs.
    /// </summary>
    public StylesheetChangedEventArgs(ICssStyleSheet stylesheet, StylesheetChangeType changeType)
    {
        Stylesheet = stylesheet;
        ChangeType = changeType;
    }
}

/// <summary>
/// Types of stylesheet changes.
/// </summary>
public enum StylesheetChangeType
{
    Added,
    Removed,
    Modified
}