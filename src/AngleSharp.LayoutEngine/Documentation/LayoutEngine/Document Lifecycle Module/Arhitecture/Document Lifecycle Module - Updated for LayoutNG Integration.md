# Document Lifecycle Module - Updated for LayoutNG Integration

## Overview

The Document Lifecycle Module is the reactive backbone of the AngleSharp Layout Engine. It tracks DOM mutations, manages invalidation of computed values, and orchestrates the update process through clearly defined lifecycle states. This updated architecture incorporates the phase separation needed for LayoutNG-inspired processing, with distinct states for style, intrinsic sizes, and layout updates.

## Core Components

### 1. DocumentLifecycleManager

Central coordinator that maintains the current lifecycle state of the document and enforces valid state transitions. It ensures that operations happen in the correct order (e.g., layout calculation never happens while styles are still out of date).

```csharp
public class DocumentLifecycleManager
{
    // Current state of the document lifecycle
    public LifecycleState CurrentState { get; private set; }
    
    // Request updates to different aspects
    public void ScheduleStyleUpdate();
    public void ScheduleIntrinsicSizesUpdate(); // NEW: For intrinsic size phase
    public void ScheduleLayoutUpdate();
    
    // Transition to a new state if valid
    public bool TryTransitionTo(LifecycleState newState);
    
    // Force a document to a clean state in a specific aspect
    public void EnsureState(LifecycleState requiredState);
    
    // Process all pending updates
    public void ProcessPendingUpdates();
    
    // Event for state changes
    public event EventHandler<LifecycleStateChangedEventArgs> StateChanged;
}

// Enhanced lifecycle states with intrinsic sizing phase
public enum LifecycleState
{
    Initial,
    StyleDirty,
    StyleClean,
    IntrinsicSizesDirty, // NEW: For intrinsic sizing phase
    IntrinsicSizesClean, // NEW: For completed intrinsic sizing
    LayoutDirty,
    LayoutClean,
    PaintDirty,
    PaintClean
}
```

### 2. InvalidationManager

Determines what needs to be recalculated when DOM mutations or other changes occur. It classifies and coordinates different types of invalidation (style, intrinsic sizes, layout) and delegates to specialized trackers.

```csharp
public class InvalidationManager
{
    // Process mutation records
    public void ProcessMutation(IMutationRecord mutation);
    public void ProcessMutationBatch(IEnumerable<IMutationRecord> mutations);
    
    // Invalidate elements for different aspects
    public void InvalidateElement(IElement element, InvalidationFlags flags);
    public void InvalidateElementStyle(IElement element);
    public void InvalidateElementIntrinsicSizes(IElement element); // NEW
    public void InvalidateElementLayout(IElement element);
    
    // Invalidate subtrees
    public void InvalidateSubtree(IElement element, InvalidationFlags flags);
    
    // Invalidate based on style changes
    public void InvalidateStylesheet(ICssStyleSheet stylesheet);
    
    // Specialized invalidation
    public void InvalidateSelector(string selector); // For selector-based invalidation
    public void InvalidateVariable(string variableName); // For CSS variable invalidation
}

// Flags for different invalidation types
[Flags]
public enum InvalidationFlags
{
    None = 0,
    Style = 1,
    IntrinsicSizes = 2, // NEW
    Layout = 4,
    Paint = 8,
    All = Style | IntrinsicSizes | Layout | Paint
}
```

### 3. MutationObserverAdapter

Bridges AngleSharp's MutationObserver with the invalidation system. It configures and manages mutation observers, processes mutation records, and forwards relevant information to the InvalidationManager.

```csharp
public class MutationObserverAdapter
{
    // Initialize with a document
    public void Initialize(IDocument document);
    
    // Callback for mutation events
    public void OnMutation(IEnumerable<IMutationRecord> mutations);
    
    // Start/stop observation
    public void StartObserving();
    public void StopObserving();
    
    // Configure observation options
    public void SetOptions(MutationObserverOptions options);
    
    // Schedule processing (deferred or immediate)
    public void ScheduleProcessing();
}

// Options for mutation observation
public class MutationObserverOptions
{
    public bool ObserveAttributes { get; set; } = true;
    public bool ObserveCharacterData { get; set; } = true;
    public bool ObserveChildNodes { get; set; } = true;
    public bool ObserveSubtree { get; set; } = true;
    public IEnumerable<string> AttributeFilter { get; set; } = null;
}
```

