namespace LayoutEngine.Contracts.StyleSystem;

using System;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when a stylesheet is added or removed.
/// </summary>
public class StyleSheetChangedEvent : EventBase
{
    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public StyleSheetChangeType ChangeType { get; }

    /// <summary>
    /// Gets the ID of the stylesheet.
    /// </summary>
    public string StyleSheetId { get; }

    /// <summary>
    /// Initializes a new instance of the StyleSheetChangedEvent class.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="styleSheetId">The ID of the stylesheet.</param>
    public StyleSheetChangedEvent(StyleSheetChangeType changeType, string styleSheetId)
    {
        ChangeType = changeType;
        StyleSheetId = styleSheetId ?? throw new ArgumentNullException(nameof(styleSheetId));
    }
}