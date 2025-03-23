namespace LayoutEngine.Contracts.LayoutSystem;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;
using Platform.Dom;

/// <summary>
/// Event published when the layout tree has been updated.
/// </summary>
public class LayoutUpdatedEvent : EventBase
{
    /// <summary>
    /// Gets the updated elements.
    /// </summary>
    public IReadOnlyList<IElement> UpdatedElements { get; }

    /// <summary>
    /// Gets the root of the layout tree.
    /// </summary>
    public ILayoutBox RootBox { get; }

    /// <summary>
    /// Gets the updated boxes for each element.
    /// </summary>
    public IReadOnlyDictionary<IElement, ILayoutBox> UpdatedBoxes { get; }

    /// <summary>
    /// Initializes a new instance of the LayoutUpdatedEvent class.
    /// </summary>
    /// <param name="updatedElements">The updated elements.</param>
    /// <param name="rootBox">The root of the layout tree.</param>
    /// <param name="updatedBoxes">The updated boxes for each element.</param>
    public LayoutUpdatedEvent(
        IReadOnlyList<IElement> updatedElements,
        ILayoutBox rootBox,
        IReadOnlyDictionary<IElement, ILayoutBox> updatedBoxes)
    {
        UpdatedElements = updatedElements ?? throw new ArgumentNullException(nameof(updatedElements));
        RootBox = rootBox ?? throw new ArgumentNullException(nameof(rootBox));
        UpdatedBoxes = updatedBoxes ?? throw new ArgumentNullException(nameof(updatedBoxes));
    }
}