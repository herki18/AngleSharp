namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using Css;
using Css.Dom;
using Dom;

public class StyleComputationEngine
{
    private readonly StyleSheetManager _stylesheetManager;
    private readonly SelectorMatcher _selectorMatcher;
    private readonly CascadeResolver _cascadeResolver;
    private readonly IRenderDevice _renderDevice;
    private readonly IBrowsingContext _browsingContext;


    public StyleComputationEngine(IRenderDevice? renderDevice = null, IBrowsingContext? context = null, IDocument? document = null)
    {
        _browsingContext = context ?? BrowsingContext.New();
        _renderDevice = renderDevice ?? new DefaultRenderDevice();
        _stylesheetManager = new StyleSheetManager(context, document);
        _selectorMatcher = new SelectorMatcher(_renderDevice);
        _cascadeResolver = new CascadeResolver(_browsingContext);
    }

    public ICssStyleDeclaration ComputeElementStyle(IElement element,
        ICssStyleDeclaration? parentStyle = null,
        string? pseudoElement = null)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var window = element.OwnerDocument?.DefaultView;
        if (window is null)
            throw new InvalidOperationException("Element must be part of a document with a default view");

        // 1. Get stylesheets
        var stylesheets = _stylesheetManager.GetStylesheets();

        // 2. Match selectors
        var matchedRules = _selectorMatcher.MatchRules(element, stylesheets, pseudoElement);

        // 3. Resolve cascade
        var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);

        // 4. Apply inheritance
        // TODO: Implement InheritanceProcessor

        // 5. Compute values
        // TODO: Implement ValueComputer

        // Return a placeholder style declaration for now
        return cascadedStyle;
    }

    private ICssStyleDeclaration CreateEmptyStyleDeclaration()
    {
        // This is a temporary implementation until we have the full pipeline working
        // In a real implementation, this would create a proper computed style declaration
        return new CssStyleDeclaration(_browsingContext);
    }
}