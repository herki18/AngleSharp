namespace AngleSharp.StyleSystem.Interfaces;

using Css.Dom;
using Dom;

/// <summary>
/// Factory for creating computed style objects.
/// </summary>
public interface IComputedStyleFactory
{
    IComputedStyle CreateComputedStyle();
    IComputedStyle CopyComputedStyle(IComputedStyle source);
    IComputedStyle CreateComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration, IPropertyTreeNode? node = null);
}