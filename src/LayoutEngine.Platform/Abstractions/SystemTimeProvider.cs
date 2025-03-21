namespace LayoutEngine.Platform.Abstractions;

using System;
using System.Diagnostics;
using LayoutEngine.Contracts.Platform.Abstractions;

/// <summary>
/// Default implementation of time provider that uses real system time
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    private readonly Stopwatch _stopwatch = new Stopwatch();

    /// <summary>
    /// Creates a new instance of SystemTimeProvider
    /// </summary>
    public SystemTimeProvider()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// Gets the current time in milliseconds
    /// </summary>
    public double GetCurrentTimeMilliseconds()
    {
        return _stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Gets the current UTC time
    /// </summary>
    public DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
}