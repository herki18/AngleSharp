using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Integration
{
    using Interfaces;

    public class DomMutationTracker : IDisposable
    {
        private readonly IStyleInvalidationTracker _invalidationTracker;
        private readonly MutationObserver _observer;
        private readonly IBrowsingContext _context;
        private IDocument? _currentDocument;
        private bool _isProcessingMutations;
        private readonly object _mutationLock = new object();

        public DomMutationTracker(IBrowsingContext context, IStyleInvalidationTracker invalidationTracker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
            _observer = new MutationObserver(HandleMutations);

            if (context.Active != null)
            {
                ConnectToDocument(context.Active);
            }

            // Instead of subscribing to a non-existent Navigated event,
            // rely on explicit document connections/disconnections through public methods
        }

        public void ConnectToDocument(IDocument document)
        {
            if (document == null)
                return;

            if (_currentDocument != null && _currentDocument != document)
            {
                DisconnectFromDocument(_currentDocument);
            }

            _currentDocument = document;
            try
            {
                if (document.DocumentElement != null)
                {
                    _observer.Connect(document.DocumentElement, childList: true, subtree: true, attributes: true);
                }
                if (document.Head != null)
                {
                    _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
                }
                document.ReadyStateChanged += Document_ReadyStateChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting mutation observer: {ex.Message}");
            }
        }

        private void Document_ReadyStateChanged(object? sender, Event ev)
        {
            var document = sender as IDocument;
            if (document?.ReadyState == DocumentReadyState.Interactive ||
                document?.ReadyState == DocumentReadyState.Complete)
            {
                if (document.Body != null)
                {
                    _observer.Connect(document.Body, childList: true, subtree: true, attributes: true);
                }
            }
        }

        public void DisconnectFromDocument(IDocument document)
        {
            if (document == null)
                return;

            _observer.Disconnect();
            document.ReadyStateChanged -= Document_ReadyStateChanged;
            if (_currentDocument == document)
            {
                _currentDocument = null;
            }
        }

        public void CheckForDocumentChange()
        {
            var activeDocument = _context.Active;
            if (activeDocument != null && activeDocument != _currentDocument)
            {
                ConnectToDocument(activeDocument);
            }
        }

        private void HandleMutations(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
        {
            lock (_mutationLock)
            {
                if (_isProcessingMutations)
                    return;
                _isProcessingMutations = true;
            }
            try
            {
                var domChanges = new List<DomChange>();
                foreach (var mutation in mutations)
                {
                    var changes = CategorizeMutation(mutation);
                    if (changes.Count > 0)
                    {
                        domChanges.AddRange(changes);
                    }
                }
                if (domChanges.Count > 0)
                {
                    _invalidationTracker.ProcessDomChanges(domChanges);
                }
            }
            finally
            {
                lock (_mutationLock)
                {
                    _isProcessingMutations = false;
                }
            }
        }

        private List<DomChange> CategorizeMutation(IMutationRecord mutation)
        {
            var changes = new List<DomChange>();
            switch (mutation.Type)
            {
                case "attributes":
                    changes.Add(CreateAttributeChange(mutation));
                    break;
                case "childList":
                    changes.AddRange(CreateChildListChanges(mutation));
                    break;
                case "characterData":
                    changes.Add(CreateCharacterDataChange(mutation));
                    break;
            }
            return changes;
        }

        private DomChange CreateAttributeChange(IMutationRecord mutation)
        {
            var element = mutation.Target as IElement;
            var attributeName = mutation.AttributeName;
            var attributeNamespace = mutation.AttributeNamespace;
            var oldValue = mutation.PreviousValue;
            var changeType = DomChangeType.AttributeChanged;

            if (attributeName == "style")
            {
                changeType = DomChangeType.StyleAttributeChanged;
            }
            else if (attributeName == "id")
            {
                changeType = DomChangeType.IdAttributeChanged;
            }
            else if (attributeName == "class")
            {
                changeType = DomChangeType.ClassAttributeChanged;
            }

            string? newValue = null;
            if (element != null && attributeName != null)
            {
                newValue = element.GetAttribute(attributeName);
            }

            return new DomChange
            {
                Type = changeType,
                Target = element,
                AttributeName = attributeName,
                AttributeNamespace = attributeNamespace,
                OldValue = oldValue,
                NewValue = newValue
            };
        }

        private List<DomChange> CreateChildListChanges(IMutationRecord mutation)
        {
            var changes = new List<DomChange>();
            var parentElement = mutation.Target as IElement;
            if (mutation.Added != null)
            {
                foreach (var node in mutation.Added)
                {
                    changes.Add(new DomChange
                    {
                        Type = DomChangeType.NodeAdded,
                        Target = parentElement,
                        Node = node,
                        PreviousSibling = node.PreviousSibling,
                        NextSibling = node.NextSibling
                    });
                    if (node is IElement)
                    {
                        changes.Add(new DomChange
                        {
                            Type = DomChangeType.ElementStructureChanged,
                            Target = parentElement
                        });
                    }
                }
            }
            if (mutation.Removed != null)
            {
                foreach (var node in mutation.Removed)
                {
                    changes.Add(new DomChange
                    {
                        Type = DomChangeType.NodeRemoved,
                        Target = parentElement,
                        Node = node,
                        PreviousSibling = mutation.PreviousSibling,
                        NextSibling = mutation.NextSibling
                    });
                    if (node is IElement)
                    {
                        changes.Add(new DomChange
                        {
                            Type = DomChangeType.ElementStructureChanged,
                            Target = parentElement
                        });
                    }
                }
            }
            return changes;
        }

        private DomChange CreateCharacterDataChange(IMutationRecord mutation)
        {
            return new DomChange
            {
                Type = DomChangeType.TextChanged,
                Target = mutation.Target as INode,
                OldValue = mutation.PreviousValue,
                NewValue = (mutation.Target as ICharacterData)?.Data
            };
        }

        public void Dispose()
        {
            if (_currentDocument != null)
            {
                DisconnectFromDocument(_currentDocument);
            }
            _observer.Disconnect();
        }
    }
}