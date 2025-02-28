#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
namespace AngleSharp.LayoutEngine.FormattingContexts;

using System.Collections.Generic;
using AngleSharp.LayoutEngine.Core;

/// <summary>
/// Defines the interface for a formatting context, which determines how elements are laid out
/// within a specific region of the document. Formatting contexts are a fundamental concept in CSS
/// where each context manages its own internal layout rules and provides isolation for layout behaviors.
/// </summary>
public interface IFormattingContext
{
    /// <summary>
    /// Gets the layout node that establishes this formatting context.
    /// </summary>
    LayoutNode EstablishingNode { get; }

    /// <summary>
    /// Performs a complete layout of all elements within this formatting context.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    void Layout(LayoutContext context);

    /// <summary>
    /// Performs an incremental update of the layout for only the affected portions
    /// of this formatting context, typically used when a subset of elements has changed.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    void Reflow(LayoutContext context);

    /// <summary>
    /// Determines if the specified node is a participant in this formatting context.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node participates in this formatting context, otherwise false.</returns>
    bool ContainsNode(LayoutNode node);

    /// <summary>
    /// Gets all nodes participating in this formatting context.
    /// </summary>
    /// <returns>An enumerable collection of layout nodes.</returns>
    IEnumerable<LayoutNode> GetParticipants();

    /// <summary>
    /// Gets all child formatting contexts nested within this one.
    /// </summary>
    /// <returns>An enumerable collection of formatting contexts.</returns>
    IEnumerable<IFormattingContext> GetChildFormattingContexts();

    /// <summary>
    /// Adds a child formatting context to this formatting context.
    /// </summary>
    /// <param name="childContext">The child formatting context to add.</param>
    void AddChildContext(IFormattingContext childContext);
}