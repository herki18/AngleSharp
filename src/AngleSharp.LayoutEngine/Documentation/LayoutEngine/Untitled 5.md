# AngleSharp.StyleSystem Architecture Documentation

## Table of Contents

1. [Architecture Overview](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#architecture-overview)
2. [Design Principles](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#design-principles)
3. [Component Architecture](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#component-architecture)
    - [Observer Pattern Components](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#observer-pattern-components)
    - [Task-Based System](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#task-based-system)
    - [Core Components](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#core-components)
    - [Integration Components](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#integration-components)
4. [Data Flow](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#data-flow)
5. [Cross-Cutting Concerns](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#cross-cutting-concerns)
6. [Component Interactions](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#component-interactions)
7. [Extension Points](https://claude.ai/chat/927708a6-93b5-4229-89bc-a373bf9db44b#extension-points)

## Architecture Overview

The AngleSharp.StyleSystem architecture follows a modular, loosely-coupled design that implements several key architectural patterns:

- **Observer Pattern**: Components communicate through well-defined observer interfaces
- **Task-Based Architecture**: Work is represented as tasks that are scheduled and prioritized
- **Dependency Injection**: Components receive their dependencies rather than creating them
- **Unidirectional Data Flow**: Style invalidation → scheduling → computation follows a one-way flow

This architecture addresses previous circular dependency issues by replacing direct component references with indirect communication through observers and a task system.

## Design Principles

1. **Single Responsibility**: Each component has a clear, focused responsibility
2. **Loose Coupling**: Components interact through interfaces rather than concrete implementations
3. **Testability**: Components can be tested in isolation with mocked dependencies
4. **Extensibility**: The system can be extended without modifying existing components
5. **Performance**: Critical-path operations are optimized and prioritized
6. **Compatibility**: Maintains compatibility with the AngleSharp framework
7. **Modularity**: Components can be replaced or extended independently

## Component Architecture

### Observer Pattern Components

#### Observer Interfaces

1. **IStyleInvalidationObserver**
    
    - Notified when elements are invalidated
    - Implemented by StyleRecalcScheduler
    - Methods:
        - `OnElementInvalidated(IElement element)`
        - `OnPropertiesInvalidated(IElement element, IEnumerable<string> properties)`
        - `OnSubtreeInvalidated(IElement rootElement)`
        - `OnDeviceDependentElementsInvalidated()`
2. **IDocumentLifecycleObserver**
    
    - Notified of document lifecycle events
    - Implemented by StyleEngine
    - Methods:
        - `OnDocumentAttached(IDocument document)`
        - `OnDocumentDetached(IDocument document)`
        - `OnDomUpdated(IDocument document)`
        - `OnReadyStateChanged(IDocument document, DocumentReadyState readyState)`
3. **IStyleComputationObserver**
    
    - Notified when styles are computed
    - Implemented by StyleEngine (self-observation for optimization)
    - Methods:
        - `OnStyleComputed(IElement element, IComputedStyle style)`
        - `OnSubtreeStylesUpdated(IElement rootElement)`

### Task-Based System

1. **IStyleTask**
    
    - Represents a unit of work in the style system
    - Implementations:
        - `ComputeElementStyleTask`: Computes style for a single element
        - `UpdateSubtreeStylesTask`: Updates styles for an entire subtree
    - Properties:
        - `RecalcPriority Priority`: Determines execution order
    - Methods:
        - `Execute(IStyleEngine engine)`: Executes the task
2. **IStyleTaskScheduler**
    
    - Schedules and prioritizes style tasks
    - Implementation: StyleTaskScheduler
    - Methods:
        - `EnqueueTask(IStyleTask task)`
        - `ProcessTasks()`
    - Properties:
        - `bool HasPendingTasks`

### Core Components

1. **StyleEngine**
    
    - Central component for style computation
    - Implements IStyleEngine, IStyleComputationObserver, IDocumentLifecycleObserver
    - Responsibilities:
        - Computing element styles
        - Managing style component lifecycle
        - Processing document lifecycle events
        - Optimizing computed styles
2. **StyleInvalidationTracker**
    
    - Tracks which elements need style recalculation
    - Responsibilities:
        - Tracking element style validity
        - Processing DOM changes
        - Notifying observers of invalidations
        - Managing element dependencies
3. **StyleTreeResolver**
    
    - Resolves styles for elements and subtrees
    - Responsibilities:
        - Traversing the DOM
        - Computing styles for elements
        - Managing style sharing
        - Resolving parent-child style relationships
4. **ComputedStyleBuilder**
    
    - Builds computed styles from declarations
    - Responsibilities:
        - Resolving CSS variables
        - Computing values
        - Building optimized ComputedStyle objects
        - Managing property tree nodes

### Integration Components

1. **DocumentLifecycleCoordinator**
    
    - Coordinates document lifecycle with the style system
    - Responsibilities:
        - Managing document attachment/detachment
        - Handling document ready state changes
        - Coordinating viewport changes
        - Notifying observers of lifecycle events
2. **DomMutationTracker**
    
    - Tracks DOM mutations and their style implications
    - Responsibilities:
        - Connecting to MutationObserver
        - Categorizing DOM changes
        - Determining style impact of mutations
        - Notifying StyleInvalidationTracker
3. **StyleRecalcScheduler**
    
    - Schedules style recalculation based on invalidations
    - Implements IStyleInvalidationObserver
    - Responsibilities:
        - Prioritizing element recalculation
        - Scheduling/throttling style work
        - Coordinating main thread and worker thread work
        - Processing style tasks
4. **StyleSystemService**
    
    - Entry point for using the style system
    - Responsibilities:
        - Initializing the style system
        - Connecting to the browsing context
        - Managing component lifecycle
        - Providing a public API

## Data Flow

### Style Invalidation Flow

1. DOM mutation occurs and is detected by DomMutationTracker
2. DomMutationTracker categorizes the change and notifies StyleInvalidationTracker
3. StyleInvalidationTracker marks affected elements as invalid
4. StyleInvalidationTracker notifies IStyleInvalidationObserver implementations
5. StyleRecalcScheduler (as an observer) creates and enqueues style tasks
6. StyleTaskScheduler schedules the tasks for processing

### Style Computation Flow

1. StyleTaskScheduler processes tasks based on priority
2. Tasks execute using the StyleEngine
3. StyleEngine uses StyleTreeResolver to resolve styles
4. StyleTreeResolver computes styles using the style computation pipeline:
    - RuleCollector matches rules
    - CascadeResolver resolves conflicts
    - InheritanceProcessor handles inheritance
    - ComputedStyleBuilder builds the final style
5. StyleEngine notifies IStyleComputationObserver implementations
6. StyleEngine (as an observer) performs style optimization

### Document Lifecycle Flow

1. Document is attached to the browsing context
2. DocumentLifecycleCoordinator attaches to the document
3. DocumentLifecycleCoordinator notifies IDocumentLifecycleObserver implementations
4. StyleEngine (as an observer) responds to document events
5. On ready state changes, StyleEngine updates document styles

## Cross-Cutting Concerns

### Performance Optimization

1. **Style Sharing**
    
    - Similar elements share style data through the PropertyTreeManager
    - StyleTreeResolver identifies sharing opportunities
    - ComputedStyleFactory manages style prototypes
2. **Task Prioritization**
    
    - Viewport-visible elements receive higher priority
    - Critical-path elements processed on main thread
    - Off-screen elements processed at lower priority
3. **Throttling and Batching**
    
    - Style recalculation is throttled to avoid excessive work
    - Similar tasks are batched for better performance
    - Animation frames are used for scheduling

### Memory Management

1. **Property Trees**
    
    - Shared property storage reduces memory usage
    - Identical properties are stored once and referenced
    - PropertyTreeManager optimizes tree structure
2. **Style Caching**
    
    - StyleCache stores computed styles for reuse
    - Property values are cached for quick access
    - Style sharing reduces redundant objects

### Thread Safety

1. **Main Thread Work**
    
    - Critical path styling occurs on the main thread
    - UI-blocking operations are minimized
2. **Worker Thread Work**
    
    - Non-critical styling can occur on worker threads
    - Results are synchronized back to the main thread

## Component Interactions

### Initialization Sequence

1. StyleSystemService creates the core components:
    
    - StyleEngine
    - StyleInvalidationTracker
    - DomMutationTracker
    - StyleTaskScheduler
    - StyleRecalcScheduler
    - DocumentLifecycleCoordinator
2. Dependencies are injected:
    
    - StyleEngine.SetInvalidationTracker(invalidationTracker)
    - StyleEngine.SetStyleTreeResolver(styleTreeResolver)
3. Observers are registered:
    
    - invalidationTracker.AddObserver(styleRecalcScheduler)
    - lifecycleCoordinator.AddObserver(styleEngine)
    - styleEngine.AddComputationObserver(styleEngine)
4. System is connected to document:
    
    - lifecycleCoordinator.AttachToDocument(document)

### Style Update Sequence

1. DOM mutation occurs
2. StyleInvalidationTracker marks elements as invalid
3. StyleRecalcScheduler creates and enqueues tasks
4. StyleTaskScheduler processes tasks
5. StyleEngine computes new styles
6. PropertyTreeManager optimizes property storage

## Extension Points

The architecture provides several extension points for custom functionality:

1. **Custom Style Tasks**
    
    - Implement IStyleTask for specialized style operations
    - Register with StyleTaskScheduler
2. **Additional Observers**
    
    - Implement observer interfaces to receive notifications
    - Register with appropriate subjects
3. **Custom Style Application**
    
    - Implement IStyleApplicationStrategy for custom display types
    - Provide to StyleTreeResolver
4. **Value Processing Extensions**
    
    - Extend ValueCalculator for custom units or calculations
    - Extend VariableResolver for custom variable handling
5. **Custom Task Scheduling**
    
    - Implement IStyleTaskScheduler for custom scheduling logic
    - Provide to StyleRecalcScheduler