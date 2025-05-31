namespace LayoutEngine.NG.Core;

using System.Collections.Generic;

/// <summary>
/// Responsible for scheduling and updating all active frames on the main thread.
/// Follows BlinkNG's main thread scheduling architecture.
/// </summary>
public class MainThreadScheduler
{
    private readonly List<Frame> _activeFrames = new();

    /// <summary>
    /// Processes all active frames, advancing their lifecycles.
    /// In BlinkNG, this is part of the main thread's lifecycle update.
    /// </summary>
    public void Update()
    {
        // Process each frame in order
        // In BlinkNG, frames are processed based on priority and throttling
        foreach (var frame in _activeFrames)
        {
            frame.Scheduler.Update();
        }
    }

    /// <summary>
    /// Registers a frame with the scheduler.
    /// </summary>
    public void RegisterFrame(Frame frame)
    {
        if (!_activeFrames.Contains(frame))
        {
            _activeFrames.Add(frame);
        }
    }

    /// <summary>
    /// Unregisters a frame from the scheduler.
    /// </summary>
    public void UnregisterFrame(Frame frame)
    {
        _activeFrames.Remove(frame);
    }

    /// <summary>
    /// Gets all active frames managed by this scheduler.
    /// </summary>
    public IReadOnlyList<Frame> ActiveFrames => _activeFrames.AsReadOnly();
}