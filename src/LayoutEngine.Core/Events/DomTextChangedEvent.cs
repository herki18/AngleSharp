namespace LayoutEngine.Core.Events;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event that is published when the text of a DOM text node is changed.
/// </summary>
public class DomTextChangedEvent : EventBase
{
    /// <summary>
    /// The text node whose content changed.
    /// </summary>
    public IText TextNode { get; }

    /// <summary>
    /// The old value of the text.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// The new value of the text.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomTextChangedEvent"/> class.
    /// </summary>
    public DomTextChangedEvent(IText textNode, string? oldValue, string? newValue)
    {
        TextNode = textNode ?? throw new ArgumentNullException(nameof(textNode));
        OldValue = oldValue;
        NewValue = newValue;
    }
}