# AngleSharp Layout Engine Architecture Document

## Blink LayoutNG-Inspired Approach

## 1. Executive Summary

This document outlines the architecture for implementing a Blink LayoutNG-inspired layout engine within the AngleSharp ecosystem. The layout engine will adopt modern approaches from Chromium's LayoutNG, including constraint-based layout, immutable fragment output, and clearly separated layout phases. This approach will provide better performance, more accurate layout results, and better support for complex CSS features compared to a traditional mutable box model approach.

## 2. Key Design Principles

### 2.1 Core Concepts from LayoutNG

1. **Constraint-Based Layout**: Layout is driven by explicitly defined constraints that flow down the tree
2. **Fragment-Based Output**: Layout produces immutable fragments rather than modifying mutable boxes
3. **Phase Separation**: Clear separation between intrinsic size calculation, layout, and positioning
4. **Logical Layout**: Use of logical (writing-mode independent) coordinates first, with conversion to physical coordinates
5. **Formatting Contexts**: Explicit modeling of formatting contexts with specialized layout algorithms
6. **Incremental Layout**: Designed for efficient incremental updates with minimal recalculation

## 3. System Components

### 3.1 LayoutEngine

The central orchestrator of the layout system, providing the main public API:

```csharp
public interface ILayoutEngine
{
    // Main entry point for layout
    LayoutResult Layout(IElement element, ConstraintSpace constraintSpace = null);
    
    // Calculate intrinsic sizes without performing full layout
    MinMaxSizes ComputeMinMaxSizes(IElement element);
    
    // Layout with specific formatting contexts
    LayoutResult LayoutBlock(IElement element, BlockConstraintSpace constraintSpace);
    LayoutResult LayoutInline(IElement element, InlineConstraintSpace constraintSpace);
    LayoutResult LayoutFlex(IElement element, FlexConstraintSpace constraintSpace);
    LayoutResult LayoutGrid(IElement element, GridConstraintSpace constraintSpace);
}
```

### 3.2 ConstraintSpace

Encapsulates all constraints for layout operations and helps with coordinate transformations:

```csharp
public interface IConstraintSpace
{
    // Available space for layout
    Size AvailableSize { get; }
    
    // Size to resolve percentages against
    Size PercentageResolutionSize { get; }
    
    // Writing mode and direction
    WritingMode WritingMode { get; }
    TextDirection Direction { get; }
    
    // Formatting context properties
    bool IsNewFormattingContext { get; }
    bool IsFloatContextEnabled { get; }
    
    // Margin collapsing state
    MarginStrut MarginStrut { get; }
    
    // For positioned elements
    Point BfcOffset { get; }
    
    // Create a new constraint space for a child
    IConstraintSpace CreateChildConstraintSpace(IElement child, ICssStyleDeclaration style);
}
```

### 3.3 LayoutFragment

The immutable output of a layout operation:

```csharp
public class LayoutFragment
{
    // Associated element
    public IElement Element { get; }
    
    // Box type (block, inline, etc.)
    public BoxType BoxType { get; }
    
    // Fragment geometry
    public LogicalSize LogicalSize { get; }
    public LogicalOffset LogicalOffset { get; }
    public PhysicalSize PhysicalSize { get; }
    public PhysicalOffset PhysicalOffset { get; }
    
    // Box model properties
    public Edges Margins { get; }
    public Edges Borders { get; }
    public Edges Paddings { get; }
    
    // Child fragments
    public IReadOnlyList<LayoutFragment> Children { get; }
    
    // For positioned elements
    public bool IsPositioned { get; }
    public PositionType PositionType { get; }
    
    // Create a new fragment with updated geometry
    public LayoutFragment CopyWithNewGeometry(LogicalSize newSize, LogicalOffset newOffset);
}
```

### 3.4 FormattingContexts

Specialized objects that implement layout algorithms:

