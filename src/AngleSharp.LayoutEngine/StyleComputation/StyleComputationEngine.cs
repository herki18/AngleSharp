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

        // Implementation will follow the computation flow diagram
        // 1. Get stylesheets
        var stylesheets = _stylesheetManager.GetStylesheets();
        // 2. Match selectors

        // 3. Resolve cascade
        // 4. Apply inheritance
        // 5. Compute values
        // Return computed style declaration

        return null!;
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
        if (_context == null) return;

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

/// <summary>
/// Handles matching CSS selectors against DOM elements.
/// </summary>
public class SelectorMatcher
{
    private readonly IRenderDevice _device;

    /// <summary>
    /// Creates a new SelectorMatcher with the specified device information.
    /// </summary>
    /// <param name="device">The render device used for media query evaluation.</param>
    public SelectorMatcher(IRenderDevice device = null!)
    {
        _device = device;
    }

    /// <summary>
    /// Finds all rules that match the specified element and returns them with their specificity.
    /// </summary>
    /// <param name="element">The element to match against.</param>
    /// <param name="stylesheets">The stylesheets to search for matching rules.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector (e.g., "::before", "::after").</param>
    /// <returns>A collection of matched rules with their specificity.</returns>
    public IEnumerable<MatchedRule> MatchRules(
        IElement element,
        IEnumerable<StylesheetEntry> stylesheets,
        string pseudoElement = null!)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var matches = new List<MatchedRule>();
        var index = 0; // Used for maintaining original order for rules with equal specificity

        // If we have a pseudo-element, try to get the virtual element
        IElement targetElement = element;
        if (!string.IsNullOrEmpty(pseudoElement))
        {
            // In AngleSharp, you can use element.Pseudo(name) to get a pseudo-element
            targetElement = element.Pseudo(pseudoElement.TrimStart(':'));

            // If no pseudo-element could be created, we won't have any matches
            if (targetElement == null)
                return Enumerable.Empty<MatchedRule>();
        }

