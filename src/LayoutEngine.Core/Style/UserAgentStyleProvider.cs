namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

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
        return _elementRules.GetValueOrDefault("DEFAULT");
    }

    private Dictionary<string, ICssStyleRule> BuildUserAgentRules()
    {
        var rules = new Dictionary<string, ICssStyleRule>();

        // HTML block elements
        rules["DIV"] = CreateRule("display: block;");
        rules["P"] = CreateRule("display: block; margin-top: 1em; margin-bottom: 1em;");
        rules["H1"] = CreateRule("display: block; font-size: 2em; font-weight: bold; margin-top: 0.67em; margin-bottom: 0.67em;");
        rules["H2"] = CreateRule("display: block; font-size: 1.5em; font-weight: bold; margin-top: 0.83em; margin-bottom: 0.83em;");
        rules["H3"] = CreateRule("display: block; font-size: 1.17em; font-weight: bold; margin-top: 1em; margin-bottom: 1em;");
        rules["H4"] = CreateRule("display: block; font-size: 1em; font-weight: bold; margin-top: 1.33em; margin-bottom: 1.33em;");
        rules["H5"] = CreateRule("display: block; font-size: 0.83em; font-weight: bold; margin-top: 1.67em; margin-bottom: 1.67em;");
        rules["H6"] = CreateRule("display: block; font-size: 0.67em; font-weight: bold; margin-top: 2.33em; margin-bottom: 2.33em;");

        // Lists
        rules["UL"] = CreateRule("display: block; margin-top: 1em; margin-bottom: 1em; padding-left: 40px;");
        rules["OL"] = CreateRule("display: block; margin-top: 1em; margin-bottom: 1em; padding-left: 40px;");
        rules["LI"] = CreateRule("display: list-item;");

        // Inline elements
        rules["SPAN"] = CreateRule("display: inline;");
        rules["A"] = CreateRule("display: inline; color: blue; text-decoration: underline; cursor: pointer;");
        rules["STRONG"] = CreateRule("display: inline; font-weight: bold;");
        rules["B"] = CreateRule("display: inline; font-weight: bold;");
        rules["EM"] = CreateRule("display: inline; font-style: italic;");
        rules["I"] = CreateRule("display: inline; font-style: italic;");
        rules["CODE"] = CreateRule("display: inline; font-family: monospace;");

        // Form elements
        rules["INPUT"] = CreateRule("display: inline-block; border: 1px solid #ccc; padding: 2px;");
        rules["BUTTON"] = CreateRule("display: inline-block; border: 1px solid #ccc; padding: 2px 6px; background-color: #f0f0f0; cursor: pointer;");
        rules["TEXTAREA"] = CreateRule("display: inline-block; border: 1px solid #ccc; padding: 2px;");

        // Other block elements
        rules["SECTION"] = CreateRule("display: block;");
        rules["ARTICLE"] = CreateRule("display: block;");
        rules["HEADER"] = CreateRule("display: block;");
        rules["FOOTER"] = CreateRule("display: block;");
        rules["NAV"] = CreateRule("display: block;");
        rules["MAIN"] = CreateRule("display: block;");
        rules["ASIDE"] = CreateRule("display: block;");

        // Default rule
        rules["DEFAULT"] = CreateRule("display: inline; color: black; font-family: sans-serif; font-size: 16px;");

        return rules;
    }

    private ICssStyleRule CreateRule(string cssText)
    {
        var declaration = _styleDeclarationFactory.Create(cssText);
        return new UserAgentStyleRule(declaration);
    }
}