namespace LayoutEngine.Core.Tests;

using System.Linq;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using NSubstitute;

public static class StyleTestHelpers
{
    public static ICssStyleSheet CreateMockStylesheet()
    {
        var stylesheet = Substitute.For<ICssStyleSheet>();
        var ruleList = Substitute.For<ICssRuleList>();
        ruleList.Length.Returns(0);
        ruleList.GetEnumerator().Returns(Enumerable.Empty<ICssRule>().GetEnumerator());
        stylesheet.Rules.Returns(ruleList);
        return stylesheet;
    }

    public static ICssStyleSheet CreateMockStylesheetWithRules(string[] cssRules)
    {
        var config = Configuration.Default.WithCss();
        using var context = BrowsingContext.New(config);
        var parser = context.GetService<ICssParser>() ?? new CssParser();

        var cssText = string.Join("\n", cssRules);
        return parser.ParseStyleSheet(cssText);
    }

    public static ICssStyleDeclaration CreateMockCssStyleDeclaration()
    {
        var config = Configuration.Default.WithCss();
        using var context = BrowsingContext.New(config);
        return new CssStyleDeclaration(context);
    }

    public static ICssStyleSheet CreateMockStylesheetWithMockRules(ICssRule[] rules)
    {
        var stylesheet = Substitute.For<ICssStyleSheet>();
        var ruleList = Substitute.For<ICssRuleList>();

        ruleList.Length.Returns(rules.Length);
        ruleList.GetEnumerator().Returns(rules.AsEnumerable().GetEnumerator());

        // Set up indexer access
        for (int i = 0; i < rules.Length; i++)
        {
            ruleList[i].Returns(rules[i]);
        }

        stylesheet.Rules.Returns(ruleList);
        return stylesheet;
    }

    public static ICssStyleRule CreateMockStyleRule(string selectorText, string cssText = "")
    {
        var rule = Substitute.For<ICssStyleRule>();
        rule.SelectorText.Returns(selectorText);
        rule.CssText.Returns(cssText);
        rule.Type.Returns(CssRuleType.Style);

        var style = CreateMockCssStyleDeclaration();
        rule.Style.Returns(style);

        return rule;
    }
}