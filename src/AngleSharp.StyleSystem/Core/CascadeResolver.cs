namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Interfaces;

/// <summary>
/// Resolves property conflicts based on CSS cascade rules.
/// </summary>
public class CascadeResolver : ICascadeResolver
{
    private readonly IBrowsingContext _context;

    public CascadeResolver(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Resolves the cascade by creating a style declaration with winning property values.
    /// </summary>
    /// <param name="matchedRules">Rules that matched the element, with specificity.</param>
    /// <param name="element">The element being styled.</param>
    /// <returns>A style declaration with resolved property values.</returns>
    public ICssStyleDeclaration ResolveCascade(
        IEnumerable<MatchedRule> matchedRules,
        IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Create a new style declaration to hold resolved properties
        var resolvedStyle = new CssStyleDeclaration(_context);

        // Group the matched rules by StylesheetOrigin for easier processing
        var rulesByOrigin = matchedRules
            .GroupBy(r => r.Origin)
            .OrderBy(g => g.Key)  // Order by origin: UserAgent < User < Author
            .ToList();

        // Dictionary to track if a property has been set with !important
        var importantProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process rules in order of increasing precedence (UserAgent, User, Author)
        foreach (var originGroup in rulesByOrigin)
        {
            // Sort rules within each origin by specificity and then original index
            var sortedRules = originGroup
                .OrderBy(r => r.Specificity)
                .ThenBy(r => r.OriginalIndex)
                .ToList();

            foreach (var rule in sortedRules)
            {
                if (rule.Rule?.Style == null)
                    continue;

                // Apply each property in the rule
                foreach (var property in rule.Rule.Style)
                {
                    bool isImportant = property.IsImportant;

                    // If this property has already been set with !important from a
                    // higher-precedence origin, skip it unless this is also !important
                    if (importantProperties.Contains(property.Name) && !isImportant)
                        continue;

                    // Update the property in the resolved style
                    resolvedStyle.SetProperty(property.Name, property.Value, isImportant ? "important" : null);

                    // If this is !important, mark it in our tracking set
                    if (isImportant)
                    {
                        importantProperties.Add(property.Name);
                    }
                }
            }
        }

        // Apply inline styles from the element's style attribute (highest precedence for non-important)
        ApplyInlineStyles(element, resolvedStyle, importantProperties);

        return resolvedStyle;
    }

    private void ApplyInlineStyles(IElement element, CssStyleDeclaration resolvedStyle, HashSet<string> importantProperties)
    {
        // Get the style attribute
        var styleAttr = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttr))
            return;

        // Parse the style attribute into a declaration
        var styleDeclaration = CssStyleDeclarationParser.Parse(_context, styleAttr);

        // Apply each property, respecting !important rules
        foreach (var property in styleDeclaration)
        {
            // Skip if this property has already been set with !important
            // from a stylesheet and this inline property is not !important
            if (importantProperties.Contains(property.Name) && !property.IsImportant)
                continue;

            resolvedStyle.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);

            if (property.IsImportant)
            {
                importantProperties.Add(property.Name);
            }
        }
    }
}

/// <summary>
/// Helper class to parse inline style declarations from style attributes.
/// </summary>
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