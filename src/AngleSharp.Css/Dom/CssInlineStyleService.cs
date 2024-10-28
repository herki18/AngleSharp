namespace AngleSharp.Css.Dom;

using System.Runtime.CompilerServices;
using AngleSharp.Dom;

/// <summary>
///
/// </summary>
public class CssInlineStyleService : ICssInlineStyleService
{
    /// <inheritdoc />
    public ConditionalWeakTable<IElement, ICssStyleDeclarationBase> Styles { get; }

    /// <inheritdoc />
    public ICssStyleDeclarationBase CreateStyle(IElement element)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public ICssStyleDeclarationBase CreateStyle(IElement element, string source)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public ICssStyleDeclarationBase GetStyle(IElement element)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void SetStyle(IElement element, string value)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void UpdateStyle(IElement element, string value)
    {
        throw new System.NotImplementedException();
    }
}