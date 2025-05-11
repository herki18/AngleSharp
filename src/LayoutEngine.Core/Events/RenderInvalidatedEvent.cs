namespace LayoutEngine.Core.Events;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when rendering is invalidated.
/// </summary>
public class RenderInvalidatedEvent : EventBase
{
    public IReadOnlyList<IElement> Elements { get; }

    public RenderInvalidatedEvent(IReadOnlyList<IElement> elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }
}