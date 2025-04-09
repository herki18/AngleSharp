// namespace AngleSharp.StyleSystem.Integration;
// using System;
// using System.Collections.Generic;
// using AngleSharp.Dom;
// using AngleSharp.Dom.Events;
// using AngleSharp.StyleSystem.Events;
// using AngleSharp.StyleSystem.Models;
//
// /// <summary>
// /// Tracks DOM mutations that may affect styles and publishes them as events.
// /// </summary>
// public class DomMutationTracker : IDisposable
// {
//     private readonly IEventAggregator _eventAggregator;
//     private readonly MutationObserver _observer;
//     private readonly IBrowsingContext _context;
//     private IDocument? _currentDocument;
//     private bool _isProcessingMutations;
//     private readonly object _mutationLock = new object();
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="DomMutationTracker"/> class.
//     /// </summary>
//     /// <param name="context">The browsing context.</param>
//     /// <param name="eventAggregator">The event aggregator.</param>
//     public DomMutationTracker(
//         IBrowsingContext context,
//         IEventAggregator eventAggregator)
//     {
//         _context = context ?? throw new ArgumentNullException(nameof(context));
//         _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
//         _observer = new MutationObserver(HandleMutations);
//
//         if (context.Active != null)
//         {
//             ConnectToDocument(context.Active);
//         }
//     }
//
//     /// <summary>
//     /// Connects the mutation tracker to a document.
//     /// </summary>
//     /// <param name="document">The document to track.</param>
//     public void ConnectToDocument(IDocument document)
//     {
//         if (document == null)
//             return;
//
//         if (_currentDocument != null && _currentDocument != document)
//         {
//             DisconnectFromDocument(_currentDocument);
//         }
//
//         _currentDocument = document;
//
//         try
//         {
//             if (document.DocumentElement != null)
//             {
//                 _observer.Connect(document.DocumentElement, childList: true, subtree: true, attributes: true);
//             }
//
//             if (document.Head != null)
//             {
//                 _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
//             }
//
//             document.ReadyStateChanged += Document_ReadyStateChanged;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Error connecting mutation observer: {ex.Message}");
//         }
//     }
//
//     private void Document_ReadyStateChanged(object? sender, Event ev)
//     {
//         var document = sender as IDocument;
//         if (document?.ReadyState == DocumentReadyState.Interactive ||
//             document?.ReadyState == DocumentReadyState.Complete)
//         {
//             if (document.Body != null)
//             {
//                 _observer.Connect(document.Body, childList: true, subtree: true, attributes: true);
//             }
//         }
//     }
//
//     /// <summary>
//     /// Disconnects the mutation tracker from a document.
//     /// </summary>
//     /// <param name="document">The document to stop tracking.</param>
//     public void DisconnectFromDocument(IDocument document)
//     {
//         if (document == null)
//             return;
//
//         _observer.Disconnect();
//         document.ReadyStateChanged -= Document_ReadyStateChanged;
//
//         if (_currentDocument == document)
//         {
//             _currentDocument = null;
//         }
//     }
//
//     /// <summary>
//     /// Checks if the active document has changed and updates tracking accordingly.
//     /// </summary>
//     public void CheckForDocumentChange()
//     {
//         var activeDocument = _context.Active;
//         if (activeDocument != null && activeDocument != _currentDocument)
//         {
//             ConnectToDocument(activeDocument);
//         }
//     }
//
//     private void HandleMutations(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
//     {
//         lock (_mutationLock)
//         {
//             if (_isProcessingMutations)
//                 return;
//
//             _isProcessingMutations = true;
//         }
//
//         try
//         {
//             var domChanges = new List<DomChange>();
//
//             foreach (var mutation in mutations)
//             {
//                 var changes = CategorizeMutation(mutation);
//                 if (changes.Count > 0)
//                 {
//                     domChanges.AddRange(changes);
//                 }
//             }
//
//             if (domChanges.Count > 0)
//             {
//                 _eventAggregator.Publish(new DomChangesEvent(domChanges));
//             }
//         }
//         finally
//         {
//             lock (_mutationLock)
//             {
//                 _isProcessingMutations = false;
//             }
//         }
//     }
//
//     private List<DomChange> CategorizeMutation(IMutationRecord mutation)
//     {
//         var changes = new List<DomChange>();
//
//         switch (mutation.Type)
//         {
//             case "attributes":
//                 changes.Add(CreateAttributeChange(mutation));
//                 break;
//
//             case "childList":
//                 changes.AddRange(CreateChildListChanges(mutation));
//                 break;
//
//             case "characterData":
//                 changes.Add(CreateCharacterDataChange(mutation));
//                 break;
//         }
//
//         return changes;
//     }
//
//     private DomChange CreateAttributeChange(IMutationRecord mutation)
//     {
//         var element = mutation.Target as IElement;
//         var attributeName = mutation.AttributeName;
//         var attributeNamespace = mutation.AttributeNamespace;
//         var oldValue = mutation.PreviousValue;
//         var changeType = DomChangeType.AttributeChanged;
//
//         if (attributeName == "style")
//         {
//             changeType = DomChangeType.StyleAttributeChanged;
//         }
//         else if (attributeName == "id")
//         {
//             changeType = DomChangeType.IdAttributeChanged;
//         }
//         else if (attributeName == "class")
//         {
//             changeType = DomChangeType.ClassAttributeChanged;
//         }
//
//         string? newValue = null;
//         if (element != null && attributeName != null)
//         {
//             newValue = element.GetAttribute(attributeName);
//         }
//
//         return new DomChange
//         {
//             Type = changeType,
//             Target = element,
//             AttributeName = attributeName,
//             AttributeNamespace = attributeNamespace,
//             OldValue = oldValue,
//             NewValue = newValue
//         };
//     }
//
//     private List<DomChange> CreateChildListChanges(IMutationRecord mutation)
//     {
//         var changes = new List<DomChange>();
//         var parentElement = mutation.Target as IElement;
//
//         if (mutation.Added != null)
//         {
//             foreach (var node in mutation.Added)
//             {
//                 changes.Add(new DomChange
//                 {
//                     Type = DomChangeType.NodeAdded,
//                     Target = parentElement,
//                     Node = node,
//                     PreviousSibling = node.PreviousSibling,
//                     NextSibling = node.NextSibling
//                 });
//
//                 if (node is IElement)
//                 {
//                     changes.Add(new DomChange
//                     {
//                         Type = DomChangeType.ElementStructureChanged,
//                         Target = parentElement
//                     });
//                 }
//             }
//         }
//
//         if (mutation.Removed != null)
//         {
//             foreach (var node in mutation.Removed)
//             {
//                 changes.Add(new DomChange
//                 {
//                     Type = DomChangeType.NodeRemoved,
//                     Target = parentElement,
//                     Node = node,
//                     PreviousSibling = mutation.PreviousSibling,
//                     NextSibling = mutation.NextSibling
//                 });
//
//                 if (node is IElement)
//                 {
//                     changes.Add(new DomChange
//                     {
//                         Type = DomChangeType.ElementStructureChanged,
//                         Target = parentElement
//                     });
//                 }
//             }
//         }
//
//         return changes;
//     }
//
//     private DomChange CreateCharacterDataChange(IMutationRecord mutation)
//     {
//         return new DomChange
//         {
//             Type = DomChangeType.TextChanged,
//             Target = mutation.Target as INode,
//             OldValue = mutation.PreviousValue,
//             NewValue = (mutation.Target as ICharacterData)?.Data
//         };
//     }
//
//     /// <summary>
//     /// Disposes the mutation tracker and stops tracking.
//     /// </summary>
//     public void Dispose()
//     {
//         if (_currentDocument != null)
//         {
//             DisconnectFromDocument(_currentDocument);
//         }
//
//         _observer.Disconnect();
//     }
// }