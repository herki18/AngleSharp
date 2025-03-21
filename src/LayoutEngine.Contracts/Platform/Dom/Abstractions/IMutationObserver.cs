namespace LayoutEngine.Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Interface for mutation observer to make testing easier
/// </summary>
public interface IMutationObserver
{
    /// <summary>
    /// Observes mutations on a target node
    /// </summary>
    /// <param name="target">DOM node to observe</param>
    /// <param name="options">Observation options</param>
    void Observe(IDomNode target, MutationObserverInit options);

    /// <summary>
    /// Stops observing mutations
    /// </summary>
    void Disconnect();
}