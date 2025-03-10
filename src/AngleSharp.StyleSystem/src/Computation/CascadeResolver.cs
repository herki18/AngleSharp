namespace AngleSharp.StyleSystem.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Interfaces;
using Models;

public class CascadeResolver : ICascadeResolver
{
    private readonly IBrowsingContext _context;

    public CascadeResolver(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Resolves the CSS cascade for an element with the given matched rules,
    /// respecting the rules of the CSS cascade algorithm:
    /// 1. Origin and importance (user agent, user, author, !important)
    /// 2. Specificity
    /// 3. Source order
    /// </summary>
    public ICssStyleDeclaration ResolveCascade(
        IEnumerable<MatchedRule> matchedRules,
        IElement element)
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

        // First, classify rules by origin and importance
        foreach (var rule in matchedRules)
        {
            if (rule.Rule?.Style == null)
                continue;

            bool hasImportantProps = rule.Rule.Style.Any(p => p.IsImportant);

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

            // Rules with normal properties need to be processed separately
            if (rule.Rule.Style.Any(p => !p.IsImportant))
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

        // Create lists in application order (least to most important)
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

        // Apply inline styles with highest priority (if they exist)
        ApplyInlineStyles(element, resolvedStyle);

        return resolvedStyle;
    }

    private IEnumerable<MatchedRule> SortRules(IEnumerable<MatchedRule> rules)
    {
        // Sort by specificity and then by source order
        return rules.OrderBy(r => r.Specificity).ThenBy(r => r.OriginalIndex);
    }

    private void ApplyRuleProperties(CssStyleDeclaration resolvedStyle, MatchedRule rule, bool isImportant)
    {
        if (rule.Rule?.Style == null)
            return;

        foreach (var property in rule.Rule.Style)
        {
            // Skip properties with different importance than what we're currently processing
            if (property.IsImportant != isImportant)
                continue;

            // Don't override existing important properties with non-important ones
            if (!isImportant && resolvedStyle.GetProperty(property.Name)?.IsImportant == true)
                continue;

            resolvedStyle.SetProperty(property.Name, property.Value, isImportant ? "important" : null);
        }
    }

    private void ApplyInlineStyles(IElement element, CssStyleDeclaration resolvedStyle)
    {
        var styleAttr = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttr))
            return;

        var inlineSpecificity = Priority.Inline;  // Inline styles have the highest specificity
        var styleDeclaration = CssStyleDeclarationParser.Parse(_context, styleAttr);

        foreach (var property in styleDeclaration)
        {
            // Inline properties always override non-important properties,
            // but important properties from style sheets override inline properties
            // unless the inline property is also important
            bool canSetProperty = property.IsImportant ||
                                 !resolvedStyle.GetProperty(property.Name)?.IsImportant == true;

            if (canSetProperty)
            {
                resolvedStyle.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : null);
            }
        }
    }
}

public static class CssStyleDeclarationParser
{
    public static IEnumerable<ICssProperty> Parse(IBrowsingContext context, string cssText)
    {
        var parser = context.GetService<ICssParser>();
        var decl = parser?.ParseDeclaration(cssText);
        if (decl != null)
        {
            return decl;
        }
        return Enumerable.Empty<ICssProperty>();
    }
}