### 4. StyleInvalidationTracker

Specialized in tracking which elements need style recalculation. It uses detailed selector-based dependencies, property-specific invalidation, and CSS variable tracking to minimize the set of elements needing new style computations.

```csharp
public class StyleInvalidationTracker
{
    // Mark elements as needing style update
    public void MarkStyleDirty(IElement element);
    
    // Mark specific properties as dirty
    public void MarkPropertiesDirty(IElement element, IEnumerable<string> properties);
    
    // Get all elements needing recalculation
    public IEnumerable<IElement> GetDirtyElements();
    
    // Check if element needs recalculation
    public bool IsStyleDirty(IElement element);
    public bool IsPropertyDirty(IElement element, string property);
    
    // Clear dirty flags
    public void ClearDirtyFlags(IElement element);
    public void ClearAllDirtyFlags();
    
    // Specialized dependency tracking
    public void TrackSelectorDependency(string selector, IElement element);
    public void TrackVariableDependency(string variableName, IElement element);
}
```

### 5. IntrinsicSizesInvalidationTracker (NEW)

New component that tracks which elements need intrinsic size recalculation. This supports the separate phase for intrinsic size calculation in the LayoutNG approach.

```csharp
public class IntrinsicSizesInvalidationTracker
{
    // Mark elements as needing size recalculation
    public void MarkIntrinsicSizesDirty(IElement element);
    
    // Get all elements needing size recalculation
    public IEnumerable<IElement> GetDirtyElements();
    
    // Check if element needs size recalculation
    public bool IsIntrinsicSizesDirty(IElement element);
    
    // Clear dirty flags
    public void ClearDirtyFlags(IElement element);
    public void ClearAllDirtyFlags();
    
    // Specialized dependency tracking
    public void TrackSizeDependency(IElement source, IElement dependent);
}
```

### 6. LayoutInvalidationTracker

Similar to the StyleInvalidationTracker but focused on layout. It identifies elements whose geometry or position might have changed due to DOM mutations, style changes, or intrinsic size changes.

```csharp
public class LayoutInvalidationTracker
{
    // Mark elements as needing layout recalculation
    public void MarkLayoutDirty(IElement element);
    
    // Mark elements as needing fragment recalculation
    public void MarkFragmentsDirty(IElement element); // NEW: For fragment invalidation
    
    // Get all elements needing layout/fragment recalculation
    public IEnumerable<IElement> GetLayoutDirtyElements();
    public IEnumerable<IElement> GetFragmentsDirtyElements(); // NEW
    
    // Check if element needs recalculation
    public bool IsLayoutDirty(IElement element);
    public bool IsFragmentDirty(IElement element); // NEW
    
    // Clear dirty flags
    public void ClearDirtyFlags(IElement element);
    public void ClearAllDirtyFlags();
    
    // Specialized dependency tracking
    public void TrackGeometryDependency(IElement source, IElement dependent);
    public void TrackPositionDependency(IElement source, IElement dependent);
}
```

### 7. EnhancedDependencyTracker

An enhanced version of CacheDependencyTracker that maintains complex relationships between elements, including selector-based dependencies, CSS variable usage, inheritance chains, and layout relationships.

