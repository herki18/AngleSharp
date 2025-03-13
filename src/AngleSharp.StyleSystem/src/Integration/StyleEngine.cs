namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using Interfaces;
using Properties;
using Storage;

public class StyleEngine : IStyleEngine, IDisposable
{
    private IRenderDevice _renderDevice;
    private readonly IStyleCache _styleCache;
    private readonly StyleSheetManager _stylesheetManager;
    private readonly IValueCalculator _valueCalculator;
    private readonly IStylePropertyMapper _stylePropertyMapper;
    private readonly IStyleApplicationStrategy _styleApplicationStrategy;
    private readonly IStyleTreeResolver _styleTreeResolver;

    // Performance tracking options
    private bool _optimizationEnabled = true;
    private bool _collectMetrics = false;

    public StyleEngine(IBrowsingContext context)
    {
        Context = context;
        _renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();
        PropertyTreeManager = new PropertyTreeManager();
        _styleCache = new StyleCache();
        _stylesheetManager = new StyleSheetManager(context);
        InvalidationTracker = new StyleInvalidationTracker();
        RuleCollector = new RuleCollector(context, _stylesheetManager);
        CascadeResolver = new CascadeResolver(context);
        InheritanceProcessor = new InheritanceProcessor(Context);
        VariableResolver = new VariableResolver(context);
        _stylePropertyMapper = new StylePropertyMapper();
        _valueCalculator = new ValueCalculator(context, _renderDevice);
        StyleFactory = new ComputedStyleFactory(this);
        ComputedStyleBuilder = new ComputedStyleBuilder(
            context,
            this,
            VariableResolver,
            _valueCalculator,
            _stylePropertyMapper,
            PropertyTreeManager,
            _renderDevice);
        _styleApplicationStrategy = new BasicStyleApplicationStrategy(this);
        _styleTreeResolver = new StyleTreeResolver(
            this,
            _styleApplicationStrategy,
            _styleCache,
            InvalidationTracker);
        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    public StyleSheetManager StylesheetManager => _stylesheetManager;
    public IRuleCollector RuleCollector { get; }

    public ICascadeResolver CascadeResolver { get; }

    public InheritanceProcessor InheritanceProcessor { get; }
    public IVariableResolver VariableResolver { get; }

    public ComputedStyleBuilder ComputedStyleBuilder { get; }

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
                PropertyTreeManager.ResetOptimizationMetrics();
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
        return PropertyTreeManager.GetOptimizationMetrics();
    }

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        _styleCache.Clear();
        if (Context.Active?.DocumentElement != null)
        {
            InvalidationTracker.InvalidateElement(Context.Active.DocumentElement);
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
        if (InvalidationTracker is StyleInvalidationTracker tracker)
        {
            tracker.InvalidateForDeviceChange();
        }
    }

    IRenderDevice IStyleEngine.RenderDevice => RenderDevice;
    public IStyleInvalidationTracker InvalidationTracker { get; }

    public IComputedStyleFactory StyleFactory { get; }
    public IBrowsingContext Context { get; }

    public IPropertyTreeManager PropertyTreeManager { get; }

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
            PropertyTreeManager.OptimizeTree(node);
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
        var treeNodes = new HashSet<IPropertyTreeNode>();

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
            PropertyTreeManager.OptimizeTree(node);
        }
    }

    public void Dispose()
    {
        _stylesheetManager?.Dispose();
    }
}