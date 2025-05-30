namespace LayoutEngine.Core.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Layout.Public;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using LayoutEngine.Core.Style.Public;
using LayoutEngine.Core.Viewport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class Engine : IEngine, IDisposable
{
    // Properties from IEngine interface
    public IBrowsingContext BrowsingContext => _documentManager.BrowsingContext;
    public IDocument? Document => _documentManager.Document;
    public IStyleSystem StyleSystem => _styleSystem;
    public ILayoutSystem LayoutSystem => _layoutSystem;
    public IRenderSystem RenderSystem => _renderSystem;
    public DocumentLifecyclePhase CurrentPhase => _lifecycleCoordinator.CurrentPhase;

    private readonly ILogger<Engine> _logger;
    private readonly IDocumentManager _documentManager;
    private readonly IDomMutationTracker _mutationTracker;
    private readonly IStyleSystem _styleSystem;
    private readonly ILayoutSystem _layoutSystem;
    private readonly IRenderSystem _renderSystem;
    private ViewportManager _viewportManager;
    private DomScrollEventBridge _domScrollEventBridge;
    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly IEventAggregator _eventAggregator;
    private readonly LayoutEngineConfiguration _configuration;
    private bool _isDisposed;

    public Engine(
        IDocumentManager documentManager,
        IDomMutationTracker mutationTracker,
        IStyleSystem styleSystem,
        ILayoutSystem layoutSystem,
        IRenderSystem renderSystem,
        IDocumentLifecycleCoordinator lifecycleCoordinator,
        IEventAggregator eventAggregator,
        LayoutEngineConfiguration configuration,
        ViewportManager viewportManager,
        DomScrollEventBridge domScrollEventBridge,
        ILogger<Engine>? logger = null
    )
    {
        _logger = logger ?? NullLogger<Engine>.Instance;
        _documentManager = documentManager ?? throw new ArgumentNullException(nameof(documentManager));
        _mutationTracker = mutationTracker ?? throw new ArgumentNullException(nameof(mutationTracker));
        _styleSystem = styleSystem ?? throw new ArgumentNullException(nameof(styleSystem));
        _layoutSystem = layoutSystem ?? throw new ArgumentNullException(nameof(layoutSystem));
        _renderSystem = renderSystem ?? throw new ArgumentNullException(nameof(renderSystem));
        _lifecycleCoordinator = lifecycleCoordinator ?? throw new ArgumentNullException(nameof(lifecycleCoordinator));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _viewportManager = viewportManager;
        _domScrollEventBridge = domScrollEventBridge;

        SubscribeToEvents();
    }

    public async Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default)
    {
        var document = await _documentManager.OpenAsync(html, cancellation);
        _mutationTracker.TrackDocument(document);

        // Set initial invalidation flags for fresh document
        if (document.DocumentElement != null)
        {
            MarkAllElementsForInitialProcessing(document.DocumentElement);
        }

        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Process the initial document lifecycle
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        _styleSystem.ComputeDocumentStyles(document);

        // Continue processing until we reach a stable state
        ProcessLifecycle();

        _domScrollEventBridge.AttachDomListeners();

        _logger.LogInformation("Document initialized and rendered");
        return document;
    }

    /// <summary>
    /// Marks all elements in a fresh document as needing complete processing.
    /// </summary>
    private void MarkAllElementsForInitialProcessing(IElement element)
    {
        // Mark for style, layout, and paint
        element.SetNeedsStyleRecalc();
        element.SetNeedsLayout();
        element.SetNeedsPaintInvalidation();

        foreach (var child in element.Children.OfType<IElement>())
        {
            MarkAllElementsForInitialProcessing(child);
        }
    }

    public void Update(double deltaTime)
    {
        if (Document == null)
        {
            _logger.LogError("Document is null, cannot update engine");
            return;
        }

        try
        {
            // Process document lifecycle based on current phase
            ProcessLifecycle();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in engine update");
        }
    }

    /// <summary>
    /// Processes the document lifecycle based on the current phase
    /// </summary>
    private void ProcessLifecycle()
    {
        var root = Document?.DocumentElement;
        if (root == null) return;

        var currentPhase = _lifecycleCoordinator.CurrentPhase;
        _logger.LogDebug($"ProcessLifecycle - Current phase: {currentPhase}");

        switch (currentPhase)
        {
            case DocumentLifecyclePhase.Inactive:
                if (HasAnyInvalidation(root))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                }
                break;

            case DocumentLifecyclePhase.StyleClean:
                if (root.NeedsStyleRecalc() || root.ChildNeedsStyleRecalc())
                {
                    _logger.LogDebug("Entering style calculation phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                    _styleSystem.ComputeDocumentStyles(Document!);
                }
                else if (root.NeedsLayout() || root.ChildNeedsLayout())
                {
                    _logger.LogDebug("Skipping to layout phase (no style changes needed)");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
                }
                else if (HasPaintInvalidation(root))
                {
                    _logger.LogDebug("Skipping to render phase (no style/layout changes needed)");
                    // Must go through LayoutClean to reach RenderReady
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
                }
                break;

            case DocumentLifecyclePhase.LayoutClean:
                if (root.NeedsLayout() || root.ChildNeedsLayout())
                {
                    _logger.LogDebug("Entering layout calculation phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                    _layoutSystem.PerformLayout(Document!);
                }
                else if (HasPaintInvalidation(root))
                {
                    _logger.LogDebug("Transitioning to render ready phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                }
                else
                {
                    _logger.LogDebug("No invalidations remaining, staying in LayoutClean");
                }
                break;

            case DocumentLifecyclePhase.RenderReady:
                if (HasPaintInvalidation(root))
                {
                    _logger.LogDebug("Entering rendering phase - paint invalidation detected");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);

                    var fragmentTree = _layoutSystem.GetFragmentTree();
                    if (fragmentTree != null)
                    {
                        _logger.LogDebug($"Processing fragment tree for rendering - root fragment: {fragmentTree.RootFragment}");
                        _renderSystem.ProcessFragmentTree(fragmentTree);
                    }
                    else
                    {
                        _logger.LogWarning("Fragment tree is null, cannot render");
                        _lifecycleCoordinator.ExitPhase(DocumentLifecyclePhase.InRender);
                    }
                }
                else
                {
                    _logger.LogDebug("No paint invalidation in RenderReady, returning to StyleClean");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                }
                break;

            case DocumentLifecyclePhase.InStyleRecalc:
            case DocumentLifecyclePhase.InLayout:
            case DocumentLifecyclePhase.InRender:
                _logger.LogDebug($"In processing phase: {currentPhase}, waiting for completion event");
                break;

            case DocumentLifecyclePhase.Disposed:
                _logger.LogWarning("Attempted to process lifecycle on a disposed document");
                break;

            default:
                _logger.LogWarning($"Unknown document lifecycle phase: {currentPhase}");
                break;
        }
    }

    // Helper methods to check node flags
    private bool HasAnyInvalidation(IElement element)
    {
        return element.NeedsStyleRecalc() || element.ChildNeedsStyleRecalc() ||
               element.NeedsLayout() || element.ChildNeedsLayout() ||
               HasPaintInvalidation(element);
    }

    private bool HasPaintInvalidation(IElement element)
    {
        if (element.NeedsPaintInvalidation()) return true;
        return element.Children.OfType<IElement>().Any(HasPaintInvalidation);
    }

    /// <summary>
    /// Subscribes to relevant events for engine coordination
    /// </summary>
    private void SubscribeToEvents()
    {
        _eventAggregator.Subscribe<PhaseChangedEvent>(e =>
        {
            _logger.LogDebug($"Document lifecycle phase changed: {e.Phase} ({e.ChangeType})");
        });
    }

    /// <summary>
    /// Disposes the engine and releases any resources
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Dispose other resources
        (_lifecycleCoordinator as IDisposable)?.Dispose();
        (_mutationTracker as IDisposable)?.Dispose();
    }
}