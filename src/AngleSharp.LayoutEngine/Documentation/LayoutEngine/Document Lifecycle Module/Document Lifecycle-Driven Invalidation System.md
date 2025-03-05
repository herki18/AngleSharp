## 1. Overview

The **Document Lifecycle-Driven Invalidation System** is a comprehensive solution for managing:

- **DOM Mutations**: Detecting and batching DOM changes
- **Cache Invalidation**: Determining when style, layout, or visual caches should be invalidated
- **Style & Layout Recalculation**: Orchestrating efficient style and layout updates
- **Scheduling & Batching**: Deferring and grouping recalculations to optimize performance

This system is **inspired by production browser engines** (e.g., WebKit, Blink) and **tailored to the AngleSharp environment**. It ensures **optimal performance** by tracking which elements need style or layout recalculation, batching updates, and using dependency tracking to limit the scope of invalidation. In short, it forms the **reactive backbone of the layout engine**, ensuring that only the necessary calculations are performed after a document change.

---

## 2. Core Components

Below is an overview of the key architectural components that form the Document Lifecycle-Driven Invalidation System. Each component has distinct responsibilities but works closely with the others:

1. **DocumentLifecycleManager**
2. **InvalidationManager**
3. **MutationObserverAdapter**
4. **StyleInvalidationTracker**
5. **LayoutInvalidationTracker**
6. **VisualInvalidationTracker** (if paint/visual updates are tracked separately)
7. **EnhancedDependencyTracker** (formerly CacheDependencyTracker)
8. **SchedulingService** / **UpdateScheduler**
9. **MutationBatchProcessor**

### 2.1 DocumentLifecycleManager

Central coordinator that maintains the **current lifecycle state** of the document and **enforces valid state transitions**. It ensures that operations such as layout calculation never happen while styles are still out of date, and it orchestrates the scheduling of updates.

- **Responsibilities**:
    
    - Maintain the current lifecycle state (e.g., _StyleDirty_, _StyleClean_, _LayoutDirty_, _LayoutClean_, _PaintDirty_, _PaintClean_)
    - Enforce valid state transitions (e.g., can’t do layout if style is dirty)
    - Schedule updates based on document state, delegating to the scheduling service
    - Provide a centralized point for tracking and debugging document state
- **Key Methods** (Examples):
    
    - `TransitionTo(LifecycleState)` – Move the document to a new lifecycle state
    - `ScheduleStyleUpdate()` – Indicate that a style update is needed
    - `ScheduleLayoutUpdate()` – Indicate that a layout update is needed
    - `ProcessPendingUpdates()` – Process all scheduled updates when triggered
    - `EnsureState(LifecycleState)` – Ensure the document has advanced to the required state

### 2.2 InvalidationManager

Responsible for **determining what needs to be recalculated** when DOM mutations or other changes occur. It classifies and coordinates different types of invalidation (style, layout, paint) and delegates to specialized trackers for each type.

- **Responsibilities**:
    
    - Process DOM mutation records
    - Determine which elements need invalidation and what _type_ of invalidation (style, layout, paint/visual)
    - Use dependency tracking to identify and **minimize** the affected elements
    - Coordinate specialized trackers (StyleInvalidationTracker, LayoutInvalidationTracker, VisualInvalidationTracker)
    - Provide a high-level API for manual invalidation (e.g., when a stylesheet changes)
- **Key Methods** (Examples):
    
    - `ProcessMutation(MutationRecord)` / `ProcessMutationBatch(IEnumerable<MutationRecord>)`
    - `InvalidateStyle(IElement)`
    - `InvalidateLayout(IElement)`
    - `InvalidateSubtree(IElement)`

### 2.3 MutationObserverAdapter

Bridges AngleSharp’s **MutationObserver** with the invalidation system. It **configures** and **manages** mutation observers, processes mutation records, and then forwards relevant information to the **InvalidationManager**.

- **Responsibilities**:
    
    - Create and configure `MutationObserver` instances
    - Filter and batch mutation records to avoid redundant processing
    - Translate AngleSharp mutation records into the system’s internal format
    - Schedule invalidation processing after collecting batches of mutations
- **Key Methods**:
    
    - `Initialize(IDocument)` – Set up observation for a document
    - `OnMutation(IEnumerable<IMutationRecord>)` – Primary callback for DOM changes
    - `ScheduleProcessing()` – Defer or throttle how invalidation processing is invoked

### 2.4 StyleInvalidationTracker

