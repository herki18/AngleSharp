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

namespace AngleSharp.StyleSystem.Integration
{
    /// <summary>
    /// Main engine responsible for style computation and management.
    /// </summary>
    public class StyleEngine : IStyleEngine, IDocumentLifecycleObserver, IStyleComputationObserver, IDisposable
    {
        #region Fields
        private IRenderDevice _renderDevice;
        private readonly List<IStyleComputationObserver> _computationObservers = new();
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
        private bool _optimizationEnabled = true;
        private bool _collectMetrics = false;
        private bool _isDisposed = false;
        #endregion

        #region Constructor with Dependency Injection
        /// <summary>
        /// Creates a new StyleEngine with all dependencies injected.
        /// </summary>
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
            IComputedStyleFactory styleFactory)
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

            // Register event handlers
            _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;

            // Add self as observer
            AddComputationObserver(this);
        }
        #endregion

        #region IStyleEngine Implementation
        /// <inheritdoc />
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

            var styleTreeResolver = GetStyleTreeResolver();
            if (styleTreeResolver == null)
                throw new InvalidOperationException("StyleTreeResolver not available");

            styleTreeResolver.ResolveStylesForSubtree(root);

            foreach (var observer in _computationObservers.ToList())
            {
                observer.OnSubtreeStylesUpdated(root);
            }
        }

        /// <inheritdoc />
        public IBrowsingContext Context => _context;

        /// <inheritdoc />
        public IStyleInvalidationTracker InvalidationTracker => _invalidationTracker;

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
        public IStyleSheetManager StylesheetManager => _stylesheetManager;

        /// <inheritdoc />
        public IRuleCollector RuleCollector => _ruleCollector;

        /// <inheritdoc />
        public ICascadeResolver CascadeResolver => _cascadeResolver;

        /// <inheritdoc />
        public IInheritanceProcessor InheritanceProcessor => _inheritanceProcessor;

        /// <inheritdoc />
        public IVariableResolver VariableResolver => _variableResolver;

        /// <inheritdoc />
        public IComputedStyleBuilder ComputedStyleBuilder => _computedStyleBuilder;

        /// <inheritdoc />
        public IPropertyTreeManager PropertyTreeManager => _propertyTreeManager;

        /// <inheritdoc />
        public IComputedStyleFactory StyleFactory => _styleFactory;

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
                if (value && !_collectMetrics)
                {
                    _propertyTreeManager.ResetOptimizationMetrics();
                }
                _collectMetrics = value;
            }
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
        public OptimizationMetrics GetOptimizationMetrics()
        {
            return _propertyTreeManager.GetOptimizationMetrics();
        }

        /// <inheritdoc />
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

        #region IDocumentLifecycleObserver Implementation
        /// <inheritdoc />
        public void OnDocumentAttached(IDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            try
            {
                _stylesheetManager.AttachToDocument(document);
                if (document.DocumentElement != null)
                {
                    UpdateStyles(document.DocumentElement);
                }
            }
            catch (Exception ex)
            {
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
                _stylesheetManager.DetachFromDocument(document);
                _styleCache.Clear();
                _ruleCollector.ClearCache();
                _variableResolver.ClearElementVariables(document.DocumentElement);
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
                if (document.DocumentElement != null)
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
                if (_optimizationEnabled && style is ComputedStyle computedStyle)
                {
                    _propertyTreeManager.OptimizeTree(computedStyle.PropertyTreeNode);
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
            // In a fully DI environment, this would be injected
            // This method simulates the current resolution behavior until full migration
            return _context.GetService<IStyleTreeResolver>();
        }
        #endregion

        #region IDisposable Implementation
        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _stylesheetManager.StylesheetChanged -= StylesheetManager_StylesheetChanged;
            _computationObservers.Clear();

            (_stylesheetManager as IDisposable)?.Dispose();

            _isDisposed = true;
        }
        #endregion
    }
}