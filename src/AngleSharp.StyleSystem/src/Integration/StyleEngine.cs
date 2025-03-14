namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Observers;
using AngleSharp.StyleSystem.Storage;

/// <summary>
/// Central component for style computation that implements the observer pattern for loose coupling.
/// </summary>
public class StyleEngine : IStyleEngine, IDocumentLifecycleObserver, IStyleComputationObserver, IDisposable
{
    #region Fields

    private IRenderDevice _renderDevice;
    private readonly List<IStyleComputationObserver> _computationObservers = new();
    private IStyleInvalidationTracker? _invalidationTracker;
    private IStyleTreeResolver? _styleTreeResolver;
    private IStyleSheetManager? _stylesheetManager;
    private IValueCalculator? _valueCalculator;
    private IVariableResolver? _variableResolver;
    private ICascadeResolver? _cascadeResolver;
    private IRuleCollector? _ruleCollector;
    private IInheritanceProcessor? _inheritanceProcessor;
    private IComputedStyleBuilder? _computedStyleBuilder;
    private IPropertyTreeManager? _propertyTreeManager;
    private IStyleCache? _styleCache;
    private bool _optimizationEnabled = true;
    private bool _collectMetrics = false;
    private bool _isDisposed = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new StyleEngine.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    public StyleEngine(IBrowsingContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();
        StyleFactory = new ComputedStyleFactory(this);

        // Add self as a computation observer for optimization
        AddComputationObserver(this);
    }

    #endregion

    #region IStyleEngine Implementation

