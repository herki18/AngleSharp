namespace AngleSharp.StyleSystem.Core;
using System;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;
using Css;

public class StyleEngine : IStyleEngine, IDisposable
{
    private readonly IBrowsingContext _context;
    private IRenderDevice _renderDevice;
    private readonly StyleCache _styleCache;
    private readonly StyleSheetManager _stylesheetManager;
    private readonly RuleCollector _ruleCollector;
    private readonly CascadeResolver _cascadeResolver;
    private readonly InheritanceProcessor _inheritanceProcessor;
    private readonly ComputedStyleBuilder _computedStyleBuilder;
    private readonly StyleInvalidationTracker _invalidationTracker;
    private readonly VariableResolver _variableResolver;
    private readonly ValueCalculator _valueCalculator;
    private readonly StylePropertyMapper _stylePropertyMapper;
    private readonly PropertyTreeManager _propertyTreeManager;

    public StyleEngine(IBrowsingContext context)
    {
        _context = context;
        _renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();

        // Initialize managers and trackers
        _propertyTreeManager = new PropertyTreeManager();
        _styleCache = new StyleCache();
        _stylesheetManager = new StyleSheetManager(context);
        _invalidationTracker = new StyleInvalidationTracker();

        // Initialize processors
        _ruleCollector = new RuleCollector(context, _stylesheetManager);
        _cascadeResolver = new CascadeResolver(context);
        _inheritanceProcessor = new InheritanceProcessor(_context);
        _variableResolver = new VariableResolver(context);
        _stylePropertyMapper = new StylePropertyMapper();

        // Initialize calculator
        _valueCalculator = new ValueCalculator(context, _renderDevice);

        // Initialize style factory
        StyleFactory = new ComputedStyleFactory(this);

        // Initialize style builder (with all required dependencies)
        _computedStyleBuilder = new ComputedStyleBuilder(
            context,
            this,
            _variableResolver,
            _valueCalculator,
            _stylePropertyMapper,
            _propertyTreeManager,
            _renderDevice);

        // Setup event handlers
        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    public StyleSheetManager StylesheetManager => _stylesheetManager;

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        _styleCache.Clear();
        if (_context.Active?.DocumentElement != null)
        {
            _invalidationTracker.InvalidateElement(_context.Active.DocumentElement);
        }
    }

    public IRenderDevice RenderDevice
    {
        get => _renderDevice;
        set
        {
            var oldDevice = _renderDevice;
            _renderDevice = value ?? new DefaultRenderDevice();
            if (ShouldInvalidateStyles(oldDevice, _renderDevice))
            {
                InvalidateDeviceDependentStyles();
            }
        }
    }

    public void NotifyViewportChanged(int width, int height)
    {
        if (_renderDevice.ViewPortWidth != width || _renderDevice.ViewPortHeight != height)
        {
            _renderDevice.SetViewport(width, height);
            InvalidateDeviceDependentStyles();
        }
    }

    private bool ShouldInvalidateStyles(IRenderDevice oldDevice, IRenderDevice newDevice)
    {
        return oldDevice.ViewPortWidth != newDevice.ViewPortWidth ||
               oldDevice.ViewPortHeight != newDevice.ViewPortHeight ||
               oldDevice.FontSize != newDevice.FontSize;
    }

    private void InvalidateDeviceDependentStyles()
    {
        _styleCache.Clear();
        if (_invalidationTracker is StyleInvalidationTracker tracker)
        {
            tracker.InvalidateForDeviceChange();
        }
    }

    IRenderDevice IStyleEngine.RenderDevice => RenderDevice;
    public IStyleInvalidationTracker InvalidationTracker => _invalidationTracker;
    public IComputedStyleFactory StyleFactory { get; }
    public IBrowsingContext Context => _context;
    internal PropertyTreeManager PropertyTreeManager => _propertyTreeManager;

    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        var cacheKey = new StyleCacheKey(element, pseudoElement);
        if (_styleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        IComputedStyle? parentStyle = null;
        if (element.ParentElement != null)
        {
            parentStyle = ComputeElementStyle(element.ParentElement);
        }

        var matchedRules = _ruleCollector.CollectMatchingRules(element, pseudoElement);
        var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);
        var inheritedStyle = _inheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);
        var computedStyle = _computedStyleBuilder.BuildComputedStyle(inheritedStyle, element, parentStyle);

        if(computedStyle is null)
            throw new Exception("Computed style is null");

        _styleCache.Store(cacheKey, computedStyle);
        _invalidationTracker.MarkAsUpToDate(element);
        return computedStyle;
    }

    public void UpdateStyles(IElement root)
    {
        var elementsToUpdate = _invalidationTracker.GetElementsToUpdate(root);
        foreach (var element in elementsToUpdate)
        {
            _styleCache.Remove(new StyleCacheKey(element, null));
            ComputeElementStyle(element);
        }
    }

    public void Dispose()
    {
        _stylesheetManager?.Dispose();
    }
}