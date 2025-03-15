namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Storage;

public class StyleEngine : IStyleEngine, IDisposable
{
    #region Fields
    private IRenderDevice _renderDevice;
    private readonly IPropertyTreeManager _propertyTreeManager;
    private readonly IStyleCache _styleCache;
    private readonly IStyleSheetManager _stylesheetManager;
    private readonly IStyleInvalidationTracker _invalidationTracker;
    private readonly IRuleCollector _ruleCollector;
    private readonly ICascadeResolver _cascadeResolver;
    private readonly IInheritanceProcessor _inheritanceProcessor;
    private readonly IVariableResolver _variableResolver;
    private readonly IComputedStyleBuilder _computedStyleBuilder;
    private readonly IComputedStyleFactory _styleFactory;
    private readonly IBrowsingContext _context;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISubscriptionToken[] _subscriptionTokens;

    private bool _optimizationEnabled = true;
    private bool _collectMetrics = false;
    private bool _isDisposed = false;
    #endregion

    #region Constructor with Dependency Injection
    public StyleEngine(
        IBrowsingContext context,
        IRenderDevice renderDevice,
        IPropertyTreeManager propertyTreeManager,
        IStyleCache styleCache,
        IStyleSheetManager stylesheetManager,
        IStyleInvalidationTracker invalidationTracker,
        IRuleCollector ruleCollector,
        ICascadeResolver cascadeResolver,
        IInheritanceProcessor inheritanceProcessor,
        IVariableResolver variableResolver,
        IComputedStyleBuilder computedStyleBuilder,
        IComputedStyleFactory styleFactory,
        IEventAggregator eventAggregator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
        _propertyTreeManager = propertyTreeManager ?? throw new ArgumentNullException(nameof(propertyTreeManager));
        _styleCache = styleCache ?? throw new ArgumentNullException(nameof(styleCache));
        _stylesheetManager = stylesheetManager ?? throw new ArgumentNullException(nameof(stylesheetManager));
        _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
        _ruleCollector = ruleCollector ?? throw new ArgumentNullException(nameof(ruleCollector));
        _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
        _inheritanceProcessor = inheritanceProcessor ?? throw new ArgumentNullException(nameof(inheritanceProcessor));
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        _computedStyleBuilder = computedStyleBuilder ?? throw new ArgumentNullException(nameof(computedStyleBuilder));
        _styleFactory = styleFactory ?? throw new ArgumentNullException(nameof(styleFactory));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;

        // Subscribe to document lifecycle and style events
        _subscriptionTokens = new ISubscriptionToken[]
        {
            _eventAggregator.Subscribe<DocumentAttachedEvent>(OnDocumentAttached),
            _eventAggregator.Subscribe<DocumentDetachedEvent>(OnDocumentDetached),
            _eventAggregator.Subscribe<DomUpdatedEvent>(OnDomUpdated),
            _eventAggregator.Subscribe<ReadyStateChangedEvent>(OnReadyStateChanged)
        };
    }
    #endregion