    /// <inheritdoc />
    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_styleTreeResolver == null)
            throw new InvalidOperationException("StyleTreeResolver not set");

        var style = _styleTreeResolver.ResolveElementStyle(element, null, pseudoElement);

        // Notify observers
        foreach (var observer in _computationObservers.ToList())
        {
            observer.OnStyleComputed(element, style);
        }

        return style;
    }

    /// <inheritdoc />
    public void UpdateStyles(IElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        if (_styleTreeResolver == null)
            throw new InvalidOperationException("StyleTreeResolver not set");

        _styleTreeResolver.ResolveStylesForSubtree(root);

        // Notify observers
        foreach (var observer in _computationObservers.ToList())
        {
            observer.OnSubtreeStylesUpdated(root);
        }
    }

    /// <inheritdoc />
    public IStyleInvalidationTracker InvalidationTracker =>
        _invalidationTracker ?? throw new InvalidOperationException("InvalidationTracker not set");

    /// <inheritdoc />
    public IComputedStyleFactory StyleFactory { get; }

    /// <inheritdoc />
    public IBrowsingContext Context { get; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IStyleSheetManager StylesheetManager =>
        _stylesheetManager ?? throw new InvalidOperationException("StylesheetManager not set");

    /// <inheritdoc />
    public IRuleCollector RuleCollector =>
        _ruleCollector ?? throw new InvalidOperationException("RuleCollector not set");

    /// <inheritdoc />
    public ICascadeResolver CascadeResolver =>
        _cascadeResolver ?? throw new InvalidOperationException("CascadeResolver not set");

    /// <inheritdoc />
    public IInheritanceProcessor InheritanceProcessor =>
        _inheritanceProcessor ?? throw new InvalidOperationException("InheritanceProcessor not set");

    /// <inheritdoc />
    public IVariableResolver VariableResolver =>
        _variableResolver ?? throw new InvalidOperationException("VariableResolver not set");

    /// <inheritdoc />
    public IComputedStyleBuilder ComputedStyleBuilder =>
        _computedStyleBuilder ?? throw new InvalidOperationException("ComputedStyleBuilder not set");

    /// <inheritdoc />
    public IPropertyTreeManager PropertyTreeManager =>
        _propertyTreeManager ?? throw new InvalidOperationException("PropertyTreeManager not set");

    /// <inheritdoc />
    public bool OptimizationEnabled
    {
        get => _optimizationEnabled;
        set => _optimizationEnabled = value;
    }

    /// <inheritdoc />
    public bool CollectMetrics
    {
        get => _collectMetrics;
        set
        {
            if (value && !_collectMetrics && _propertyTreeManager != null)
            {
                _propertyTreeManager.ResetOptimizationMetrics();
            }
            _collectMetrics = value;
        }
    }

    /// <inheritdoc />
    public OptimizationMetrics GetOptimizationMetrics()
    {
        return PropertyTreeManager.GetOptimizationMetrics();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void AddComputationObserver(IStyleComputationObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        if (!_computationObservers.Contains(observer))
        {
            _computationObservers.Add(observer);
        }
    }

    /// <inheritdoc />
    public void RemoveComputationObserver(IStyleComputationObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        _computationObservers.Remove(observer);
    }

    /// <inheritdoc />
    public void OptimizeAllStyles()
    {
        if (!_optimizationEnabled || _styleCache == null || _propertyTreeManager == null)
            return;

        var treeNodes = new HashSet<IPropertyTreeNode>();

        // Collect unique property tree nodes
        foreach (var style in _styleCache.GetAllStyles())
        {
            if (style is ComputedStyle computedStyle)
            {
                treeNodes.Add(computedStyle.PropertyTreeNode);
            }
        }

        // Optimize each node
        foreach (var node in treeNodes)
        {
            _propertyTreeManager.OptimizeTree(node);
        }
    }

    #endregion

    #region Observer Pattern Methods

    /// <summary>
    /// Sets the style invalidation tracker.
    /// </summary>
    /// <param name="invalidationTracker">The tracker to use.</param>
    public void SetInvalidationTracker(IStyleInvalidationTracker invalidationTracker)
    {
        _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
    }

    /// <summary>
    /// Sets the style tree resolver.
    /// </summary>
    /// <param name="styleTreeResolver">The resolver to use.</param>
    public void SetStyleTreeResolver(IStyleTreeResolver styleTreeResolver)
    {
        _styleTreeResolver = styleTreeResolver ?? throw new ArgumentNullException(nameof(styleTreeResolver));
    }

    /// <summary>
    /// Sets the stylesheet manager.
    /// </summary>
    /// <param name="stylesheetManager">The manager to use.</param>
    public void SetStylesheetManager(IStyleSheetManager stylesheetManager)
    {
        _stylesheetManager = stylesheetManager ?? throw new ArgumentNullException(nameof(stylesheetManager));
        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    /// <summary>
    /// Sets the value calculator.
    /// </summary>
    /// <param name="valueCalculator">The calculator to use.</param>
    public void SetValueCalculator(IValueCalculator valueCalculator)
    {
        _valueCalculator = valueCalculator ?? throw new ArgumentNullException(nameof(valueCalculator));
    }

    /// <summary>
    /// Sets the variable resolver.
    /// </summary>
    /// <param name="variableResolver">The resolver to use.</param>
    public void SetVariableResolver(IVariableResolver variableResolver)
    {
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
    }

    /// <summary>
    /// Sets the cascade resolver.
    /// </summary>
    /// <param name="cascadeResolver">The resolver to use.</param>
    public void SetCascadeResolver(ICascadeResolver cascadeResolver)
    {
        _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
    }

    /// <summary>
    /// Sets the rule collector.
    /// </summary>
    /// <param name="ruleCollector">The collector to use.</param>
    public void SetRuleCollector(IRuleCollector ruleCollector)
    {
        _ruleCollector = ruleCollector ?? throw new ArgumentNullException(nameof(ruleCollector));
    }

    /// <summary>
    /// Sets the inheritance processor.
    /// </summary>
    /// <param name="inheritanceProcessor">The processor to use.</param>
    public void SetInheritanceProcessor(IInheritanceProcessor inheritanceProcessor)
    {
        _inheritanceProcessor = inheritanceProcessor ?? throw new ArgumentNullException(nameof(inheritanceProcessor));
    }

    /// <summary>
    /// Sets the computed style builder.
    /// </summary>
    /// <param name="computedStyleBuilder">The builder to use.</param>
    public void SetComputedStyleBuilder(IComputedStyleBuilder computedStyleBuilder)
    {
        _computedStyleBuilder = computedStyleBuilder ?? throw new ArgumentNullException(nameof(computedStyleBuilder));
    }

    /// <summary>
    /// Sets the property tree manager.
    /// </summary>
    /// <param name="propertyTreeManager">The manager to use.</param>
    public void SetPropertyTreeManager(IPropertyTreeManager propertyTreeManager)
    {
        _propertyTreeManager = propertyTreeManager ?? throw new ArgumentNullException(nameof(propertyTreeManager));
    }

    /// <summary>
    /// Sets the style cache.
    /// </summary>
    /// <param name="styleCache">The cache to use.</param>
    public void SetStyleCache(IStyleCache styleCache)
    {
        _styleCache = styleCache ?? throw new ArgumentNullException(nameof(styleCache));
    }

    #endregion

    #region IDocumentLifecycleObserver Implementation

    /// <inheritdoc />
    public void OnDocumentAttached(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        try
        {
            _stylesheetManager?.AttachToDocument(document);

            if (document.DocumentElement != null)
            {
                UpdateStyles(document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            // Log exceptions instead of letting them propagate
            Console.WriteLine($"Error attaching to document: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void OnDocumentDetached(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        try
        {
            _stylesheetManager?.DetachFromDocument(document);

            // Clear caches for this document
            _styleCache?.Clear();
            _ruleCollector?.ClearCache();

            if (_valueCalculator != null)
            {
                _valueCalculator.ClearCache();
            }

            if (_variableResolver != null && document.DocumentElement != null)
            {
                // Clear variables for all elements in this document
                ClearVariablesRecursively(document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detaching from document: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void OnDomUpdated(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        try
        {
            // This could be a viewport resize or other DOM change
            if (document.DocumentElement != null && _invalidationTracker != null)
            {
                _invalidationTracker.InvalidateElement(document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling DOM update: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void OnReadyStateChanged(IDocument document, DocumentReadyState readyState)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        try
        {
            if ((readyState == DocumentReadyState.Interactive ||
                 readyState == DocumentReadyState.Complete) &&
                document.DocumentElement != null)
            {
                UpdateStyles(document.DocumentElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling ready state change: {ex.Message}");
        }
    }

    #endregion

    #region IStyleComputationObserver Implementation

    /// <inheritdoc />
    public void OnStyleComputed(IElement element, IComputedStyle style)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (style == null)
            throw new ArgumentNullException(nameof(style));

        try
        {
            // For self-observation (optimization)
            if (_optimizationEnabled && style is ComputedStyle computedStyle &&
                _propertyTreeManager != null && computedStyle.PropertyTreeNode is PropertyTreeNode node)
            {
                _propertyTreeManager.OptimizeTree(node);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in style computation observer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void OnSubtreeStylesUpdated(IElement rootElement)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));

        try
        {
            // For self-observation (optimization)
            if (_optimizationEnabled)
            {
                OptimizeAllStyles();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in subtree styles observer: {ex.Message}");
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
        _styleCache?.Clear();

        if (_invalidationTracker != null)
        {
            _invalidationTracker.InvalidateForDeviceChange();
        }

        if (_valueCalculator != null)
        {
            _valueCalculator.ClearCache();
        }
    }

    private void ClearVariablesRecursively(IElement element)
    {
        if (_variableResolver == null)
            return;

        _variableResolver.ClearElementVariables(element);

        foreach (var child in element.Children)
        {
            ClearVariablesRecursively(child);
        }
    }

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        // When a stylesheet changes, we need to invalidate styles
        _styleCache?.Clear();

        if (Context.Active?.DocumentElement != null && _invalidationTracker != null)
        {
            _invalidationTracker.InvalidateElement(Context.Active.DocumentElement);
        }

        if (_ruleCollector != null)
        {
            _ruleCollector.ClearCache();
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        // Unhook events
        if (_stylesheetManager != null)
        {
            _stylesheetManager.StylesheetChanged -= StylesheetManager_StylesheetChanged;
            _stylesheetManager.Dispose();
        }

        // Clear references
        _computationObservers.Clear();
        _invalidationTracker = null;
        _styleTreeResolver = null;
        _stylesheetManager = null;
        _valueCalculator = null;
        _variableResolver = null;
        _cascadeResolver = null;
        _ruleCollector = null;
        _inheritanceProcessor = null;
        _computedStyleBuilder = null;
        _propertyTreeManager = null;
        _styleCache = null;

        _isDisposed = true;
    }

    #endregion
}