namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using Caching;
using Css;
using Css.Dom;
using Dom;

public class StyleComputationEngine
{
    private readonly StyleSheetManager _stylesheetManager;
    private readonly SelectorMatcher _selectorMatcher;
    private readonly CascadeResolver _cascadeResolver;
    private readonly InheritanceProcessor _inheritanceProcessor;
    private readonly ValueComputer _valueComputer;
    private readonly IRenderDevice _renderDevice;
    private readonly IBrowsingContext _browsingContext;
    private readonly LayoutEngineCacheManager _cacheManager;

    private ICssStyleDeclaration? _engineRootComputedStyle;

    public StyleComputationEngine(
        IRenderDevice? renderDevice = null,
        IBrowsingContext? context = null,
        IDocument? document = null,
        LayoutEngineCacheManager? cacheManager = null)
    {
        _browsingContext = context ?? BrowsingContext.New();
        _renderDevice = renderDevice ?? new DefaultRenderDevice();
        _stylesheetManager = new StyleSheetManager(context, document);
        _selectorMatcher = new SelectorMatcher(_renderDevice);
        _cascadeResolver = new CascadeResolver(_browsingContext);
        _inheritanceProcessor = new InheritanceProcessor(_browsingContext);
        _valueComputer = new ValueComputer(_renderDevice, _browsingContext);
        _cacheManager = cacheManager ?? new LayoutEngineCacheManager();
    }

    public ICssStyleDeclaration ComputeElementStyle(IElement element,
        ICssStyleDeclaration? parentStyle = null,
        string? pseudoElement = null)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var window = element.OwnerDocument?.DefaultView;
        if (window is null)
            throw new InvalidOperationException("Element must be part of a document with a default view");

        // Create a cache key for this computation
        var cacheKey = new StyleCacheKey(element, pseudoElement);

        // Try to get from cache first
        if (_cacheManager.StyleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        if(_engineRootComputedStyle is null)
        {
            _engineRootComputedStyle = CreateEngineRootComputedStyle();
        }

        // Check if this is the root element
        bool isRootElement = IsRootElement(element);

        return _cacheManager.StyleCache.GetOrAdd(cacheKey, _ =>
        {

            // For non-root elements, compute parent style if not provided
            if (parentStyle is null && !isRootElement && element.ParentElement is not null)
            {
                parentStyle = ComputeElementStyle(element.ParentElement, null, null);
            }

            // 1. Get stylesheets
            var stylesheets = _stylesheetManager.GetStylesheets();

            // 2. Match selectors
            var matchedRules = _selectorMatcher.MatchRules(element, stylesheets, pseudoElement);

            // 3. Resolve cascade
            var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);

            // 4. Apply inheritance
            var inheritedStyle = _inheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);

            // 5. Compute values
            var computedStyle = _valueComputer.ComputeValues(inheritedStyle, element, parentStyle!, _engineRootComputedStyle);

            // Return a placeholder style declaration for now
            return computedStyle;
        });
    }

    private bool IsRootElement(IElement element)
    {
        return element.ParentElement is null && element.OwnerDocument?.DocumentElement == element;
    }

    // This will only contain font size in px value
    private ICssStyleDeclaration CreateEngineRootComputedStyle()
    {
        var declaration = new CssStyleDeclaration(_browsingContext);
        declaration.SetProperty(PropertyNames.FontSize, "16px");
        return declaration;
    }
}