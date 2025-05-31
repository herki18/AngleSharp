namespace LayoutEngine.NG.Core;

/// <summary>
/// The main orchestrator of the browser engine, following BlinkNG conventions.
/// This is analogous to Blink's main thread coordinator.
/// </summary>
public class Engine
{
    private readonly MainThreadScheduler _scheduler;

    public Engine()
    {
        _scheduler = new MainThreadScheduler();
    }

    /// <summary>
    /// Advances the engine by one step, processing all frames.
    /// In BlinkNG, this would be called as part of the main thread's run loop.
    /// </summary>
    public void Update()
    {
        _scheduler.Update();
    }

    /// <summary>
    /// Gets the main thread scheduler for frame management.
    /// </summary>
    public MainThreadScheduler Scheduler => _scheduler;
}