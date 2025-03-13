# AngleSharp.LayoutEngine StyleSystem Integration

This document outlines the integration between the AngleSharp.LayoutEngine and the existing AngleSharp.StyleSystem. It details how the components interact, the data flow between systems, and the coordination mechanisms that ensure efficient operation.

## Integration Philosophy

The LayoutEngine is designed to work seamlessly with the StyleSystem, following these principles:

1. **Clear Boundaries**: Well-defined interfaces between systems with minimal dependencies
2. **Efficient Coordination**: Shared coordination mechanisms for style and layout updates
3. **Optimized Invalidation**: Coordinated invalidation to minimize unnecessary work
4. **Consistent Architecture**: Similar architectural patterns and component responsibilities

## Key Integration Points

### 1. Style to Layout Data Flow

The primary data flow from StyleSystem to LayoutEngine occurs through the `IComputedStyle` interface:

```
ComputedStyle → LayoutTreeBuilder → LayoutTreeNode → Layout Algorithms
```

#### Computed Style Consumption

- **Direct Referencing**: LayoutTreeNodes maintain a reference to their associated IComputedStyle
- **Property Access**: Layout algorithms access style properties through IComputedStyle interface
- **Box Model Properties**: Layout extracts margin, border, padding, and size properties from ComputedStyle
- **Specialized Access**: Layout algorithms have optimized access to relevant style properties (e.g., flex, grid)

#### Style Update Propagation

```
StyleEngine → StyleInvalidationTracker → LayoutInvalidationTracker → LayoutEngine
```

- Style changes are detected by StyleInvalidationTracker
- LayoutInvalidationTracker listens for style invalidation events
- Layout recalculation is triggered for affected elements

### 2. Shared Lifecycle Management

Both systems share the DocumentLifecycleCoordinator to ensure proper coordination:

```
DocumentLifecycleCoordinator
├── StyleSheetManager
├── StyleRecalcScheduler
├── StyleInvalidationTracker
├── LayoutScheduler
└── LayoutInvalidationTracker
```

#### Lifecycle Phases

1. **Style Phase**:
    
    - Style sheets are processed
    - Style recalculation is scheduled
    - Style computation is performed
2. **Layout Phase**:
    
    - Layout tree is updated based on style changes
    - Layout is recalculated for affected nodes
    - Layout results are prepared for rendering

#### Coordination Mechanisms

- **Phase Ordering**: Style phase always precedes layout phase
- **Dependency Tracking**: Layout system tracks which nodes depend on which style properties
- **Batch Processing**: Multiple style and layout operations are batched when possible
- **Animation Frame Synchronization**: Updates synchronized with animation frames

### 3. Invalidation Coordination

The StyleInvalidationTracker and LayoutInvalidationTracker work together to minimize work:

```
DOM/Style Change → StyleInvalidationTracker → LayoutInvalidationTracker
```

#### Cross-System Invalidation

- **Style to Layout**: Style changes automatically invalidate dependent layout
- **Property Dependency**: Layout system knows which style properties affect layout
- **Containment Awareness**: CSS containment is respected across both systems
- **Priority Coordination**: Critical path elements are prioritized in both systems

#### Optimization Strategies

- **Minimal Propagation**: Only invalidate what's necessary
- **Two-tier Invalidation**: Style-only changes don't always require layout updates
- **Subtree Optimization**: Containment and other isolation mechanisms limit invalidation scope

### 4. Component-Level Integration

#### StyleEngine ↔ LayoutEngine

```csharp
public class LayoutEngine
{
    private readonly IStyleEngine _styleEngine;
    
    public LayoutEngine(IStyleEngine styleEngine)
    {
        _styleEngine = styleEngine;
    }
    
    // Methods to coordinate with StyleEngine
}
```

- LayoutEngine depends on StyleEngine for computed styles
- StyleEngine can signal LayoutEngine when styles are updated
- Both engines work with the same DOM elements

#### ComputedStyleBuilder ↔ LayoutTreeBuilder

```
ComputedStyleBuilder → ComputedStyle → LayoutTreeBuilder → LayoutTreeNode
```

- LayoutTreeBuilder consumes ComputedStyle objects produced by ComputedStyleBuilder
- Layout node creation is triggered after style computation is complete
- Layout nodes maintain references to their element's ComputedStyle

#### StyleTreeResolver ↔ LayoutTreeResolver

```
StyleTreeResolver → LayoutTreeResolver
```

- Similar tree traversal patterns
- StyleTreeResolver signals LayoutTreeResolver when style tree is updated
- Both optimize traversal based on what needs to be updated

#### StyleInvalidationTracker ↔ LayoutInvalidationTracker

```
StyleInvalidationTracker → LayoutInvalidationTracker
```

- LayoutInvalidationTracker subscribes to StyleInvalidationTracker events
- Layout properties depend on style properties (dependency tracking)
- Coordinated invalidation to minimize work

#### StyleCache ↔ LayoutCache

```
StyleCache → LayoutCache
```

- Similar caching strategies
- LayoutCache keys often include StyleCache keys as part of their identity
- Cache invalidation is coordinated

### 5. Shared Service Components

#### DocumentLifecycleCoordinator

```csharp
public class DocumentLifecycleCoordinator
{
    private readonly StyleEngine _styleEngine;
    private readonly LayoutEngine _layoutEngine;
    
    // Methods to coordinate lifecycle phases
}
```

- Orchestrates style and layout phase ordering
- Manages document attachment and detachment for both systems
- Coordinates lifecycle events like document ready and load

#### DomMutationTracker

