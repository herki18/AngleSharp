namespace LayoutEngine.Core.Style;

using System;
using System.IO;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Provides browser default styles for HTML elements
/// </summary>
public interface IUserAgentStyleProvider
{
    ICssStyleRule? GetRuleForElement(IElement element);
}

/// <summary>
/// Simple implementation of ICssStyleRule for user agent styles
/// </summary>
public class UserAgentStyleRule : ICssStyleRule
{
    public ICssStyleDeclaration Style { get; }

    public ISelector Selector => throw new NotImplementedException();

    public Boolean TryMatch(IElement element, IElement scope, out Priority specificity)
    {
        throw new NotImplementedException();
    }

    public ICssRuleList Rules => throw new NotImplementedException();

    public string SelectorText { get; set; } = string.Empty;
    public CssRuleType Type => CssRuleType.Style;
    public string CssText { get; set; } = string.Empty;
    public ICssRule? Parent { get; set; }
    public ICssStyleSheet? Owner { get; set; }
    public void SetParent(ICssRule rule)
    {
        throw new NotImplementedException();
    }

    public void SetOwner(ICssStyleSheet sheet)
    {
        throw new NotImplementedException();
    }

    public UserAgentStyleRule(ICssStyleDeclaration style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    public void ToCss(TextWriter writer, IStyleFormatter formatter)
    {
        throw new NotImplementedException();
    }
}