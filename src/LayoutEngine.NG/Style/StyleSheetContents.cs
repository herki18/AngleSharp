namespace LayoutEngine.NG.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Wraps an AngleSharp ICssStyleSheet and provides efficient rule indexing for style matching.
/// In BlinkNG, this represents the parsed and indexed contents of a stylesheet.
/// </summary>
internal class StyleSheetContents
{
    private readonly ICssStyleSheet _angleSharpStyleSheet;
    private readonly StylesheetOrigin _origin;
    private readonly RuleSet _ruleSet;
    private bool _isParsed = false;

    /// <summary>
    /// Creates a new StyleSheetContents wrapper.
    /// </summary>
    /// <param name="styleSheet">The AngleSharp stylesheet to wrap.</param>
    /// <param name="origin">The origin of this stylesheet in the cascade.</param>
    public StyleSheetContents(ICssStyleSheet styleSheet, StylesheetOrigin origin)
    {
        _angleSharpStyleSheet = styleSheet ?? throw new ArgumentNullException(nameof(styleSheet));
        _origin = origin;
        _ruleSet = new RuleSet();
    }

    /// <summary>
    /// Gets the wrapped AngleSharp stylesheet.
    /// </summary>
    public ICssStyleSheet StyleSheet => _angleSharpStyleSheet;

    /// <summary>
    /// Gets the origin of this stylesheet.
    /// </summary>
    public StylesheetOrigin Origin => _origin;

    /// <summary>
    /// Gets the rule set containing indexed rules for fast matching.
    /// </summary>
    public RuleSet RuleSet => _ruleSet;

    /// <summary>
    /// Gets whether this stylesheet is currently loading (for external sheets).
    /// </summary>
    public bool IsLoading => false; // TODO: Implement for external stylesheets

    /// <summary>
    /// Gets whether this stylesheet contains any media queries.
    /// </summary>
    public bool HasMediaQueries { get; private set; }

    /// <summary>
    /// Gets whether this stylesheet contains any @import rules.
    /// </summary>
    public bool HasImportRules { get; private set; }

    /// <summary>
    /// Gets the owner node (e.g., <style> or <link> element).
    /// </summary>
    public IElement? OwnerNode => _angleSharpStyleSheet.OwnerNode as IElement;

    /// <summary>
    /// Parses and indexes all rules in the stylesheet.
    /// Called lazily on first access or explicitly by StyleEngine.
    /// </summary>
    internal void ParseRules()
    {
        if (_isParsed)
            return;

        _isParsed = true;
        _ruleSet.Clear();

        // Parse all rules recursively
        ParseRuleList(_angleSharpStyleSheet.Rules, 0);
    }

    /// <summary>
    /// Recursively parses a list of CSS rules, handling nested rules.
    /// </summary>
    private int ParseRuleList(ICssRuleList rules, int startIndex)
    {
        var index = startIndex;

        foreach (var rule in rules)
        {
            switch (rule.Type)
            {
                case CssRuleType.Style:
                    if (rule is ICssStyleRule styleRule)
                    {
                        _ruleSet.AddRule(styleRule, index++, _origin);
                    }
                    break;

                case CssRuleType.Media:
                    HasMediaQueries = true;
                    if (rule is ICssMediaRule mediaRule)
                    {
                        // TODO: Store media query for evaluation
                        index = ParseRuleList(mediaRule.Rules, index);
                    }
                    break;

                case CssRuleType.Import:
                    HasImportRules = true;
                    // TODO: Handle @import rules
                    break;

                case CssRuleType.Supports:
                    if (rule is ICssSupportsRule supportsRule)
                    {
                        // TODO: Evaluate @supports condition
                        index = ParseRuleList(supportsRule.Rules, index);
                    }
                    break;

                // TODO: Handle other rule types (layer, container, etc.)
            }
        }

        return index;
    }

    /// <summary>
    /// Invalidates the parsed rules, forcing a re-parse on next access.
    /// Called when the stylesheet content changes.
    /// </summary>
    internal void InvalidateRules()
    {
        _isParsed = false;
        _ruleSet.Clear();
        HasMediaQueries = false;
        HasImportRules = false;
    }

    /// <summary>
    /// Checks if this stylesheet is disabled.
    /// </summary>
    public bool IsDisabled => _angleSharpStyleSheet.IsDisabled;

    /// <summary>
    /// Gets the media list for this stylesheet.
    /// </summary>
    public IMediaList? Media => _angleSharpStyleSheet.Media;
}