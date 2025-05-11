namespace LayoutEngine.Core.Events;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event that is published when a DOM attribute is changed.
/// </summary>
public class DomAttributeChangedEvent : EventBase
{
    /// <summary>
    /// The element whose attribute changed.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// The name of the attribute that changed.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// The old value of the attribute.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// The new value of the attribute.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomAttributeChangedEvent"/> class.
    /// </summary>
    public DomAttributeChangedEvent(IElement element, string attributeName, string? oldValue, string? newValue)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        OldValue = oldValue;
        NewValue = newValue;
    }
}