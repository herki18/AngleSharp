namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
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

    // Performance tracking options
    private bool _optimizationEnabled = true;
    private bool _collectMetrics = false;

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

    /// <summary>
    /// Gets or sets whether style tree optimization is enabled.
    /// </summary>
    public bool OptimizationEnabled
    {
        get => _optimizationEnabled;
        set => _optimizationEnabled = value;
    }

    /// <summary>
    /// Gets or sets whether to collect optimization metrics.
    /// </summary>
    public bool CollectMetrics
    {
        get => _collectMetrics;
        set
        {
            if (value && !_collectMetrics)
            {
                // Reset metrics when starting collection
                _propertyTreeManager.ResetOptimizationMetrics();
            }
            _collectMetrics = value;
        }
    }

    /// <summary>
    /// Gets the current optimization metrics.
    /// </summary>
    /// <returns>The optimization metrics.</returns>
    public OptimizationMetrics GetOptimizationMetrics()
    {
        return _propertyTreeManager.GetOptimizationMetrics();
    }

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
        return _styleTreeResolver.ResolveElementStyle(element, null, pseudoElement);
    }

    public void UpdateStyles(IElement root)
    {
        _styleTreeResolver.ResolveStylesForSubtree(root);
    }

    /// <summary>
    /// Optimizes a single property tree node.
    /// </summary>
    /// <param name="node">The node to optimize.</param>
    public void OptimizeTreeNode(PropertyTreeNode node)
    {
        if (_optimizationEnabled)
        {
            _propertyTreeManager.OptimizeTree(node);
        }
    }

    /// <summary>
    /// Forces optimization of all cached styles.
    /// </summary>
    public void OptimizeAllStyles()
    {
        if (!_optimizationEnabled)
            return;

        // This is an expensive operation that should be used sparingly
        // It's primarily for benchmarking or when memory usage needs to be reduced
        var treeNodes = new HashSet<PropertyTreeNode>();

        // First, collect all unique PropertyTreeNodes from the style cache
        foreach (var style in _styleCache.GetAllStyles())
        {
            if (style is ComputedStyle computedStyle)
            {
                treeNodes.Add(computedStyle.PropertyTreeNode);
            }
        }

        // Then optimize them
        foreach (var node in treeNodes)
        {
            _propertyTreeManager.OptimizeTree(node);
        }
    }

    public void Dispose()
    {
        _stylesheetManager?.Dispose();
    }
}