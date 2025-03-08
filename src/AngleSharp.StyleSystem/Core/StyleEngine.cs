namespace AngleSharp.StyleSystem.Core;

using System;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;
using Css;

/// <summary>
/// The main entry point for style computation in the LayoutEngine.
/// </summary>
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

    /// <summary>
    /// Creates a new StyleEngine instance.
    /// </summary>
    public StyleEngine(IBrowsingContext context)
    {
        _context = context;
        PropertyTreeManager = new PropertyTreeManager();
        _styleCache = new StyleCache();
        _stylesheetManager = new StyleSheetManager(context);

        _ruleCollector = new RuleCollector(context, _stylesheetManager);
        _cascadeResolver = new CascadeResolver(context);
        _inheritanceProcessor = new InheritanceProcessor(_context);
        _computedStyleBuilder = new ComputedStyleBuilder(this);
        _invalidationTracker = new StyleInvalidationTracker();
        StyleFactory = new ComputedStyleFactory(this);

        _renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();

        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    // Expose the stylesheet manager
    public StyleSheetManager StylesheetManager => _stylesheetManager;

    // Handle stylesheet changes
    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        // Clear the style cache
        _styleCache.Clear();

        // For now, invalidate the entire document when stylesheets change
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

            // If significant properties changed, invalidate dependent styles
            if (ShouldInvalidateStyles(oldDevice, _renderDevice))
            {
                InvalidateDeviceDependentStyles();
            }
        }
    }

    /// <summary>
        /// Notifies the style engine of a viewport size change.
        /// </summary>
        /// <param name="width">The new viewport width.</param>
        /// <param name="height">The new viewport height.</param>
        public void NotifyViewportChanged(int width, int height)
        {
            if (_renderDevice.ViewPortWidth != width || _renderDevice.ViewPortHeight != height)
            {
                // Update the render device
                _renderDevice.SetViewport(width, height);

                // Invalidate dependent styles
                InvalidateDeviceDependentStyles();
            }
        }

    private bool ShouldInvalidateStyles(IRenderDevice oldDevice, IRenderDevice newDevice)
    {
        // Check if properties that affect CSS calculations have changed
        return oldDevice.ViewPortWidth != newDevice.ViewPortWidth ||
               oldDevice.ViewPortHeight != newDevice.ViewPortHeight ||
               oldDevice.FontSize != newDevice.FontSize;
    }

    private void InvalidateDeviceDependentStyles()
    {
        // Clear the style cache to force recalculation
        _styleCache.Clear();

        // Inform the invalidation tracker about the device change
        if (_invalidationTracker is StyleInvalidationTracker tracker)
        {
            tracker.InvalidateForDeviceChange();
        }
    }

    IRenderDevice IStyleEngine.RenderDevice => RenderDevice;

    /// <summary>
    /// Gets the style invalidation tracker.
    /// </summary>
    public IStyleInvalidationTracker InvalidationTracker => _invalidationTracker;

    /// <summary>
    /// Gets the factory for creating computed style objects.
    /// </summary>
    public IComputedStyleFactory StyleFactory { get; }

    /// <summary>
    /// Gets the browsing context.
    /// </summary>
    public IBrowsingContext Context => _context;

    /// <summary>
    /// Gets the property tree manager.
    /// </summary>
    internal PropertyTreeManager PropertyTreeManager { get; }

    /// <summary>
    /// Computes the style for an element.
    /// </summary>
    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        // Check the style cache first
        var cacheKey = new StyleCacheKey(element, pseudoElement);
        if (_styleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        // Compute parent style first (if applicable)
        IComputedStyle? parentStyle = null;
        if (element.ParentElement != null)
        {
            parentStyle = ComputeElementStyle(element.ParentElement);
        }

        // Collect matching rules
        var matchedRules = _ruleCollector.CollectMatchingRules(element, pseudoElement);

        // Resolve the cascade to determine winning declarations
        var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Apply inheritance
        var inheritedStyle = _inheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);

        // Build final computed style
        var computedStyle = _computedStyleBuilder.BuildComputedStyle(inheritedStyle, element, parentStyle);

        // Cache the result

        if(computedStyle is null) throw new Exception("Computed style is null");

        _styleCache.Store(cacheKey, computedStyle);

        // Mark element as up-to-date in the invalidation tracker
        _invalidationTracker.MarkAsUpToDate(element);

        return computedStyle;
    }

    /// <summary>
    /// Updates styles after a change to the DOM or stylesheets.
    /// </summary>
    public void UpdateStyles(IElement root)
    {
        // First, update the invalidation state
        var elementsToUpdate = _invalidationTracker.GetElementsToUpdate(root);

        // Then update each element's style
        foreach (var element in elementsToUpdate)
        {
            // Remove from cache to force recomputation
            _styleCache.Remove(new StyleCacheKey(element, null));

            // Recompute style
            ComputeElementStyle(element);
        }
    }

    public void Dispose()
    {
        _stylesheetManager?.Dispose();
    }
}