// using AngleSharp.Dom;
// using AngleSharp.Html.Dom;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using System;
//
// namespace LayoutEngine
// {
//     /// <summary>
//     /// Represents a rendering context for a specific document.
//     /// </summary>
//     internal class DocumentRenderingContext : IDocumentRenderingContext, IDisposable
//     {
//         private readonly IDocument _document;
//         private readonly IStyleEngine _styleEngine;
//         private readonly ILayoutEngine _layoutEngine;
//         private readonly IRenderEngine _renderEngine;
//         private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
//         private readonly ILogger _logger;
//         private bool _isDisposed;
//
//         internal DocumentRenderingContext(IDocument document, IServiceProvider serviceProvider, ILogger logger)
//         {
//             _document = document;
//             _logger = logger;
//
//             // Get factories from DI
//             var styleEngineFactory = serviceProvider.GetRequiredService<IStyleEngineFactory>();
//             var layoutEngineFactory = serviceProvider.GetRequiredService<ILayoutEngineFactory>();
//             var renderEngineFactory = serviceProvider.GetRequiredService<IRenderEngineFactory>();
//
//             // Get lifecycle coordinator
//             _lifecycleCoordinator = serviceProvider.GetRequiredService<IDocumentLifecycleCoordinator>();
//
//             // Create engines for this document
//             _logger.LogDebug("Creating style engine for document");
//             _styleEngine = styleEngineFactory.CreateStyleEngine(document);
//
//             _logger.LogDebug("Creating layout engine for document");
//             _layoutEngine = layoutEngineFactory.CreateLayoutEngine(document);
//
//             _logger.LogDebug("Creating render engine for document");
//             _renderEngine = renderEngineFactory.CreateRenderEngine(document);
//
//             // Initialize the document lifecycle
//             _logger.LogDebug("Initializing document lifecycle");
//             _lifecycleCoordinator.Initialize(document);
//         }
//
//         /// <summary>
//         /// Gets the document associated with this context.
//         /// </summary>
//         public IDocument Document => _document;
//
//         /// <summary>
//         /// Forces a complete render cycle (style, layout, paint).
//         /// </summary>
//         public void ForceRender()
//         {
//             _logger.LogDebug("Forcing render cycle");
//             _lifecycleCoordinator.ProcessFullCycle(_document);
//         }
//
//         /// <summary>
//         /// Renders the document to the specified target.
//         /// </summary>
//         public void RenderTo(IRenderTarget target)
//         {
//             if (target == null)
//                 throw new ArgumentNullException(nameof(target));
//
//             _logger.LogDebug($"Rendering to target: {target.GetType().Name}");
//             _renderEngine.SetTarget(target);
//             ForceRender();
//         }
//
//         /// <summary>
//         /// Gets the computed style for an element.
//         /// </summary>
//         public IComputedStyle GetComputedStyle(IElement element)
//         {
//             if (element == null)
//                 throw new ArgumentNullException(nameof(element));
//
//             return _styleEngine.GetComputedStyle(element);
//         }
//
//         /// <summary>
//         /// Gets layout information for an element.
//         /// </summary>
//         public ILayoutInfo GetLayoutInfo(IElement element)
//         {
//             if (element == null)
//                 throw new ArgumentNullException(nameof(element));
//
//             return _layoutEngine.GetLayoutInfo(element);
//         }
//
//         public void Dispose()
//         {
//             if (_isDisposed)
//                 return;
//
//             _logger.LogDebug("Disposing document rendering context");
//
//             // Dispose engines
//             (_styleEngine as IDisposable)?.Dispose();
//             (_layoutEngine as IDisposable)?.Dispose();
//             (_renderEngine as IDisposable)?.Dispose();
//
//             // Shut down lifecycle for this document
//             _lifecycleCoordinator.Shutdown(_document);
//
//             _isDisposed = true;
//         }
//     }
// }