```csharp
public interface IFormattingContext
{
    // Perform layout in this formatting context
    LayoutFragment Layout(IElement element, IConstraintSpace constraintSpace);
    
    // Calculate intrinsic sizes without full layout
    MinMaxSizes ComputeIntrinsicSizes(IElement element);
}

public class BlockFormattingContext : IFormattingContext { /* ... */ }
public class InlineFormattingContext : IFormattingContext { /* ... */ }
public class FlexFormattingContext : IFormattingContext { /* ... */ }
public class GridFormattingContext : IFormattingContext { /* ... */ }
```

### 3.5 BoxGeometryResolver

Handles box model calculations:

```csharp
public class BoxGeometryResolver
{
    // Calculate content box dimensions
    public LogicalSize ComputeContentBoxLogical(ICssStyleDeclaration style, IConstraintSpace space);
    
    // Calculate box model properties
    public Edges ComputeMargins(ICssStyleDeclaration style, IConstraintSpace space);
    public Edges ComputeBorders(ICssStyleDeclaration style);
    public Edges ComputePadding(ICssStyleDeclaration style, IConstraintSpace space);
    
    // Resolve percentage-based values
    public LogicalSize ResolvePercentages(LogicalSize size, IConstraintSpace space);
}
```

### 3.6 MarginCollapsingEngine

Handles margin collapsing according to CSS specifications:

```csharp
public class MarginCollapsingEngine
{
    // Collapse adjacent margins
    public MarginStrut CollapseAdjacentMargins(MarginStrut previous, MarginStrut current);
    
    // Determine if an element's margins can collapse
    public bool IsMarginCollapsible(ICssStyleDeclaration style);
    
    // Handle special case of empty blocks
    public bool IsEmptyBlockCollapsible(LayoutFragment fragment);
}
```

### 3.7 Specialized Components

Additional components for specific layout needs:

- **LineBreaker**: Handles line breaking for inline layout
- **IntrinsicSizesCalculator**: Calculates min/max content sizes
- **BlockFragmentationEngine**: Handles pagination and multicol fragmentation
- **LayoutCacheAdapter**: Interfaces with the caching system

## 4. Layout Process

### 4.1 Layout Phases

The layout process is divided into clear phases:

1. **Intrinsic Size Calculation**
    
    - Measure intrinsic min/max content sizes
    - Happens before actual layout
    - Needed for certain layout algorithms
    - Results can be cached independently
2. **Constraint Space Creation**
    
    - Create appropriate constraint space for element
    - Establish formatting context
    - Set up available space and percentage resolution size
    - Determine writing mode and direction
3. **Layout Algorithm Selection**
    
    - Based on display property and other style attributes
    - Select appropriate formatting context
    - Determine layout strategy (block, inline, flex, grid, etc.)
4. **Layout Execution**
    
    - Perform actual layout calculations
    - Calculate box model dimensions
    - Position children as appropriate
    - Resolve auto values
5. **Fragment Construction**
    
    - Build immutable fragment representing layout result
    - Assemble child fragments
    - Apply positioning
    - Create final layout result

### 4.2 Calculation Flow

1. **Top-down constraint propagation**
    
    - Constraints flow from parent to children
    - Each parent creates appropriate constraints for its children
2. **Bottom-up fragment assembly**
    
    - Layout starts from leaf nodes
    - Child fragments are assembled into parent fragments
    - Final fragments represent complete layout
3. **Interleaved intrinsic size calculation**
    
    - Some layout algorithms require knowledge of intrinsic sizes
    - Calculated on demand and cached

### 4.3 Specialized Layout Algorithms

1. **Block Formatting Context**
    
    - Handles normal flow block layout
    - Manages margin collapsing
    - Calculates block dimensions according to CSS rules
2. **Inline Formatting Context**
    
    - Creates and positions line boxes
    - Handles text layout
    - Manages inline box positioning
3. **Flex Formatting Context**
    
    - Implements flexbox layout algorithm
    - Distributes space according to flex factors
    - Handles alignment and justification
4. **Grid Formatting Context**
    
    - Establishes grid tracks
    - Places items in grid cells
    - Handles alignment and spanning

## 5. Integration with Existing Modules

### 5.1 Style Computation Module Integration

The existing StyleComputationEngine will need minimal changes but will be used differently:

