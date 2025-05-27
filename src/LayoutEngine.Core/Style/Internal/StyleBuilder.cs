namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;

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

        // Convert IMatchResult to format expected by CascadeResolver
        var allMatchedRules = ConvertToMatchedRules(matchResult);

        // Use cascade resolver to resolve the final declarations
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

        // Add user agent rules
        rules.AddRange(matchResult.UserAgentRules);

        // Add author rules
        rules.AddRange(matchResult.AuthorRules);

        return rules;
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