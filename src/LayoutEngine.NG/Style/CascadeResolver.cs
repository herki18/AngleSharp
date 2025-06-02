namespace LayoutEngine.NG.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;

/// <summary>
/// Resolves the CSS cascade by applying the proper order of style rules based on origin,
/// specificity, and declaration order as specified by the CSS specification.
/// </summary>
public class CascadeResolver : ICascadeResolver
{
    private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
    private readonly ICssParser _cssParser;

    public CascadeResolver(ICssStyleDeclarationFactory styleDeclarationFactory, ICssParser cssParser)
    {
        _styleDeclarationFactory = styleDeclarationFactory;
        _cssParser = cssParser;
    }

    public ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var resolvedStyle = _styleDeclarationFactory.Create();

        // Group rules by importance first
        var normalRules = new List<MatchedRule>();
        var importantRules = new List<MatchedRule>();

        foreach (var rule in matchedRules)
        {
            if (rule.Rule?.Style == null)
                continue;

            if (HasImportantProperties(rule))
                importantRules.Add(rule);
            if (HasNormalProperties(rule))
                normalRules.Add(rule);
        }

        // Apply normal declarations first (user agent -> user -> author)
        ApplyRulesByOriginAndSpecificity(normalRules, resolvedStyle, false);

        // Apply important declarations (author -> user -> user agent for !important)
        // Note: For !important, the cascade order is reversed
        ApplyImportantRulesByOrigin(importantRules, resolvedStyle);

        // Apply inline styles last (highest specificity for normal declarations)
        ApplyInlineStyles(element, resolvedStyle);

        return resolvedStyle;
    }

    private void ApplyRulesByOriginAndSpecificity(
        IEnumerable<MatchedRule> rules,
        ICssStyleDeclaration resolvedStyle,
        bool onlyImportant)
    {
        // Sort by: origin (user agent -> user -> author), then specificity, then document order
        var sortedRules = rules
            .OrderBy(GetOriginPriority)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.Position); // Use Position instead of OriginalIndex

        foreach (var rule in sortedRules)
        {
            ApplyRuleProperties(resolvedStyle, rule, onlyImportant);
        }
    }

    private void ApplyImportantRulesByOrigin(
        IEnumerable<MatchedRule> rules,
        ICssStyleDeclaration resolvedStyle)
    {
        // For !important declarations, the order is reversed:
        // author -> user -> user agent
        var sortedRules = rules
            .OrderBy(GetImportantOriginPriority)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.Position);

        foreach (var rule in sortedRules)
        {
            ApplyRuleProperties(resolvedStyle, rule, true);
        }
    }

    private int GetOriginPriority(MatchedRule rule)
    {
        return rule.Origin switch
        {
            StylesheetOrigin.UserAgent => 1,
            StylesheetOrigin.User => 2,
            StylesheetOrigin.Author => 3,
            _ => 3
        };
    }

    private int GetImportantOriginPriority(MatchedRule rule)
    {
        // For !important, author rules have highest priority
        return rule.Origin switch
        {
            StylesheetOrigin.Author => 1,
            StylesheetOrigin.User => 2,
            StylesheetOrigin.UserAgent => 3,
            _ => 1
        };
    }

    private bool HasImportantProperties(MatchedRule rule)
    {
        return rule.Rule?.Style?.Any(p => p.IsImportant) ?? false;
    }

    private bool HasNormalProperties(MatchedRule rule)
    {
        return rule.Rule?.Style?.Any(p => !p.IsImportant) ?? false;
    }

    private void ApplyRuleProperties(ICssStyleDeclaration resolvedStyle, MatchedRule rule, bool onlyImportant)
    {
        if (rule.Rule?.Style == null)
            return;

        foreach (var property in rule.Rule.Style)
        {
            // Only apply properties with matching importance flag
            if (property.IsImportant != onlyImportant)
                continue;

            var existingProperty = resolvedStyle.GetProperty(property.Name);

            // For important properties, always override
            // For normal properties, don't override existing important properties
            if (onlyImportant || existingProperty?.IsImportant != true)
            {
                resolvedStyle.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : null);
            }
        }
    }

    private void ApplyInlineStyles(IElement element, ICssStyleDeclaration resolvedStyle)
    {
        var styleAttr = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttr))
            return;

        var inlineStyle = _cssParser.ParseDeclaration(styleAttr);
        if (inlineStyle == null)
            return;

        // Inline styles have higher specificity than author styles
        // but can still be overridden by important declarations from any origin
        foreach (var property in inlineStyle)
        {
            var existingProperty = resolvedStyle.GetProperty(property.Name);

            // Inline normal properties can override existing normal properties
            // Inline important properties can override any existing property
            if (property.IsImportant || existingProperty?.IsImportant != true)
            {
                resolvedStyle.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : null);
            }
        }
    }
}