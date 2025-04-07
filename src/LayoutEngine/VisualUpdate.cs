namespace LayoutEngine;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// Standard implementation of IVisualUpdate for scheduling visual updates.
/// </summary>
public class VisualUpdate : IVisualUpdate
{
    /// <summary>
    /// Gets the update ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the update type.
    /// </summary>
    public UpdateType Type { get; }

    /// <summary>
    /// Gets the element to update.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// Gets the properties that were changed.
    /// </summary>
    public IReadOnlyList<string> ChangedProperties { get; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Creates a new visual update for an element.
    /// </summary>
    /// <param name="type">The type of update.</param>
    /// <param name="element">The element to update.</param>
    /// <param name="changedProperties">The properties that were changed, if any.</param>
    public VisualUpdate(UpdateType type, IElement element, IReadOnlyList<string>? changedProperties = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        Id = Guid.NewGuid();
        Type = type;
        Element = element;
        ChangedProperties = changedProperties ?? Array.Empty<string>();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new visual update for the entire document.
    /// </summary>
    /// <param name="type">The type of update.</param>
    /// <param name="documentElement">The document element to update.</param>
    public static VisualUpdate CreateDocumentUpdate(UpdateType type, IElement documentElement)
    {
        return new VisualUpdate(type, documentElement);
    }

    /// <summary>
    /// Creates a new style update for an element.
    /// </summary>
    /// <param name="element">The element to update.</param>
    /// <param name="changedProperties">The style properties that changed.</param>
    public static VisualUpdate CreateStyleUpdate(IElement element, IReadOnlyList<string>? changedProperties = null)
    {
        return new VisualUpdate(UpdateType.Style, element, changedProperties);
    }

    /// <summary>
    /// Creates a new layout update for an element.
    /// </summary>
    /// <param name="element">The element to update.</param>
    public static VisualUpdate CreateLayoutUpdate(IElement element)
    {
        return new VisualUpdate(UpdateType.Layout, element);
    }

    /// <summary>
    /// Creates a new render update.
    /// </summary>
    /// <param name="element">The element to update, or the document element for a full render.</param>
    public static VisualUpdate CreateRenderUpdate(IElement element)
    {
        return new VisualUpdate(UpdateType.Render, element);
    }
}