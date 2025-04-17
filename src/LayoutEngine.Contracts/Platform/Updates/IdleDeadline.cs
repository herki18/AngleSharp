namespace LayoutEngine.Contracts.Platform.Updates;

using System;

/// <summary>
/// Represents an idle deadline.
/// </summary>
public interface IdleDeadline
{
    /// <summary>
    /// Gets the time remaining in the idle period.
    /// </summary>
    TimeSpan TimeRemaining { get; }

    /// <summary>
    /// Gets whether the callback is being called because the timeout fired.
    /// </summary>
    bool DidTimeout { get; }
}