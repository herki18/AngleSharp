namespace LayoutEngine.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class Engine : IEngine
{
    public IBrowsingContext BrowsingContext => _documentManager.BrowsingContext;
    public IDocument? Document => _documentManager.Document;

    private readonly ILogger<Engine> _logger;
    private readonly IDocumentManager _documentManager;
    private readonly IDomMutationTracker _mutationTracker;

    public Engine(
        IDocumentManager documentManager,
        IDomMutationTracker mutationTracker,
        ILogger<Engine>? logger = null
    )
    {
        _logger = logger ?? NullLogger<Engine>.Instance;
        _documentManager = documentManager;
        _mutationTracker = mutationTracker;
    }

    public async Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default)
    {
        var document = await _documentManager.OpenAsync(html, cancellation);

        // Start tracking mutations for the loaded document
        _mutationTracker.TrackDocument(document);

        return document;
    }

    public void Update(Double deltaTime) {
        
    }
}