**Current Usage**:

```csharp
// Current direct usage
var style = _styleEngine.ComputeElementStyle(element);
```

**LayoutNG-style Usage**:

```csharp
// More optimized property access
var style = _styleEngine.ComputeElementStyle(element);
var display = style.GetComputedValue(CssProperties.Display);
var position = style.GetComputedValue(CssProperties.Position);

// Use style in constraint-based calculations
var constraintSpace = CreateConstraintSpace(style);
var fragment = _formattingContext.Layout(element, constraintSpace);
```

**Recommended Enhancements**:

1. Add fast-path property access methods for layout-critical properties
2. Consider adding computed value caching for repeated property access
3. Ensure efficient handling of logical properties based on writing mode

### 5.2 Cache Module Integration

The existing caching system needs significant enhancement to support fragment-based caching:

**Current Approach**:

```csharp
// Current element-based caching
var key = new LayoutCacheKey(element, containerWidth, containerHeight);
var layoutBox = _layoutCache.GetOrAdd(key, _ => ComputeLayout(element, constraints));
```

**LayoutNG-style Approach**:

```csharp
// Fragment and constraint-based caching
var key = new FragmentCacheKey(element, constraintSpace);
var fragment = _fragmentCache.GetOrAdd(key, _ => {
    // Only compute if not in cache
    return _formattingContext.Layout(element, constraintSpace);
});

// Separate caching for intrinsic sizes
var sizeKey = new IntrinsicSizesCacheKey(element);
var minMaxSizes = _intrinsicSizesCache.GetOrAdd(sizeKey, _ => {
    return _intrinsicSizesCalculator.ComputeMinMaxSizes(element);
});
```

**Recommended Enhancements**:

1. Create a dedicated FragmentCache to store immutable fragments
2. Implement separate IntrinsicSizeCache for min/max size calculations
3. Develop more sophisticated cache keys that incorporate constraint properties
4. Enhance dependency tracking to support fragment invalidation

### 5.3 Document Lifecycle Module Integration

The DocumentLifecycleModule will need enhancements to support fine-grained invalidation:

**Current Approach**:

```csharp
// Current element-level invalidation
_invalidationManager.InvalidateElement(element, InvalidationType.Layout);
```

**LayoutNG-style Approach**:

```csharp
// More granular invalidation
_invalidationManager.InvalidateElementLayout(element, InvalidationReason.StyleChange);
_invalidationManager.InvalidateFragmentsInSubtree(element, subtreeRoot);
_invalidationManager.InvalidateIntrinsicSizes(element);
```

**Recommended Enhancements**:

1. Add more granular invalidation types (intrinsic sizes, fragments, constraints)
2. Enhance LayoutInvalidationTracker to track fragment dependencies
3. Implement containment-aware invalidation for performance
4. Create special handling for layout-impacting style changes

## 6. Key Data Structures

### 6.1 ConstraintSpace

The ConstraintSpace is central to the layout process:

```csharp
public class BlockConstraintSpace : IConstraintSpace
{
    // Available size for content
    public Size AvailableSize { get; }
    
    // Size for percentage resolution
    public Size PercentageResolutionSize { get; }
    
    // Writing mode properties
    public WritingMode WritingMode { get; }
    public TextDirection Direction { get; }
    
    // Formatting context flags
    public bool IsNewFormattingContext { get; }
    public bool IsFloatContextEnabled { get; }
    public bool IsInsideFloatContext { get; }
    
    // Margin collapsing state
    public MarginStrut MarginStrut { get; }
    
    // For absolute positioning
    public Point BfcOffset { get; }
    
    // Create a constraint space for children
    public IConstraintSpace CreateChildConstraintSpace(IElement child, ICssStyleDeclaration style)
    {
        // Determine if child creates a new formatting context
        bool isNewBfc = DetermineIfNewFormattingContext(style);
        
        // Calculate available size for child
        Size childAvailableSize = CalculateAvailableSizeForChild(style);
        
        // Create appropriate constraint space
        return new BlockConstraintSpace
        {
            AvailableSize = childAvailableSize,
            PercentageResolutionSize = this.PercentageResolutionSize,
            WritingMode = DetermineWritingMode(style, this.WritingMode),
            Direction = DetermineDirection(style, this.Direction),
            IsNewFormattingContext = isNewBfc,
            // Other properties...
        };
    }
}
```

