using System;
using System.Collections.Generic;
using AngleSharp.Dom;

namespace AngleSharp.StyleSystem.Observers;

using Interfaces;

/// <summary>
/// Observer interface for style invalidation events.
/// Implemented by components that need to react to elements requiring style recalculation.
/// </summary>
public interface IStyleInvalidationObserver
{
    /// <summary>
    /// Called when a single element's style is invalidated.
    /// </summary>
    /// <param name="element">The element whose style is invalidated.</param>
    void OnElementInvalidated(IElement element);

    /// <summary>
    /// Called when specific properties of an element are invalidated.
    /// </summary>
    /// <param name="element">The element with invalidated properties.</param>
    /// <param name="properties">The names of the invalidated properties.</param>
    void OnPropertiesInvalidated(IElement element, IEnumerable<string> properties);

    /// <summary>
    /// Called when all elements in a subtree need style recalculation.
    /// </summary>
    /// <param name="rootElement">The root element of the invalidated subtree.</param>
    void OnSubtreeInvalidated(IElement rootElement);

    /// <summary>
    /// Called when device-dependent elements need recalculation due to viewport changes.
    /// </summary>
    void OnDeviceDependentElementsInvalidated();
}

/// <summary>
/// Observer interface for document lifecycle events.
/// Implemented by components that need to respond to document state changes.
/// </summary>
public interface IDocumentLifecycleObserver
{
    /// <summary>
    /// Called when a document is attached to the browsing context.
    /// </summary>
    /// <param name="document">The attached document.</param>
    void OnDocumentAttached(IDocument document);

    /// <summary>
    /// Called when a document is detached from the browsing context.
    /// </summary>
    /// <param name="document">The detached document.</param>
    void OnDocumentDetached(IDocument document);

    /// <summary>
    /// Called when DOM changes are applied to the document.
    /// </summary>
    /// <param name="document">The document with DOM updates.</param>
    void OnDomUpdated(IDocument document);

    /// <summary>
    /// Called when the document's ready state changes.
    /// </summary>
    /// <param name="document">The document whose ready state changed.</param>
    /// <param name="readyState">The new ready state.</param>
    void OnReadyStateChanged(IDocument document, DocumentReadyState readyState);
}

/// <summary>
/// Observer interface for style computation events.
/// Implemented by components that need to react to style computation results.
/// </summary>
public interface IStyleComputationObserver
{
    /// <summary>
    /// Called when a style is computed for an element.
    /// </summary>
    /// <param name="element">The element with the computed style.</param>
    /// <param name="style">The computed style.</param>
    void OnStyleComputed(IElement element, IComputedStyle style);

    /// <summary>
    /// Called when styles for a subtree of elements are updated.
    /// </summary>
    /// <param name="rootElement">The root element of the updated subtree.</param>
    void OnSubtreeStylesUpdated(IElement rootElement);
}