```csharp
public class EnhancedDependencyTracker
{
    // Track basic relationships
    public void TrackDependency(IElement dependent, IElement source, DependencyFlags flags);
    public void TrackDocumentDependency(IElement element, IDocument document);
    
    // Track specialized dependencies
    public void TrackSelectorDependency(string selector, IElement element);
    public void TrackVariableDependency(string variableName, IElement element);
    public void TrackInheritedPropertyDependency(string property, IElement element);
    public void TrackLayoutDependency(IElement dependent, IElement source);
    
    // Get dependents
    public IEnumerable<IElement> GetDependents(IElement element, DependencyFlags flags);
    public IEnumerable<IElement> GetStyleDependents(IElement element);
    public IEnumerable<IElement> GetSizeDependents(IElement element); // NEW
    public IEnumerable<IElement> GetLayoutDependents(IElement element);
    
    // Get elements affected by changes
    public IEnumerable<IElement> GetElementsAffectedBySelector(string selector);
    public IEnumerable<IElement> GetElementsAffectedByVariable(string variableName);
    public IEnumerable<IElement> GetElementsAffectedByProperty(string property);
}

// Flags for different dependency types
[Flags]
public enum DependencyFlags
{
    None = 0,
    ElementDependency = 1,
    StyleDependency = 2,
    SizeDependency = 4, // NEW
    LayoutDependency = 8,
    AllDependencies = ElementDependency | StyleDependency | SizeDependency | LayoutDependency
}
```

### 8. SchedulingService

Controls when update operations happen, balancing performance with responsiveness. It can manage various scheduling strategies such as immediate, deferred, throttled, etc.

```csharp
public class SchedulingService
{
    // Schedule updates with different priorities
    public void ScheduleUpdate(UpdateType type, UpdatePriority priority = UpdatePriority.Normal);
    
    // Process pending updates
    public void ProcessPendingUpdates();
    
    // Cancel pending updates
    public void CancelPendingUpdates();
    
    // Change scheduling strategy
    public void SetStrategy(SchedulingStrategy strategy);
    
    // Check if updates are pending
    public bool HasPendingUpdates(UpdateType type = UpdateType.Any);
}

// Types of updates
public enum UpdateType
{
    Style,
    IntrinsicSizes, // NEW
    Layout,
    Paint,
    Any
}

// Update priorities
public enum UpdatePriority
{
    Low,
    Normal,
    High,
    Critical
}

// Scheduling strategies
public enum SchedulingStrategy
{
    Immediate,    // Process updates as soon as requested
    Deferred,     // Process updates during idle time
    Throttled,    // Limit update frequency
    Batched       // Group updates together
}
```

### 9. MutationBatchProcessor

Used to batch and optimize large volumes of mutations:

```csharp
public class MutationBatchProcessor
{
    // Process a batch of mutation records
    public IEnumerable<ProcessedMutation> ProcessBatch(IEnumerable<IMutationRecord> mutations);
    
    // Optimize by removing redundant mutations
    public IEnumerable<ProcessedMutation> OptimizeMutations(IEnumerable<ProcessedMutation> mutations);
    
    // Group related mutations
    public IEnumerable<MutationGroup> GroupMutations(IEnumerable<ProcessedMutation> mutations);
    
    // Prioritize mutations
    public IEnumerable<MutationGroup> PrioritizeMutations(IEnumerable<MutationGroup> groups);
}

// Processed mutation with metadata
public class ProcessedMutation
{
    public IMutationRecord OriginalRecord { get; }
    public MutationType Type { get; }
    public IElement TargetElement { get; }
    public IEnumerable<IElement> AffectedElements { get; }
    public InvalidationFlags SuggestedInvalidation { get; }
}

// Group of related mutations
public class MutationGroup
{
    public IEnumerable<ProcessedMutation> Mutations { get; }
    public UpdatePriority Priority { get; }
    public IElement CommonAncestor { get; }
    public InvalidationFlags CombinedInvalidation { get; }
}
```

## Document Lifecycle Process

The document lifecycle process has been enhanced to support the LayoutNG approach:

### Document Lifecycle State Transitions

```
[Initial] 
    ↓ (DOM Mutation)
[StyleDirty] 
    ↓ (Style Recalculation)
[StyleClean] 
    ↓ (If style affects sizes)
[IntrinsicSizesDirty] 
    ↓ (Intrinsic Size Calculation)
[IntrinsicSizesClean] 
    ↓ (If sizes affect layout)
[LayoutDirty] 
    ↓ (Layout Recalculation)
[LayoutClean] 
    ↓ (If layout affects paint)
[PaintDirty] 
    ↓ (Paint Update)
[PaintClean]
```

