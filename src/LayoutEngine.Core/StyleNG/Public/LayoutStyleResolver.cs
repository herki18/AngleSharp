namespace LayoutEngine.Core.LayoutStyle.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using LayoutEngine.Core.LayoutNG.Public;
using LayoutEngine.Core.LayoutStyle.Public;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Css.Parser;
using Microsoft.Extensions.Logging;
using Style.Public;

/// <summary>
/// Resolves styles for layout objects, handling both element-backed and anonymous objects.
/// </summary>
public class LayoutStyleResolver : ILayoutStyleResolver
{
    private readonly IAnonymousStyleSynthesizer _anonymousStyleSynthesizer;
    private readonly ICssParser _cssParser;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly ICssStyleDeclarationFactory _declarationFactory;
    private readonly ILogger<LayoutStyleResolver> _logger;

    // Inherited properties according to CSS spec
    private static readonly HashSet<string> InheritedProperties = new()
    {
        "color", "font-family", "font-size", "font-weight", "font-style",
        "line-height", "text-align", "text-indent", "text-transform",
        "white-space", "word-spacing", "letter-spacing", "visibility",
        "direction", "unicode-bidi", "quotes", "list-style-type",
        "list-style-position", "list-style-image", "cursor"
    };

    public LayoutStyleResolver(
        IAnonymousStyleSynthesizer anonymousStyleSynthesizer,
        ICssParser cssParser,
        IStyleSheetManager styleSheetManager,
        ICssStyleDeclarationFactory declarationFactory,
        ILogger<LayoutStyleResolver> logger)
    {
        _anonymousStyleSynthesizer = anonymousStyleSynthesizer ?? throw new ArgumentNullException(nameof(anonymousStyleSynthesizer));
        _cssParser = cssParser ?? throw new ArgumentNullException(nameof(cssParser));
        _styleSheetManager = styleSheetManager ?? throw new ArgumentNullException(nameof(styleSheetManager));
        _declarationFactory = declarationFactory ?? throw new ArgumentNullException(nameof(declarationFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILayoutComputedStyle ResolveStyle(ILayoutObject layoutObject, ILayoutStyleContext context)
    {
        _logger.LogDebug("Resolving style for {Type} (Anonymous: {IsAnonymous})",
            layoutObject.Type, layoutObject.IsAnonymous);

        // Handle anonymous objects
        if (layoutObject.IsAnonymous)
        {
            return _anonymousStyleSynthesizer.SynthesizeStyle(layoutObject, context.ParentStyle!);
        }

        // For element-backed objects
        if (layoutObject.Element == null)
        {
            throw new InvalidOperationException($"Non-anonymous layout object must have an element");
        }

        // Create base declaration
        var declaration = _declarationFactory.Create();

        // Apply inheritance first
        ApplyInheritance(declaration, context.ParentStyle);

        // Collect and apply matching rules
        var matchedRules = CollectMatchingRules(layoutObject);
        var inlineStyle = GetInlineStyle(layoutObject.Element);
        var cascadedDeclaration = ApplyCascade(matchedRules, inlineStyle);

        // Merge cascaded values into declaration
        MergeDeclarations(declaration, cascadedDeclaration);

        // Compute final values
        ComputeFinalValues(declaration, context);

        // Create computed style
        return new LayoutComputedStyle(layoutObject, declaration, false);
    }

    public IEnumerable<IMatchedLayoutRule> CollectMatchingRules(ILayoutObject layoutObject)
    {
        if (layoutObject.Element == null)
        {
            return Enumerable.Empty<IMatchedLayoutRule>();
        }

        var matchedRules = new List<MatchedLayoutRule>();
        var ruleIndex = 0;

        // Collect from all stylesheets
        foreach (var origin in Enum.GetValues<StylesheetOrigin>())
        {
            var stylesheets = _styleSheetManager.GetStylesheetsByOrigin(origin);

            foreach (var stylesheet in stylesheets)
            {
                CollectMatchingRulesFromStylesheet(
                    stylesheet,
                    layoutObject.Element,
                    matchedRules,
                    origin,
                    ref ruleIndex);
            }
        }

        _logger.LogDebug("Collected {Count} matching rules for element", matchedRules.Count);
        return matchedRules;
    }

    private void CollectMatchingRulesFromStylesheet(
        ICssStyleSheet stylesheet,
        IElement element,
        List<MatchedLayoutRule> matchedRules,
        StylesheetOrigin origin,
        ref int ruleIndex)
    {
        foreach (var rule in stylesheet.Rules)
        {
            if (rule is ICssStyleRule styleRule)
            {
                if (styleRule.Selector?.Match(element) == true)
                {
                    var specificity = CalculateSpecificity(styleRule.Selector);
                    matchedRules.Add(new MatchedLayoutRule
                    {
                        Rule = styleRule,
                        Specificity = specificity,
                        Origin = origin,
                        DocumentOrder = ruleIndex++
                    });
                }
            }
            else if (rule is ICssGroupingRule groupingRule)
            {
                // Handle @media, @supports, etc.
                // TODO: Check if the condition matches
                foreach (var nestedRule in groupingRule.Rules)
                {
                    if (nestedRule is ICssStyleRule nestedStyleRule)
                    {
                        if (nestedStyleRule.Selector?.Match(element) == true)
                        {
                            var specificity = CalculateSpecificity(nestedStyleRule.Selector);
                            matchedRules.Add(new MatchedLayoutRule
                            {
                                Rule = nestedStyleRule,
                                Specificity = specificity,
                                Origin = origin,
                                DocumentOrder = ruleIndex++
                            });
                        }
                    }
                }
            }
        }
    }

    public ILayoutComputedStyle SynthesizeAnonymousStyle(ILayoutObject anonymousObject, ILayoutStyleContext context)
    {
        if (context.ParentStyle == null)
        {
            throw new InvalidOperationException("Anonymous objects must have a parent style");
        }

        return _anonymousStyleSynthesizer.SynthesizeStyle(anonymousObject, context.ParentStyle);
    }

    public ICssStyleDeclaration ApplyCascade(IEnumerable<IMatchedLayoutRule> matchedRules, ICssStyleDeclaration? inlineStyle)
    {
        var declaration = _declarationFactory.Create();

        // Sort rules by cascade order: origin, specificity, document order
        var sortedRules = matchedRules
            .OrderBy(r => r.Origin)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.DocumentOrder);

        // Apply rules in order
        foreach (var matchedRule in sortedRules)
        {
            if (matchedRule.Rule.Style != null)
            {
                foreach (var property in matchedRule.Rule.Style)
                {
                    declaration.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);
                }
            }
        }

        // Apply inline styles last (highest specificity)
        if (inlineStyle != null)
        {
            foreach (var property in inlineStyle)
            {
                declaration.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);
            }
        }

        return declaration;
    }

