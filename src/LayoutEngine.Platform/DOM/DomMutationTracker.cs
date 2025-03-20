using System;
using System.Collections.Generic;

namespace LayoutEngine.Platform.DOM;

using Contracts.Platform.Dom;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Tracks DOM mutations and generates events for DOM changes.
/// Uses MutationObserver to detect changes in the DOM.
/// </summary>
public sealed class DomMutationTracker : IDomMutationTracker, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IElementAdapter _elementAdapter;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private MutationObserver? _observer;
    private IDocument? _document;
    private bool _isTracking;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomMutationTracker"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing DOM events.</param>
    /// <param name="elementAdapter">The element adapter for DOM element operations.</param>
    public DomMutationTracker(IEventAggregator eventAggregator, IElementAdapter elementAdapter)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _elementAdapter = elementAdapter ?? throw new ArgumentNullException(nameof(elementAdapter));
    }

    /// <summary>
    /// Starts tracking DOM mutations for the specified document.
    /// </summary>
    /// <param name="document">The document to track.</param>
    public void StartTracking(IDocument document)
    {
        ThrowIfDisposed();

        if (_isTracking)
        {
            StopTracking();
        }

        _document = document ?? throw new ArgumentNullException(nameof(document));

        // Configure mutation observer options
        var options = new MutationObserverInit
        {
            Attributes = true,
            CharacterData = true,
            ChildList = true,
            Subtree = true,
            AttributeOldValue = true,
            CharacterDataOldValue = true
        };

        // Create and start observer
        _observer = new MutationObserver(OnMutation);
        _observer.Observe(_document, options);
        _isTracking = true;
    }

    /// <summary>
    /// Stops tracking DOM mutations.
    /// </summary>
    public void StopTracking()
    {
        ThrowIfDisposed();

        if (!_isTracking || _observer == null)
            return;

        _observer.Disconnect();
        _observer = null;
        _document = null;
        _isTracking = false;
    }

    /// <summary>
    /// Manually signals an attribute change for an element.
    /// </summary>
    /// <param name="element">The element that changed.</param>
    /// <param name="attributeName">The name of the attribute that changed.</param>
    /// <param name="oldValue">The old attribute value.</param>
    /// <param name="newValue">The new attribute value.</param>
    public void SignalAttributeChanged(IElement element, string attributeName, string? oldValue, string? newValue)
    {
        ThrowIfDisposed();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (string.IsNullOrEmpty(attributeName))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));

        PublishAttributeChangedEvent(element, attributeName, oldValue, newValue);
    }

    /// <summary>
    /// Manually signals a node was added.
    /// </summary>
    /// <param name="node">The node that was added.</param>
    /// <param name="parent">The parent node.</param>
    public void SignalNodeAdded(IDomNode node, IDomNode parent)
    {
        ThrowIfDisposed();

        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        PublishNodeAddedEvent(node, parent);
    }

    /// <summary>
    /// Manually signals a node was removed.
    /// </summary>
    /// <param name="node">The node that was removed.</param>
    /// <param name="parent">The parent node.</param>
    public void SignalNodeRemoved(IDomNode node, IDomNode parent)
    {
        ThrowIfDisposed();

        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        PublishNodeRemovedEvent(node, parent);
    }

    /// <summary>
    /// Handles mutation records from the MutationObserver.
    /// </summary>
    /// <param name="mutations">The array of mutation records.</param>
    private void OnMutation(MutationRecord[] mutations)
    {
        if (_isDisposed || !_isTracking)
            return;

        // Process each mutation record
        foreach (var mutation in mutations)
        {
            ProcessMutation(mutation);
        }
    }

    /// <summary>
    /// Processes a single mutation record.
    /// </summary>
    /// <param name="mutation">The mutation record to process.</param>
    private void ProcessMutation(MutationRecord mutation)
    {
        switch (mutation.Type)
        {
            case "attributes":
                if (mutation.Target is IElement element)
                {
                    PublishAttributeChangedEvent(
                        element,
                        mutation.AttributeName,
                        mutation.OldValue,
                        _elementAdapter.GetAttribute(element, mutation.AttributeName));
                }
                break;

            case "childList":
                // Handle added nodes
                if (mutation.AddedNodes != null && mutation.AddedNodes.Length > 0)
                {
                    foreach (var node in mutation.AddedNodes)
                    {
                        PublishNodeAddedEvent(node, mutation.Target);
                    }
                }

                // Handle removed nodes
                if (mutation.RemovedNodes != null && mutation.RemovedNodes.Length > 0)
                {
                    foreach (var node in mutation.RemovedNodes)
                    {
                        PublishNodeRemovedEvent(node, mutation.Target);
                    }
                }
                break;

            case "characterData":
                if (mutation.Target is IText textNode)
                {
                    PublishTextChangedEvent(textNode, mutation.OldValue, textNode.Data);
                }
                break;
        }
    }

    /// <summary>
    /// Publishes a DomAttributeChangedEvent.
    /// </summary>
    private void PublishAttributeChangedEvent(
        IElement element,
        string attributeName,
        string? oldValue,
        string? newValue)
    {
        _eventAggregator.Publish(new DomAttributeChangedEvent(
            element, attributeName, oldValue, newValue));
    }

    /// <summary>
    /// Publishes a DomNodeAddedEvent.
    /// </summary>
    private void PublishNodeAddedEvent(IDomNode node, IDomNode parent)
    {
        _eventAggregator.Publish(new DomNodeAddedEvent(node, parent));
    }

    /// <summary>
    /// Publishes a DomNodeRemovedEvent.
    /// </summary>
    private void PublishNodeRemovedEvent(IDomNode node, IDomNode parent)
    {
        _eventAggregator.Publish(new DomNodeRemovedEvent(node, parent));
    }

    /// <summary>
    /// Publishes a DomTextChangedEvent.
    /// </summary>
    private void PublishTextChangedEvent(IText textNode, string? oldValue, string? newValue)
    {
        _eventAggregator.Publish(new DomTextChangedEvent(textNode, oldValue, newValue));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(DomMutationTracker));
        }
    }

    /// <summary>
    /// Disposes the DomMutationTracker and stops tracking.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        StopTracking();

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();
    }
}