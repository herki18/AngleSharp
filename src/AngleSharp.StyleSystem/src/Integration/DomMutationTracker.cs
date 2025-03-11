using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Integration
{
    using Interfaces;

    /// <summary>
    /// Tracks DOM mutations and reports them to the style invalidation system.
    /// Following Blink architecture, this component does not make invalidation decisions,
    /// but rather categorizes and forwards mutations to the StyleInvalidationTracker.
    /// </summary>
    public class DomMutationTracker : IDisposable
    {
        private readonly IStyleInvalidationTracker _invalidationTracker;
        private readonly MutationObserver _observer;
        private readonly IBrowsingContext _context;
        private IDocument? _currentDocument;
        private bool _isProcessingMutations;
        private readonly object _mutationLock = new object();

        /// <summary>
        /// Creates a new DOM mutation tracker.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <param name="invalidationTracker">The style invalidation tracker.</param>
        public DomMutationTracker(IBrowsingContext context, IStyleInvalidationTracker invalidationTracker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
            _observer = new MutationObserver(HandleMutations);

            // Connect to the current document if available
            if (context.Active != null)
            {
                ConnectToDocument(context.Active);
            }

            // Listen for document changes
            context.Navigated += (sender, e) =>
            {
                if (_currentDocument != null)
                {
                    DisconnectFromDocument(_currentDocument);
                }

                if (e.NewDocument != null)
                {
                    ConnectToDocument(e.NewDocument);
                }
            };
        }

        /// <summary>
        /// Connects the mutation observer to a document.
        /// </summary>
        /// <param name="document">The document to observe.</param>
        public void ConnectToDocument(IDocument document)
        {
            if (document == null)
                return;

            _currentDocument = document;

            try
            {
                // Observe the document element
                if (document.DocumentElement != null)
                {
                    _observer.Connect(document.DocumentElement, childList: true, subtree: true, attributes: true);
                }

                // Observe the head element specifically for style changes
                if (document.Head != null)
                {
                    _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
                }

                // Add handler for DOM ready state changes
                document.ReadyStateChanged += Document_ReadyStateChanged;
            }
            catch (Exception ex)
            {
                // Log or handle initialization errors
                Console.WriteLine($"Error connecting mutation observer: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles document ready state changes.
        /// </summary>
        private void Document_ReadyStateChanged(object? sender, Event ev)
        {
            var document = sender as IDocument;
            if (document?.ReadyState == DocumentReadyState.Interactive ||
                document?.ReadyState == DocumentReadyState.Complete)
            {
                // Ensure we're observing the body once it becomes available
                if (document.Body != null)
                {
                    _observer.Connect(document.Body, childList: true, subtree: true, attributes: true);
                }
            }
        }

        /// <summary>
        /// Disconnects the mutation observer from a document.
        /// </summary>
        /// <param name="document">The document to disconnect from.</param>
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

        /// <summary>
        /// Handles mutation records from the observer.
        /// </summary>
        private void HandleMutations(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
        {
            // Use a lock to prevent concurrent processing of mutations
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
                    // Categorize the mutation into a DomChange
                    var changes = CategorizeMutation(mutation);
                    if (changes.Count > 0)
                    {
                        domChanges.AddRange(changes);
                    }
                }

                // Forward the collected changes to the StyleInvalidationTracker
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

        /// <summary>
        /// Categorizes a mutation into one or more DomChange objects.
        /// </summary>
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

        /// <summary>
        /// Creates a DomChange object for an attribute mutation.
        /// </summary>
        private DomChange CreateAttributeChange(IMutationRecord mutation)
        {
            var element = mutation.Target as IElement;
            var attributeName = mutation.AttributeName;
            var attributeNamespace = mutation.AttributeNamespace;
            var oldValue = mutation.PreviousValue;

            var changeType = DomChangeType.AttributeChanged;

            // Identify special attributes that need specific handling
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

            return new DomChange
            {
                Type = changeType,
                Target = element,
                AttributeName = attributeName,
                AttributeNamespace = attributeNamespace,
                OldValue = oldValue,
                NewValue = element?.GetAttribute(attributeName)
            };
        }

        /// <summary>
        /// Creates DomChange objects for a childList mutation.
        /// </summary>
        private List<DomChange> CreateChildListChanges(IMutationRecord mutation)
        {
            var changes = new List<DomChange>();
            var parentElement = mutation.Target as IElement;

            // Handle added nodes
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

                    // If this is an element, also check for structural changes
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

            // Handle removed nodes
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

                    // If this is an element, also check for structural changes
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

        /// <summary>
        /// Creates a DomChange object for a characterData mutation.
        /// </summary>
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

        /// <summary>
        /// Disposes the mutation tracker and releases resources.
        /// </summary>
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