### 6.2 LayoutResult

The output of layout operations:

```csharp
public class LayoutResult
{
    // The root fragment of this layout operation
    public LayoutFragment Fragment { get; }
    
    // Intrinsic sizes
    public MinMaxSizes IntrinsicSizes { get; }
    
    // Overflow information
    public OverflowData OverflowData { get; }
    
    // For fragmentation (pagination/multicol)
    public BreakToken BreakToken { get; }
    public bool HasBlockFragmentation { get; }
    
    // For positioned elements
    public IReadOnlyList<OutOfFlowFragment> OutOfFlowFragments { get; }
}
```

### 6.3 FragmentBuilder

Helper for constructing fragments:

```csharp
public class FragmentBuilder
{
    // Associated element and style
    public IElement Element { get; set; }
    public ICssStyleDeclaration Style { get; set; }
    
    // Box properties
    public BoxType BoxType { get; set; }
    public LogicalSize LogicalSize { get; set; }
    public LogicalOffset LogicalOffset { get; set; }
    public Edges Margins { get; set; }
    public Edges Borders { get; set; }
    public Edges Paddings { get; set; }
    
    // Child fragments
    public List<LayoutFragment> Children { get; } = new List<LayoutFragment>();
    
    // Build the fragment
    public LayoutFragment ToFragment()
    {
        return new LayoutFragment(
            Element,
            BoxType,
            LogicalSize,
            LogicalOffset,
            Margins,
            Borders,
            Paddings,
            Children.AsReadOnly());
    }
}
```

## 7. Changes to Existing Modules

### 7.1 Style Computation Module Changes

1. **Add Layout-Optimized Property Access**
    
    ```csharp
    // Add specialized methods for layout access patterns
    public static class StyleExtensions 
    {
        public static Display GetDisplay(this ICssStyleDeclaration style) 
        {
            // Fast-path for display property
            // Cache result for repeated access
        }
        
        public static Position GetPosition(this ICssStyleDeclaration style) 
        {
            // Fast-path for position property
        }
        
        // More specialized accessors
    }
    ```
    
2. **Enhance Logical Property Support**
    
    ```csharp
    // Add logical property resolution
    public static LogicalLength GetInlineSize(this ICssStyleDeclaration style, WritingMode writingMode)
    {
        // Return width or height depending on writing mode
    }
    
    public static LogicalLength GetBlockSize(this ICssStyleDeclaration style, WritingMode writingMode)
    {
        // Return height or width depending on writing mode
    }
    ```
    

### 7.2 Cache Module Changes

1. **Create Fragment Cache Classes**
    
    ```csharp
    public class FragmentCache : IComputationCache<FragmentCacheKey, LayoutFragment>
    {
        // Similar to existing cache but with fragment-specific optimizations
    }
    
    public class IntrinsicSizeCache : IComputationCache<IntrinsicSizesCacheKey, MinMaxSizes>
    {
        // Specialized for caching min/max sizes
    }
    ```
    
2. **Enhance Layout Cache Keys**
    
    ```csharp
    public class FragmentCacheKey : IEquatable<FragmentCacheKey>
    {
        public IElement Element { get; }
        public IConstraintSpace ConstraintSpace { get; }
        
        // Add equality and hashing implementation
    }
    ```
    
3. **Update LayoutEngineCacheManager**
    
    ```csharp
    public interface ILayoutEngineCacheManager
    {
        // Add new methods
        FragmentCache FragmentCache { get; }
        IntrinsicSizeCache IntrinsicSizeCache { get; }
        
        // Specialized invalidation
        void InvalidateFragments(IElement element);
        void InvalidateIntrinsicSizes(IElement element);
    }
    ```
    

### 7.3 Document Lifecycle Module Changes

