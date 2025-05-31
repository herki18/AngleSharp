namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;

/// <summary>
/// Main interface for the style system, following BlinkNG's StyleEngine.
/// Responsible for style recalculation, invalidation, and computed style access.
/// In BlinkNG, this is the central coordinator for all style-related operations.
/// </summary>
public interface IStyleEngine
{
    /// <summary>
    /// Checks if any nodes in the document need style recalculation.
    /// In BlinkNG, this corresponds to NeedsStyleRecalc().
    /// </summary>
    bool NeedsStyleRecalc(IDocument document);

    /// <summary>
    /// Checks if the layout tree needs to be rebuilt.
    /// In BlinkNG, this is NeedsLayoutTreeRebuild().
    /// </summary>
    bool NeedsLayoutTreeRebuild(IDocument document);

    /// <summary>
    /// Performs style recalculation for all dirty nodes.
    /// In BlinkNG, this is RecalcStyle().
    /// </summary>
    void RecalcStyle(IDocument document);

    /// <summary>
    /// Updates both style and layout tree.
    /// In BlinkNG, this is UpdateStyleAndLayoutTree().
    /// </summary>
    void UpdateStyleAndLayoutTree(IDocument document);

    /// <summary>
    /// Gets the computed style for a given element.
    /// In BlinkNG, this would delegate to StyleResolver.
    /// </summary>
    ComputedStyle? GetComputedStyle(IElement element);

    /// <summary>
    /// Marks an element as needing style recalculation.
    /// In BlinkNG, this is part of the invalidation system.
    /// </summary>
    void SetNeedsStyleRecalc(IElement element, StyleChangeType changeType);

    /// <summary>
    /// Gets the StyleResolver instance.
    /// In BlinkNG, StyleEngine contains a StyleResolver.
    /// </summary>
    IStyleResolver StyleResolver { get; }
}