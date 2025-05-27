namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;

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
        IEventAggregator eventAggregator,
        bool loadUserAgentStylesheets = true)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _observer = new MutationObserver(MutationCallback);

        if (_context.Active != null)
        {
            AttachToDocument(_context.Active);
        }

        if(loadUserAgentStylesheets)
        {
            LoadUserAgentStylesheets();
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

        _eventAggregator.Publish(new StylesheetChangedEvent(stylesheet, StylesheetChangeType.Added));
    }

    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        if (stylesheet == null)
            throw new ArgumentNullException(nameof(stylesheet));

        _stylesheets.RemoveAll(e => e.Stylesheet == stylesheet);
        _originCache.Remove(stylesheet);

        _eventAggregator.Publish(new StylesheetChangedEvent(stylesheet, StylesheetChangeType.Removed));
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
        if (_originCache.TryGetValue(stylesheet, out var origin))
        {
            return origin;
        }

        return StylesheetOrigin.Author;
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
            collectedRules.Add(rule);

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

        ClearStylesheetsByOrigin(StylesheetOrigin.Author);
        LoadDocumentStylesheets(_currentDocument);

        _eventAggregator.Publish(new StylesheetsRefreshedEvent(_currentDocument));
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
            if (document.StyleSheets.All(sheet => sheet.OwnerNode != styleElement))
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
                if (mutation.Added != null)
                {
                    foreach (var node in mutation.Added)
                    {
                        if (IsStyleElement(node))
                        {
                            needsRefresh = true;
                            break;
                        }
                    }
                }

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

/// <summary>
/// Types of stylesheet changes.
/// </summary>
public enum StylesheetChangeType
{
    Added,
    Removed,
    Modified
}