Specialized in tracking **which elements need style recalculation**. It uses detailed **selector-based dependencies**, **property-specific** invalidation, and **CSS variable** tracking to minimize the set of elements needing new style computations.

- **Responsibilities**:
    - Identify which style properties are affected by mutations
    - Maintain selector dependency information
    - Mark only the necessary elements for style updates
    - Work with the `EnhancedDependencyTracker` to propagate changes (e.g., inherited properties, CSS variables)

### 2.5 LayoutInvalidationTracker

Similar to the StyleInvalidationTracker but focused on **layout**. It identifies elements whose geometry or position might have changed due to DOM mutations or style changes.

- **Responsibilities**:
    - Identify elements whose geometry needs recalculation
    - Track dependencies between elements (e.g., for layout flow)
    - Determine containment boundaries for layout recalculations
    - Minimize layout updates (often more expensive than style updates)

### 2.7 EnhancedDependencyTracker

An enhanced version of `CacheDependencyTracker` that maintains **complex relationships** between elements, including selector-based dependencies, CSS variable usage, inheritance chains, and more.

- **Responsibilities**:
    
    - Track parent-child DOM relationships
    - Track style property dependencies (including inheritance)
    - Track selector dependencies (e.g., when class or ID changes might affect multiple elements)
    - Track CSS variable usage
    - Provide efficient lookups to determine which elements are affected by any given change
- **Key Methods** (Examples):
    
    - `RegisterSelectorDependency(string selector, IElement element)`
    - `RegisterVariableDependency(string variableName, IElement element)`
    - `GetAffectedElements(MutationRecord record)` – For a given mutation, find which elements might need updates
    - `GetDependents(IElement element)` – Return all elements that depend on the given element

### 2.8 SchedulingService / UpdateScheduler

**Controls when** update operations happen, balancing performance with responsiveness. It can manage various scheduling strategies such as immediate, deferred, throttled, etc.

- **Responsibilities**:
    
    - Determine _when_ to process updates
    - Provide different scheduling strategies (e.g., immediate for testing, batch updates to reduce overhead, etc.)
    - Prioritize visible content if relevant
    - Batch and throttle updates to prevent performance bottlenecks
    - Integrate with the DocumentLifecycleManager to trigger style/layout calculations
- **Key Methods**:
    
    - `ScheduleUpdate(UpdateType type)` – Request a scheduled update for a given type (style, layout, or paint)
    - `ProcessPendingUpdates()` – Execute any queued updates
    - `CancelPendingUpdates()` – Cancel any scheduled updates
    - `SetStrategy(SchedulingStrategy strategy)` – Change scheduling approach on the fly

### 2.9 MutationBatchProcessor

Used to **batch** and **optimize** large volumes of mutations:

- **Responsibilities**:
    - Collect mutations as they come in
    - Remove redundant entries (e.g., multiple attribute changes in quick succession)
    - Convert complex node operations (insertions/removals) into single subtree changes
    - Order mutations so structure changes are handled before attribute changes
    - Provide metadata for prioritization or scheduling

---

## 3. Key Relationships & Interactions

Below is a high-level flow showing how the components interact once the DOM changes:

1. **DOM Mutation → MutationObserverAdapter**
    
    - DOM changes trigger AngleSharp’s `MutationObserver`
    - The `MutationObserverAdapter` batches and preprocesses them using `MutationBatchProcessor`
2. **MutationObserverAdapter → InvalidationManager**
    
    - Processed/batched mutations are sent to `InvalidationManager`
    - `InvalidationManager` determines whether style, layout, or paint invalidation is needed
3. **InvalidationManager → Trackers**
    
    - Specific invalidation details are delegated to `StyleInvalidationTracker`, `LayoutInvalidationTracker`, or `VisualInvalidationTracker`
    - These trackers mark the relevant elements as dirty
4. **Trackers → EnhancedDependencyTracker**
    
    - For each mutation, dependency tracking is consulted to figure out which additional elements might be affected (e.g., through inheritance or variable usage)
5. **InvalidationManager → DocumentLifecycleManager**
    
    - The `InvalidationManager` updates the `DocumentLifecycleManager` about the document’s “dirty” states
6. **DocumentLifecycleManager → SchedulingService / UpdateScheduler**
    
    - The `DocumentLifecycleManager` requests style/layout updates at the appropriate time
    - The `SchedulingService` decides how and when these updates should be processed (immediate, deferred, throttled, etc.)
