namespace AngleSharp.Css.Dom;

using System;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using Parser;

/// <summary>
///
/// </summary>
public class CssInlineStyleService : ICssInlineStyleService
{
    /// <inheritdoc />
    public ConditionalWeakTable<IElement, ICssStyleDeclarationBase> Styles { get; } = new ();

    /// <inheritdoc />
    public ICssStyleDeclarationBase CreateStyle(IElement element)
    {
        return CreateStyle(element, null);
    }

    /// <inheritdoc />
    public ICssStyleDeclarationBase CreateStyle(IElement element, String source)
    {
        var document = element.OwnerDocument;
        var context = document.Context;
        var parser = context?.GetService<ICssParser>();

        // Seems to be run from a context with CSS
        if (parser != null)
        {
            var style = new CssStyleDeclaration(context);
            style.Update(source ?? element.GetAttribute(AttributeNames.Style));
            style.Changed += value => element.SetAttribute(AttributeNames.Style, value);
            return style;
        }

        return null;
    }

    /// <inheritdoc />
    public ICssStyleDeclarationBase GetStyle(IElement element)
    {
        return Styles.GetValue(element, CreateStyle);
    }

    /// <inheritdoc />
    public void SetStyle(IElement element, String value)
    {
        element.SetAttribute(AttributeNames.Style, value);
    }

    /// <inheritdoc />
    public void UpdateStyle(IElement element, string value)
    {
        element.GetStyle()?.Update(value);
    }
}