namespace LayoutEngine.Core.Style;

using System;
using AngleSharp.Css.Dom;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when a stylesheet is added, removed, or modified in the system.
/// </summary>
public class StylesheetChangedEvent : EventBase
{
    /// <summary>
    /// Gets the stylesheet that was changed.
    /// </summary>
    public ICssStyleSheet Stylesheet { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public StylesheetChangeType ChangeType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StylesheetChangedEvent"/> class.
    /// </summary>
    /// <param name="stylesheet">The stylesheet that was changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public StylesheetChangedEvent(ICssStyleSheet stylesheet, StylesheetChangeType changeType)
    {
        Stylesheet = stylesheet ?? throw new ArgumentNullException(nameof(stylesheet));
        ChangeType = changeType;
    }
}