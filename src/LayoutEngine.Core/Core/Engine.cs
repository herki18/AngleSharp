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
        LayoutEngineConfiguration configuration, ViewportManager viewportManager, DomScrollEventBridge domScrollEventBridge, ILogger<Engine>? logger = null
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

        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);

        _styleSystem.ComputeDocumentStyles(document);

        // Let the event system and state machine handle the next transitions.
        // Do NOT force InLayout here.

        // Optionally, you can process the lifecycle to let it advance naturally:
        ProcessLifecycle();

        _domScrollEventBridge.AttachDomListeners();
        _logger.LogInformation("Document initialized and rendered");
        return document;
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

        switch (currentPhase)
        {
            case DocumentLifecyclePhase.Inactive:
                if (HasAnyInvalidation(root))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                }
                break;

            case DocumentLifecyclePhase.StyleClean:
                // NEW: Check node flags instead of assuming we need phases
                if (root.NeedsStyleRecalc() || root.ChildNeedsStyleRecalc())
                {
                    _logger.LogDebug("Entering style calculation phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                    _styleSystem.ComputeDocumentStyles(Document!);
                }
                else if (root.NeedsLayout() || root.ChildNeedsLayout())
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
                break;

            case DocumentLifecyclePhase.LayoutClean:
                if (HasPaintInvalidation(root))
                {
                    _logger.LogDebug("Transitioning to render ready phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                }
                break;

            case DocumentLifecyclePhase.RenderReady:
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.Rendering))
                {
                    _logger.LogDebug("Entering rendering phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                    var fragmentTree = _layoutSystem.GetFragmentTree();
                    _renderSystem.ProcessFragmentTree(fragmentTree);
                }
                break;

            // REMOVED: All dirty state cases (StyleDirty, LayoutDirty, RenderDirty)

            case DocumentLifecyclePhase.InStyleRecalc:
            case DocumentLifecyclePhase.InLayout:
            case DocumentLifecyclePhase.InRender:
                // These phases handle themselves via event completion
                break;

            case DocumentLifecyclePhase.Disposed:
                _logger.LogWarning("Attempted to process lifecycle on a disposed document");
                break;

            default:
                _logger.LogWarning($"Unknown document lifecycle phase: {currentPhase}");
                break;
        }
    }

    // NEW: Helper methods to check node flags
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

        // _eventAggregator.Subscribe<DomAttributeChangedEvent>(e =>
        // {
        //     if (e.AttributeName == "style" || e.AttributeName == "class")
        //     {
        //         _styleSystem.InvalidateStyle(e.Element);
        //     }
        // });
        //
        // _eventAggregator.Subscribe<DomNodeAddedEvent>(e =>
        // {
        //     if (e.Node is IElement addedElement)
        //     {
        //         _styleSystem.InvalidateStyle(addedElement);
        //     }
        //
        //     if (e.Parent is IElement parentElement)
        //     {
        //         _styleSystem.InvalidateStyle(parentElement);
        //     }
        // });
        //
        // _eventAggregator.Subscribe<DomNodeRemovedEvent>(e =>
        // {
        //     if (e.Parent is IElement parentElement)
        //     {
        //         _styleSystem.InvalidateStyle(parentElement);
        //     }
        // });
        //
        // // Subscribe to style invalidation events to cascade to layout
        // _eventAggregator.Subscribe<StyleInvalidatedEvent>(e =>
        // {
        //     foreach (var element in e.Elements)
        //     {
        //         _layoutSystem.InvalidateLayout(element);
        //     }
        // });
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