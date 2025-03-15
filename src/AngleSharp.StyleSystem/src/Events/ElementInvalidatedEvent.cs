namespace AngleSharp.StyleSystem.Events;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Interfaces;
using Css.Dom;
using Integration;
using Models;

// Style Invalidation Events
public class ElementInvalidatedEvent
{
    public IElement Element { get; }

    public ElementInvalidatedEvent(IElement element)
    {
        Element = element;
    }
}

public class PropertiesInvalidatedEvent
{
    public IElement Element { get; }
    public IEnumerable<string> Properties { get; }

    public PropertiesInvalidatedEvent(IElement element, IEnumerable<string> properties)
    {
        Element = element;
        Properties = properties;
    }
}

public class SubtreeInvalidatedEvent
{
    public IElement RootElement { get; }

    public SubtreeInvalidatedEvent(IElement rootElement)
    {
        RootElement = rootElement;
    }
}

public class DeviceDependentElementsInvalidatedEvent
{
}

// Document Lifecycle Events
public class DocumentAttachedEvent
{
    public IDocument Document { get; }

    public DocumentAttachedEvent(IDocument document)
    {
        Document = document;
    }
}

public class DocumentDetachedEvent
{
    public IDocument Document { get; }

    public DocumentDetachedEvent(IDocument document)
    {
        Document = document;
    }
}

public class DomUpdatedEvent
{
    public IDocument Document { get; }

    public DomUpdatedEvent(IDocument document)
    {
        Document = document;
    }
}

public class ReadyStateChangedEvent
{
    public IDocument Document { get; }
    public DocumentReadyState ReadyState { get; }

    public ReadyStateChangedEvent(IDocument document, DocumentReadyState readyState)
    {
        Document = document;
        ReadyState = readyState;
    }
}

/// <summary>
/// Event published when DOM changes are detected that may affect styles.
/// </summary>
public class DomChangesEvent
{
    /// <summary>
    /// Gets the collection of DOM changes that were detected.
    /// </summary>
    public IReadOnlyList<DomChange> Changes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomChangesEvent"/> class.
    /// </summary>
    /// <param name="changes">The collection of DOM changes.</param>
    public DomChangesEvent(IEnumerable<DomChange> changes)
    {
        if (changes == null)
            throw new ArgumentNullException(nameof(changes));

        Changes = new List<DomChange>(changes);
    }
}

/// <summary>
/// Event published when a stylesheet is added, removed, or modified in the system.
/// </summary>
public class StylesheetChangedEvent
{
    /// <summary>
    /// Gets the stylesheet that was changed.
    /// </summary>
    public ICssStyleSheet Stylesheet { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public StylesheetChangeType ChangeType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StylesheetChangedEvent"/> class.
    /// </summary>
    /// <param name="stylesheet">The stylesheet that was changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public StylesheetChangedEvent(ICssStyleSheet stylesheet, StylesheetChangeType changeType)
    {
        Stylesheet = stylesheet ?? throw new ArgumentNullException(nameof(stylesheet));
        ChangeType = changeType;
    }
}

/// <summary>
/// Event published when all stylesheets have been refreshed.
/// </summary>
public class StylesheetsRefreshedEvent
{
    /// <summary>
    /// Gets the document to which the stylesheets belong.
    /// </summary>
    public IDocument Document { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StylesheetsRefreshedEvent"/> class.
    /// </summary>
    /// <param name="document">The document whose stylesheets were refreshed.</param>
    public StylesheetsRefreshedEvent(IDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }
}