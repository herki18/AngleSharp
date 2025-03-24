namespace LayoutEngine.Contracts.Platform.Events;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when styles are invalidated.
/// </summary>
public class StyleInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the elements with invalidated styles.
    /// </summary>
    public IReadOnlyList<IElement> Elements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="elements">The elements with invalidated styles.</param>
    public StyleInvalidatedEvent(IReadOnlyList<IElement> elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }
}