namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using System.Linq;
using AngleSharp.Css.Dom;

/// <summary>
/// Analyzes stylesheet changes to determine which elements need style recalculation.
/// In BlinkNG, this is critical for performance when stylesheets are modified.
/// </summary>
internal class StyleInvalidationAnalyzer
{
    /// <summary>
    /// Analyzes the difference between two stylesheet versions and creates invalidation sets.
    /// In BlinkNG: StyleInvalidationAnalyzer::Analyze
    /// </summary>
    internal StyleInvalidationSet AnalyzeStyleSheetChange(
        StyleSheetContents oldContents,
        StyleSheetContents newContents)
    {
        var invalidationSet = new StyleInvalidationSet();

        // If either is null, invalidate everything
        if (oldContents == null || newContents == null)
        {
            invalidationSet.InvalidatesDescendants = true;
            return invalidationSet;
        }

        // Parse rules if needed
        oldContents.ParseRules();
        newContents.ParseRules();

        // Compare rules
        var oldRules = oldContents.RuleSet.AllRules;
        var newRules = newContents.RuleSet.AllRules;

        // If rule count changed significantly, invalidate all
        if (System.Math.Abs(oldRules.Count - newRules.Count) > 10)
        {
            invalidationSet.InvalidatesDescendants = true;
            return invalidationSet;
        }

        // Analyze each new rule
        foreach (var newRule in newRules)
        {
            // Check if this rule existed in old stylesheet
            var matchingOldRule = oldRules.FirstOrDefault(r =>
                r.Rule.SelectorText == newRule.Rule.SelectorText);

            if (matchingOldRule == null)
            {
                // New rule added - analyze its selector
                AnalyzeSelector(newRule.Rule.SelectorText, invalidationSet);
            }
            else if (!AreDeclarationsEqual(matchingOldRule.Rule.Style, newRule.Rule.Style))
            {
                // Rule modified - analyze its selector
                AnalyzeSelector(newRule.Rule.SelectorText, invalidationSet);
            }
        }

        // Check for removed rules
        foreach (var oldRule in oldRules)
        {
            var stillExists = newRules.Any(r =>
                r.Rule.SelectorText == oldRule.Rule.SelectorText);

            if (!stillExists)
            {
                // Rule removed - analyze its selector
                AnalyzeSelector(oldRule.Rule.SelectorText, invalidationSet);
            }
        }

        return invalidationSet;
    }

    /// <summary>
    /// Invalidates style for elements matching the invalidation set.
    /// In BlinkNG: StyleInvalidator::Invalidate
    /// </summary>
    internal void InvalidateStyle(StyleInvalidationSet invalidationSet, IElement root)
    {
        if (invalidationSet == null || root == null)
            return;

        // If invalidating all descendants, mark entire subtree
        if (invalidationSet.InvalidatesDescendants)
        {
            InvalidateSubtree(root);
            return;
        }

        // Otherwise, traverse and check each element
        InvalidateElement(root, invalidationSet);
        InvalidateDescendants(root, invalidationSet);
    }

    /// <summary>
    /// Analyzes a selector to determine what needs invalidation.
    /// </summary>
    private void AnalyzeSelector(string selectorText, StyleInvalidationSet invalidationSet)
    {
        if (string.IsNullOrWhiteSpace(selectorText))
            return;

        // Simplified selector analysis
        // In real BlinkNG, this would parse the selector properly

        // Check for ID selectors
        if (selectorText.Contains("#"))
        {
            var idMatch = System.Text.RegularExpressions.Regex.Match(
                selectorText, @"#([\w-]+)");
            if (idMatch.Success)
            {
                invalidationSet.Ids.Add(idMatch.Groups[1].Value);
            }
        }

        // Check for class selectors
        if (selectorText.Contains("."))
        {
            var classMatches = System.Text.RegularExpressions.Regex.Matches(
                selectorText, @"\.([\w-]+)");
            foreach (System.Text.RegularExpressions.Match match in classMatches)
            {
                invalidationSet.Classes.Add(match.Groups[1].Value);
            }
        }

        // Check for tag selectors
        var tagMatch = System.Text.RegularExpressions.Regex.Match(
            selectorText, @"^([\w-]+)");
        if (tagMatch.Success && !tagMatch.Value.StartsWith(".") && !tagMatch.Value.StartsWith("#"))
        {
            invalidationSet.TagNames.Add(tagMatch.Value.ToLowerInvariant());
        }

        // Check for attribute selectors
        if (selectorText.Contains("["))
        {
            var attrMatches = System.Text.RegularExpressions.Regex.Matches(
                selectorText, @"\[([\w-]+)");
            foreach (System.Text.RegularExpressions.Match match in attrMatches)
            {
                invalidationSet.Attributes.Add(match.Groups[1].Value);
            }
        }

        // Check for descendant/child combinators
        if (selectorText.Contains(" ") || selectorText.Contains(">"))
        {
            invalidationSet.InvalidatesDescendants = true;
        }

        // Check for sibling combinators
        if (selectorText.Contains("+") || selectorText.Contains("~"))
        {
            invalidationSet.InvalidatesSiblings = true;
        }
    }

    /// <summary>
    /// Checks if two style declarations are equal.
    /// </summary>
    private bool AreDeclarationsEqual(ICssStyleDeclaration? style1, ICssStyleDeclaration? style2)
    {
        if (style1 == null && style2 == null)
            return true;

        if (style1 == null || style2 == null)
            return false;

        // Compare CSS text
        // In real BlinkNG, this would compare individual properties
        return style1.CssText == style2.CssText;
    }

    /// <summary>
    /// Invalidates an entire subtree.
    /// </summary>
    private void InvalidateSubtree(IElement element)
    {
        // Mark element for style recalc
        // Note: We can't directly access ElementEngineData here,
        // so this would need to be done through the style engine
        // For now, this is a placeholder

        // Recurse to children
        foreach (var child in element.Children)
        {
            InvalidateSubtree(child);
        }
    }

    /// <summary>
    /// Invalidates a single element if it matches the invalidation set.
    /// </summary>
    private void InvalidateElement(IElement element, StyleInvalidationSet invalidationSet)
    {
        bool shouldInvalidate = false;

        // Check ID
        if (!string.IsNullOrEmpty(element.Id) && invalidationSet.Ids.Contains(element.Id))
        {
            shouldInvalidate = true;
        }

        // Check classes
        foreach (var className in element.ClassList)
        {
            if (invalidationSet.Classes.Contains(className))
            {
                shouldInvalidate = true;
                break;
            }
        }

        // Check tag name
        if (invalidationSet.TagNames.Contains(element.LocalName.ToLowerInvariant()))
        {
            shouldInvalidate = true;
        }

        // Check attributes
        foreach (var attr in invalidationSet.Attributes)
        {
            if (element.HasAttribute(attr))
            {
                shouldInvalidate = true;
                break;
            }
        }

        if (shouldInvalidate || invalidationSet.InvalidatesSelf)
        {
            // Mark for style recalc
            // Note: This would need to be done through the style engine
        }
    }

    /// <summary>
    /// Invalidates descendants of an element.
    /// </summary>
    private void InvalidateDescendants(IElement element, StyleInvalidationSet invalidationSet)
    {
        foreach (var child in element.Children)
        {
            InvalidateElement(child, invalidationSet);
            InvalidateDescendants(child, invalidationSet);
        }
    }
}