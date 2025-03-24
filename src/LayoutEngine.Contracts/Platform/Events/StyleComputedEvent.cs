namespace LayoutEngine.Contracts.Platform.Events;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when styles are computed.
/// </summary>
public class StyleComputedEvent : EventBase
{
    /// <summary>
    /// Gets the element for which styles were computed.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// Gets the computed styles.
    /// </summary>
    public object ComputedStyle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleComputedEvent"/> class.
    /// </summary>
    /// <param name="element">The element for which styles were computed.</param>
    /// <param name="computedStyle">The computed styles.</param>
    public StyleComputedEvent(IElement element, object computedStyle)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        ComputedStyle = computedStyle ?? throw new ArgumentNullException(nameof(computedStyle));
    }
}