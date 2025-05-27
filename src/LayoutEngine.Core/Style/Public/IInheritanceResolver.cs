namespace LayoutEngine.Core.Style.Public;

using AngleSharp.Css.Dom;

/// <summary>
/// Handles CSS property inheritance
/// </summary>
public interface IInheritanceResolver
{
    void ApplyInheritance(ICssStyleDeclaration childDeclaration, ICssStyleDeclaration parentDeclaration);
}