namespace LayoutEngine.Contracts.Platform.Events;

using System;
using System.Collections.Generic;
using Dom;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Contracts.Resource;
using Lifecycle;
using Updates;

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

/// <summary>
/// Event raised when an element attribute changes.
/// </summary>
public class DomAttributeChangedEvent : EventBase
{
    /// <summary>
    /// Gets the DOM node that changed.
    /// </summary>
    public IDomNode Node { get; }

    /// <summary>
    /// Gets the name of the attribute that changed.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Gets the old value of the attribute.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new value of the attribute.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomAttributeChangedEvent"/> class.
    /// </summary>
    /// <param name="node">The DOM node that changed.</param>
    /// <param name="attributeName">The name of the attribute that changed.</param>
    /// <param name="oldValue">The old value of the attribute.</param>
    /// <param name="newValue">The new value of the attribute.</param>
    public DomAttributeChangedEvent(IDomNode node, string attributeName, string? oldValue, string? newValue)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Event raised when a node is added to the DOM.
/// </summary>
public class DomNodeAddedEvent : EventBase
{
    /// <summary>
    /// Gets the node that was added.
    /// </summary>
    public IDomNode Node { get; }

    /// <summary>
    /// Gets the parent node.
    /// </summary>
    public IDomNode Parent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomNodeAddedEvent"/> class.
    /// </summary>
    /// <param name="node">The node that was added.</param>
    /// <param name="parent">The parent node.</param>
    public DomNodeAddedEvent(IDomNode node, IDomNode parent)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }
}

/// <summary>
/// Event raised when a node is removed from the DOM.
/// </summary>
public class DomNodeRemovedEvent : EventBase
{
    /// <summary>
    /// Gets the node that was removed.
    /// </summary>
    public IDomNode Node { get; }

    /// <summary>
    /// Gets the parent node.
    /// </summary>
    public IDomNode Parent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomNodeRemovedEvent"/> class.
    /// </summary>
    /// <param name="node">The node that was removed.</param>
    /// <param name="parent">The parent node.</param>
    public DomNodeRemovedEvent(IDomNode node, IDomNode parent)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }
}

/// <summary>
/// Event raised when a text node changes.
/// </summary>
public class DomTextChangedEvent : EventBase
{
    /// <summary>
    /// Gets the text node that changed.
    /// </summary>
    public IText TextNode { get; }

    /// <summary>
    /// Gets the old value of the text.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new value of the text.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomTextChangedEvent"/> class.
    /// </summary>
    /// <param name="textNode">The text node that changed.</param>
    /// <param name="oldValue">The old value of the text.</param>
    /// <param name="newValue">The new value of the text.</param>
    public DomTextChangedEvent(IText textNode, string? oldValue, string? newValue)
    {
        TextNode = textNode ?? throw new ArgumentNullException(nameof(textNode));
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Event raised when the viewport changes.
/// </summary>
public class ViewportChangedEvent : EventBase
{
    /// <summary>
    /// Gets the old viewport size.
    /// </summary>
    public Size OldSize { get; }

    /// <summary>
    /// Gets the new viewport size.
    /// </summary>
    public Size NewSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewportChangedEvent"/> class.
    /// </summary>
    /// <param name="oldSize">The old viewport size.</param>
    /// <param name="newSize">The new viewport size.</param>
    public ViewportChangedEvent(Size oldSize, Size newSize)
    {
        OldSize = oldSize;
        NewSize = newSize;
    }
}

/// <summary>
/// Event raised when the device pixel ratio changes.
/// </summary>
public class DevicePixelRatioChangedEvent : EventBase
{
    /// <summary>
    /// Gets the old device pixel ratio.
    /// </summary>
    public double OldDevicePixelRatio { get; }

    /// <summary>
    /// Gets the new device pixel ratio.
    /// </summary>
    public double NewDevicePixelRatio { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePixelRatioChangedEvent"/> class.
    /// </summary>
    /// <param name="oldDevicePixelRatio">The old device pixel ratio.</param>
    /// <param name="newDevicePixelRatio">The new device pixel ratio.</param>
    public DevicePixelRatioChangedEvent(double oldDevicePixelRatio, double newDevicePixelRatio)
    {
        OldDevicePixelRatio = oldDevicePixelRatio;
        NewDevicePixelRatio = newDevicePixelRatio;
    }
}

/// <summary>
/// Event raised when styles are computed.
/// </summary>
public class StyleComputedEvent : EventBase
{
    /// <summary>
    /// Gets the element for which styles were computed.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// Gets the computed styles.
    /// </summary>
    public object ComputedStyle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleComputedEvent"/> class.
    /// </summary>
    /// <param name="element">The element for which styles were computed.</param>
    /// <param name="computedStyle">The computed styles.</param>
    public StyleComputedEvent(IElement element, object computedStyle)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        ComputedStyle = computedStyle ?? throw new ArgumentNullException(nameof(computedStyle));
    }
}

/// <summary>
/// Event raised when styles are invalidated.
/// </summary>
public class StyleInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the elements with invalidated styles.
    /// </summary>
    public IReadOnlyList<IElement> Elements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="elements">The elements with invalidated styles.</param>
    public StyleInvalidatedEvent(IReadOnlyList<IElement> elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }
}

/// <summary>
/// Event raised when layout is invalidated.
/// </summary>
public class LayoutInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the elements with invalidated layout.
    /// </summary>
    public IReadOnlyList<IElement> Elements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="elements">The elements with invalidated layout.</param>
    public LayoutInvalidatedEvent(IReadOnlyList<IElement> elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }
}

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

