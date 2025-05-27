using AngleSharp.Css.Dom;

/// <summary>
/// Resolves CSS cascade conflicts - mirrors Blink's cascade logic
/// </summary>
public interface ICascadeResolver
{
    /// <summary>
    /// Resolve cascade for all properties
    /// Input: Matched rules from ElementRuleCollector
    /// Output: Winning value for each CSS property
    /// </summary>
    ICascadeResult ResolveCascade(IMatchResult matchResult);
}

/// <summary>
/// Result of cascade resolution
/// </summary>
public interface ICascadeResult
{
    /// <summary>
    /// Get the winning value for a CSS property after cascade resolution
    /// Returns: ICssValue (AngleSharp type) or null if not set
    /// </summary>
    ICssValue? GetPropertyValue(string propertyName);

    /// <summary>
    /// Check if property has !important declaration
    /// </summary>
    bool IsImportant(string propertyName);
}