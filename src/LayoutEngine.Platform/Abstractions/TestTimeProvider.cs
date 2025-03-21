namespace LayoutEngine.Platform.Abstractions;

using System;
using LayoutEngine.Contracts.Platform.Abstractions;

/// <summary>
/// Time provider for tests that allows controlling time
/// </summary>
public class TestTimeProvider : ITimeProvider
{
    private double _currentTimeMs;
    private DateTime _currentUtcTime;

    /// <summary>
    /// Creates a new instance of TestTimeProvider
    /// </summary>
    public TestTimeProvider(double initialTimeMs = 0, DateTime? initialUtcTime = null)
    {
        _currentTimeMs = initialTimeMs;
        _currentUtcTime = initialUtcTime ?? new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets the current time in milliseconds
    /// </summary>
    public double GetCurrentTimeMilliseconds()
    {
        return _currentTimeMs;
    }

    /// <summary>
    /// Gets the current UTC time
    /// </summary>
    public DateTime GetUtcNow()
    {
        return _currentUtcTime;
    }

    /// <summary>
    /// Advances time by the specified milliseconds
    /// </summary>
    public void AdvanceTime(double milliseconds)
    {
        _currentTimeMs += milliseconds;
        _currentUtcTime = _currentUtcTime.AddMilliseconds(milliseconds);
    }

    /// <summary>
    /// Sets the current time
    /// </summary>
    public void SetCurrentTime(double milliseconds, DateTime? utcTime = null)
    {
        _currentTimeMs = milliseconds;
        if (utcTime.HasValue)
        {
            _currentUtcTime = utcTime.Value;
        }
    }
}