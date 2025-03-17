using System;

namespace LayoutEngine.Contracts.Platform.Lifecycle
{
    /// <summary>
    /// Defines the possible states of the document lifecycle.
    /// </summary>
    public enum DocumentLifecycleState
    {
        /// <summary>
        /// Document is not active.
        /// </summary>
        Inactive,

        /// <summary>
        /// Style calculations are complete and no changes are pending.
        /// </summary>
        StyleClean,

        /// <summary>
        /// Style calculations are in progress.
        /// </summary>
        InStyleRecalc,

        /// <summary>
        /// Style calculations are needed.
        /// </summary>
        StyleDirty,

        /// <summary>
        /// Layout calculations are complete and no changes are pending.
        /// </summary>
        LayoutClean,

        /// <summary>
        /// Layout calculations are in progress.
        /// </summary>
        InLayout,

        /// <summary>
        /// Layout calculations are needed.
        /// </summary>
        LayoutDirty,

        /// <summary>
        /// Paint operations are complete and no changes are pending.
        /// </summary>
        PaintClean,

        /// <summary>
        /// Paint operations are in progress.
        /// </summary>
        InPaint,

        /// <summary>
        /// Paint operations are needed.
        /// </summary>
        PaintDirty,

        /// <summary>
        /// Document has been disposed.
        /// </summary>
        Disposed
    }

    /// <summary>
    /// Defines the phases in the document lifecycle.
    /// </summary>
    public enum DocumentLifecyclePhase
    {
        /// <summary>
        /// Style calculation phase.
        /// </summary>
        Style,

        /// <summary>
        /// Layout calculation phase.
        /// </summary>
        Layout,

        /// <summary>
        /// Paint phase.
        /// </summary>
        Paint
    }

    /// <summary>
    /// Coordinates the document lifecycle and phase transitions.
    /// </summary>
    public interface IDocumentLifecycleCoordinator
    {
        /// <summary>
        /// Gets the current state of the document lifecycle.
        /// </summary>
        DocumentLifecycleState CurrentState { get; }

        /// <summary>
        /// Determines whether the document is currently in the specified phase.
        /// </summary>
        /// <param name="phase">The phase to check.</param>
        /// <returns>true if in the specified phase; otherwise, false.</returns>
        bool IsInPhase(DocumentLifecyclePhase phase);

        /// <summary>
        /// Schedules a style update.
        /// </summary>
        void ScheduleStyleUpdate();

        /// <summary>
        /// Schedules a layout update.
        /// </summary>
        void ScheduleLayout();

        /// <summary>
        /// Schedules a paint update.
        /// </summary>
        void SchedulePaint();

        /// <summary>
        /// Enters the specified phase.
        /// </summary>
        /// <param name="phase">The phase to enter.</param>
        void EnterPhase(DocumentLifecyclePhase phase);

        /// <summary>
        /// Exits the specified phase.
        /// </summary>
        /// <param name="phase">The phase to exit.</param>
        /// <param name="hasChanges">Whether changes occurred during the phase.</param>
        void ExitPhase(DocumentLifecyclePhase phase, bool hasChanges);

        /// <summary>
        /// Shuts down the lifecycle coordinator.
        /// </summary>
        void Shutdown();
    }
}