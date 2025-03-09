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
    private readonly IStyleApplicationStrategy _styleApplicationStrategy;
    private readonly IStyleTreeResolver _styleTreeResolver;

    public StyleEngine(IBrowsingContext context)
    {
        _context = context;
        _renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();
        _propertyTreeManager = new PropertyTreeManager();
        _styleCache = new StyleCache();
        _stylesheetManager = new StyleSheetManager(context);
        _invalidationTracker = new StyleInvalidationTracker();
        _ruleCollector = new RuleCollector(context, _stylesheetManager);
        _cascadeResolver = new CascadeResolver(context);
        _inheritanceProcessor = new InheritanceProcessor(_context);
        _variableResolver = new VariableResolver(context);
        _stylePropertyMapper = new StylePropertyMapper();
        _valueCalculator = new ValueCalculator(context, _renderDevice);
        StyleFactory = new ComputedStyleFactory(this);
        _computedStyleBuilder = new ComputedStyleBuilder(
            context,
            this,
            _variableResolver,
            _valueCalculator,
            _stylePropertyMapper,
            _propertyTreeManager,
            _renderDevice);

        // Initialize strategy with IStyleEngine interface (this) instead of concrete type
        _styleApplicationStrategy = new BasicStyleApplicationStrategy(this);
        _styleTreeResolver = new StyleTreeResolver(
            this,
            _styleApplicationStrategy,
            _styleCache,
            _invalidationTracker);

        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    public StyleSheetManager StylesheetManager => _stylesheetManager;

    public RuleCollector RuleCollector => _ruleCollector;

    public CascadeResolver CascadeResolver => _cascadeResolver;

    public InheritanceProcessor InheritanceProcessor => _inheritanceProcessor;

    public ComputedStyleBuilder ComputedStyleBuilder => _computedStyleBuilder;

    public VariableResolver VariableResolver => _variableResolver;

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
        // Delegate to the style tree resolver
        return _styleTreeResolver.ResolveElementStyle(element, null, pseudoElement);
    }

    public void UpdateStyles(IElement root)
    {
        // Delegate to the style tree resolver
        _styleTreeResolver.ResolveStylesForSubtree(root);
    }

    public void Dispose()
    {
        _stylesheetManager?.Dispose();
    }
}