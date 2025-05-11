namespace LayoutEngine.Core.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a document lifecycle phase changes.
/// </summary>
public class PhaseChangedEvent : PrioritizedEventBase
{
    /// <summary>
    /// Gets the phase of the document lifecycle.
    /// </summary>
    public DocumentLifecyclePhase Phase { get; }

    /// <summary>
    /// Gets the type of phase change.
    /// </summary>
    public PhaseChangeType ChangeType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhaseChangedEvent"/> class.
    /// </summary>
    /// <param name="phase">The phase of the document lifecycle.</param>
    /// <param name="changeType">The type of phase change.</param>
    public PhaseChangedEvent(DocumentLifecyclePhase phase, PhaseChangeType changeType)
        : base(EventPriority.High)
    {
        Phase = phase;
        ChangeType = changeType;
    }
}