1. **Enhance LayoutInvalidationTracker**
    
    ```csharp
    public class LayoutInvalidationTracker
    {
        // Add granular tracking
        private HashSet<IElement> _fragmentsDirty = new HashSet<IElement>();
        private HashSet<IElement> _intrinsicSizesDirty = new HashSet<IElement>();
        
        // Specialized methods
        public void MarkFragmentsDirty(IElement element)
        {
            _fragmentsDirty.Add(element);
        }
        
        public void MarkIntrinsicSizesDirty(IElement element)
        {
            _intrinsicSizesDirty.Add(element);
        }
        
        // Getters for dirty elements
        public IEnumerable<IElement> GetFragmentsDirtyElements() => _fragmentsDirty;
        public IEnumerable<IElement> GetIntrinsicSizesDirtyElements() => _intrinsicSizesDirty;
    }
    ```
    
2. **Update DocumentLifecycleManager**
    
    ```csharp
    // Add new states
    public enum LifecycleState
    {
        Initial,
        StyleDirty,
        StyleClean,
        IntrinsicSizesDirty,
        IntrinsicSizesClean,
        LayoutDirty,
        LayoutClean,
        // Other states...
    }
    
    // Update process method
    public void ProcessPendingUpdates()
    {
        if (CurrentState == LifecycleState.StyleDirty)
        {
            RecalculateStyles();
            CurrentState = LifecycleState.StyleClean;
        }
        
        if (CurrentState == LifecycleState.IntrinsicSizesDirty)
        {
            RecalculateIntrinsicSizes();
            CurrentState = LifecycleState.IntrinsicSizesClean;
        }
        
        if (CurrentState == LifecycleState.LayoutDirty)
        {
            RecalculateLayouts();
            CurrentState = LifecycleState.LayoutClean;
        }
    }
    ```
    

## 8. Implementation Strategy and Phasing

### 8.1 Implementation Phases

1. **Phase 1: Foundation (2-3 months)**
    
    - Implement core data structures (ConstraintSpace, LayoutFragment, etc.)
    - Create basic BoxGeometryResolver
    - Develop fragment construction mechanism
    - Set up fragment-based caching foundation
    - Update document lifecycle for fragment invalidation
2. **Phase 2: Block Layout (2-3 months)**
    
    - Implement block formatting context
    - Create margin collapsing engine
    - Develop basic block layout algorithms
    - Add intrinsic size calculation
    - Build integration with style computation
3. **Phase 3: Positioned Elements (1-2 months)**
    
    - Implement positioning algorithms
    - Add absolute/fixed positioning
    - Develop relative positioning
    - Create stacking context handling
    - Implement z-index sorting
4. **Phase 4: Inline Layout (2-3 months)**
    
    - Implement inline formatting context
    - Create line breaking algorithm
    - Add text measurement and layout
    - Implement inline box model
    - Develop mixed inline/block contexts
5. **Phase 5: Modern Layout Models (3-4 months)**
    
    - Implement flex formatting context
    - Create grid formatting context
    - Add multi-column layout
    - Develop table layout algorithm
    - Implement specialized layouts
6. **Phase 6: Optimization & Finalization (2-3 months)**
    
    - Implement containment optimization
    - Add incremental layout improvements
    - Optimize performance
    - Implement threading support
    - Finalize APIs and documentation

### 8.2 Transition Strategy

1. **Parallel Development**
    
    - Develop the LayoutNG-inspired engine alongside existing components
    - Create adapters to work with current StyleComputation and Cache modules
    - Build unit tests to validate layout against browser results
2. **Incremental Integration**
    
    - Update StyleComputation Module first
    - Enhance Cache Module for fragment support
    - Modify DocumentLifecycle for new invalidation model
    - Integrate gradually with client code
3. **Bridge Classes**
    
    - Create adapter classes to expose traditional box-model interface
    - Allow gradual transition from old APIs to new ones
    - Maintain backward compatibility where possible
4. **Evaluation Metrics**
    
    - Define clear success criteria for each phase
    - Create performance benchmarks to measure improvement
    - Implement visual regression testing against browsers
    - Track memory and CPU usage

