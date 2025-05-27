namespace LayoutEngine.Core.Style.Public;

using AngleSharp.Css.Dom;

/// <summary>
/// A single matched CSS rule with metadata
/// </summary>
public interface IMatchedRule
{
    ICssRule Rule { get; } // AngleSharp type
    int Specificity { get; }
    int DocumentOrder { get; }
}