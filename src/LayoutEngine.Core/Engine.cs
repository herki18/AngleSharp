namespace LayoutEngine.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Layout;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Render;
using Style;
using PhaseChangedEvent = Events.PhaseChangedEvent;

public class Engine : IEngine
{
    public IBrowsingContext BrowsingContext => _documentManager.BrowsingContext;
    public IDocument? Document => _documentManager.Document;

    private readonly ILogger<Engine> _logger;
    private readonly IDocumentManager _documentManager;
    private readonly IDomMutationTracker _mutationTracker;

    private readonly IStyleSystem _styleSystem;
    private readonly ILayoutSystem _layoutSystem;
    private readonly IRenderSystem _renderSystem;

    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly IEventAggregator _eventAggregator;

    public Engine(
        IDocumentManager documentManager,
        IDomMutationTracker mutationTracker,
        StyleSystem styleSystem,
        ILayoutSystem layoutSystem,
        IDocumentLifecycleCoordinator lifecycleCoordinator,
        IEventAggregator eventAggregator, IRenderSystem renderSystem, ILogger<Engine>? logger = null
    )
    {
        _logger = logger ?? NullLogger<Engine>.Instance;
        _documentManager = documentManager;
        _mutationTracker = mutationTracker;
        _styleSystem = styleSystem;
        _layoutSystem = layoutSystem;
        _lifecycleCoordinator = lifecycleCoordinator;
        _eventAggregator = eventAggregator;
        _renderSystem = renderSystem;

        SubscribeToEvents();
    }

    public async Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default)
    {
        var document = await _documentManager.OpenAsync(html, cancellation);

        // Start tracking mutations for the loaded document
        _mutationTracker.TrackDocument(document);

        // Initialize the document lifecycle
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);

        // Perform initial style calculation
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        _styleSystem.ComputeDocumentStyles(document);

        // Perform initial layout
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
        var layoutResult = _layoutSystem.PerformLayout(document);

        // Perform initial render
        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
        var fragmentTree = _layoutSystem.GetFragmentTree();
        _renderSystem.ProcessFragmentTree(fragmentTree);

        _logger.LogInformation("Document initialized and rendered");

        return document;
    }

    public void Update(double deltaTime)
    {
        if (Document == null)
        {
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

    private void ProcessLifecycle()
    {
        var currentPhase = _lifecycleCoordinator.CurrentPhase;

        switch (currentPhase)
        {
            case DocumentLifecyclePhase.StyleDirty:
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.StyleCalculation))
                {
                    _logger.LogDebug("Entering style calculation phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                    _styleSystem.ComputeDocumentStyles(Document!);
                    // StyleComputedEvent will be published by the style system
                    // which will cause the lifecycle coordinator to exit InStyleRecalc
                }
                break;

            case DocumentLifecyclePhase.StyleClean:
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.LayoutCalculation))
                {
                    _logger.LogDebug("Entering layout calculation phase");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                    _layoutSystem.PerformLayout(Document!);
                    // FragmentTreeUpdatedEvent will be published by the layout system
                    // which will cause the lifecycle coordinator to exit InLayout
                }
                break;

            case DocumentLifecyclePhase.LayoutClean:
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.RenderPreparation))
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
                    // RenderCompletedEvent will be published by the render system
                    // which will cause the lifecycle coordinator to exit InRender
                }
                break;

            case DocumentLifecyclePhase.RenderDirty:
                if (_lifecycleCoordinator.IsOperationAllowed(DocumentOperation.Rendering))
                {
                    _logger.LogDebug("Rendering from dirty state");
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                    var fragmentTree = _layoutSystem.GetFragmentTree();
                }
                break;
        }
    }

    private void SubscribeToEvents()
    {
        // This is just a sample of what event subscriptions might look like
        // The actual implementation would depend on your event system

        // When a phase change occurs, log it
        _eventAggregator.Subscribe<PhaseChangedEvent>(e =>
        {
            _logger.LogDebug($"Document lifecycle phase changed: {e.Phase} ({e.ChangeType})");
        });

        // DOM mutations should invalidate styles
        _eventAggregator.Subscribe<DomAttributeChangedEvent>(e =>
        {
            if (e.AttributeName == "style" || e.AttributeName == "class")
            {
                _styleSystem.InvalidateStyle(e.Element);
            }
        });

        _eventAggregator.Subscribe<DomNodeAddedEvent>(e =>
        {
            if (e.Parent is IElement parentElement)
            {
                _styleSystem.InvalidateStyle(parentElement);
            }
        });

        _eventAggregator.Subscribe<DomNodeRemovedEvent>(e =>
        {
            if (e.Parent is IElement parentElement)
            {
                _styleSystem.InvalidateStyle(parentElement);
            }
        });
    }
}