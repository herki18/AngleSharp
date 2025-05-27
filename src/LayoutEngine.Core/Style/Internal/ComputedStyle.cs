namespace LayoutEngine.Core.Style.Internal;

using System;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.Core.Style.Public;

/// <summary>
/// Immutable computed style data holder using ICssStyleDeclaration
/// </summary>
public class ComputedStyle : IComputedStyle
{
    public IElement Element { get; }
    public ICssStyleDeclaration Declaration { get; }

    public ComputedStyle(IElement element, ICssStyleDeclaration declaration)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
    }

    public string GetPropertyValue(string propertyName)
    {
        return Declaration.GetPropertyValue(propertyName) ?? string.Empty;
    }

    public void SetProperty(string propertyName, string value)
    {
        Declaration.SetProperty(propertyName, value);
    }
}