7. **SchedulingService → StyleComputationEngine / LayoutEngine**
    
    - When the scheduled time arrives, style or layout recalculation is triggered
    - Only affected elements (as identified by the trackers) are updated
8. **StyleComputationEngine / LayoutEngine → Cache**
    
    - Newly computed results are cached
    - `EnhancedDependencyTracker` is updated with any new or removed dependencies

---

## 4. Detailed Processes & Algorithms

### 4.1 Handling DOM Mutations

1. **Mutation Detection**
    
    - `MutationObserver` detects DOM changes (insertions, removals, attribute changes, text changes)
    - Calls `MutationObserverAdapter.OnMutation()`
2. **Batching & Preprocessing**
    
    - `MutationBatchProcessor` groups mutations, removes redundancies, and sorts them for optimal processing
3. **Invalidation Determination**
    
    - `InvalidationManager` analyzes the preprocessed mutations
    - Uses the `EnhancedDependencyTracker` to find directly and indirectly affected elements
    - Classifies changes (style, layout, paint)
4. **Mark Elements & Update Lifecycle**
    
    - `StyleInvalidationTracker` and/or `LayoutInvalidationTracker` mark elements as dirty
    - `DocumentLifecycleManager` moves to the correct “dirty” state (e.g., _StyleDirty_ or _LayoutDirty_)
5. **Scheduling**
    
    - `DocumentLifecycleManager` notifies `SchedulingService` of pending updates
    - `SchedulingService` decides when to process them
6. **Recalculation**
    
    - At the scheduled time, the system recalculates style and/or layout for the marked elements
    - Caches and dependency data are updated accordingly

### 4.2 Style Recalculation Algorithm

1. **Gather Invalidated Elements**
    
    - `StyleInvalidationTracker` provides a set of elements needing style updates
2. **Compute Styles**
    
    - The `StyleComputationEngine` recalculates style for those elements, using data from the `EnhancedDependencyTracker` (e.g., selector matches, variable usage)
3. **Propagate Changes**
    
    - If a style change affects inherited properties or CSS variables, the tracker may expand the set of invalidated elements
4. **Update State**
    
    - Updated styles are stored in the `StyleCache`
    - `DocumentLifecycleManager` transitions to _StyleClean_ state (unless layout is still dirty)

### 4.3 Layout Recalculation (Future or Optional)

1. **Gather Invalidated Elements**
    
    - `LayoutInvalidationTracker` identifies which elements need geometry recalculation
2. **Recalculate Layout**
    
    - The `LayoutEngine` updates positions, sizes, and geometry for those elements
3. **Propagate Containment / Flow Changes**
    
    - Some elements might cause changes in siblings or parents if they expand or collapse
4. **Update State**
    
    - New layout data is stored (e.g., `LayoutCache`)
    - `DocumentLifecycleManager` transitions to _LayoutClean_ state
    - If paint is needed, transitions to _PaintDirty_ and schedules paint updates

### 4.4 Mutation Batch Optimization

1. **Redundancy Removal**
    
    - Multiple attribute changes for the same attribute can be collapsed
    - Insert/remove events can sometimes combine into a single subtree insertion/removal
2. **Order Mutations**
    
    - Structural changes are processed before attribute changes
    - Parent-level changes before child-level changes
3. **Metadata Generation**
    
    - Each batch may include priority info or timestamps for scheduling
    - The final optimized batch is given to `InvalidationManager`

---

## 5. Integration with Existing and Future Modules

### 5.1 Integration with StyleComputationModule

- **Triggering Recalculation**: `SchedulingService` notifies the `StyleComputationEngine` to recalc styles when `StyleDirty`
- **Dependency Tracking**: The `EnhancedDependencyTracker` uses data from the `StyleCache` to track relationships (e.g., selector matches, variable usage)
- **Optimization**: The `StyleInvalidationTracker` narrows down which elements need updating
- **Cache Coordination**: Caches are invalidated or updated based on changes

### 5.2 Integration with CacheModule

- **Cache Invalidation**: `InvalidationManager` determines which cache entries to invalidate
- **Dependency Updates**: When style or layout changes, the `EnhancedDependencyTracker` recalculates dependencies
- **Selective Invalidation**: Only the necessary entries are cleared or recalculated
- **Lifecycle Awareness**: The cache can hook into the document lifecycle to optimize data retrieval

### 5.3 Integration with LayoutEngineModule (Future)

