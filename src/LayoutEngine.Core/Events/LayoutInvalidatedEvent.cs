namespace LayoutEngine.Core.Events;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when layout is invalidated.
/// </summary>
public class LayoutInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the elements with invalidated layout.
    /// </summary>
    public IReadOnlyList<IElement> Elements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="elements">The elements with invalidated layout.</param>
    public LayoutInvalidatedEvent(IReadOnlyList<IElement> elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }
}