```
DomMutationTracker → StyleInvalidationTracker → LayoutInvalidationTracker
```

- Shared tracking of DOM mutations
- Categorizes mutations as style-affecting, layout-affecting, or both
- Optimizes mutation handling to minimize redundant work

#### ViewportDetector

```
ViewportDetector → StyleSystem + LayoutSystem
```

- Provides viewport information to both systems
- Detects elements in or near viewport for prioritization
- Coordinates viewport-dependent style and layout properties

## Integration Challenges and Solutions

### Challenge: Cascading Invalidation

**Problem**: Style changes can cause cascading layout invalidation through parent-child relationships.

**Solution**:

- LayoutInvalidationTracker builds dependency graphs
- Layout containment boundaries limit invalidation scope
- Smart batching of layout operations for related elements

### Challenge: Divergent Caching Strategies

**Problem**: Style and layout have different caching needs and invalidation patterns.

**Solution**:

- Layered caching strategy
- Layout cache keys incorporate style cache invalidation tokens
- Cache hierarchies that mirror DOM structure

### Challenge: Priority Balancing

**Problem**: Critical style and layout operations need to be prioritized without blocking each other.

**Solution**:

- Shared priority system
- Critical path identification
- Interleaved processing of style and layout operations

### Challenge: Synchronization Points

**Problem**: Style and layout phases need to be properly synchronized to prevent inconsistent states.

**Solution**:

- Clear phase boundaries
- Synchronization points in DocumentLifecycleCoordinator
- Promise/Task-based coordination for async operations

## Data Structures and Interfaces

### StyleSystem to LayoutSystem

```csharp
// Key interfaces for StyleSystem to LayoutSystem integration
public interface ILayoutTreeBuilder
{
    ILayoutTreeNode BuildLayoutTree(IElement root, IComputedStyle rootStyle);
    ILayoutTreeNode CreateLayoutNode(IElement element, IComputedStyle style);
}

public interface ILayoutTreeNode
{
    IElement Element { get; }
    IComputedStyle Style { get; }
    // Layout-specific properties
}

public interface ILayoutAlgorithm
{
    DisplayMode DisplayMode { get; }
    ILayoutResult CalculateLayout(ILayoutTreeNode node, ILayoutContext context);
}
```

### LayoutSystem to StyleSystem

```csharp
// Key interfaces for LayoutSystem to StyleSystem integration
public interface IStyleDependentLayout
{
    void NotifyStyleChanged(IElement element, IComputedStyle newStyle);
    bool DependsOnStyleProperty(string propertyName);
}

public interface ILayoutInvalidationListener
{
    void OnStyleInvalidated(IElement element, IEnumerable<string> properties);
}
```

## Integration Example Scenarios

### Scenario 1: Element Style Change

1. Style attribute on element changes
2. DomMutationTracker detects change and notifies StyleInvalidationTracker
3. StyleInvalidationTracker marks element as needing style recalculation
4. StyleRecalcScheduler schedules style update
5. StyleEngine recalculates style for element
6. StyleInvalidationTracker notifies LayoutInvalidationTracker
7. LayoutInvalidationTracker marks layout as invalid for element
8. LayoutScheduler schedules layout update
9. LayoutEngine recalculates layout for element and affected descendants
10. RenderLayerBuilder updates rendering based on new layout

### Scenario 2: Stylesheet Change

1. Stylesheet is added, removed, or modified
2. StyleSheetManager detects change
3. StyleInvalidationTracker marks affected elements for recalculation
4. StyleRecalcScheduler schedules batch style update
5. StyleEngine recalculates styles for affected elements
6. StyleInvalidationTracker notifies LayoutInvalidationTracker for each affected element
7. LayoutScheduler batches layout updates by priority
8. LayoutEngine processes layout updates starting with highest priority
9. RenderLayerBuilder updates rendering incrementally as layouts complete

### Scenario 3: Window Resize

1. Window resize event is detected
2. ViewportDetector updates viewport dimensions
3. StyleEngine is notified of device change for viewport-relative units
4. StyleInvalidationTracker marks elements with viewport-dependent styles
5. Style recalculation occurs for affected elements
6. LayoutEngine is notified of viewport size change
7. LayoutInvalidationTracker marks all elements needing re-layout
8. Layout recalculation prioritizes visible elements
9. RenderLayerBuilder updates rendering with new layout

## Best Practices for Integration

1. **Depend on Interfaces, Not Implementations**
    
    - Use IStyleEngine, IComputedStyle interfaces for dependencies
    - Avoid direct coupling to implementation classes
2. **Respect System Boundaries**
    
    - Layout system should not modify styles
    - Style system should not modify layout
3. **Coordinate Invalidation**
    
    - Use invalidation listeners to react to changes
    - Track dependencies between style and layout properties
4. **Share Service Components**
    
    - Use shared DocumentLifecycleCoordinator
    - Share DOM observation mechanisms
5. **Follow Similar Patterns**
    
    - Use similar architecture patterns in both systems
    - Keep consistent naming and design principles

## Conclusion

The integration between StyleSystem and LayoutEngine follows the core principles of clear boundaries, efficient coordination, and optimized invalidation. By leveraging well-defined interfaces and shared coordination mechanisms, the two systems work together seamlessly to transform DOM elements with styles into fully laid out visual representations.

The design respects the distinct responsibilities of each system while ensuring efficient operation through coordinated invalidation, caching, and prioritization. This integration approach enables accurate layout results with good performance characteristics, successfully bringing AngleSharp closer to a complete browser engine implementation.