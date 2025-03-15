namespace AngleSharp.StyleSystem.Events;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;

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

// Style Computation Events
public class StyleComputedEvent
{
    public IElement Element { get; }
    public IComputedStyle Style { get; }

    public StyleComputedEvent(IElement element, IComputedStyle style)
    {
        Element = element;
        Style = style;
    }
}

public class SubtreeStylesUpdatedEvent
{
    public IElement RootElement { get; }

    public SubtreeStylesUpdatedEvent(IElement rootElement)
    {
        RootElement = rootElement;
    }
}