    public void ApplyInheritance(ICssStyleDeclaration childDeclaration, ILayoutComputedStyle? parentStyle)
    {
        if (parentStyle == null) return;

        foreach (var propertyName in InheritedProperties)
        {
            // Only inherit if child doesn't have an explicit value
            if (string.IsNullOrEmpty(childDeclaration.GetPropertyValue(propertyName)))
            {
                var parentValue = parentStyle.GetPropertyValue(propertyName);
                if (!string.IsNullOrEmpty(parentValue))
                {
                    childDeclaration.SetProperty(propertyName, parentValue);
                }
            }
        }
    }

    public void ComputeFinalValues(ICssStyleDeclaration declaration, ILayoutStyleContext context)
    {
        // Apply initial values for properties that weren't set
        ApplyInitialValues(declaration);

        // Resolve 'inherit' and 'initial' keywords
        ResolveKeywords(declaration, context);

        // Compute relative values (em, rem, percentages)
        ComputeRelativeValues(declaration, context);

        // Ensure required properties have values
        EnsureRequiredProperties(declaration);
    }

    private void ApplyInitialValues(ICssStyleDeclaration declaration)
    {
        // Set initial values for common properties if not already set
        if (string.IsNullOrEmpty(declaration.GetPropertyValue("display")))
            declaration.SetProperty("display", "inline");

        if (string.IsNullOrEmpty(declaration.GetPropertyValue("position")))
            declaration.SetProperty("position", "static");

        if (string.IsNullOrEmpty(declaration.GetPropertyValue("color")))
            declaration.SetProperty("color", "black");

        if (string.IsNullOrEmpty(declaration.GetPropertyValue("background-color")))
            declaration.SetProperty("background-color", "transparent");
    }

    private void ResolveKeywords(ICssStyleDeclaration declaration, ILayoutStyleContext context)
    {
        // Handle 'inherit' keyword
        var properties = declaration.ToList(); // Create a copy to iterate
        foreach (var property in properties)
        {
            if (property.Value == "inherit" && context.ParentStyle != null)
            {
                var parentValue = context.ParentStyle.GetPropertyValue(property.Name);
                if (!string.IsNullOrEmpty(parentValue))
                {
                    declaration.SetProperty(property.Name, parentValue);
                }
            }
            else if (property.Value == "initial")
            {
                // Set to initial value
                declaration.RemoveProperty(property.Name);
                ApplyInitialValueForProperty(declaration, property.Name);
            }
        }
    }

    private void ApplyInitialValueForProperty(ICssStyleDeclaration declaration, string propertyName)
    {
        // Apply CSS initial values
        var initialValue = propertyName switch
        {
            "display" => "inline",
            "position" => "static",
            "color" => "black",
            "background-color" => "transparent",
            "font-size" => "16px",
            "font-family" => "serif",
            "line-height" => "normal",
            "text-align" => "start",
            _ => null
        };

        if (initialValue != null)
        {
            declaration.SetProperty(propertyName, initialValue);
        }
    }

    private void ComputeRelativeValues(ICssStyleDeclaration declaration, ILayoutStyleContext context)
    {
        // TODO: Implement computation of em, rem, percentages, etc.
        // This is a complex topic that would need proper value resolution
    }

    private void EnsureRequiredProperties(ICssStyleDeclaration declaration)
    {
        // Ensure display has a valid value
        var display = declaration.GetPropertyValue("display");
        if (string.IsNullOrEmpty(display))
        {
            declaration.SetProperty("display", "inline");
        }
    }

    private ICssStyleDeclaration? GetInlineStyle(IElement element)
    {
        var styleAttr = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttr))
            return null;

        try
        {
            return _cssParser.ParseDeclaration(styleAttr);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse inline style: {Style}", styleAttr);
            return null;
        }
    }

    private void MergeDeclarations(ICssStyleDeclaration target, ICssStyleDeclaration source)
    {
        foreach (var property in source)
        {
            target.SetProperty(
                property.Name,
                property.Value,
                property.IsImportant ? "important" : null);
        }
    }

    private int CalculateSpecificity(ISelector selector)
    {
        // Simplified specificity calculation
        // Real implementation would properly calculate a,b,c values
        return selector.Specificity.CompareTo(Priority.Zero);
    }

    // Helper implementation classes
    private class MatchedLayoutRule : IMatchedLayoutRule
    {
        public ICssStyleRule Rule { get; set; } = null!;
        public int Specificity { get; set; }
        public StylesheetOrigin Origin { get; set; }
        public int DocumentOrder { get; set; }
    }
}