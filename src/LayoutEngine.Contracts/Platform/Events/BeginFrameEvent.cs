namespace LayoutEngine.Contracts.Platform.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a frame begins.
/// </summary>
public class BeginFrameEvent : EventBase
{
    /// <summary>
    /// Gets the frame number.
    /// </summary>
    public long FrameNumber { get; }

    /// <summary>
    /// Gets the frame timestamp.
    /// </summary>
    public double FrameTimestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeginFrameEvent"/> class.
    /// </summary>
    /// <param name="frameNumber">The frame number.</param>
    /// <param name="timestamp">The frame timestamp.</param>
    public BeginFrameEvent(long frameNumber, double frameTimestamp)
    {
        FrameNumber = frameNumber;
        FrameTimestamp = frameTimestamp;
    }
}