# AngleSharp.StyleSystem Architecture Update

This implementation introduces a new architecture for the AngleSharp.StyleSystem that uses the observer pattern and task-based system to break circular dependencies and improve modularity, maintainability, and testability.

## Key Changes

1. **Observer Pattern**: Components communicate through well-defined observer interfaces instead of direct references
2. **Task-Based System**: Style work is represented as tasks that can be scheduled and prioritized
3. **Dependency Injection**: Components receive dependencies through setter methods
4. **Unidirectional Data Flow**: Clear flow from style invalidation through scheduling to computation

## Observer Interfaces

The architecture provides three main observer interfaces:

1. **IStyleInvalidationObserver**: Notified when elements are invalidated
2. **IDocumentLifecycleObserver**: Notified of document lifecycle events
3. **IStyleComputationObserver**: Notified when styles are computed

## Task System

The task system includes:

1. **IStyleTask**: Represents a unit of work in the style system
2. **IStyleTaskScheduler**: Schedules and prioritizes style tasks

## Usage Examples

### Setting up the StyleSystemService

```csharp
// Create a browsing context
var context = BrowsingContext.New(Configuration.Default.WithStyleSystem());

// Access the style system
var styleSystem = context.GetStyleSystem();

// Compute styles for a specific element
var element = document.QuerySelector("div.example");
var computedStyle = element.GetComputedStyle();

// Force a style update for the entire document
context.RecalculateStyles();
```

### Implementing a Custom Observer

```csharp
public class MyStyleObserver : IStyleComputationObserver
{
    public void OnStyleComputed(IElement element, IComputedStyle style)
    {
        Console.WriteLine($"Style computed for {element.NodeName}#{element.Id}");
    }

    public void OnSubtreeStylesUpdated(IElement rootElement)
    {
        Console.WriteLine($"Subtree styles updated for {rootElement.NodeName}#{rootElement.Id}");
    }
}

// Register the observer
var styleEngine = context.GetStyleSystem().StyleEngine;
styleEngine.AddComputationObserver(new MyStyleObserver());
```

### Creating Custom Style Tasks

```csharp
public class MyCustomStyleTask : IStyleTask
{
    private readonly IElement _element;
    
    public MyCustomStyleTask(IElement element)
    {
        _element = element;
    }
    
    public RecalcPriority Priority => RecalcPriority.High;
    
    public IElement Element => _element;
    
    public void Execute(IStyleEngine engine)
    {
        // Custom style processing logic
        engine.ComputeElementStyle(_element);
        Console.WriteLine("Custom style task executed!");
    }
}

// Schedule the custom task
var taskScheduler = (styleSystem as StyleSystemService)?.GetService<IStyleTaskScheduler>();
taskScheduler?.EnqueueTask(new MyCustomStyleTask(element));
```

## Migrating from the Old Architecture

The new architecture is designed to be backward-compatible with existing code. The main changes are in the internal workings of the style system, with the public API remaining largely the same.

If you have code that extends or customizes the style system, you may need to update it to use the new observer pattern:

1. Replace direct dependencies with observer implementations
2. Use dependency injection instead of creating components directly
3. Use the task system for scheduling work instead of direct method calls

## Testing the New Architecture

The new architecture is designed to be more testable. Components can be tested in isolation with mock implementations of their dependencies:

```csharp
// Test a StyleEngine with mock dependencies
var mockInvalidationTracker = new Mock<IStyleInvalidationTracker>();
var mockStyleTreeResolver = new Mock<IStyleTreeResolver>();

var styleEngine = new StyleEngine(context);
styleEngine.SetInvalidationTracker(mockInvalidationTracker.Object);
styleEngine.SetStyleTreeResolver(mockStyleTreeResolver.Object);

// Now you can test the StyleEngine's behavior with the mock dependencies
```

## Performance Considerations

The new architecture includes several performance optimizations:

1. **Task Prioritization**: Critical-path elements are processed first
2. **Throttling and Batching**: Style recalculation is throttled to avoid excessive work
3. **Style Sharing**: Similar elements share style data to reduce memory usage

## Future Enhancements

The observer pattern and task-based system provide a foundation for future enhancements:

1. **Worker Thread Support**: Offload non-critical style computation to worker threads
2. **Display Locking**: Defer processing for non-visible content
3. **Animation Optimization**: Special handling for animated properties
4. **Incremental Computation**: Process only what has changed