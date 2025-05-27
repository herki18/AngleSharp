namespace LayoutEngine.Core.Style;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when all stylesheets have been refreshed.
/// </summary>
public class StylesheetsRefreshedEvent : EventBase
{
    /// <summary>
    /// Gets the document to which the stylesheets belong.
    /// </summary>
    public IDocument Document { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StylesheetsRefreshedEvent"/> class.
    /// </summary>
    /// <param name="document">The document whose stylesheets were refreshed.</param>
    public StylesheetsRefreshedEvent(IDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }
}