- **Layout Invalidation**: `LayoutInvalidationTracker` triggers geometry updates
- **Dependency Information**: The `EnhancedDependencyTracker` supports layout relationships (e.g., block formatting contexts)
- **Scheduling Coordination**: Style recalc is completed before layout recalc
- **Cache Integration**: Layout results are cached, then selectively invalidated on DOM or style changes

---

## 6. Error Handling & Resilience

- **Graceful Degradation**: If part of the system fails, fallback to full document recalculation
- **Timeout Protection**: Long-running operations can be aborted to preserve responsiveness
- **State Recovery**: `DocumentLifecycleManager` can revert or reset states if something goes wrong
- **Logging & Diagnostics**: Comprehensive logs to assist in debugging issues
- **Failure Isolation**: Errors in style invalidation do not necessarily break layout invalidation, and vice versa

---

## 7. Performance & Design Considerations

1. **Selective Invalidation**
    
    - Only recalc styles or layouts for elements actually impacted
2. **Batched Processing**
    
    - Process multiple mutations together to avoid repeated overhead
3. **Deferred Updates**
    
    - Non-critical operations can be deferred until idle times
4. **Priority-Based Processing**
    
    - Visible or critical content can be prioritized first
5. **Dependency Tracking**
    
    - Fine-grained tracking helps minimize spurious invalidations
6. **Memory Efficiency**
    
    - Avoid duplicating data; leverage existing AngleSharp structures where possible
7. **Thread Safety**
    
    - Primarily single-threaded, but concurrency concerns are addressed where needed
8. **Extensibility**
    
    - Additional invalidation strategies, scheduling policies, or dependency types can be added without major rewrites

---

## 8. Extension Points & Configuration

- **Custom Invalidation Strategies**: Plug in new strategies to handle domain-specific invalidation rules
- **Scheduling Policies**: Swap in different scheduling approaches (immediate, throttled, idle, etc.)
- **Dependency Tracking**: Extend `EnhancedDependencyTracker` for specialized usage (e.g., custom CSS properties)
- **Lifecycle States**: Additional states can be added if more granular control is required (e.g., _PreRenderDirty_)
- **Performance Monitoring**: Hooks for logging or metrics can be added to track performance bottlenecks

### 8.1 Configuration Options

- **UpdateStrategy**: Immediate, batched, throttled, etc.
- **InvalidationGranularity**: How precise invalidation should be (subtree vs. individual elements)
- **BatchingBehavior**: How aggressively to combine mutations
- **DiagnosticLevel**: The verbosity of logs for debugging
- **PerformanceMode**: Balance between raw speed and accuracy (useful for large documents with frequent changes)

---

## 9. Integration Interface (Example)

A sample C#-style interface for how external modules or consumers might interact with the system:

```csharp
// Main interface for the module
public interface IDocumentLifecycle
{
    LifecycleState CurrentState { get; }
    void InvalidateElement(IElement element, InvalidationType type);
    void EnsureUpToDate(UpdateType updateType);
    event EventHandler<LifecycleStateChangedEventArgs> StateChanged;
}

// Possible states in the document lifecycle
public enum LifecycleState
{
    Initial,
    StyleDirty,
    StyleClean,
    LayoutDirty,
    LayoutClean,
    PaintDirty,
    PaintClean
}

// Types of invalidation
[Flags]
public enum InvalidationType
{
    None = 0,
    Style = 1,
    Layout = 2,
    Visual = 4,
    All = Style | Layout | Visual
}

// Types of updates
public enum UpdateType
{
    Style,
    Layout,
    Visual,
    All
}
```

This example illustrates how **external** modules (e.g., style computation engine, higher-level application code) could trigger invalidation or ensure the document is in a fully up-to-date state (style, layout, or both).

---

## 10. Conclusion

The **Document Lifecycle-Driven Invalidation System** provides a **robust**, **scalable**, and **high-performance** mechanism for handling DOM mutations and orchestrating the resulting style/layout recalculations in AngleSharp. By combining **lifecycle state management**, **fine-grained dependency tracking**, **batched mutation processing**, and **flexible scheduling**, the system ensures that:

1. Only the necessary elements are recalculated
2. Expensive operations (like layout) are deferred until absolutely needed
3. The document’s state transitions remain valid and logically consistent

This design draws on the best practices from modern browser engines while remaining adaptable to AngleSharp’s unique constraints and extensibility requirements. As AngleSharp expands (e.g., with a future layout engine), this system is designed to **seamlessly integrate** new modules or advanced features like paint invalidation and more complex scheduling policies.