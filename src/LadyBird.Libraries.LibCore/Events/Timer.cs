// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Timer.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Timer.cpp

namespace LadyBird.Libraries.LibCore.Events;

using System;

/// <summary>
/// A timer that can fire events at regular intervals or after a single delay.
/// </summary>
public class Timer : EventReceiver
{
    private int _interval;
    private bool _singleShot;
    private bool _active;

    /// <summary>
    /// The interval in milliseconds between timer events.
    /// </summary>
    public int Interval
    {
        get => _interval;
        set
        {
            if (_interval != value)
            {
                _interval = value;
                if (_active)
                {
                    Stop();
                    Start();
                }
            }
        }
    }

    /// <summary>
    /// Whether the timer should fire only once (true) or repeatedly (false).
    /// </summary>
    public bool IsSingleShot
    {
        get => _singleShot;
        set => _singleShot = value;
    }

    /// <summary>
    /// Whether the timer is currently active.
    /// </summary>
    public bool IsActive => _active;

    /// <summary>
    /// Event fired when the timer expires.
    /// </summary>
    public event Action? Timeout;

    public Timer(EventReceiver? parent = null) : base(parent)
    {
        _interval = 1000; // Default 1 second
        _singleShot = false;
        _active = false;
    }

    public Timer(int intervalMs, EventReceiver? parent = null) : base(parent)
    {
        _interval = intervalMs;
        _singleShot = false;
        _active = false;
    }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    public void Start()
    {
        if (_active)
            return;

        _active = true;
        StartTimer(_interval, TimerShouldFireWhenNotVisible.No);
    }

    /// <summary>
    /// Starts the timer with a specific interval.
    /// </summary>
    public void Start(int intervalMs)
    {
        _interval = intervalMs;
        Start();
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void Stop()
    {
        if (!_active)
            return;

        _active = false;
        StopTimer();
    }

    /// <summary>
    /// Restarts the timer.
    /// </summary>
    public void Restart()
    {
        Stop();
        Start();
    }

    /// <summary>
    /// Restarts the timer with a new interval.
    /// </summary>
    public void Restart(int intervalMs)
    {
        Stop();
        Start(intervalMs);
    }

    protected override void TimerEvent(TimerEvent e)
    {
        if (_singleShot)
            Stop();

        Timeout?.Invoke();
    }

    /// <summary>
    /// Creates a single-shot timer that fires after the specified delay.
    /// </summary>
    public static Timer CreateSingleShot(int delayMs, Action callback, EventReceiver? parent = null)
    {
        var timer = new Timer(delayMs, parent)
        {
            IsSingleShot = true
        };
        timer.Timeout += callback;
        timer.Start();
        return timer;
    }

    public override void Dispose()
    {
        Stop();
        base.Dispose();
    }
}