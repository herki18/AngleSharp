namespace LayoutEngine.Core.Events;

using System;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when the fragment tree is updated.
/// </summary>
public class FragmentTreeUpdatedEvent : EventBase
{
    /// <summary>
    /// Gets the updated fragment tree.
    /// </summary>
    public object FragmentTree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentTreeUpdatedEvent"/> class.
    /// </summary>
    /// <param name="fragmentTree">The updated fragment tree.</param>
    public FragmentTreeUpdatedEvent(object fragmentTree)
    {
        FragmentTree = fragmentTree ?? throw new ArgumentNullException(nameof(fragmentTree));
    }
}