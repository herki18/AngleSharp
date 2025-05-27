namespace LayoutEngine.Core.Style.Public;

using AngleSharp.Dom;

/// <summary>
/// Collects CSS rules that match an element - mirrors Blink's ElementRuleCollector
/// </summary>
public interface IElementRuleCollector
{
    /// <summary>
    /// Collect all CSS rules that match the given element
    /// Returns: List of ICssRule (AngleSharp type) with specificity
    /// </summary>
    IMatchResult CollectMatchingRules(IElement element, IStyleRecalcContext context);
}