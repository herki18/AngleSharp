namespace LayoutEngine.Core.Style.Public;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;

public interface IComputedStyle
{
    IElement Element { get; }
    ICssStyleDeclaration Declaration { get; }
    string GetPropertyValue(string propertyName);
    void SetProperty(string propertyName, string value);
}