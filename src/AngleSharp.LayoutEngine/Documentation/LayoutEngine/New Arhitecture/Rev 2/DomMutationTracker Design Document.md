# DomMutationTracker Design Document

## Overview

The DomMutationTracker is a critical component in the StyleSystem architecture that monitors DOM changes through AngleSharp's MutationObserver and translates them into appropriate style invalidation operations. It serves as the bridge between DOM mutations and the style invalidation system, following Blink's architecture.

## Responsibilities

- Connect to AngleSharp's MutationObserver to receive DOM change notifications
- Filter mutations to identify those that affect styling
- Map DOM changes to specific style invalidation operations
- Batch related mutations for efficient processing
- Optimize invalidation scope to minimize unnecessary recalculation

## Integration with AngleSharp

The DomMutationTracker integrates with AngleSharp's DOM and MutationObserver API:

```csharp
public class DomMutationTracker : IDisposable
{
    private readonly MutationObserver _observer;
    private readonly IStyleInvalidationTracker _invalidationTracker;
    private readonly IBrowsingContext _context;

    public DomMutationTracker(IBrowsingContext context, IStyleInvalidationTracker invalidationTracker)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
        _observer = new MutationObserver(HandleMutations);
        
        if (context.Active != null)
        {
            ConnectToDocument(context.Active);
        }
        
        // Listen for document changes
        context.Active = async (_, e) => 
        {
            if (e.OldValue != null)
                DetachFromDocument(e.OldValue);
            
            if (e.NewValue != null)
                ConnectToDocument(e.NewValue);
        };
    }
    
    private void ConnectToDocument(IDocument document)
    {
        // Connect to both head and body to catch all relevant mutations
        if (document.Head != null)
        {
            _observer.Connect(document.Head, childList: true, subtree: true, attributes: true);
        }
        
        if (document.Body != null)
        {
            _observer.Connect(document.Body, childList: true, subtree: true, attributes: true);
        }
        
        // Also observe document element to catch top-level changes
        if (document.DocumentElement != null)
        {
            _observer.Connect(document.DocumentElement, attributes: true);
        }
    }
    
    private void DetachFromDocument(IDocument document)
    {
        _observer.Disconnect();
    }
}
```

## Mutation Handling

The DomMutationTracker processes different types of mutations based on their style impact:

```csharp
private void HandleMutations(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
{
    var elementInvalidations = new Dictionary<IElement, HashSet<string>>();
    var subtreeInvalidations = new HashSet<IElement>();
    
    foreach (var mutation in mutations)
    {
        switch (mutation.Type)
        {
            case "attributes":
                ProcessAttributeMutation(mutation, elementInvalidations, subtreeInvalidations);
                break;
                
            case "childList":
                ProcessChildListMutation(mutation, elementInvalidations, subtreeInvalidations);
                break;
                
            case "characterData":
                ProcessCharacterDataMutation(mutation, elementInvalidations, subtreeInvalidations);
                break;
        }
    }
    
    // Batch process invalidations for better performance
    ApplyInvalidations(elementInvalidations, subtreeInvalidations);
}
```

## Attribute Mutation Handling

Different attributes affect styling in different ways:

```csharp
private void ProcessAttributeMutation(
    IMutationRecord mutation, 
    Dictionary<IElement, HashSet<string>> elementInvalidations, 
    HashSet<IElement> subtreeInvalidations)
{
    if (mutation.Target is IElement element)
    {
        string attributeName = mutation.AttributeName;
        
        if (attributeName == "style")
        {
            // Style attribute changes only affect the element itself
            AddElementInvalidation(element, null, elementInvalidations);
        }
        else if (attributeName == "id")
        {
            // ID changes can affect multiple elements via selectors
            subtreeInvalidations.Add(element.OwnerDocument.DocumentElement);
        }
        else if (attributeName == "class")
        {
            // Class changes affect the element and potentially descendants with :has() selectors
            AddElementInvalidation(element, null, elementInvalidations);
            
            // If document uses complex selectors like :has(), we might need wider invalidation
            if (DocumentUsesComplexSelectors())
            {
                subtreeInvalidations.Add(element.ParentElement);
            }
        }
        else if (IsStyleAffectingAttribute(attributeName))
        {
            // Handle other style-affecting attributes
            AddElementInvalidation(element, null, elementInvalidations);
        }
    }
}
```

## Child List Mutation Handling

DOM structure changes require special handling:

```csharp
private void ProcessChildListMutation(
    IMutationRecord mutation, 
    Dictionary<IElement, HashSet<string>> elementInvalidations, 
    HashSet<IElement> subtreeInvalidations)
{
    if (mutation.Target is IElement parentElement)
    {
        // For added nodes
        if (mutation.Added != null)
        {
            foreach (var node in mutation.Added)
            {
                ProcessAddedNode(node, parentElement, elementInvalidations, subtreeInvalidations);
            }
        }
        
        // For removed nodes
        if (mutation.Removed != null)
        {
            foreach (var node in mutation.Removed)
            {
                ProcessRemovedNode(node, parentElement, elementInvalidations, subtreeInvalidations);
            }
        }
        
        // Structural pseudo-classes like :first-child might be affected
        InvalidateStructuralDependencies(parentElement, elementInvalidations);
    }
}
```

## Style-Affecting Attributes