        // For each stylesheet
        foreach (var entry in stylesheets)
        {
            if (entry.Stylesheet.IsDisabled)
                continue;

            // Check if the stylesheet applies to current media
            if (!MediaMatches(entry.Stylesheet.Media))
                continue;

            // Extract all style rules (including those in nested at-rules)
            var styleRules = GetAllStyleRules(entry.Stylesheet);

            // For each rule in the stylesheet
            foreach (var rule in styleRules)
            {
                // Try to match the rule against the target element
                // Pass null as scope (letting AngleSharp use the document element)
                if (rule.TryMatch(targetElement, null, out var specificity))
                {
                    matches.Add(new MatchedRule(rule, specificity, entry.Origin, index++));
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Determines if the current media context matches the media list.
    /// </summary>
    private bool MediaMatches(IMediaList mediaList)
    {
        // If no media specified or "all", then it matches all media
        if (mediaList.Length == 0 || mediaList.MediaText == "all" || string.IsNullOrEmpty(mediaList.MediaText))
            return true;

        // Enumerate through all media in the list
        // In real implementation, this would check against device capabilities
        foreach (var medium in mediaList)
        {
            // Check if the medium type is applicable
            if (medium.Type == "all" || medium.Type == "screen")
            {
                // Check media features if any
                // This is simplified - real implementation would evaluate each feature
                if (!medium.Features.Any() || EvaluateMediaFeatures(medium.Features))
                {
                    return !medium.IsInverse; // Return true unless this is a 'not' condition
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Evaluates media features against current device capabilities.
    /// </summary>
    private bool EvaluateMediaFeatures(IEnumerable<IMediaFeature> features)
    {
        // If we have no device information or no features, return true
        if (_device == null || !features.Any())
            return true;

        foreach (var feature in features)
        {
            // Check common media features
            switch (feature.Name.ToLowerInvariant())
            {
                case "width":
                    if (!MatchesExactValue(_device.ViewPortWidth, feature.Value))
                        return false;
                    break;

                case "min-width":
                    if (!MatchesMinValue(_device.ViewPortWidth, feature.Value))
                        return false;
                    break;

                case "max-width":
                    if (!MatchesMaxValue(_device.ViewPortWidth, feature.Value))
                        return false;
                    break;

                case "height":
                    if (!MatchesExactValue(_device.ViewPortHeight, feature.Value))
                        return false;
                    break;

                case "min-height":
                    if (!MatchesMinValue(_device.ViewPortHeight, feature.Value))
                        return false;
                    break;

                case "max-height":
                    if (!MatchesMaxValue(_device.ViewPortHeight, feature.Value))
                        return false;
                    break;

                case "orientation":
                    var isPortrait = _device.ViewPortHeight >= _device.ViewPortWidth;
                    if (feature.Value == "portrait" && !isPortrait)
                        return false;
                    if (feature.Value == "landscape" && isPortrait)
                        return false;
                    break;

                // Add more media feature checks as needed
                // e.g., resolution, color, aspect-ratio, etc.

                default:
                    // Unknown feature - assume it matches
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if device value matches exact feature value.
    /// </summary>
    private bool MatchesExactValue(int deviceValue, string featureValue)
    {
        if (int.TryParse(featureValue, out var value))
            return deviceValue == value;

        // Handle unit conversions (px, em, etc.) - simplified
        return true; // Placeholder
    }

    /// <summary>
    /// Checks if device value is at least the minimum feature value.
    /// </summary>
    private bool MatchesMinValue(int deviceValue, string featureValue)
    {
        if (int.TryParse(featureValue, out var value))
            return deviceValue >= value;

        // Handle unit conversions (px, em, etc.) - simplified
        return true; // Placeholder
    }

    /// <summary>
    /// Checks if device value is at most the maximum feature value.
    /// </summary>
    private bool MatchesMaxValue(int deviceValue, string featureValue)
    {
        if (int.TryParse(featureValue, out var value))
            return deviceValue <= value;

        // Handle unit conversions (px, em, etc.) - simplified
        return true; // Placeholder
    }

    /// <summary>
    /// Recursively gets all style rules from a stylesheet, including nested ones.
    /// </summary>
    private IEnumerable<ICssStyleRule> GetAllStyleRules(ICssStyleSheet stylesheet)
    {
        return GetAllStyleRulesFromRuleList(stylesheet.Rules);
    }

    /// <summary>
    /// Recursively processes a rule list to extract all style rules.
    /// </summary>
    private IEnumerable<ICssStyleRule> GetAllStyleRulesFromRuleList(ICssRuleList rules)
    {
        foreach (var rule in rules)
        {
            // If it's a style rule, return it
            if (rule is ICssStyleRule styleRule)
            {
                yield return styleRule;
            }
            // If it's a container rule (like @media, @supports), process its nested rules
            else if (rule is ICssGroupingRule groupingRule)
            {
                foreach (var nestedRule in GetAllStyleRulesFromRuleList(groupingRule.Rules))
                {
                    yield return nestedRule;
                }
            }
        }
    }
}

/// <summary>
/// Represents a CSS rule that matched an element, along with its specificity and origin.
/// </summary>
public class MatchedRule
{
    /// <summary>
    /// The CSS style rule that matched.
    /// </summary>
    public ICssStyleRule Rule { get; }

    /// <summary>
    /// The specificity of the match.
    /// </summary>
    public Priority Specificity { get; }

    /// <summary>
    /// The origin of the stylesheet containing the rule.
    /// </summary>
    public StylesheetOrigin Origin { get; }

    /// <summary>
    /// The original index for rules with equal specificity.
    /// </summary>
    public int OriginalIndex { get; }

    /// <summary>
    /// Creates a new instance of MatchedRule.
    /// </summary>
    public MatchedRule(ICssStyleRule rule, Priority specificity, StylesheetOrigin origin, int originalIndex)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Specificity = specificity;
        Origin = origin;
        OriginalIndex = originalIndex;
    }

    /// <summary>
    /// Whether this rule has an !important declaration for the specified property.
    /// </summary>
    public bool HasImportantFor(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;

        var property = Rule.Style.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return property != null && property.IsImportant;
    }
}