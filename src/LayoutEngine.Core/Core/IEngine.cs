namespace LayoutEngine.Core.Core;

using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Style.Public;

public interface IEngine
{
    IBrowsingContext BrowsingContext { get; }
    IDocument? Document { get; }

    Task<IDocument> OpenAsync(string html, CancellationToken cancellation = default);

    // Access to systems
    IStyleSystem StyleSystem { get; }
    ILayoutSystem LayoutSystem { get; }

    // Current phase of the document lifecycle
    DocumentLifecyclePhase CurrentPhase { get; }

    void Update(double deltaTIme);
}