### Key Processes

#### 1. Mutation Handling

When a DOM mutation occurs:

1. `MutationObserver` detects the change and notifies `MutationObserverAdapter`
2. `MutationObserverAdapter` forwards to `MutationBatchProcessor` for optimization
3. `InvalidationManager` analyzes processed mutations
4. Based on the changes, it marks elements in the appropriate tracker:
    - `StyleInvalidationTracker` for style changes
    - `IntrinsicSizesInvalidationTracker` for size changes
    - `LayoutInvalidationTracker` for layout changes
5. `DocumentLifecycleManager` updates the document state
6. `SchedulingService` schedules appropriate updates

#### 2. Style Recalculation Process

When the document is in the `StyleDirty` state:

1. `DocumentLifecycleManager` processes style updates
2. `StyleInvalidationTracker` provides the list of elements needing updates
3. `StyleComputationEngine` recalculates styles for those elements
4. Results are stored in `StyleCache`
5. Document transitions to `StyleClean` state
6. If styles affect sizes, transitions to `IntrinsicSizesDirty`

#### 3. Intrinsic Size Calculation Process (NEW)

When the document is in the `IntrinsicSizesDirty` state:

1. `DocumentLifecycleManager` processes intrinsic size updates
2. `IntrinsicSizesInvalidationTracker` provides elements needing size calculation
3. `IntrinsicSizesCalculator` calculates min/max sizes
4. Results are stored in `IntrinsicSizeCache`
5. Document transitions to `IntrinsicSizesClean` state
6. If sizes affect layout, transitions to `LayoutDirty`

#### 4. Layout Calculation Process

When the document is in the `LayoutDirty` state:

1. `DocumentLifecycleManager` processes layout updates
2. `LayoutInvalidationTracker` provides elements needing layout recalculation
3. `LayoutEngine` uses constraint-based approach:
    - Creates appropriate `ConstraintSpace` for each element
    - Selects appropriate `FormattingContext` based on style
    - Computes layout and creates `LayoutFragment` tree
4. Results are stored in `FragmentCache`
5. Document transitions to `LayoutClean` state

## Integration with Other Modules

### Integration with Style Computation Module

The Document Lifecycle Module interacts with the Style Computation Module through these interfaces:

```csharp
// Style computation integration
public interface IStyleInvalidationHandler
{
    // Respond to various invalidation triggers
    void HandleSelectorInvalidation(string selector);
    void HandlePropertyInvalidation(IElement element, string propertyName);
    void HandleVariableInvalidation(string variableName);
    void HandleStylesheetInvalidation(ICssStyleSheet stylesheet);
    
    // Process style recalculation
    void RecalculateStyles(IEnumerable<IElement> elements);
}
```

### Integration with Layout Engine Module

The Document Lifecycle Module interacts with the Layout Engine Module through these interfaces:

```csharp
// Layout engine integration
public interface ILayoutInvalidationHandler
{
    // Respond to various invalidation triggers
    void HandleIntrinsicSizeInvalidation(IElement element);
    void HandleLayoutInvalidation(IElement element);
    void HandleFragmentInvalidation(IElement element);
    
    // Process recalculation
    void RecalculateIntrinsicSizes(IEnumerable<IElement> elements);
    void RecalculateLayouts(IEnumerable<IElement> elements);
}
```

### Integration with Cache Module

The Document Lifecycle Module interacts with the Cache Module through these interfaces:

```csharp
// Cache integration
public interface ICacheInvalidationHandler
{
    // Invalidate various types of cached data
    void InvalidateStyleCache(IElement element);
    void InvalidateIntrinsicSizeCache(IElement element);
    void InvalidateFragmentCache(IElement element);
    
    // Bulk invalidation
    void InvalidateDocument(IDocument document);
    void InvalidateAll();
}
```

