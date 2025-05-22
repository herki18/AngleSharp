namespace LayoutEngine.Contracts.StyleSystem;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;
using Platform.Dom;

/// <summary>
/// Event published when styles have been computed for elements.
/// </summary>
public class StyleComputedEvent : EventBase
{
    public IReadOnlyList<IElement> Elements { get; }
    public IReadOnlyDictionary<IElement, IComputedStyle> ComputedStyles { get; }

    public StyleComputedEvent(
        IReadOnlyList<IElement> elements,
        IReadOnlyDictionary<IElement, IComputedStyle> computedStyles)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        ComputedStyles = computedStyles ?? throw new ArgumentNullException(nameof(computedStyles));
    }
}