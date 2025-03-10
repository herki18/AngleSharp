namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Models;

/// <summary>
/// Manages stylesheets from different origins and provides centralized access to them.
/// </summary>
public class StyleSheetManager : IDisposable
{
    private readonly List<StylesheetEntry> _stylesheets = new();
    private readonly IBrowsingContext _context;
    private readonly Dictionary<ICssStyleSheet, StylesheetOrigin> _originCache = new();
    private IDocument? _currentDocument;
    private MutationObserver? _observer;

    /// <summary>
    /// Event raised when stylesheets are added, removed, or modified.
    /// </summary>
    public event EventHandler<StylesheetChangedEventArgs>? StylesheetChanged;

    /// <summary>
    /// Creates a new StyleSheetManager instance.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <param name="loadUserAgentStylesheets"></param>
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

    /// <summary>
    /// Attaches to a document and loads its stylesheets.
    /// </summary>
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

    /// <summary>
    /// Detaches from the current document.
    /// </summary>
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

    /// <summary>
    /// Registers a stylesheet with the manager.
    /// </summary>
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

    /// <summary>
    /// Unregisters a stylesheet from the manager.
    /// </summary>
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

    /// <summary>
    /// Gets all registered stylesheets in priority order.
    /// </summary>
    public IEnumerable<StylesheetEntry> GetStylesheets()
    {
        // Return in cascade order: user agent, user, author
        return _stylesheets.OrderBy(e => e.Origin);
    }

    /// <summary>
    /// Gets stylesheets from a specific origin.
    /// </summary>
    public IEnumerable<ICssStyleSheet> GetStylesheetsByOrigin(StylesheetOrigin origin)
    {
        return _stylesheets
            .Where(e => e.Origin == origin)
            .Select(e => e.Stylesheet);
    }

    /// <summary>
    /// Gets the origin of a stylesheet.
    /// </summary>
    public StylesheetOrigin GetStylesheetOrigin(ICssStyleSheet stylesheet)
    {
        if (_originCache.TryGetValue(stylesheet, out var origin))
        {
            return origin;
        }

        return StylesheetOrigin.Author; // Default
    }

    /// <summary>
    /// Gets all rules from all stylesheets in cascade order.
    /// </summary>
    public IEnumerable<ICssRule> GetAllRules()
    {
        var rules = new List<ICssRule>();

        foreach (var entry in GetStylesheets())
        {
            CollectRulesRecursively(entry.Stylesheet.Rules, rules);
        }

        return rules;
    }

    /// <summary>
    /// Gets all style rules from all stylesheets in cascade order.
    /// </summary>
    public IEnumerable<ICssStyleRule> GetAllStyleRules()
    {
        return GetAllRules().OfType<ICssStyleRule>();
    }

    /// <summary>
    /// Recursively collects all rules, including those nested in container rules.
    /// </summary>
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

    /// <summary>
    /// Checks for changes in the document's stylesheets and updates if needed.
    /// </summary>
    public void RefreshDocumentStylesheets()
    {
        if (_currentDocument == null)
            return;
        // Clear existing author stylesheets
        ClearStylesheetsByOrigin(StylesheetOrigin.Author);

        // Re-load all stylesheets from the document
        LoadDocumentStylesheets(_currentDocument);
    }

    /// <summary>
    /// Disposes the StyleSheetManager.
    /// </summary>
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

    protected virtual void OnStylesheetChanged(StylesheetChangedEventArgs e)
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