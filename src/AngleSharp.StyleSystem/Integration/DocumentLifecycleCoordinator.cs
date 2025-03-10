namespace AngleSharp.StyleSystem.Core;

using Dom;

public class DocumentLifecycleCoordinator
{
    private readonly StyleEngine _styleEngine;
    private IDocument? _currentDocument;

    public DocumentLifecycleCoordinator(IBrowsingContext context, StyleEngine styleEngine)
    {
        _styleEngine = styleEngine;

        // Initialize with current document if any
        if (context.Active != null)
        {
            AttachToDocument(context.Active);
        }
    }

    public void AttachToDocument(IDocument document)
    {
        // Detach from old document
        if (_currentDocument != null)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;

        // Attach stylesheet manager to the document
        _styleEngine.StylesheetManager.AttachToDocument(document);

        // Perform initial style calculation
        _styleEngine.UpdateStyles(document.DocumentElement);
    }

    public void DetachFromDocument(IDocument document)
    {
        _styleEngine.StylesheetManager.DetachFromDocument(document);

        if (_currentDocument == document)
        {
            _currentDocument = null;
        }
    }

    // Other lifecycle methods...
}