## LayoutNG-Specific Enhancements

### Containment Optimization

The module now supports CSS containment for optimizing invalidation:

```csharp
public class ContainmentOptimizer
{
    // Check if an element has containment
    public ContainmentType GetContainment(IElement element);
    
    // Limit invalidation scope based on containment
    public IEnumerable<IElement> LimitInvalidationScope(
        IElement root, 
        IEnumerable<IElement> elements, 
        InvalidationFlags flags);
    
    // Find nearest containing ancestor
    public IElement FindNearestContainingAncestor(IElement element, ContainmentType type);
}

// Types of containment
[Flags]
public enum ContainmentType
{
    None = 0,
    Size = 1,
    Layout = 2,
    Style = 4,
    Paint = 8,
    All = Size | Layout | Style | Paint
}
```

### Phase-Specific Optimization

The module supports optimizations for each processing phase:

```csharp
public class PhaseOptimizer
{
    // Optimize style processing
    public IEnumerable<IElement> OptimizeStyleProcessing(IEnumerable<IElement> dirtyElements);
    
    // Optimize intrinsic size processing
    public IEnumerable<IElement> OptimizeSizeProcessing(IEnumerable<IElement> dirtyElements);
    
    // Optimize layout processing
    public IEnumerable<IElement> OptimizeLayoutProcessing(IEnumerable<IElement> dirtyElements);
    
    // Prioritize elements for processing
    public IEnumerable<IElement> PrioritizeElements(
        IEnumerable<IElement> elements, 
        UpdateType phase);
}
```

### Fragment Reuse

The module supports detecting when fragments can be reused:

```csharp
public class FragmentReuseDetector
{
    // Check if a fragment can be reused
    public bool CanReuseFragment(IElement element, ConstraintSpace newConstraints);
    
    // Check if children fragments can be reused
    public bool CanReuseChildFragments(IElement parent);
    
    // Find which fragments need rebuild
    public IEnumerable<IElement> FindFragmentsNeedingRebuild(IElement root);
}
```

## Error Handling and Recovery

The module includes robust error handling:

```csharp
public class LifecycleErrorHandler
{
    // Handle errors during different phases
    public void HandleStyleError(IElement element, Exception ex);
    public void HandleIntrinsicSizeError(IElement element, Exception ex);
    public void HandleLayoutError(IElement element, Exception ex);
    
    // Recover from invalid state
    public void RecoverFromInvalidState(LifecycleState currentState);
    
    // Timeout protection
    public void StartTimeout(UpdateType phase, TimeSpan timeout);
    public void CancelTimeout();
}
```

## Performance Monitoring

The module includes performance monitoring facilities:

```csharp
public class LifecyclePerformanceMonitor
{
    // Start monitoring a phase
    public IDisposable StartPhase(UpdateType phase);
    
    // Track element processing
    public void TrackElementProcessing(IElement element, UpdateType phase);
    
    // Get metrics
    public PhaseMetrics GetPhaseMetrics(UpdateType phase);
    public ElementMetrics GetElementMetrics(IElement element);
    
    // Log performance report
    public string GeneratePerformanceReport();
}

// Performance metrics
public class PhaseMetrics
{
    public TimeSpan TotalTime { get; }
    public int ElementsProcessed { get; }
    public TimeSpan AverageTimePerElement { get; }
    public int InvocationCount { get; }
}
```

## Conclusion

The enhanced Document Lifecycle Module provides comprehensive support for the LayoutNG-inspired layout engine. The additions include:

1. Enhanced lifecycle states with separate phase for intrinsic sizing
2. New invalidation trackers for intrinsic sizes and fragments
3. Specialized dependency tracking for different phases
4. Containment-aware optimization
5. Fragment reuse detection
6. Comprehensive error handling and recovery
7. Detailed performance monitoring

These enhancements ensure that the Document Lifecycle Module can efficiently coordinate the complex process of reactive updates in a modern layout engine while maintaining optimal performance.