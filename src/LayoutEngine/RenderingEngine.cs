// namespace LayoutEngine;
//
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using AngleSharp;
// using AngleSharp.Dom;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
//
// /// <summary>
// /// The main entry point for the LayoutEngine rendering system.
// /// Manages document contexts and coordinates the rendering pipeline.
// /// </summary>
// public class RenderingEngine : IDisposable
// {
//     private readonly IServiceProvider _serviceProvider;
//     private readonly ILogger<RenderingEngine> _logger;
//     private readonly Dictionary<IDocument, DocumentRenderingContext> _documentContexts = new();
//
//     /// <summary>
//     /// Creates a new instance of the RenderingEngine.
//     /// </summary>
//     public RenderingEngine(IServiceProvider serviceProvider, ILogger<RenderingEngine> logger)
//     {
//         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//         _logger.LogInformation("RenderingEngine initialized");
//     }
//
//     /// <summary>
//     /// Creates and initializes a rendering context for the specified document.
//     /// </summary>
//     public IDocumentRenderingContext CreateContext(IDocument document)
//     {
//         if (document == null)
//             throw new ArgumentNullException(nameof(document));
//
//         if (_documentContexts.ContainsKey(document))
//             throw new InvalidOperationException("A rendering context already exists for this document");
//
//         _logger.LogInformation("Creating rendering context for document");
//         var context = new DocumentRenderingContext(document, _serviceProvider, _logger);
//         _documentContexts[document] = context;
//         return context;
//     }
//
//     /// <summary>
//     /// Closes and disposes the rendering context for the specified document.
//     /// </summary>
//     public void CloseContext(IDocument document)
//     {
//         if (document == null)
//             throw new ArgumentNullException(nameof(document));
//
//         if (_documentContexts.TryGetValue(document, out var context))
//         {
//             _logger.LogInformation("Closing rendering context for document");
//             context.Dispose();
//             _documentContexts.Remove(document);
//         }
//     }
//
//     /// <summary>
//     /// Creates a new document from HTML content and initializes a rendering context for it.
//     /// </summary>
//     public async Task<IDocumentRenderingContext> CreateFromHtmlAsync(string html)
//     {
//         if (string.IsNullOrEmpty(html))
//             throw new ArgumentException("HTML content cannot be null or empty", nameof(html));
//
//         _logger.LogInformation("Creating document from HTML");
//
//         // Get AngleSharp configuration
//         var angleSharpConfig = _serviceProvider.GetRequiredService<IConfiguration>();
//         var context = BrowsingContext.New(angleSharpConfig);
//
//         // Parse HTML into document
//         var document = await context.OpenAsync(req => req.Content(html));
//
//         // Create rendering context
//         return CreateContext(document);
//     }
//
//     /// <summary>
//     /// Gets an existing rendering context for the specified document.
//     /// </summary>
//     public IDocumentRenderingContext GetContext(IDocument document)
//     {
//         if (document == null)
//             throw new ArgumentNullException(nameof(document));
//
//         if (_documentContexts.TryGetValue(document, out var context))
//             return context;
//
//         throw new KeyNotFoundException("No rendering context exists for the specified document");
//     }
//
//     /// <summary>
//     /// Disposes all resources used by the RenderingEngine.
//     /// </summary>
//     public void Dispose()
//     {
//         _logger.LogInformation("Disposing RenderingEngine");
//
//         foreach (var context in _documentContexts.Values)
//         {
//             context.Dispose();
//         }
//         _documentContexts.Clear();
//     }
// }