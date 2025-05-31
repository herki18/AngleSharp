namespace LayoutEngine.Core.LayoutStyle.Events;

using System;
using System.Collections.Generic;
using LayoutEngine.Core.LayoutNG.Public;
using LayoutEngine.Core.LayoutStyle.Public;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event published when styles have been resolved for the entire layout tree.
/// </summary>
public class LayoutTreeStylesResolvedEvent : EventBase
{
    /// <summary>
    /// Gets the computed styles for all layout objects.
    /// </summary>
    public IReadOnlyDictionary<ILayoutObject, ILayoutComputedStyle> ComputedStyles { get; }

    /// <summary>
    /// Gets the number of layout objects that were styled.
    /// </summary>
    public int LayoutObjectCount => ComputedStyles.Count;

    /// <summary>
    /// Gets the number of anonymous objects that were styled.
    /// </summary>
    public int AnonymousObjectCount { get; }

    public LayoutTreeStylesResolvedEvent(IReadOnlyDictionary<ILayoutObject, ILayoutComputedStyle> computedStyles)
    {
        ComputedStyles = computedStyles ?? throw new ArgumentNullException(nameof(computedStyles));

        // Count anonymous objects
        int anonymousCount = 0;
        foreach (var kvp in computedStyles)
        {
            if (kvp.Key.IsAnonymous)
            {
                anonymousCount++;
            }
        }
        AnonymousObjectCount = anonymousCount;
    }
}

/// <summary>
/// Event published when layout object styles are invalidated.
/// </summary>
public class LayoutStyleInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the layout objects with invalidated styles.
    /// </summary>
    public IReadOnlyList<ILayoutObject> InvalidatedObjects { get; }

    /// <summary>
    /// Gets whether the invalidation was recursive.
    /// </summary>
    public bool WasRecursive { get; }

    public LayoutStyleInvalidatedEvent(IReadOnlyList<ILayoutObject> invalidatedObjects, bool wasRecursive = true)
    {
        InvalidatedObjects = invalidatedObjects ?? throw new ArgumentNullException(nameof(invalidatedObjects));
        WasRecursive = wasRecursive;
    }
}

/// <summary>
/// Event published when a single layout object's style is computed.
/// </summary>
public class LayoutObjectStyleComputedEvent : EventBase
{
    /// <summary>
    /// Gets the layout object whose style was computed.
    /// </summary>
    public ILayoutObject LayoutObject { get; }

    /// <summary>
    /// Gets the computed style.
    /// </summary>
    public ILayoutComputedStyle ComputedStyle { get; }

    /// <summary>
    /// Gets whether this was a synthesized style for an anonymous object.
    /// </summary>
    public bool WasSynthesized { get; }

    public LayoutObjectStyleComputedEvent(
        ILayoutObject layoutObject,
        ILayoutComputedStyle computedStyle)
    {
        LayoutObject = layoutObject ?? throw new ArgumentNullException(nameof(layoutObject));
        ComputedStyle = computedStyle ?? throw new ArgumentNullException(nameof(computedStyle));
        WasSynthesized = computedStyle.IsSynthesized;
    }
}

/// <summary>
/// Event published when style synthesis occurs for anonymous objects.
/// </summary>
public class AnonymousStyleSynthesizedEvent : EventBase
{
    /// <summary>
    /// Gets the anonymous layout object.
    /// </summary>
    public ILayoutObject AnonymousObject { get; }

    /// <summary>
    /// Gets the parent style used for synthesis.
    /// </summary>
    public ILayoutComputedStyle ParentStyle { get; }

    /// <summary>
    /// Gets the synthesized style.
    /// </summary>
    public ILayoutComputedStyle SynthesizedStyle { get; }

    /// <summary>
    /// Gets the type of anonymous object.
    /// </summary>
    public LayoutObjectType AnonymousType { get; }

    public AnonymousStyleSynthesizedEvent(
        ILayoutObject anonymousObject,
        ILayoutComputedStyle parentStyle,
        ILayoutComputedStyle synthesizedStyle)
    {
        AnonymousObject = anonymousObject ?? throw new ArgumentNullException(nameof(anonymousObject));
        ParentStyle = parentStyle ?? throw new ArgumentNullException(nameof(parentStyle));
        SynthesizedStyle = synthesizedStyle ?? throw new ArgumentNullException(nameof(synthesizedStyle));
        AnonymousType = anonymousObject.Type;
    }
}