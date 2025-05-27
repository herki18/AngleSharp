namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

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
}