## 9. Component Responsibilities and Interactions

### 9.1 LayoutEngine Responsibilities

- Serve as the main entry point for layout operations
- Select appropriate formatting context for elements
- Create initial constraint space
- Coordinate with caching system
- Handle interruption and resumption of layout
- Provide API for layout operations

### 9.2 FormattingContext Responsibilities

- Implement specific layout algorithms
- Layout children according to CSS rules
- Calculate intrinsic sizes when needed
- Position fragments within the formatting context
- Handle specialized layout features

### 9.3 BoxGeometryResolver Responsibilities

- Calculate box model dimensions
- Resolve percentage-based values
- Apply 'box-sizing' rules
- Handle min/max constraints
- Compute margins, borders, padding

### 9.4 LayoutFragmentTree Responsibilities

- Build the fragment tree from layout operations
- Assemble child fragments into parent fragments
- Apply writing mode transformations
- Handle fragment geometry updates
- Provide access to the fragment tree

### 9.5 Key Component Interactions

1. **Layout Request Flow**
    
    ```
    Client → LayoutEngine → FormattingContextFactory → 
    Appropriate FormattingContext → BoxGeometryResolver →
    Child Layout Operations → Fragment Construction → 
    Layout Result
    ```
    
2. **Caching Interaction**
    
    ```
    LayoutEngine → Check Cache → Cache Hit → Return Cached Fragment
                               → Cache Miss → Perform Layout → 
                                              Store in Cache → Return Fragment
    ```
    
3. **Invalidation Flow**
    
    ```
    DOM Mutation → InvalidationManager → LayoutInvalidationTracker →
    Mark Elements Dirty → DocumentLifecycleManager → 
    Schedule Layout Updates → ProcessPendingUpdates →
    LayoutEngine → Perform Layout on Dirty Elements
    ```
    

## 10. Technical Considerations and Challenges

### 10.1 Performance Considerations

1. **Memory Usage**
    
    - Immutable fragments may increase memory usage
    - Implement fragment pooling for reuse
    - Consider object sharing for common values
    - Add memory monitoring utilities
2. **CPU Efficiency**
    
    - Optimize constraint space creation
    - Implement fast paths for common layout scenarios
    - Use specialized algorithms for different layout types
    - Consider JIT-friendly data structures
3. **Layout Speed**
    
    - Focus on incremental layout for dynamic content
    - Implement containment optimization
    - Leverage multi-threading where possible
    - Optimize critical layout paths

### 10.2 Technical Challenges

1. **Margin Collapsing**
    
    - Complex CSS specification rules
    - Interaction with BFCs
    - Empty blocks handling
    - Nested collapsing cases
2. **Writing Modes Support**
    
    - Logical vs. physical coordinates
    - BiDi text handling
    - Complex writing mode transformations
    - Unified algorithm for all writing modes
3. **Float Layout**
    
    - Complex positioning requirements
    - Interaction with BFCs
    - Clear property handling
    - Line breaking around floats
4. **Flexbox and Grid**
    
    - Complex distribution algorithms
    - Intrinsic sizing dependencies
    - Auto placement algorithms
    - Track sizing complexity
5. **Incremental Layout**
    
    - Determining minimal subtree to relayout
    - Preserving layout information
    - Handling dependencies between elements
    - Balancing performance and accuracy

## 11. Conclusion

The proposed LayoutNG-inspired architecture for AngleSharp provides a modern, high-performance approach to CSS layout that aligns with current browser engine design principles. By adopting constraint-based layout, immutable fragments, and clear phase separation, the system will be able to handle complex CSS features accurately while providing good performance characteristics.

The implementation strategy acknowledges the complexity involved and proposes a phased approach that builds incrementally toward the full vision. Integration with existing AngleSharp modules is carefully considered, with appropriate changes recommended to support the new architecture.

While more complex than a traditional mutable box model approach, this architecture offers significant benefits in terms of correctness, performance, and future extensibility. The system will be better positioned to handle modern CSS features and will provide more accurate layout results that match browser behavior.