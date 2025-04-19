using System;

namespace LayoutEngine.Core;

using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public interface IEngine
{
    IBrowsingContext BrowsingContext { get; }
    IDocument? Document { get; }

    Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default);
}

public class Engine : IEngine
{
    public IBrowsingContext BrowsingContext => _documentManager.BrowsingContext;
    public IDocument? Document => _documentManager.Document;

    private readonly ILogger<Engine> _logger;
    private readonly IDocumentManager _documentManager;

    public Engine(
        IDocumentManager documentManager,
        ILogger<Engine>? logger = null
        )
    {
        _logger = logger ?? NullLogger<Engine>.Instance;
        _documentManager = documentManager;
    }

    public async Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default) => await _documentManager.OpenAsync(html, cancellation);
}

public interface IDocumentManager
{
    IBrowsingContext BrowsingContext { get; }
    IDocument? Document { get; }
    Task<IDocument> OpenAsync(String html, CancellationToken cancellation = default);
}

internal class DocumentManager : IDocumentManager
{
    public IBrowsingContext BrowsingContext { get; private set; }
    public IDocument? Document { get; private set; }

    private readonly ILogger<DocumentManager> _logger;


    public DocumentManager(
        IConfiguration configuration,
        ILogger<DocumentManager>? logger = null
        )
    {
        _logger = logger ?? NullLogger<DocumentManager>.Instance;;
        BrowsingContext = AngleSharp.BrowsingContext.New(configuration);
    }

    public async Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default)
    {
        _logger.LogInformation("Opening document with HTML content.");
        var document = await BrowsingContext.OpenAsync(req => req.Content(html), cancellation);
        Document = document;
        return document;
    }
}