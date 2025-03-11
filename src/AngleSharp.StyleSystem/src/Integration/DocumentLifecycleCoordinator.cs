namespace AngleSharp.StyleSystem.Integration;
using System;
using AngleSharp.Dom;

public class DocumentLifecycleCoordinator
{
    private readonly StyleEngine _styleEngine;
    private readonly DomMutationTracker? _mutationTracker;
    private IDocument? _currentDocument;

    public DocumentLifecycleCoordinator(IBrowsingContext context, StyleEngine styleEngine, DomMutationTracker? mutationTracker = null)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _mutationTracker = mutationTracker;

        if (context.Active != null)
        {
            AttachToDocument(context.Active);
        }
    }

    public void AttachToDocument(IDocument document)
    {
        if (_currentDocument != null && _currentDocument != document)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;
        _styleEngine.StylesheetManager.AttachToDocument(document);

        // Connect the mutation tracker if available
        _mutationTracker?.ConnectToDocument(document);

        // Initialize styles for the document
        if (document.DocumentElement != null)
        {
            _styleEngine.UpdateStyles(document.DocumentElement);
        }
    }

    public void DetachFromDocument(IDocument document)
    {
        // Disconnect the mutation tracker if available
        _mutationTracker?.DisconnectFromDocument(document);

        _styleEngine.StylesheetManager.DetachFromDocument(document);

        if (_currentDocument == document)
        {
            _currentDocument = null;
        }
    }

    public void CheckForDocumentChange(IBrowsingContext context)
    {
        var activeDocument = context.Active;
        if (activeDocument != null && activeDocument != _currentDocument)
        {
            AttachToDocument(activeDocument);
        }
    }
}