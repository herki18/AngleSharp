namespace LayoutEngine.Core.Style;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;

public interface IComputedStyle
{
    IElement Element { get; }
    ICssStyleDeclaration Declaration { get; }
}