namespace AngleSharp.StyleSystem.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Interfaces;
using Css.Parser;

/// <summary>
/// Resolves the CSS cascade by applying the proper order of style rules based on origin,
/// specificity, and declaration order as specified by the CSS specification.
/// </summary>
public class CascadeResolver : ICascadeResolver
{
    private readonly IBrowsingContext _context;

    /// <summary>
    /// Creates a new cascade resolver for CSS style rules.
    /// </summary>
    /// <param name="context">The browsing context to operate with.</param>
    public CascadeResolver(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

        var resolvedStyle = new CssStyleDeclaration(_context);

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
                    case StylesheetOrigin.Author:
                        importantAuthorRules.Add(rule);
                        break;
                    case StylesheetOrigin.User:
                        importantUserRules.Add(rule);
                        break;
                    case StylesheetOrigin.UserAgent:
                        importantUserAgentRules.Add(rule);
                        break;
                }
            }

            if (hasNormalProps)
            {
                switch (rule.Origin)
                {
                    case StylesheetOrigin.Author:
                        normalAuthorRules.Add(rule);
                        break;
                    case StylesheetOrigin.User:
                        normalUserRules.Add(rule);
                        break;
                    case StylesheetOrigin.UserAgent:
                        normalUserAgentRules.Add(rule);
                        break;
                }
            }
        }

        // Create cascade order according to CSS specification:
        // 1. User agent normal declarations
        // 2. User normal declarations
        // 3. Author normal declarations
        // 4. Author !important declarations
        // 5. User !important declarations
        // 6. User agent !important declarations
        //
        // Within each category, sort by specificity and then by source order
        var orderedRuleSets = new List<(IEnumerable<MatchedRule> Rules, bool Important)>
        {
            (SortRules(normalUserAgentRules), false),
            (SortRules(normalUserRules), false),
            (SortRules(normalAuthorRules), false),
            (SortRules(importantAuthorRules), true),
            (SortRules(importantUserRules), true),
            (SortRules(importantUserAgentRules), true)
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
    private void ApplyRuleProperties(ICssStyleDeclaration resolvedStyle, MatchedRule rule, bool isImportant)
    {
        if (rule.Rule?.Style == null)
            return;

        foreach (var property in rule.Rule.Style)
        {
            // Only apply properties with matching importance flag
            if (property.IsImportant != isImportant)
                continue;

            // For non-important declarations, avoid overriding existing important declarations
            if (!isImportant && resolvedStyle is CssStyleDeclaration cssDecl)
            {
                var existingProp = cssDecl.GetProperty(property.Name);
                if (existingProp != null && existingProp.IsImportant)
                    continue;
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
        if (string.IsNullOrWhiteSpace(styleAttr))
            return;

        // Get an ICssParser from the context
        var cssParser = _context.GetService<ICssParser>();
        if (cssParser == null)
            return;

        // Parse the inline style declaration
        var inlineStyle = cssParser.ParseDeclaration(styleAttr);
        if (inlineStyle == null)
            return;

        // Inline styles have highest specificity but can be overridden by !important rules
        foreach (var property in inlineStyle)
        {
            // Only override non-important properties
            if (resolvedStyle is CssStyleDeclaration cssDecl)
            {
                var existingProp = cssDecl.GetProperty(property.Name);
                if (existingProp != null && existingProp.IsImportant && !property.IsImportant)
                    continue;
            }

            resolvedStyle.SetProperty(
                property.Name,
                property.Value,
                property.IsImportant ? "important" : null);
        }
    }
}