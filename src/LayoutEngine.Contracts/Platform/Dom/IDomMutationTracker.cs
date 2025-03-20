namespace LayoutEngine.Contracts.Platform.Dom;

using System;
using System.Collections.Generic;

/// <summary>
/// Tracks DOM mutations and generates events for DOM changes.
/// </summary>
public interface IDomMutationTracker
{
    /// <summary>
    /// Starts tracking DOM mutations for the specified document.
    /// </summary>
    /// <param name="document">The document to track.</param>
    void StartTracking(IDocument document);

    /// <summary>
    /// Stops tracking DOM mutations.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Manually signals an attribute change for an element.
    /// </summary>
    /// <param name="element">The element that changed.</param>
    /// <param name="attributeName">The name of the attribute that changed.</param>
    /// <param name="oldValue">The old attribute value.</param>
    /// <param name="newValue">The new attribute value.</param>
    void SignalAttributeChanged(IElement element, string attributeName, string? oldValue, string? newValue);

    /// <summary>
    /// Manually signals a node was added.
    /// </summary>
    /// <param name="node">The node that was added.</param>
    /// <param name="parent">The parent node.</param>
    void SignalNodeAdded(IDomNode node, IDomNode parent);

    /// <summary>
    /// Manually signals a node was removed.
    /// </summary>
    /// <param name="node">The node that was removed.</param>
    /// <param name="parent">The parent node.</param>
    void SignalNodeRemoved(IDomNode node, IDomNode parent);
}

/// <summary>
/// Adapts DOM elements for the rendering pipeline.
/// </summary>
public interface IElementAdapter
{
    /// <summary>
    /// Gets the element ID.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element ID.</returns>
    string GetId(IElement element);

    /// <summary>
    /// Gets the element tag name.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element tag name in lowercase.</returns>
    string GetTagName(IElement element);

    /// <summary>
    /// Gets the value of an attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value, or null if the attribute doesn't exist.</returns>
    string? GetAttribute(IElement element, string name);

    /// <summary>
    /// Sets the value of an attribute.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    void SetAttribute(IElement element, string name, string? value);

    /// <summary>
    /// Gets the children of an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The element children.</returns>
    IReadOnlyList<IElement> GetChildren(IElement element);

    /// <summary>
    /// Gets the parent of an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The parent element, or null if the element has no parent.</returns>
    IElement? GetParent(IElement element);
}

/// <summary>
/// Tracks viewport dimensions and changes.
/// </summary>
public interface IViewportDetector
{
    /// <summary>
    /// Gets the current viewport size.
    /// </summary>
    Size ViewportSize { get; }

    /// <summary>
    /// Gets the current device pixel ratio.
    /// </summary>
    double DevicePixelRatio { get; }

    /// <summary>
    /// Starts tracking viewport changes.
    /// </summary>
    void StartTracking();

    /// <summary>
    /// Stops tracking viewport changes.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Forces a check for viewport changes.
    /// </summary>
    /// <returns>True if the viewport changed, otherwise false.</returns>
    bool CheckForChanges();
}

/// <summary>
/// Represents a DOM node.
/// </summary>
public interface IDomNode
{
    /// <summary>
    /// Gets the node type.
    /// </summary>
    NodeType NodeType { get; }

    /// <summary>
    /// Gets the node name.
    /// </summary>
    string NodeName { get; }

    /// <summary>
    /// Gets the parent node.
    /// </summary>
    IDomNode? ParentNode { get; }
}

/// <summary>
/// Represents an HTML element.
/// </summary>
public interface IElement : IDomNode
{
    /// <summary>
    /// Gets the element ID.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the element tag name.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Gets the parent element.
    /// </summary>
    IElement? ParentElement { get; }

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    IDomNode[] ChildNodes { get; }

    /// <summary>
    /// Gets the value of an attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value, or null if the attribute doesn't exist.</returns>
    string? GetAttribute(string name);

    /// <summary>
    /// Sets the value of an attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    void SetAttribute(string name, string? value);

