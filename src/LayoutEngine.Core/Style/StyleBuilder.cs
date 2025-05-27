using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using Microsoft.Extensions.Logging;

namespace LayoutEngine.Core.Style;

/// <summary>
/// Builds final computed style - mirrors Blink's StyleBuilder
/// </summary>
public class StyleBuilder : IStyleBuilder
{
    private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
    private readonly ICascadeResolver _cascadeResolver;
    private readonly IInheritanceResolver _inheritanceResolver;
    private readonly ILogger<StyleBuilder> _logger;

    private ICssStyleDeclaration? _currentDeclaration;

    public StyleBuilder(
        ICssStyleDeclarationFactory styleDeclarationFactory,
        ICascadeResolver cascadeResolver,
        IInheritanceResolver inheritanceResolver,
        ILogger<StyleBuilder> logger)
    {
        _styleDeclarationFactory = styleDeclarationFactory ?? throw new ArgumentNullException(nameof(styleDeclarationFactory));
        _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
        _inheritanceResolver = inheritanceResolver ?? throw new ArgumentNullException(nameof(inheritanceResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ApplyMatchedProperties(IMatchResult matchResult, IStyleRecalcContext context)
    {
        _logger.LogDebug("Applying matched properties for element: {TagName}", context.CurrentElement?.TagName);

        // Create new style declaration
        _currentDeclaration = _styleDeclarationFactory.Create();

        // Convert IMatchResult to format expected by your existing CascadeResolver
        var allMatchedRules = ConvertToMatchedRules(matchResult);

        // Use your existing cascade resolver
        var resolvedDeclaration = _cascadeResolver.ResolveCascade(allMatchedRules, context.CurrentElement!);

        // Copy resolved properties to current declaration
        CopyDeclaration(resolvedDeclaration, _currentDeclaration);
    }

    public void ApplyInheritance(ICssStyleDeclaration parentDeclaration)
    {
        if (_currentDeclaration == null)
            throw new InvalidOperationException("Must call ApplyMatchedProperties first");

        _logger.LogDebug("Applying inheritance from parent style");

        _inheritanceResolver.ApplyInheritance(_currentDeclaration, parentDeclaration);
    }

    public IComputedStyle TakeStyle(IElement element)
    {
        if (_currentDeclaration == null)
            throw new InvalidOperationException("Must call ApplyMatchedProperties first");

        var result = new ComputedStyle(element, _currentDeclaration);
        _currentDeclaration = null; // Clear for next use

        return result;
    }

    private IEnumerable<MatchedRule> ConvertToMatchedRules(IMatchResult matchResult)
    {
        var rules = new List<MatchedRule>();
        int index = 0;

        // Convert user agent rules
        foreach (var rule in matchResult.UserAgentRules)
        {
            if (rule.Rule is ICssStyleRule styleRule)
            {
                rules.Add(new MatchedRule(
                    styleRule,
                    new Priority(0, 0, 0, 0), // User agent has lowest specificity
                    StylesheetOrigin.UserAgent,
                    index++));
            }
        }

        // Convert author rules
        foreach (var rule in matchResult.AuthorRules)
        {
            if (rule.Rule is ICssStyleRule styleRule)
            {
                rules.Add(new MatchedRule(
                    styleRule,
                    CalculateSpecificity(rule.Specificity),
                    StylesheetOrigin.Author,
                    index++));
            }
        }

        return rules;
    }

    private Priority CalculateSpecificity(int specificity)
    {
        // Convert simple int specificity to Priority object
        // This is a simplified conversion - you might need to adjust based on your needs
        return new Priority(0, 0, 0, specificity);
    }

    private void CopyDeclaration(ICssStyleDeclaration source, ICssStyleDeclaration target)
    {
        for (int i = 0; i < source.Length; i++)
        {
            var propertyName = source[i];
            var value = source.GetPropertyValue(propertyName);
            var priority = source.GetPropertyPriority(propertyName);

            target.SetProperty(propertyName, value, priority);
        }
    }
}