namespace LayoutEngine.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using DomAttributeChangedEvent = Events.DomAttributeChangedEvent;
using DomNodeAddedEvent = Events.DomNodeAddedEvent;
using DomNodeRemovedEvent = Events.DomNodeRemovedEvent;
using DomTextChangedEvent = Events.DomTextChangedEvent;
using LayoutInvalidatedEvent = Events.LayoutInvalidatedEvent;
using StyleInvalidatedEvent = Events.StyleInvalidatedEvent;

/// <summary>
/// Implements DOM mutation tracking using AngleSharp's MutationObserver.
/// </summary>
public sealed class DomMutationTracker : IDomMutationTracker
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationObserverFactory _observerFactory;
    private readonly Dictionary<Guid, TrackingInfo> _trackingInfoById = new();
    private readonly Dictionary<IElement, Guid> _trackingIdByElement = new();
    private bool _isDisposed;

    public DomMutationTracker(
        IEventAggregator eventAggregator,
        IMutationObserverFactory observerFactory)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _observerFactory = observerFactory ?? throw new ArgumentNullException(nameof(observerFactory));
    }

    /// <summary>
    /// Starts tracking mutations for the specified element with default options.
    /// </summary>
    public Guid TrackElement(IElement element)
    {
        return TrackElement(element, new MutationTrackerOptions());
    }

    /// <summary>
    /// Starts tracking mutations for the specified element with custom options.
    /// </summary>
    public Guid TrackElement(IElement element, MutationTrackerOptions options)
    {
        ThrowIfDisposed();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // If we're already tracking this element, return the existing tracking ID
        if (_trackingIdByElement.TryGetValue(element, out var existingId))
        {
            return existingId;
        }

        // Create a new observer for this element
        var observer = _observerFactory.Create(ProcessMutations);

        // Connect the observer to the element with the specified options
        observer.Connect(
            element,
            options.TrackChildList,
            options.TrackSubtree,
            options.TrackAttributes,
            options.TrackCharacterData,
            options.RecordAttributeOldValue,
            options.RecordCharacterDataOldValue,
            options.AttributeFilter);

        // Generate a tracking ID
        var trackingId = Guid.NewGuid();

        // Store tracking information
        var trackingInfo = new TrackingInfo
        {
            Element = element,
            Observer = observer,
            Options = options
        };

        _trackingInfoById[trackingId] = trackingInfo;
        _trackingIdByElement[element] = trackingId;

        return trackingId;
    }

    /// <summary>
    /// Starts tracking mutations for the document's document element with default options.
    /// </summary>
    public Guid TrackDocument(IDocument document)
    {
        return TrackDocument(document, new MutationTrackerOptions());
    }

    /// <summary>
    /// Starts tracking mutations for the document's document element with custom options.
    /// </summary>
    public Guid TrackDocument(IDocument document, MutationTrackerOptions options)
    {
        ThrowIfDisposed();

        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (document.DocumentElement == null)
            throw new InvalidOperationException("Document does not have a document element.");

        return TrackElement(document.DocumentElement, options);
    }

    /// <summary>
    /// Stops tracking the specified element.
    /// </summary>
    public void StopTracking(IElement element)
    {
        ThrowIfDisposed();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_trackingIdByElement.TryGetValue(element, out var trackingId))
        {
            StopTracking(trackingId);
        }
    }

    /// <summary>
    /// Stops tracking by tracking ID.
    /// </summary>
    public void StopTracking(Guid trackingId)
    {
        ThrowIfDisposed();

        if (_trackingInfoById.TryGetValue(trackingId, out var trackingInfo))
        {
            // Disconnect the observer
            trackingInfo.Observer.Disconnect();

            // Remove tracking info
            _trackingInfoById.Remove(trackingId);
            _trackingIdByElement.Remove(trackingInfo.Element);
        }
    }

    /// <summary>
    /// Stops all tracking.
    /// </summary>
    public void StopAllTracking()
    {
        ThrowIfDisposed();

        // Disconnect all observers
        foreach (var info in _trackingInfoById.Values)
        {
            info.Observer.Disconnect();
        }

        // Clear tracking info
        _trackingInfoById.Clear();
        _trackingIdByElement.Clear();
    }

    /// <summary>
    /// Processes mutations manually if needed.
    /// </summary>
    public void ProcessMutations(IMutationRecord[] mutations, IMutationObserver? observer = null)
    {
        ThrowIfDisposed();

        if (mutations == null || mutations.Length == 0)
            return;

        // Process each mutation record and publish appropriate events
        var attributeChanges = new HashSet<IElement>();
        var structureChanges = new HashSet<IElement>();
        var textChanges = new HashSet<IElement>();

        foreach (var mutation in mutations)
        {
            switch (mutation.Type)
            {
                case "attributes":
                    if (mutation.Target is IElement element && mutation.AttributeName != null)
                    {
                        // Publish attribute changed event
                        string? newValue = element.GetAttribute(mutation.AttributeName);

                        _eventAggregator.Publish(new DomAttributeChangedEvent(
                            element,
                            mutation.AttributeName,
                            mutation.PreviousValue,
                            newValue
                        ));

                        // Track elements with attribute changes
                        attributeChanges.Add(element);

                        // If the attribute affects layout, track it for layout changes too
                        if (IsLayoutAffectingAttribute(mutation.AttributeName))
                        {
                            structureChanges.Add(element);
                        }
                    }
                    break;

                case "characterData":
                    if (mutation.Target is ICharacterData charData &&
                        FindParentElement(mutation.Target) is IElement parentElement)
                    {
                        // Publish text content changed event
                        if (charData is IText textNode)
                        {
                            _eventAggregator.Publish(new DomTextChangedEvent(
                                textNode,
                                mutation.PreviousValue,
                                textNode.TextContent
                            ));
                        }

                        // Track elements with text changes
                        textChanges.Add(parentElement);

                        // Text changes often affect layout
                        structureChanges.Add(parentElement);
                    }
                    break;

                case "childList":
                    if (mutation.Target is IElement parentNode)
                    {
                        // Handle added nodes
                        if (mutation.Added != null && mutation.Added.Length > 0)
                        {
                            foreach (var addedNode in mutation.Added)
                            {
                                _eventAggregator.Publish(new DomNodeAddedEvent(
                                    addedNode,
                                    parentNode
                                ));
                            }
                        }

                        // Handle removed nodes
                        if (mutation.Removed != null && mutation.Removed.Length > 0)
                        {
                            foreach (var removedNode in mutation.Removed)
                            {
                                _eventAggregator.Publish(new DomNodeRemovedEvent(
                                    removedNode,
                                    parentNode
                                ));
                            }
                        }

                        // Track elements with structure changes
                        structureChanges.Add(parentNode);
                    }
                    break;
            }
        }

        // Convert HashSet to List for the event parameters
        if (attributeChanges.Count > 0)
        {
            _eventAggregator.Publish(new StyleInvalidatedEvent(attributeChanges.ToList()), EventPriority.High);
        }

        if (structureChanges.Count > 0)
        {
            _eventAggregator.Publish(new LayoutInvalidatedEvent(structureChanges.ToList()), EventPriority.High);
        }
    }

    /// <summary>
    /// Find the parent element of a node.
    /// </summary>
    private IElement? FindParentElement(INode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is IElement element)
            {
                return element;
            }
            parent = parent.Parent;
        }
        return null;
    }

    /// <summary>
    /// Determines if an attribute affects element layout.
    /// </summary>
    private bool IsLayoutAffectingAttribute(string attributeName)
    {
        return attributeName.Equals("style", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("class", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("width", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("height", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("display", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("position", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("margin", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("padding", StringComparison.OrdinalIgnoreCase) ||
               attributeName.StartsWith("margin-", StringComparison.OrdinalIgnoreCase) ||
               attributeName.StartsWith("padding-", StringComparison.OrdinalIgnoreCase);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(DomMutationTracker));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        StopAllTracking();
    }

    /// <summary>
    /// Class to hold information about a tracked element.
    /// </summary>
    private class TrackingInfo
    {
        public IElement Element { get; set; } = null!;
        public IMutationObserver Observer { get; set; } = null!;
        public MutationTrackerOptions Options { get; set; } = null!;
    }
}