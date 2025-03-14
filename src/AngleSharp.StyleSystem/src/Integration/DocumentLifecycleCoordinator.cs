namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Observers;

/// <summary>
/// Coordinates document lifecycle with the style system using the observer pattern.
/// </summary>
public sealed class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator
{
    private readonly IBrowsingContext _context;
    private readonly DomMutationTracker? _mutationTracker;
    private readonly List<IDocumentLifecycleObserver> _observers = new();
    private IDocument? _currentDocument;
    private bool _disposed;

    /// <summary>
    /// Creates a new DocumentLifecycleCoordinator.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <param name="mutationTracker">Optional DOM mutation tracker.</param>
    public DocumentLifecycleCoordinator(
        IBrowsingContext context,
        DomMutationTracker? mutationTracker = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mutationTracker = mutationTracker;

        // Automatically attach to the active document if there is one
        if (context.Active != null)
        {
            AttachToDocument(context.Active);
        }

        // Subscribe to context events
        // _context.Navigated += Context_Navigated;
    }

    /// <inheritdoc />
    public IDocument? CurrentDocument => _currentDocument;

    /// <inheritdoc />
    public void AddObserver(IDocumentLifecycleObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        lock (_observers)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);

                // If we already have a document, notify the new observer
                if (_currentDocument != null)
                {
                    observer.OnDocumentAttached(_currentDocument);
                }
            }
        }
    }

    /// <inheritdoc />
    public void RemoveObserver(IDocumentLifecycleObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        lock (_observers)
        {
            _observers.Remove(observer);
        }
    }

    /// <inheritdoc />
    public void AttachToDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (_currentDocument != null && _currentDocument != document)
        {
            DetachFromDocument(_currentDocument);
        }

        _currentDocument = document;

        // Set up event handlers
        document.ReadyStateChanged += Document_ReadyStateChanged;
        document.Focused += Document_Focused;
        // document.StyleSheets.Changed += StyleSheets_Changed;

        if (document.DefaultView != null)
        {
            document.DefaultView.Resized += Window_Resized;
            document.DefaultView.Scrolled += Window_Scrolled;
        }

        // Connect mutation tracker if available
        _mutationTracker?.ConnectToDocument(document);

        // Notify observers
        NotifyObservers(observer => observer.OnDocumentAttached(document));

        // If document is already interactive or complete, notify about ready state
        if (document.ReadyState == DocumentReadyState.Interactive ||
            document.ReadyState == DocumentReadyState.Complete)
        {
            NotifyObservers(observer => observer.OnReadyStateChanged(document, document.ReadyState));
        }
    }

    /// <inheritdoc />
    public void DetachFromDocument(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        // Remove event handlers
        document.ReadyStateChanged -= Document_ReadyStateChanged;
        document.Focused -= Document_Focused;
        // document.StyleSheets.Changed -= StyleSheets_Changed;

        if (document.DefaultView != null)
        {
            document.DefaultView.Resized -= Window_Resized;
            document.DefaultView.Scrolled -= Window_Scrolled;
        }

        // Disconnect mutation tracker if available
        _mutationTracker?.DisconnectFromDocument(document);

        // Notify observers
        NotifyObservers(observer => observer.OnDocumentDetached(document));

        if (_currentDocument == document)
        {
            _currentDocument = null;
        }
    }

    /// <inheritdoc />
    public void CheckForDocumentChange()
    {
        var activeDocument = _context.Active;
        if (activeDocument != null && activeDocument != _currentDocument)
        {
            AttachToDocument(activeDocument);
        }
    }

    /// <inheritdoc />
    public void NotifyDomUpdated(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        NotifyObservers(observer => observer.OnDomUpdated(document));
    }

    /// <inheritdoc />
    public void NotifyViewportChanged(int width, int height)
    {
        if (_currentDocument == null)
            return;

        // Store viewport dimensions in the document or context if needed
        // This is a simplification - real implementation might use IRenderDevice

        // Notify observers about the DOM update which includes viewport changes
        NotifyDomUpdated(_currentDocument);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by this coordinator.
    /// </summary>
    /// <param name="disposing">Whether the method is called from Dispose.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Clean up managed resources
            if (_currentDocument != null)
            {
                DetachFromDocument(_currentDocument);
            }

            // _context.Navigated -= Context_Navigated;
            _observers.Clear();
            (_mutationTracker as IDisposable)?.Dispose();
        }

        _disposed = true;
    }
    //
    // private void Context_Navigated(object? sender, global::AngleSharp.Navigation.NavigationEventArgs e)
    // {
    //     // When navigation occurs, check for document changes
    //     CheckForDocumentChange();
    // }

    private void Document_ReadyStateChanged(object? sender, Event ev)
    {
        var document = sender as IDocument;
        if (document != null)
        {
            NotifyObservers(observer => observer.OnReadyStateChanged(document, document.ReadyState));

            // For interactive and complete states, we may need additional handling
            if (document.ReadyState == DocumentReadyState.Interactive ||
                document.ReadyState == DocumentReadyState.Complete)
            {
                // This could trigger style recalculation, layout, etc.
                NotifyDomUpdated(document);
            }
        }
    }

    private void Document_Focused(object? sender, Event ev)
    {
        if (sender is IDocument document)
        {
            // When the document receives focus, it might affect styles (like :focus-within)
            NotifyDomUpdated(document);
        }
    }

    private void StyleSheets_Changed(object? sender, EventArgs e)
    {
        if (_currentDocument != null)
        {
            // When stylesheets change, we need to recalculate styles
            NotifyDomUpdated(_currentDocument);
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
            // Scrolling can affect which elements are in the viewport
            // This may affect prioritization of style calculations
            NotifyDomUpdated(_currentDocument);
        }
    }

    private void NotifyObservers(Action<IDocumentLifecycleObserver> notification)
    {
        lock (_observers)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    notification(observer);
                }
                catch (Exception ex)
                {
                    // Log exception but continue notifying other observers
                    Console.WriteLine($"Error notifying observer: {ex.Message}");
                }
            }
        }
    }
}