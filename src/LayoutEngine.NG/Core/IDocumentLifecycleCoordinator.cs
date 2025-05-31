namespace LayoutEngine.NG.Core;

using System;

public interface IDocumentLifecycleCoordinator
{
    DocumentLifecyclePhase CurrentPhase { get; }
    bool CanAdvancePhase();
    void AdvancePhase();
    event Action<DocumentLifecyclePhase> PhaseChanged;
}