Different HTML attributes affect style in different ways:

```csharp
private bool IsStyleAffectingAttribute(string attributeName)
{
    // Global attributes that affect styling
    var globalStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "class", "style", "id", "lang", "dir", "hidden", "tabindex", "draggable", "contenteditable"
    };
    
    // Element-specific attributes that affect presentation
    var elementSpecificStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Form elements
        "disabled", "readonly", "checked", "required", "placeholder", "multiple",
        
        // Table elements
        "colspan", "rowspan", "headers", "scope", "border",
        
        // Image elements
        "width", "height", "align", "valign",
        
        // Link elements
        "href", "target", "rel",
        
        // Other
        "src", "alt", "title"
    };
    
    return globalStyleAttributes.Contains(attributeName) || 
           elementSpecificStyleAttributes.Contains(attributeName);
}
```

## Invalidation Application

Batched invalidation is more efficient than individual calls:

```csharp
private void ApplyInvalidations(
    Dictionary<IElement, HashSet<string>> elementInvalidations, 
    HashSet<IElement> subtreeInvalidations)
{
    // Process subtree invalidations first (wider scope)
    foreach (var element in subtreeInvalidations)
    {
        _invalidationTracker.InvalidateElement(element);
    }
    
    // Process element-specific invalidations
    foreach (var kvp in elementInvalidations)
    {
        var element = kvp.Key;
        var properties = kvp.Value;
        
        if (properties == null || properties.Count == 0)
        {
            _invalidationTracker.InvalidateElement(element);
        }
        else
        {
            _invalidationTracker.InvalidateProperties(element, properties);
        }
    }
}
```

## Optimization Strategies

### 1. Containment-Aware Invalidation

```csharp
private void LimitInvalidationScope(IElement element, HashSet<IElement> subtreeInvalidations)
{
    // Find nearest containment boundary
    var current = element;
    while (current != null && current.ParentElement != null)
    {
        if (HasStyleContainment(current))
        {
            // If we find a containment boundary, we can limit the invalidation scope
            subtreeInvalidations.Remove(element.OwnerDocument.DocumentElement);
            subtreeInvalidations.Add(current);
            return;
        }
        current = current.ParentElement;
    }
}

private bool HasStyleContainment(IElement element)
{
    var containValue = element.GetAttribute("style")?.Contains("contain:") == true || 
                       element.GetAttribute("style")?.Contains("contain-") == true;
                       
    // Further parse the contain value to check for style containment
    return containValue;
}
```

### 2. Selector Dependency Tracking

```csharp
private void InvalidateStructuralDependencies(
    IElement element, 
    Dictionary<IElement, HashSet<string>> elementInvalidations)
{
    // Invalidate siblings for nth-child and related selectors
    foreach (var sibling in element.Children)
    {
        if (sibling is IElement siblingElement)
        {
            AddElementInvalidation(siblingElement, null, elementInvalidations);
        }
    }
    
    // Invalidate parent for :empty and similar selectors
    if (element.ParentElement != null)
    {
        AddElementInvalidation(element.ParentElement, null, elementInvalidations);
    }
}
```

### 3. Batch Processing

```csharp
private void AddElementInvalidation(
    IElement element, 
    HashSet<string>? properties, 
    Dictionary<IElement, HashSet<string>> elementInvalidations)
{
    if (!elementInvalidations.TryGetValue(element, out var existingProps))
    {
        existingProps = new HashSet<string>();
        elementInvalidations[element] = existingProps;
    }
    
    if (properties != null)
    {
        foreach (var prop in properties)
        {
            existingProps.Add(prop);
        }
    }
}
```

## Thread Safety Considerations

The DomMutationTracker may be called from different threads, especially when handling asynchronous DOM updates:

```csharp
private readonly object _mutationLock = new object();

private void HandleMutations(IEnumerable<IMutationRecord> mutations, MutationObserver observer)
{
    lock (_mutationLock)
    {
        // Process mutations as shown in previous examples
    }
}
```

## Performance Considerations

1. **Batching**: Group related invalidations to reduce processing overhead
2. **Scoping**: Limit invalidation to affected subtrees
3. **Filtering**: Ignore mutations that don't affect styling
4. **Debouncing**: For rapidly firing changes, debounce invalidations
5. **Containment Awareness**: Respect CSS containment for isolated style contexts

## Integration with StyleInvalidationTracker

The DomMutationTracker serves as the source of invalidation events for the StyleInvalidationTracker, forming a key part of the Blink-style architecture:

```csharp
// DOM Mutation → Style Invalidation → Style Recalc → Style Engine
// This component handles the first arrow in the flow
public void Dispose()
{
    _observer.Disconnect();
}
```

## Future Enhancements

1. **Improved Selector Dependency Analysis**: More sophisticated tracking of which elements might be affected by which selectors
2. **Pseudo-Element Handling**: Specific handling for pseudo-element styling changes
3. **Smarter Batch Processing**: Advanced batching based on update frequency and element relationships
4. **Animation-Aware Invalidation**: Special handling for animated properties
5. **Shadow DOM Support**: Handle style encapsulation in shadow DOM contexts

This design document outlines the core functionality and implementation approach for the DomMutationTracker, a key component in connecting AngleSharp's DOM to the StyleSystem following Blink's architecture.