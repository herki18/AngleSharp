namespace LayoutEngine.Contracts.Platform.Abstractions;

using System;

/// <summary>
/// Interface for providing time, allowing control in tests
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current time in milliseconds
    /// </summary>
    double GetCurrentTimeMilliseconds();

    /// <summary>
    /// Gets the current UTC time
    /// </summary>
    DateTime GetUtcNow();
}