    /// <summary>
    /// Checks if an element has the specified attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>True if the element has the attribute, otherwise false.</returns>
    bool HasAttribute(string name);

    /// <summary>
    /// Removes an attribute from an element.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    void RemoveAttribute(string name);

    /// <summary>
    /// Gets elements by tag name.
    /// </summary>
    /// <param name="tagName">The tag name to search for.</param>
    /// <returns>A collection of elements with the specified tag name.</returns>
    IElement[] GetElementsByTagName(string tagName);
}

/// <summary>
/// Represents a text node.
/// </summary>
public interface IText : IDomNode
{
    /// <summary>
    /// Gets or sets the text data.
    /// </summary>
    string Data { get; set; }
}

/// <summary>
/// Represents an HTML document.
/// </summary>
public interface IDocument : IDomNode
{
    /// <summary>
    /// Gets the document element (root element).
    /// </summary>
    IElement DocumentElement { get; }

    /// <summary>
    /// Finds an element by ID.
    /// </summary>
    /// <param name="id">The element ID.</param>
    /// <returns>The element with the specified ID, or null if no such element exists.</returns>
    IElement? GetElementById(string id);
}

/// <summary>
/// Represents a browser window.
/// </summary>
public interface IWindow
{
    /// <summary>
    /// Gets the document associated with the window.
    /// </summary>
    IDocument Document { get; }

    /// <summary>
    /// Gets the inner width of the window.
    /// </summary>
    int InnerWidth { get; }

    /// <summary>
    /// Gets the inner height of the window.
    /// </summary>
    int InnerHeight { get; }

    /// <summary>
    /// Gets the device pixel ratio.
    /// </summary>
    double DevicePixelRatio { get; }

    /// <summary>
    /// Adds an event listener to the window.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="listener">The event listener.</param>
    void AddEventListener(string eventType, Action<Event> listener);

    /// <summary>
    /// Removes an event listener from the window.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="listener">The event listener.</param>
    void RemoveEventListener(string eventType, Action<Event> listener);
}

/// <summary>
/// Represents a size with width and height.
/// </summary>
public struct Size : IEquatable<Size>
{
    /// <summary>
    /// Gets the width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Equals(Size other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Size size && Equals(size);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Size left, Size right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Size left, Size right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Represents a DOM event.
/// </summary>
public class Event
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the event target.
    /// </summary>
    public IDomNode Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="target">The event target.</param>
    public Event(string type, IDomNode target)
    {
        Type = type;
        Target = target;
    }
}

/// <summary>
/// Defines the type of a DOM node.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Element node (e.g., &lt;div&gt;).
    /// </summary>
    Element = 1,

    /// <summary>
    /// Text node.
    /// </summary>
    Text = 3,

    /// <summary>
    /// Comment node.
    /// </summary>
    Comment = 8,

    /// <summary>
    /// Document node.
    /// </summary>
    Document = 9
}

/// <summary>
/// Represents a mutation record from MutationObserver.
/// </summary>
public class MutationRecord
{
    /// <summary>
    /// Gets the type of mutation.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the target of the mutation.
    /// </summary>
    public IDomNode Target { get; }

    /// <summary>
    /// Gets the added nodes.
    /// </summary>
    public IDomNode[]? AddedNodes { get; }

    /// <summary>
    /// Gets the removed nodes.
    /// </summary>
    public IDomNode[]? RemovedNodes { get; }

    /// <summary>
    /// Gets the previous sibling.
    /// </summary>
    public IDomNode? PreviousSibling { get; }

    /// <summary>
    /// Gets the next sibling.
    /// </summary>
    public IDomNode? NextSibling { get; }

    /// <summary>
    /// Gets the attribute name for attribute mutations.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Gets the attribute namespace for attribute mutations.
    /// </summary>
    public string? AttributeNamespace { get; }

