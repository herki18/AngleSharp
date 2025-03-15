namespace AngleSharp.StyleSystem.Integration;
using System;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Interfaces;

public sealed class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator
{
    private readonly IBrowsingContext _context;
    private readonly DomMutationTracker? _mutationTracker;
    private readonly IEventAggregator _eventAggregator;
    private IDocument? _currentDocument;
    private bool _disposed;

    public DocumentLifecycleCoordinator(
        IBrowsingContext context,
        IEventAggregator eventAggregator,
        DomMutationTracker? mutationTracker = null)
    {
        _context = context;
        _eventAggregator = eventAggregator;
        _mutationTracker = mutationTracker;

        if (context.Active != null)
        {
            AttachToDocument(context.Active);
        }
    }

    public IDocument? CurrentDocument => _currentDocument;

    public void AttachToDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (_currentDocument != null && _currentDocument != document)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;
        document.ReadyStateChanged += Document_ReadyStateChanged;
        document.Focused += Document_Focused;

        if (document.DefaultView != null)
        {
            document.DefaultView.Resized += Window_Resized;
            document.DefaultView.Scrolled += Window_Scrolled;
        }

        _mutationTracker?.ConnectToDocument(document);
        _eventAggregator.Publish(new DocumentAttachedEvent(document));

        if (document.ReadyState == DocumentReadyState.Interactive ||
            document.ReadyState == DocumentReadyState.Complete)
        {
            _eventAggregator.Publish(new ReadyStateChangedEvent(document, document.ReadyState));
        }
    }

    public void DetachFromDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        document.ReadyStateChanged -= Document_ReadyStateChanged;
        document.Focused -= Document_Focused;

        if (document.DefaultView != null)
        {
            document.DefaultView.Resized -= Window_Resized;
            document.DefaultView.Scrolled -= Window_Scrolled;
        }

        _mutationTracker?.DisconnectFromDocument(document);
        _eventAggregator.Publish(new DocumentDetachedEvent(document));

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
            AttachToDocument(activeDocument);
        }
    }

    public void NotifyDomUpdated(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        _eventAggregator.Publish(new DomUpdatedEvent(document));
    }

    public void NotifyViewportChanged(int width, int height)
    {
        if (_currentDocument == null)
            return;

        NotifyDomUpdated(_currentDocument);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_currentDocument != null)
            {
                DetachFromDocument(_currentDocument);
            }

            (_mutationTracker as IDisposable)?.Dispose();
        }

        _disposed = true;
    }

    private void Document_ReadyStateChanged(object? sender, Event ev)
    {
        var document = sender as IDocument;
        if (document != null)
        {
            _eventAggregator.Publish(new ReadyStateChangedEvent(document, document.ReadyState));

            if (document.ReadyState == DocumentReadyState.Interactive ||
                document.ReadyState == DocumentReadyState.Complete)
            {
                NotifyDomUpdated(document);
            }
        }
    }

    private void Document_Focused(object? sender, Event ev)
    {
        if (sender is IDocument document)
        {
            NotifyDomUpdated(document);
        }
    }

    private void Window_Resized(object? sender, Event e)
    {
        if (sender is IWindow window && _currentDocument != null)
        {
            NotifyViewportChanged(
                window.OuterWidth > 0 ? window.OuterWidth : 1024,
                window.OuterHeight > 0 ? window.OuterHeight : 768);
        }
    }

    private void Window_Scrolled(object? sender, Event e)
    {
        if (sender is IWindow && _currentDocument != null)
        {
            NotifyDomUpdated(_currentDocument);
        }
    }
}