namespace LayoutEngine.Core.Events;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Contracts.StyleSystem;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when styles have been computed for elements.
/// </summary>
public class StyleComputedEvent : EventBase
{
    /// <summary>
    /// Gets the elements for which styles were computed.
    /// </summary>
    public IReadOnlyList<IElement> Elements { get; }

    /// <summary>
    /// Gets the computed styles for the elements.
    /// </summary>
    public IReadOnlyDictionary<IElement, IComputedStyle> ComputedStyles { get; }

    /// <summary>
    /// Initializes a new instance of the StyleComputedEvent class.
    /// </summary>
    /// <param name="elements">The elements for which styles were computed.</param>
    /// <param name="computedStyles">The computed styles for the elements.</param>
    public StyleComputedEvent(
        IReadOnlyList<IElement> elements,
        IReadOnlyDictionary<IElement, IComputedStyle> computedStyles)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        ComputedStyles = computedStyles ?? throw new ArgumentNullException(nameof(computedStyles));
    }
}