namespace LayoutEngine.Core.Style;

public class IUserAgentStyleProvider
{

}ausing AngleSharp.Dom;
using AngleSharp.Css.Dom;
using AngleSharp.Css;

namespace LayoutEngine.Core.Style
{
    using System;
    using System.Collections.Generic;
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

    public class UserAgentStyleProvider : IUserAgentStyleProvider
    {
        private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
        private readonly Dictionary<string, ICssStyleRule> _elementRules;

        public UserAgentStyleProvider(ICssStyleDeclarationFactory styleDeclarationFactory)
        {
            _styleDeclarationFactory = styleDeclarationFactory ?? throw new ArgumentNullException(nameof(styleDeclarationFactory));
            _elementRules = BuildUserAgentRules();
        }

        public ICssStyleRule? GetRuleForElement(IElement element)
        {
            var tagName = element.TagName?.ToUpperInvariant();
            if (tagName != null && _elementRules.TryGetValue(tagName, out var rule))
            {
                return rule;
            }

            // Return default rule for unknown elements
            return _elementRules.GetValueOrDefault("DEFAULT");
        }

        private Dictionary<string, ICssStyleRule> BuildUserAgentRules()
        {
            var rules = new Dictionary<string, ICssStyleRule>();

            // Block elements
            rules["DIV"] = CreateRule("display: block;");
            rules["P"] = CreateRule("display: block; margin-top: 1em; margin-bottom: 1em;");
            rules["H1"] = CreateRule("display: block; font-size: 2em; font-weight: bold; margin-top: 0.67em; margin-bottom: 0.67em;");
            rules["H2"] = CreateRule("display: block; font-size: 1.5em; font-weight: bold; margin-top: 0.83em; margin-bottom: 0.83em;");
            rules["H3"] = CreateRule("display: block; font-size: 1.17em; font-weight: bold; margin-top: 1em; margin-bottom: 1em;");

            // Inline elements
            rules["SPAN"] = CreateRule("display: inline;");
            rules["A"] = CreateRule("display: inline; color: blue; text-decoration: underline;");
            rules["STRONG"] = CreateRule("display: inline; font-weight: bold;");
            rules["EM"] = CreateRule("display: inline; font-style: italic;");

            // Default for unknown elements
            rules["DEFAULT"] = CreateRule("display: inline; color: black; font-family: sans-serif; font-size: 16px;");

            return rules;
        }

        private ICssStyleRule CreateRule(string cssText)
        {
            var declaration = _styleDeclarationFactory.Create(cssText);
            return new UserAgentStyleRule(declaration);
        }
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
}