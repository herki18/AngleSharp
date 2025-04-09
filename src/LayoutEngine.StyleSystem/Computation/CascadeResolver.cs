namespace LayoutEngine.StyleSystem.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Contracts.StyleSystem;

/// <summary>
/// Resolves the CSS cascade by applying the proper order of style rules based on origin,
/// specificity, and declaration order as specified by the CSS specification.
/// </summary>
public class CascadeResolver : ICascadeResolver
{
    private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
    private readonly ICssParser _cssParser;

    /// <summary>
    /// Creates a new cascade resolver for CSS style rules.
    /// </summary>
    /// <param name="styleDeclarationFactory"></param>
    /// <param name="cssParser"></param>
    public CascadeResolver(ICssStyleDeclarationFactory styleDeclarationFactory, ICssParser cssParser)
    {
        _styleDeclarationFactory = styleDeclarationFactory;
        _cssParser = cssParser;
    }

    /// <summary>
    /// Resolves the cascade for all matched rules and returns the resulting style declaration.
    /// </summary>
    /// <param name="matchedRules">The rules that matched the element.</param>
    /// <param name="element">The element being styled.</param>
    /// <returns>The cascaded style for the element.</returns>
    public ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var resolvedStyle = _styleDeclarationFactory.Create();

        // Group rules by origin and importance
        var importantAuthorRules = new List<MatchedRule>();
        var importantUserRules = new List<MatchedRule>();
        var importantUserAgentRules = new List<MatchedRule>();
        var normalAuthorRules = new List<MatchedRule>();
        var normalUserRules = new List<MatchedRule>();
        var normalUserAgentRules = new List<MatchedRule>();

        // Categorize rules based on origin and importance
        foreach (var rule in matchedRules)
        {
            if (rule.Rule?.Style == null)
                continue;

            // Check for important properties
            bool hasImportantProps = false;
            bool hasNormalProps = false;

            foreach (var prop in rule.Rule.Style)
            {
                if (prop.IsImportant)
                    hasImportantProps = true;
                else
                    hasNormalProps = true;

                if (hasImportantProps && hasNormalProps)
                    break;
            }

            // Distribute rules based on origin and importance
            if (hasImportantProps)
            {
                switch (rule.Origin)
                {
                    case StyleSheetOrigin.Author:
                        importantAuthorRules.Add(rule);
                        break;
                    case StyleSheetOrigin.User:
                        importantUserRules.Add(rule);
                        break;
                    case StyleSheetOrigin.UserAgent:
                        importantUserAgentRules.Add(rule);
                        break;
                }
            }

            if (hasNormalProps)
            {
                switch (rule.Origin)
                {
                    case StyleSheetOrigin.Author:
                        normalAuthorRules.Add(rule);
                        break;
                    case StyleSheetOrigin.User:
                        normalUserRules.Add(rule);
                        break;
                    case StyleSheetOrigin.UserAgent:
                        normalUserAgentRules.Add(rule);
                        break;
                }
            }
        }

        // Create cascade order according to modern CSS cascade behavior:
        // Normal declarations (applied in order):
        // 1. User agent normal declarations
        // 2. User normal declarations
        // 3. Author normal declarations
        //
        // !important declarations (applied in order):
        // 4. User agent !important declarations
        // 5. User !important declarations
        // 6. Author !important declarations
        //
        // Within each category, rules are sorted by specificity (ascending)
        // and then by source order (ascending).
        var orderedRuleSets = new List<(IEnumerable<MatchedRule> Rules, bool Important)>
        {
            (SortRules(normalUserAgentRules), false),
            (SortRules(normalUserRules), false),
            (SortRules(normalAuthorRules), false),
            (SortRules(importantUserAgentRules), true),
            (SortRules(importantUserRules), true),
            (SortRules(importantAuthorRules), true)
        };

        // Apply rules in cascade order
        foreach (var (rules, isImportant) in orderedRuleSets)
        {
            foreach (var rule in rules)
            {
                ApplyRuleProperties(resolvedStyle, rule, isImportant);
            }
        }

        // Apply inline styles (highest specificity for non-important declarations)
        ApplyInlineStyles(element, resolvedStyle);

        return resolvedStyle;
    }

    /// <summary>
    /// Sorts rules by specificity (ascending) and then by original index (ascending).
    /// </summary>
    private IEnumerable<MatchedRule> SortRules(IEnumerable<MatchedRule> rules)
    {
        return rules.OrderBy(r => r.Specificity).ThenBy(r => r.OriginalIndex);
    }

    /// <summary>
    /// Applies the properties from a rule to the resolved style.
    /// </summary>
    private void ApplyRuleProperties(ICssStyleDeclaration resolvedStyle, MatchedRule rule, Boolean isImportant)
    {
        if (rule.Rule?.Style == null)
        {
            return;
        }

        foreach (var property in rule.Rule.Style)
        {
            // Only apply properties with matching importance flag
            if (property.IsImportant != isImportant)
                continue;

            // For non-important declarations, avoid overriding existing important declarations
            if (!isImportant && resolvedStyle is { } cssDecl)
            {
                var existingProp = cssDecl.GetProperty(property.Name);
                if (existingProp.IsImportant)
                {
                    continue;
                }
            }

            resolvedStyle.SetProperty(
                property.Name,
                property.Value,
                isImportant ? "important" : null);
        }
    }

    /// <summary>
    /// Applies inline styles from the element's style attribute.
    /// </summary>
    private void ApplyInlineStyles(IElement element, ICssStyleDeclaration resolvedStyle)
    {
        var styleAttr = element.GetAttribute("style");
        if (String.IsNullOrWhiteSpace(styleAttr))
        {
            return;
        }

        // Parse the inline style declaration
        var inlineStyle = _cssParser.ParseDeclaration(styleAttr);

        // Inline styles have highest specificity but can be overridden by !important rules
        foreach (var property in inlineStyle)
        {
            // Only override non-important properties
            if (resolvedStyle is { } cssDecl)
            {
                var existingProp = cssDecl.GetProperty(property.Name);
                if (existingProp.IsImportant && !property.IsImportant)
                {
                    continue;
                }
            }

            resolvedStyle.SetProperty(
                property.Name,
                property.Value,
                property.IsImportant ? "important" : null);
        }
    }
}