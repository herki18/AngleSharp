// using System;
// using System.Collections.Generic;
// using AngleSharp.Dom;
// using LayoutEngine.Contracts.Platform.Dom;
// using LayoutEngine.Contracts.Platform.Dom.Abstractions;
// using LayoutEngine.Contracts.Platform.Events;
// using Infrastructure.EventAggregator.API.Aggregation;
//
// namespace LayoutEngine.Platform.DOM;
//
// /// <summary>
// /// Implementation of IDomMutationTracker that tracks DOM mutations and publishes corresponding events.
// /// Uses AngleSharp's mutation observer to track changes.
// /// </summary>
// public sealed class DomMutationTracker : IDomMutationTracker, IDisposable
// {
//     private readonly IEventAggregator _eventAggregator;
//     private readonly IElementAdapter _elementAdapter;
//     private readonly IMutationObserverFactory _mutationObserverFactory;
//     private readonly List<ISubscriptionToken> _subscriptions = new();
//     private IMutationObserver? _observer;
//     private IDocument? _document;
//     private bool _isTracking;
//     private bool _isDisposed;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="DomMutationTracker"/> class.
//     /// </summary>
//     /// <param name="eventAggregator">The event aggregator.</param>
//     /// <param name="elementAdapter">The element adapter.</param>
//     /// <param name="mutationObserverFactory">The mutation observer factory.</param>
//     public DomMutationTracker(
//         IEventAggregator eventAggregator,
//         IElementAdapter elementAdapter,
//         IMutationObserverFactory mutationObserverFactory)
//     {
//         _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
//         _elementAdapter = elementAdapter ?? throw new ArgumentNullException(nameof(elementAdapter));
//         _mutationObserverFactory = mutationObserverFactory ?? throw new ArgumentNullException(nameof(mutationObserverFactory));
//     }
//
//     /// <inheritdoc />
//     public void StartTracking(IDocument document)
//     {
//         ThrowIfDisposed();
//
//         if (_isTracking)
//         {
//             StopTracking();
//         }
//
//         _document = document ?? throw new ArgumentNullException(nameof(document));
//
//         // Create a new mutation observer with our callback
//         _observer = _mutationObserverFactory.Create(OnMutation);
//
//         // Set up the options for observation
//         var options = new MutationObserverInit
//         {
//             Attributes = true,
//             CharacterData = true,
//             ChildList = true,
//             Subtree = true,
//             AttributeOldValue = true,
//             CharacterDataOldValue = true
//         };
//
//         // Start observing the document
//         _observer.Observe(document, options);
//         _isTracking = true;
//     }
//
//     /// <inheritdoc />
//     public void StopTracking()
//     {
//         ThrowIfDisposed();
//
//         if (!_isTracking || _observer == null)
//             return;
//
//         _observer.Disconnect();
//         _observer = null;
//         _document = null;
//         _isTracking = false;
//     }
//
//     /// <inheritdoc />
//     public void SignalAttributeChanged(IElement element, string attributeName, string? oldValue, string? newValue)
//     {
//         ThrowIfDisposed();
//
//         if (element == null)
//             throw new ArgumentNullException(nameof(element));
//         if (string.IsNullOrEmpty(attributeName))
//             throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));
//
//         PublishAttributeChangedEvent(element, attributeName, oldValue, newValue);
//     }
//
//     /// <inheritdoc />
//     public void SignalNodeAdded(INode node, INode parent)
//     {
//         ThrowIfDisposed();
//
//         if (node == null)
//             throw new ArgumentNullException(nameof(node));
//         if (parent == null)
//             throw new ArgumentNullException(nameof(parent));
//
//         PublishNodeAddedEvent(node, parent);
//     }
//
//     /// <inheritdoc />
//     public void SignalNodeRemoved(INode node, INode parent)
//     {
//         ThrowIfDisposed();
//
//         if (node == null)
//             throw new ArgumentNullException(nameof(node));
//         if (parent == null)
//             throw new ArgumentNullException(nameof(parent));
//
//         PublishNodeRemovedEvent(node, parent);
//     }
//
//     /// <summary>
//     /// Handles mutation records from the mutation observer.
//     /// </summary>
//     /// <param name="mutations">The mutation records.</param>
//     private void OnMutation(IMutationRecord[] mutations)
//     {
//         if (_isDisposed || !_isTracking)
//             return;
//
//         foreach (var mutation in mutations)
//         {
//             ProcessMutation(mutation);
//         }
//     }
//
//     /// <summary>
//     /// Processes a single mutation record.
//     /// </summary>
//     /// <param name="mutation">The mutation record to process.</param>
//     private void ProcessMutation(IMutationRecord mutation)
//     {
//         switch (mutation.Type)
//         {
//             case "attributes":
//                 if (mutation.Target is IElement element)
//                 {
//                     PublishAttributeChangedEvent(
//                         element,
//                         mutation.AttributeName ?? string.Empty,
//                         mutation.PreviousValue,
//                         _elementAdapter.GetAttribute(element, mutation.AttributeName ?? string.Empty));
//                 }
//                 break;
//
//             case "childList":
//                 if (mutation.Added != null && mutation.Added.Length > 0)
//                 {
//                     foreach (var node in mutation.Added)
//                     {
//                         PublishNodeAddedEvent(node, mutation.Target);
//                     }
//                 }
//
//                 if (mutation.Removed != null && mutation.Removed.Length > 0)
//                 {
//                     foreach (var node in mutation.Removed)
//                     {
//                         PublishNodeRemovedEvent(node, mutation.Target);
//                     }
//                 }
//                 break;
//
//             case "characterData":
//                 if (mutation.Target is IText textNode)
//                 {
//                     PublishTextChangedEvent(textNode, mutation.PreviousValue, textNode.TextContent);
//                 }
//                 break;
//         }
//     }
//
//     /// <summary>
//     /// Publishes an attribute changed event.
//     /// </summary>
//     private void PublishAttributeChangedEvent(
//         IElement element,
//         string attributeName,
//         string? oldValue,
//         string? newValue)
//     {
//         _eventAggregator.Publish(new DomAttributeChangedEvent(
//             element, attributeName, oldValue, newValue));
//     }
//
//     /// <summary>
//     /// Publishes a node added event.
//     /// </summary>
//     private void PublishNodeAddedEvent(INode node, INode parent)
//     {
//         _eventAggregator.Publish(new DomNodeAddedEvent(node, parent));
//     }
//
//     /// <summary>
//     /// Publishes a node removed event.
//     /// </summary>
//     private void PublishNodeRemovedEvent(INode node, INode parent)
//     {
//         _eventAggregator.Publish(new DomNodeRemovedEvent(node, parent));
//     }
//
//     /// <summary>
//     /// Publishes a text changed event.
//     /// </summary>
//     private void PublishTextChangedEvent(IText textNode, string? oldValue, string? newValue)
//     {
//         _eventAggregator.Publish(new DomTextChangedEvent(textNode, oldValue, newValue));
//     }
//
//     /// <summary>
//     /// Throws an exception if this object has been disposed.
//     /// </summary>
//     private void ThrowIfDisposed()
//     {
//         if (_isDisposed)
//         {
//             throw new ObjectDisposedException(nameof(DomMutationTracker));
//         }
//     }
//
//     /// <inheritdoc />
//     public void Dispose()
//     {
//         if (_isDisposed)
//             return;
//
//         _isDisposed = true;
//         StopTracking();
//
//         foreach (var subscription in _subscriptions)
//         {
//             _eventAggregator.Unsubscribe(subscription);
//         }
//
//         _subscriptions.Clear();
//     }
// }