/// <summary>
/// Event raised when rendering is invalidated.
/// </summary>
public class RenderInvalidatedEvent : EventBase
{
    /// <summary>
    /// Gets the fragment tree to render.
    /// </summary>
    public object? FragmentTree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="fragmentTree">The fragment tree to render.</param>
    public RenderInvalidatedEvent(object? fragmentTree)
    {
        FragmentTree = fragmentTree;
    }
}

/// <summary>
/// Event raised when rendering is completed.
/// </summary>
public class RenderCompletedEvent : EventBase
{
    /// <summary>
    /// Gets information about the completed render.
    /// </summary>
    public object? RenderInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCompletedEvent"/> class.
    /// </summary>
    /// <param name="renderInfo">Information about the completed render.</param>
    public RenderCompletedEvent(object? renderInfo = null)
    {
        RenderInfo = renderInfo;
    }
}

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
    public double Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeginFrameEvent"/> class.
    /// </summary>
    /// <param name="frameNumber">The frame number.</param>
    /// <param name="timestamp">The frame timestamp.</param>
    public BeginFrameEvent(long frameNumber, double timestamp)
    {
        FrameNumber = frameNumber;
        Timestamp = timestamp;
    }
}

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
    public double Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndFrameEvent"/> class.
    /// </summary>
    /// <param name="frameNumber">The frame number.</param>
    /// <param name="timestamp">The frame timestamp.</param>
    public EndFrameEvent(long frameNumber, double timestamp)
    {
        FrameNumber = frameNumber;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when an update is processed.
/// </summary>
public class UpdateProcessedEvent : EventBase
{
    /// <summary>
    /// Gets the update that was processed.
    /// </summary>
    public IVisualUpdate Update { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProcessedEvent"/> class.
    /// </summary>
    /// <param name="update">The update that was processed.</param>
    public UpdateProcessedEvent(IVisualUpdate update)
    {
        Update = update ?? throw new ArgumentNullException(nameof(update));
    }
}

/// <summary>
/// Event raised when memory pressure is detected.
/// </summary>
public class MemoryPressureEvent : PrioritizedEventBase
{
    /// <summary>
    /// Gets the severity of memory pressure.
    /// </summary>
    public MemoryPressureSeverity Severity { get; }

    /// <summary>
    /// Gets the current memory usage.
    /// </summary>
    public long CurrentMemoryUsage { get; }

    /// <summary>
    /// Gets the memory usage threshold.
    /// </summary>
    public long MemoryThreshold { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPressureEvent"/> class.
    /// </summary>
    /// <param name="severity">The severity of memory pressure.</param>
    /// <param name="currentMemoryUsage">The current memory usage.</param>
    /// <param name="memoryThreshold">The memory usage threshold.</param>
    public MemoryPressureEvent(
        MemoryPressureSeverity severity,
        long currentMemoryUsage,
        long memoryThreshold)
        : base(EventPriority.Critical)
    {
        Severity = severity;
        CurrentMemoryUsage = currentMemoryUsage;
        MemoryThreshold = memoryThreshold;
    }
}

/// <summary>
/// Event raised when a resource is loading.
/// </summary>
public class ResourceLoadingEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadingEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    public ResourceLoadingEvent(string url)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
    }
}

/// <summary>
/// Event raised when a resource is loaded.
/// </summary>
public class ResourceLoadedEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the loaded resource.
    /// </summary>
    public IResource Resource { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadedEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="resource">The loaded resource.</param>
    public ResourceLoadedEvent(string url, IResource resource)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }
}

/// <summary>
/// Event raised when a resource fails to load.
/// </summary>
public class ResourceErrorEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the error that occurred.
    /// </summary>
    public Exception Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceErrorEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="error">The error that occurred.</param>
    public ResourceErrorEvent(string url, Exception error)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }
}