    #region IStyleEngine Implementation
    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_styleCache.TryGetValue(new StyleCacheKey(element, pseudoElement), out var cachedStyle))
        {
            return cachedStyle;
        }

        var styleTreeResolver = GetStyleTreeResolver();
        if (styleTreeResolver == null)
            throw new InvalidOperationException("StyleTreeResolver not available");

        var style = styleTreeResolver.ResolveElementStyle(element, null, pseudoElement);
        _eventAggregator.Publish(new StyleComputedEvent(element, style));

        return style;
    }

    public void UpdateStyles(IElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        var styleTreeResolver = GetStyleTreeResolver();
        if (styleTreeResolver == null)
            throw new InvalidOperationException("StyleTreeResolver not available");

        styleTreeResolver.ResolveStylesForSubtree(root);
        _eventAggregator.Publish(new SubtreeStylesUpdatedEvent(root));
    }

    public IBrowsingContext Context => _context;
    public IStyleInvalidationTracker InvalidationTracker => _invalidationTracker;

    public IRenderDevice RenderDevice
    {
        get => _renderDevice;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var oldDevice = _renderDevice;
            _renderDevice = value;

            if (ShouldInvalidateStyles(oldDevice, _renderDevice))
            {
                InvalidateDeviceDependentStyles();
            }
        }
    }

    public IStyleSheetManager StylesheetManager => _stylesheetManager;
    public IRuleCollector RuleCollector => _ruleCollector;
    public ICascadeResolver CascadeResolver => _cascadeResolver;
    public IInheritanceProcessor InheritanceProcessor => _inheritanceProcessor;
    public IVariableResolver VariableResolver => _variableResolver;
    public IComputedStyleBuilder ComputedStyleBuilder => _computedStyleBuilder;
    public IPropertyTreeManager PropertyTreeManager => _propertyTreeManager;
    public IComputedStyleFactory StyleFactory => _styleFactory;

    public bool OptimizationEnabled
    {
        get => _optimizationEnabled;
        set => _optimizationEnabled = value;
    }

    public bool CollectMetrics
    {
        get => _collectMetrics;
        set
        {
            if (value && !_collectMetrics)
            {
                _propertyTreeManager.ResetOptimizationMetrics();
            }
            _collectMetrics = value;
        }
    }

    public void NotifyViewportChanged(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Viewport width must be positive");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Viewport height must be positive");

        if (_renderDevice.ViewPortWidth != width || _renderDevice.ViewPortHeight != height)
        {
            _renderDevice.SetViewport(width, height);
            InvalidateDeviceDependentStyles();
        }
    }

    public OptimizationMetrics GetOptimizationMetrics()
    {
        return _propertyTreeManager.GetOptimizationMetrics();
    }

    public void OptimizeAllStyles()
    {
        if (!_optimizationEnabled)
            return;

        var treeNodes = new HashSet<IPropertyTreeNode>();
        foreach (var style in _styleCache.GetAllStyles())
        {
            if (style is ComputedStyle computedStyle)
            {
                treeNodes.Add(computedStyle.PropertyTreeNode);
            }
        }

        foreach (var node in treeNodes)
        {
            _propertyTreeManager.OptimizeTree(node);
        }
    }
    #endregion

    #region Event Handlers
    private void OnDocumentAttached(DocumentAttachedEvent eventData)
    {
        try
        {
            _stylesheetManager.AttachToDocument(eventData.Document);
            if (eventData.Document.DocumentElement != null)
            {
                UpdateStyles(eventData.Document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error attaching to document: {ex.Message}");
        }
    }

    private void OnDocumentDetached(DocumentDetachedEvent eventData)
    {
        try
        {
            _stylesheetManager.DetachFromDocument(eventData.Document);
            _styleCache.Clear();
            _ruleCollector.ClearCache();
            _variableResolver.ClearElementVariables(eventData.Document.DocumentElement);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detaching from document: {ex.Message}");
        }
    }

    private void OnDomUpdated(DomUpdatedEvent eventData)
    {
        try
        {
            if (eventData.Document.DocumentElement != null)
            {
                _invalidationTracker.InvalidateElement(eventData.Document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling DOM update: {ex.Message}");
        }
    }

    private void OnReadyStateChanged(ReadyStateChangedEvent eventData)
    {
        try
        {
            if ((eventData.ReadyState == DocumentReadyState.Interactive ||
                 eventData.ReadyState == DocumentReadyState.Complete) &&
                eventData.Document.DocumentElement != null)
            {
                UpdateStyles(eventData.Document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling ready state change: {ex.Message}");
        }
    }

    private void OnStyleComputed(StyleComputedEvent eventData)
    {
        try
        {
            if (_optimizationEnabled && eventData.Style is ComputedStyle computedStyle)
            {
                _propertyTreeManager.OptimizeTree(computedStyle.PropertyTreeNode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in style computation handler: {ex.Message}");
        }
    }

    private void OnSubtreeStylesUpdated(SubtreeStylesUpdatedEvent eventData)
    {
        try
        {
            if (_optimizationEnabled)
            {
                OptimizeAllStyles();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in subtree styles handler: {ex.Message}");
        }
    }
    #endregion

    #region Helper Methods
    private bool ShouldInvalidateStyles(IRenderDevice oldDevice, IRenderDevice newDevice)
    {
        return oldDevice.ViewPortWidth != newDevice.ViewPortWidth ||
               oldDevice.ViewPortHeight != newDevice.ViewPortHeight ||
               oldDevice.FontSize != newDevice.FontSize ||
               oldDevice.Resolution != newDevice.Resolution;
    }

    private void InvalidateDeviceDependentStyles()
    {
        _styleCache.Clear();
        _invalidationTracker.InvalidateForDeviceChange();
    }

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        _styleCache.Clear();
        if (_context.Active?.DocumentElement != null)
        {
            _invalidationTracker.InvalidateElement(_context.Active.DocumentElement);
        }
        _ruleCollector.ClearCache();
    }

    private IStyleTreeResolver? GetStyleTreeResolver()
    {
        return _context.GetService<IStyleTreeResolver>();
    }
    #endregion

    #region IDisposable Implementation
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _stylesheetManager.StylesheetChanged -= StylesheetManager_StylesheetChanged;

        // Dispose all subscription tokens
        foreach (var token in _subscriptionTokens)
        {
            token.Dispose();
        }

        (_stylesheetManager as IDisposable)?.Dispose();
        _isDisposed = true;
    }
    #endregion
}