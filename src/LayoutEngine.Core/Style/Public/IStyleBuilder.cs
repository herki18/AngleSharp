using AngleSharp.Css.Dom;
using AngleSharp.Dom;

namespace LayoutEngine.Core.Style.Public;

/// <summary>
/// Builds final computed style - mirrors Blink's StyleBuilder
/// </summary>
public interface IStyleBuilder
{
    /// <summary>
    /// Apply matched CSS rules to build computed style
    /// </summary>
    void ApplyMatchedProperties(IMatchResult matchResult, IStyleRecalcContext context);

    /// <summary>
    /// Apply inheritance from parent computed style
    /// </summary>
    void ApplyInheritance(ICssStyleDeclaration parentDeclaration);

    /// <summary>
    /// Build final computed style (Blink: TakeStyle)
    /// Returns: ICssStyleDeclaration (AngleSharp type) with all computed values
    /// </summary>
    IComputedStyle TakeStyle(IElement element);
}