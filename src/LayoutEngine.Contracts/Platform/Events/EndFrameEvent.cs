namespace LayoutEngine.Contracts.Platform.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a frame ends.
/// </summary>
public class EndFrameEvent : EventBase
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
    /// Initializes a new instance of the <see cref="EndFrameEvent"/> class.
    /// </summary>
    /// <param name="frameNumber">The frame number.</param>
    /// <param name="timestamp">The frame timestamp.</param>
    public EndFrameEvent(long frameNumber, double frameTimestamp)
    {
        FrameNumber = frameNumber;
        FrameTimestamp = frameTimestamp;
    }
}