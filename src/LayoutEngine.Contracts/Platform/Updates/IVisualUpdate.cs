namespace LayoutEngine.Contracts.Platform.Updates;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Represents a visual update to be scheduled.
/// </summary>
public interface IVisualUpdate
{
    /// <summary>
    /// Gets the update ID.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the update type.
    /// </summary>
    UpdateType Type { get; }

    /// <summary>
    /// Gets the element to update.
    /// </summary>
    IElement Element { get; }

    /// <summary>
    /// Gets the properties that were changed.
    /// </summary>
    IReadOnlyList<string> ChangedProperties { get; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    DateTime Timestamp { get; }
}