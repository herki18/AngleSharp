namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Collections.Generic;
using System.Linq;
using Css;
using Css.Dom;
using Dom;

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
        String? pseudoElement = null!)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var matches = new List<MatchedRule>();
        var index = 0; // Used for maintaining original order for rules with equal specificity

        // Get the target element (either the original or a pseudo-element)
        IElement targetElement = element;
        bool isPseudoElementQuery = !string.IsNullOrEmpty(pseudoElement);

        if (isPseudoElementQuery)
        {
            // Get the pseudo-element
            targetElement = element.Pseudo(pseudoElement!.TrimStart(':'));

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
                // For pseudo-element queries, only consider selectors with the requested pseudo-element
                if (isPseudoElementQuery)
                {
                    // Skip rules that don't target the requested pseudo-element
                    if (!rule.SelectorText.Contains(pseudoElement))
                        continue;
                }
                // For regular element queries, skip rules that target pseudo-elements
                else if (rule.SelectorText.Contains("::"))
                {
                    continue;
                }

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
        // Handle various unit formats (e.g., "800px", "800")
        var numericValue = ParseNumericValue(featureValue);
        if (numericValue.HasValue)
        {
            return deviceValue >= numericValue.Value;
        }
        return true; // If we can't parse, assume it matches
    }

    /// <summary>
    /// Checks if device value is at most the maximum feature value.
    /// </summary>
    private bool MatchesMaxValue(int deviceValue, string featureValue)
    {
        // Handle various unit formats (e.g., "500px", "500")
        var numericValue = ParseNumericValue(featureValue);
        if (numericValue.HasValue)
        {
            return deviceValue <= numericValue.Value;
        }
        return true; // If we can't parse, assume it matches
    }

    private int? ParseNumericValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Try to parse as is first
        if (int.TryParse(value, out int result))
            return result;

        // Try to extract numeric portion by removing non-digit characters
        var numericPart = new string(value.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (int.TryParse(numericPart, out result))
            return result;

        return null;
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
            // If it's a media rule, check if it applies before processing its nested rules
            else if (rule is ICssMediaRule mediaRule)
            {
                // Only process rules inside media queries that match the current device
                if (MediaMatches(mediaRule.Media))
                {
                    foreach (var nestedRule in GetAllStyleRulesFromRuleList(mediaRule.Rules))
                    {
                        yield return nestedRule;
                    }
                }
            }
            // If it's a container rule (like @media, @supports), process its nested rules
            else if (rule is ICssGroupingRule groupingRule && !(rule is ICssMediaRule))
            {
                foreach (var nestedRule in GetAllStyleRulesFromRuleList(groupingRule.Rules))
                {
                    yield return nestedRule;
                }
            }
        }
    }
}