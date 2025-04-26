namespace LayoutEngine.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

public interface IDocumentManager
{
    IBrowsingContext BrowsingContext { get; }
    IDocument? Document { get; }
    Task<IDocument> OpenAsync(String html, CancellationToken cancellation = default);
}