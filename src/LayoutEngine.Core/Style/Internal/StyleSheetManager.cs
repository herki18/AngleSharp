namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Style.Public;

public sealed class StyleSheetManager : IStyleSheetManager
{
    private readonly List<StylesheetEntry> _stylesheets = new();
    private readonly IBrowsingContext _context;
    private readonly Dictionary<ICssStyleSheet, StylesheetOrigin> _originCache = new();
    private readonly IEventAggregator _eventAggregator;
    private IDocument? _currentDocument;
    private MutationObserver? _observer;

    public StyleSheetManager(
        IBrowsingContext context,
        IEventAggregator eventAggregator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _observer = new MutationObserver(MutationCallback);

        // AngleSharp automatically provides user agent stylesheets
        // We just need to register them properly
        LoadAngleSharpUserAgentStylesheets();

        if (_context.Active != null)
        {
            AttachToDocument(_context.Active);
        }
    }

    public void AttachToDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (_currentDocument != null)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;
        ClearStylesheetsByOrigin(StylesheetOrigin.Author);
        LoadDocumentStylesheets(document);
        SetupMutationObserver(document);
    }

    public void DetachFromDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        _observer?.Disconnect();
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

        if (_originCache.ContainsKey(stylesheet))
            return;

        var entry = new StylesheetEntry(stylesheet, origin);
        _stylesheets.Add(entry);
        _originCache[stylesheet] = origin;

        _eventAggregator.Publish(new StylesheetChangedEvent(stylesheet, StyleSheetChangeType.Added));
    }

    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        _stylesheets.RemoveAll(e => e.Stylesheet == stylesheet);
        _originCache.Remove(stylesheet);

        _eventAggregator.Publish(new StylesheetChangedEvent(stylesheet, StyleSheetChangeType.Removed));
    }

    public IEnumerable<StylesheetEntry> GetStylesheets()
    {
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
        return _originCache.TryGetValue(stylesheet, out var origin) ? origin : StylesheetOrigin.Author;
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

    public void RefreshDocumentStylesheets()
    {
        if (_currentDocument == null)
            return;

        ClearStylesheetsByOrigin(StylesheetOrigin.Author);
        LoadDocumentStylesheets(_currentDocument);
        _eventAggregator.Publish(new StylesheetsRefreshedEvent(_currentDocument));
    }

    public void Dispose()
    {
        _observer?.Disconnect();
        _observer = null;
    }

    private void LoadAngleSharpUserAgentStylesheets()
    {
        // Get AngleSharp's built-in user agent stylesheets
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
        // Load document's stylesheets
        foreach (var stylesheet in document.StyleSheets.OfType<ICssStyleSheet>())
        {
            RegisterStylesheet(stylesheet, StylesheetOrigin.Author);
        }

        // Load inline <style> elements
        foreach (var styleElement in document.QuerySelectorAll("style"))
        {
            if (!document.StyleSheets.Any(sheet => sheet.OwnerNode == styleElement))
            {
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

    private void CollectRulesRecursively(IEnumerable<ICssRule> rules, List<ICssRule> collectedRules)
    {
        foreach (var rule in rules)
        {
            collectedRules.Add(rule);
            if (rule is ICssGroupingRule groupingRule)
            {
                CollectRulesRecursively(groupingRule.Rules, collectedRules);
            }
        }
    }

    private void SetupMutationObserver(IDocument document)
    {
        if (_observer == null)
            return;

        if (document.Head != null)
        {
            _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
        }

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
            if (mutation.Type == "childList")
            {
                if (mutation.Added?.Any(IsStyleElement) == true ||
                    mutation.Removed?.Any(IsStyleElement) == true)
                {
                    needsRefresh = true;
                    break;
                }
            }
            else if (mutation.Type == "attributes" &&
                     IsLinkElement(mutation.Target) &&
                     (mutation.AttributeName == "href" || mutation.AttributeName == "rel"))
            {
                needsRefresh = true;
                break;
            }
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
            return rel?.Contains("stylesheet", StringComparison.OrdinalIgnoreCase) == true;
        }
        return false;
    }
}

public class StylesheetEntry
{
    public ICssStyleSheet Stylesheet { get; }
    public StylesheetOrigin Origin { get; }

    public StylesheetEntry(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        Stylesheet = stylesheet ?? throw new ArgumentNullException(nameof(stylesheet));
        Origin = origin;
    }
}