    /// <summary>
    /// Gets the old value for attribute or character data mutations.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationRecord"/> class.
    /// </summary>
    public MutationRecord(
        string type,
        IDomNode target,
        string attributeName = "",
        string? attributeNamespace = null,
        string? oldValue = null,
        IDomNode[]? addedNodes = null,
        IDomNode[]? removedNodes = null,
        IDomNode? previousSibling = null,
        IDomNode? nextSibling = null)
    {
        Type = type;
        Target = target;
        AttributeName = attributeName;
        AttributeNamespace = attributeNamespace;
        OldValue = oldValue;
        AddedNodes = addedNodes;
        RemovedNodes = removedNodes;
        PreviousSibling = previousSibling;
        NextSibling = nextSibling;
    }
}

/// <summary>
/// Represents a mutation observer.
/// </summary>
public class MutationObserver
{
    private readonly Action<MutationRecord[]> _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationObserver"/> class.
    /// </summary>
    /// <param name="callback">The callback to invoke when mutations occur.</param>
    public MutationObserver(Action<MutationRecord[]> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Starts observing mutations on the specified target.
    /// </summary>
    /// <param name="target">The target to observe.</param>
    /// <param name="options">The options for observation.</param>
    public void Observe(IDomNode target, MutationObserverInit options)
    {
        // This would be implemented by platform-specific code
    }

    /// <summary>
    /// Stops observing mutations.
    /// </summary>
    public void Disconnect()
    {
        // This would be implemented by platform-specific code
    }
}

/// <summary>
/// Options for mutation observation.
/// </summary>
public class MutationObserverInit
{
    /// <summary>
    /// Gets or sets whether to observe attribute changes.
    /// </summary>
    public bool Attributes { get; set; }

    /// <summary>
    /// Gets or sets whether to observe character data changes.
    /// </summary>
    public bool CharacterData { get; set; }

    /// <summary>
    /// Gets or sets whether to observe child list changes.
    /// </summary>
    public bool ChildList { get; set; }

    /// <summary>
    /// Gets or sets whether to observe changes in the subtree.
    /// </summary>
    public bool Subtree { get; set; }

    /// <summary>
    /// Gets or sets whether to record attribute old values.
    /// </summary>
    public bool AttributeOldValue { get; set; }

    /// <summary>
    /// Gets or sets whether to record character data old values.
    /// </summary>
    public bool CharacterDataOldValue { get; set; }

    /// <summary>
    /// Gets or sets which attribute changes to observe.
    /// </summary>
    public string[]? AttributeFilter { get; set; }
}

/// <summary>
/// Represents a resize observer.
/// </summary>
public class ResizeObserver
{
    private readonly Action<ResizeObserverEntry[]> _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeObserver"/> class.
    /// </summary>
    /// <param name="callback">The callback to invoke when resize occurs.</param>
    public ResizeObserver(Action<ResizeObserverEntry[]> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Starts observing resize on the specified target.
    /// </summary>
    /// <param name="target">The target to observe.</param>
    public void Observe(IElement target)
    {
        // This would be implemented by platform-specific code
    }

    /// <summary>
    /// Stops observing resize.
    /// </summary>
    public void Disconnect()
    {
        // This would be implemented by platform-specific code
    }
}

/// <summary>
/// Represents a resize observer entry.
/// </summary>
public class ResizeObserverEntry
{
    /// <summary>
    /// Gets the target of the resize.
    /// </summary>
    public IElement Target { get; }

    /// <summary>
    /// Gets the content rectangle of the target.
    /// </summary>
    public Rectangle ContentRect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeObserverEntry"/> class.
    /// </summary>
    /// <param name="target">The target of the resize.</param>
    /// <param name="contentRect">The content rectangle of the target.</param>
    public ResizeObserverEntry(IElement target, Rectangle contentRect)
    {
        Target = target;
        ContentRect = contentRect;
    }
}

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public struct Rectangle
{
    /// <summary>
    /// Gets the x-coordinate of the rectangle.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the y-coordinate of the rectangle.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle.</param>
    /// <param name="y">The y-coordinate of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rectangle(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}