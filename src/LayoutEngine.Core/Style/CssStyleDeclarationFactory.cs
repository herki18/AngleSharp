namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Css.Dom;

public class CssStyleDeclarationFactory : ICssStyleDeclarationFactory
{
    private readonly IBrowsingContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CssStyleDeclarationFactory"/> class.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    public CssStyleDeclarationFactory(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public ICssStyleDeclaration Create()
    {
        return new CssStyleDeclaration(_context);
    }

    /// <inheritdoc />
    public ICssStyleDeclaration Create(string cssText)
    {
        var declaration = new CssStyleDeclaration(_context);
        if (!string.IsNullOrEmpty(cssText))
        {
            declaration.CssText = cssText;
        }
        return declaration;
    }

    /// <inheritdoc />
    public ICssStyleDeclaration Create(IEnumerable<ICssProperty> properties)
    {
        var declaration = new CssStyleDeclaration(_context);
        if (declaration is CssStyleDeclaration cssDeclaration && properties != null)
        {
            cssDeclaration.SetDeclarations(properties.ToList());
        }
        else if (properties != null)
        {
            foreach (var property in properties)
            {
                declaration.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : null);
            }
        }
        return declaration;
    }

    /// <inheritdoc />
    public ICssStyleDeclaration CreateCopy(ICssStyleDeclaration source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var declaration = new CssStyleDeclaration(_context);
        if (declaration is CssStyleDeclaration cssDeclaration)
        {
            cssDeclaration.SetDeclarations(source.ToList());
        }
        else
        {
            foreach (var property in source)
            {
                declaration.SetProperty(
                    property.Name,
                    source.GetPropertyValue(property.Name),
                    source.GetPropertyPriority(property.Name));
            }
        }
        return declaration;
    }
}