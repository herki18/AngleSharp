# AngleSharp Layout Engine - Updated Architecture Overview

## 1. System Overview

The AngleSharp Layout Engine is a comprehensive system that extends AngleSharp with full styling, layout computation, and rendering capabilities. It is designed as a modular system with clear boundaries between components, allowing for independent development, testing, and maintenance.

The system consists of four primary modules:

1. **Style Computation Module**: Computes CSS styles for DOM elements
2. **Document Lifecycle Module**: Tracks DOM mutations and manages invalidation
3. **Layout Engine Module**: Computes element layout and positioning using a modern constraint-based approach
4. **Caching Module**: Provides efficient caching with dependency tracking

These modules work together to provide a complete pipeline from DOM mutations to final rendering, with optimizations at each stage to ensure performance.

## 2. Core Architecture Principles

The architecture is guided by the following principles:

1. **Clear Module Boundaries**: Each module has a well-defined responsibility and interface
2. **Reactive Updates**: Changes to the DOM trigger appropriate invalidations and recalculations
3. **Minimal Recomputation**: Only affected elements are recalculated
4. **Efficient Caching**: Results are cached with intelligent invalidation
5. **Browser-Like Architecture**: Follows patterns used in modern browser engines
6. **Performance Optimization**: Designed for efficient handling of complex documents
7. **Immutable Output**: Layout produces immutable fragments rather than modifying mutable boxes
8. **Constraint-Based Layout**: Layout is driven by explicitly defined constraints
9. **Phase Separation**: Clear separation between box tree construction, intrinsic size calculation, layout, and positioning
10. **Intermediate Representation**: Box tree provides an optimized intermediate representation for layout algorithms

## 3. Module Descriptions

### 3.1 Style Computation Module

The Style Computation Module is responsible for computing CSS styles for DOM elements. It processes style rules, matches them against elements, and computes final property values.

#### Key Components:

- **StyleComputationEngine**: Main orchestrator for style computation
- **StyleSheetManager**: Manages stylesheets from different origins
- **SelectorMatcher**: Matches selectors against elements
- **CascadeResolver**: Resolves property conflicts
- **InheritanceProcessor**: Handles inheritance chains
- **ValueComputer**: Computes final property values
- **VariableResolver**: Resolves CSS custom property (variable) references
- **VariableRegistry**: Tracks and manages CSS variables

#### Primary Interfaces:

```csharp
// Main entry point for style computation
public interface IStyleComputationEngine
{
    ICssStyleDeclaration ComputeElementStyle(IElement element, 
        ICssStyleDeclaration parentStyle = null, 
        string pseudoElement = null);
        
    // New method to support layout-specific property access
    T GetComputedValue<T>(IElement element, string propertyName);
}

// Manages stylesheets from different origins
public interface IStyleSheetManager
{
    void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);
    void UnregisterStylesheet(ICssStyleSheet stylesheet);
    void SetDocument(IDocument document);
    IEnumerable<StylesheetEntry> GetStylesheets();
}

// New interface for logical property resolution
public interface IStylePropertyResolver
{
    LogicalLength GetInlineSize(ICssStyleDeclaration style, WritingMode writingMode);
    LogicalLength GetBlockSize(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalMargins(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalPadding(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalBorders(ICssStyleDeclaration style, WritingMode writingMode);
}
```

### 3.2 Document Lifecycle Module

The Document Lifecycle Module is responsible for observing DOM mutations, determining what needs to be invalidated, and coordinating updates.

#### Key Components:

- **DocumentLifecycleManager**: Manages document state and transitions
- **MutationObserverAdapter**: Bridges to AngleSharp's MutationObserver
- **InvalidationManager**: Determines what needs invalidation
- **StyleInvalidationTracker**: Tracks elements needing style recalculation
- **BoxTreeInvalidationTracker**: Tracks elements needing box tree reconstruction
- **LayoutInvalidationTracker**: Tracks elements needing layout recalculation
- **IntrinsicSizesInvalidationTracker**: Tracks elements needing intrinsic size recalculation
- **SchedulingService**: Controls when updates happen

#### Primary Interfaces:

```csharp
// Central coordinator for document lifecycle
public interface IDocumentLifecycleManager
{
    LifecycleState CurrentState { get; }
    void ScheduleStyleUpdate();
    void ScheduleBoxTreeUpdate(); // New method for box tree phase
    void ScheduleIntrinsicSizesUpdate();
    void ScheduleLayoutUpdate();
    void ProcessPendingUpdates();
    event EventHandler<LifecycleStateChangedEventArgs> StateChanged;
}

// Manages invalidation across different aspects
public interface IInvalidationManager
{
    void ProcessMutation(IMutationRecord mutation);
    void InvalidateElement(IElement element, InvalidationFlags flags);
    void InvalidateElementBoxTree(IElement element); // New method for box tree invalidation
    void InvalidateElementIntrinsicSizes(IElement element);
    void InvalidateElementFragments(IElement element);
    void InvalidateStylesheet(ICssStyleSheet stylesheet);
}

// Enhanced lifecycle states with box tree phase
public enum LifecycleState
{
    Initial,
    StyleDirty,
    StyleClean,
    BoxTreeDirty, // New state for box tree phase
    BoxTreeClean, // New state for completed box tree
    IntrinsicSizesDirty,
    IntrinsicSizesClean,
    LayoutDirty,
    LayoutClean,
    PaintDirty,
    PaintClean
}
```

### 3.3 Layout Engine Module (LayoutNG-Inspired)

The Layout Engine Module is responsible for computing element layout and positioning using a modern constraint-based approach inspired by browser engines like Blink's LayoutNG.

#### Key Components:

- **LayoutEngine**: Main orchestrator for layout computation
- **BoxTreeBuilder**: Constructs the box tree from the DOM and computed styles
- **FormattingContextFactory**: Creates appropriate formatting contexts
- **LayoutFragmentTree**: Manages the tree of layout fragments
- **BoxGeometryResolver**: Computes box model dimensions
- **ConstraintSpace**: Encapsulates layout constraints and available space
- **MarginCollapsingEngine**: Handles margin collapsing per CSS spec
- **IntrinsicSizesCalculator**: Calculates min/max content sizes
- **LineBreaker**: Handles text wrapping and line creation

#### Box Tree Components:

- **NGBox**: Base class for all box tree nodes
- **NGBlockBox**: Represents a block-level box
- **NGInlineBox**: Represents an inline-level box
- **NGTextBox**: Represents a text node
- **NGFlexBox**: Represents a flex container box
- **NGGridBox**: Represents a grid container box
- **NGBoxFragment**: Represents a box fragment after layout

#### Formatting Contexts:

- **BlockFormattingContext**: Handles block layout
- **InlineFormattingContext**: Handles inline and text layout
- **FlexFormattingContext**: Handles flexbox layout
- **GridFormattingContext**: Handles grid layout

#### Data Structures:

- **LayoutFragment**: Immutable result of layout computation
- **FragmentBuilder**: Helper for constructing fragments
- **LayoutResult**: Container for complete layout results
- **MinMaxSizes**: Contains intrinsic size information

#### Primary Interfaces:

```csharp
// Main entry point for layout computation
public interface ILayoutEngine
{
    // Main layout method
    LayoutResult Layout(IElement element, ConstraintSpace constraintSpace = null);
    
    // Box tree construction
    NGBox BuildBoxTree(IElement element, ICssStyleDeclaration style = null);
    
    // Intrinsic size calculation (separate from full layout)
    MinMaxSizes ComputeMinMaxSizes(NGBox box);
    
    // Create constraint space
    ConstraintSpace CreateConstraintSpace(NGBox box);
    
    // Specialized layout methods
    LayoutResult LayoutBlock(NGBlockBox box, BlockConstraintSpace constraintSpace);
    LayoutResult LayoutInline(NGInlineBox box, InlineConstraintSpace constraintSpace);
    LayoutResult LayoutFlex(NGFlexBox box, FlexConstraintSpace constraintSpace);
    LayoutResult LayoutGrid(NGGridBox box, GridConstraintSpace constraintSpace);
}

// Box tree node interface
public interface INGBox
{
    // Box properties
    BoxType BoxType { get; }
    ICssStyleDeclaration Style { get; }
    IElement DomElement { get; }
    
    // Box relationships
    INGBox Parent { get; }
    IReadOnlyList<INGBox> Children { get; }
    
    // Box context
    WritingMode WritingMode { get; }
    TextDirection Direction { get; }
    
    // Box flags
    bool IsAnonymous { get; }
    bool CreatesFormattingContext { get; }
}

// Constraint space for layout
public interface IConstraintSpace
{
    // Available space for layout
    Size AvailableSize { get; }
    
    // Size to resolve percentages against
    Size PercentageResolutionSize { get; }
    
    // Writing mode properties
    WritingMode WritingMode { get; }
    TextDirection Direction { get; }
    
    // Formatting context flags
    bool IsNewFormattingContext { get; }
    bool IsFloatContextEnabled { get; }
    
    // Margin collapsing state
    MarginStrut MarginStrut { get; }
    
    // For positioned elements
    Point BfcOffset { get; }
    
    // Create child constraint space
    IConstraintSpace CreateChildConstraintSpace(INGBox childBox);
}

// Formatting context interface
public interface IFormattingContext
{
    // Perform layout in this formatting context
    LayoutFragment Layout(INGBox box, IConstraintSpace constraintSpace);
    
    // Calculate intrinsic sizes
    MinMaxSizes ComputeIntrinsicSizes(INGBox box);
}

// Box tree builder interface
public interface IBoxTreeBuilder
{
    // Build box tree for an element
    NGBox BuildBoxTree(IElement element, ICssStyleDeclaration style = null);
    
    // Update existing box tree
    NGBox UpdateBoxTree(NGBox existingBox, IElement element, ICssStyleDeclaration style = null);
    
    // Special box creation
    NGBlockBox CreateAnonymousBlockBox();
    NGInlineBox CreateAnonymousInlineBox();
}

// Immutable layout result
public class LayoutFragment
{
    // Associated box
    public INGBox SourceBox { get; }
    
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

### 3.4 Caching Module

The Caching Module provides efficient caching of computed styles and layouts with dependency tracking for intelligent invalidation.

#### Key Components:

- **LayoutEngineCacheManager**: Central cache coordinator
- **StyleCache**: Caches computed styles
- **BoxTreeCache**: Caches box tree nodes
- **FragmentCache**: Caches layout fragments
- **IntrinsicSizeCache**: Caches intrinsic sizes
- **CacheDependencyTracker**: Tracks dependencies for invalidation
- **EnhancedDependencyTracker**: Enhanced dependency tracking

#### Primary Interfaces:

```csharp
// Generic caching interface
public interface IComputationCache<TKey, TValue> where TKey : notnull
{
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);
    bool TryGetValue(TKey key, out TValue value);
    void AddOrUpdate(TKey key, TValue value);
    bool Remove(TKey key);
    void Clear();
    int Count { get; }
}

// Central cache manager with box tree caching
public interface ILayoutEngineCacheManager
{
    IStyleCache StyleCache { get; }
    IBoxTreeCache BoxTreeCache { get; } // New: Box tree cache
    IFragmentCache FragmentCache { get; }
    IIntrinsicSizeCache IntrinsicSizeCache { get; }
    ILayoutBoxCache<TLayoutData> GetLayoutCache<TLayoutData>();
    void InvalidateAll();
    void InvalidateElement(IElement element);
    void InvalidateDocument(IDocument document);
    void InvalidateBoxTree(IElement element); // New: Box tree invalidation
    void InvalidateFragments(IElement element);
    void InvalidateIntrinsicSizes(IElement element);
}

// Box tree cache key
public class BoxTreeCacheKey : IEquatable<BoxTreeCacheKey>
{
    public IElement Element { get; }
    public string PseudoElement { get; }
    
    // Equality and hashing implementation
}

// Fragment cache key
public class FragmentCacheKey : IEquatable<FragmentCacheKey>
{
    public INGBox Box { get; } // Changed from Element to Box
    public IConstraintSpace ConstraintSpace { get; }
    
    // Equality and hashing implementation
}

// Intrinsic size cache key
public class IntrinsicSizeCacheKey : IEquatable<IntrinsicSizeCacheKey>
{
    public INGBox Box { get; } // Changed from Element to Box
    public WritingMode WritingMode { get; }
    
    // Equality and hashing implementation
}
```

## 4. Box Tree Architecture

### 4.1 Purpose and Benefits

The Box Tree is a crucial intermediate representation between the DOM and Layout Fragments. It provides several key benefits:

1. **Optimization**: Simplifies layout algorithms by preprocessing the DOM into a structure optimized for layout
2. **Anonymous Box Handling**: Manages creation and tracking of anonymous boxes required by CSS specifications
3. **Text Processing**: Provides specialized handling for text nodes, including whitespace collapsing and line breaking
4. **Layout Consistency**: Ensures proper nesting of block and inline elements according to CSS rules
5. **Algorithm Specialization**: Allows specialized box types for different layout models (block, inline, flex, grid)
6. **Efficiency**: Reduces redundant style lookups during layout operations
7. **Memory Management**: Controls object allocation and reuse during layout

### 4.2 Box Tree Construction Process

The box tree is constructed in a dedicated phase before layout:

1. **Style-Driven Construction**: Uses computed styles to determine box types
2. **Normalization**: Creates anonymous boxes as needed to normalize the structure
3. **Box Determination**:
    - Elements with `display: block` become `NGBlockBox`
    - Elements with `display: inline` become `NGInlineBox`
    - Elements with `display: flex` become `NGFlexBox`
    - Elements with `display: grid` become `NGGridBox`
    - Text nodes become `NGTextBox`
4. **Formatting Context Determination**: Identifies which boxes create new formatting contexts
5. **Writing Mode Processing**: Attaches writing mode and direction information
6. **Box Properties**: Attaches computed styles and other layout-critical properties

### 4.3 Box Types and Hierarchy

The Box Tree uses a specialized hierarchy of box types:

```
NGBox (abstract base)
├── NGBlockBox
│   ├── NGFlexBox
│   ├── NGGridBox
│   └── NGTableBox
├── NGInlineBox
│   ├── NGInlineBlockBox
│   ├── NGInlineFlexBox
│   └── NGReplacedInlineBox
├── NGTextBox
├── NGListItemBox
└── NGReplacedBox
```

Each box type encapsulates specialized behavior for different layout models and CSS features.

### 4.4 Anonymous Box Creation

The Box Tree handles creation of anonymous boxes required by CSS:

1. **Block-in-Inline Fixup**: Creates anonymous block boxes when block children appear within inline parents
2. **Text Wrapper**: Creates anonymous inline boxes to contain text nodes within block containers
3. **Table Structure**: Creates anonymous table structure boxes for proper table layout
4. **Flex/Grid Items**: Creates anonymous flex/grid items as needed for proper container layout

### 4.5 Integration with Layout Process

The Box Tree integrates with the layout process as follows:

1. **Construction Phase**:
    
    - DOM + Styles → Box Tree
    - Cached when possible for performance
2. **Intrinsic Size Phase**:
    
    - Box Tree → Intrinsic Sizes
    - Calculated on box tree nodes
3. **Layout Phase**:
    
    - Box Tree + Constraints → Layout Fragments
    - Fragments reference their source boxes

## 5. Cross-Module Interactions

The modules interact in the following ways:

### 5.1 DOM Mutation → Lifecycle → Style → Box Tree → Layout Pipeline

1. **DOM Mutation Detection**:
    - `MutationObserver` detects DOM changes
    - `MutationObserverAdapter` processes mutation records
2. **Invalidation Analysis**:
    - `InvalidationManager` analyzes mutations
    - `DependencyTracker` identifies affected elements
    - Specialized trackers mark elements for different invalidation types
3. **Document Lifecycle Management**:
    - `DocumentLifecycleManager` updates document state
    - `SchedulingService` schedules updates
4. **Style Recalculation**:
    - `StyleComputationEngine` recalculates styles for invalidated elements
    - `StyleCache` is updated with new computed styles
5. **Box Tree Construction**:
    - `BoxTreeBuilder` constructs the box tree for elements with updated styles
    - `BoxTreeCache` is updated with new box trees
6. **Intrinsic Size Calculation**:
    - `IntrinsicSizesCalculator` calculates min/max sizes for box tree nodes
    - Results are stored in `IntrinsicSizeCache`
7. **Layout Calculation**:
    - `LayoutEngine` creates constraint spaces for box tree nodes
    - Appropriate `FormattingContext` runs layout algorithm
    - Produces `LayoutFragment` tree
    - Results stored in `FragmentCache`

### 5.2 Constraint Propagation and Fragment Assembly

The layout process follows a two-phase approach:

1. **Top-down Constraint Propagation**:
    - Parent boxes create constraint spaces for children
    - Constraints flow down the box tree
    - Each box receives an appropriate `ConstraintSpace`
2. **Bottom-up Fragment Assembly**:
    - Leaf boxes (those without children) are laid out first
    - Child fragments are assembled into parent fragments
    - Final fragments represent the complete layout

### 5.3 Style → Box Tree → Layout Integration

The integration between modules is enhanced with the box tree:

1. **Style to Box Tree Integration**:
    - Computed styles determine box types
    - Box tree normalizes structure based on CSS rules
    - Style properties are stored in box objects
2. **Box Tree to Layout Integration**:
    - Layout algorithms operate on specialized box types
    - Box properties provide cached access to critical information
    - Formatting contexts work with the box tree

### 5.4 Cache Invalidation Paths

1. **DOM Mutation → Cache Invalidation**:
    - Mutations trigger cache invalidation through the invalidation system
    - `InvalidationManager` determines which cache entries to invalidate
    - `CacheDependencyTracker` provides information about dependencies
2. **Style Update → Box Tree Invalidation**:
    - Style changes invalidate related box tree entries
    - `StyleInvalidationTracker` informs `BoxTreeInvalidationTracker`
3. **Box Tree Update → Size and Layout Cache Invalidation**:
    - Box tree changes invalidate related size and layout cache entries
    - `BoxTreeInvalidationTracker` informs `IntrinsicSizesInvalidationTracker` and `LayoutInvalidationTracker`
4. **Stylesheet Change → Style Cache Invalidation**:
    - Stylesheet changes invalidate affected style cache entries
    - `InvalidationManager.InvalidateStylesheet` triggers appropriate invalidation

## 6. System States and Transitions

The system maintains state through the `DocumentLifecycleManager`, which tracks the current state of the document and manages transitions between states:

### 6.1 Enhanced Lifecycle States

- **Initial**: Document is in its initial state
- **StyleDirty**: Document needs style recalculation
- **StyleClean**: Styles are up to date
- **BoxTreeDirty**: Document needs box tree reconstruction
- **BoxTreeClean**: Box tree is up to date
- **IntrinsicSizesDirty**: Document needs intrinsic size recalculation
- **IntrinsicSizesClean**: Intrinsic sizes are up to date
- **LayoutDirty**: Document needs layout recalculation
- **LayoutClean**: Layout is up to date
- **PaintDirty**: Document needs visual update (future)
- **PaintClean**: Visual representation is up to date (future)

### 6.2 Key State Transitions

1. **DOM Mutation → StyleDirty**: Mutations that affect styles
2. **StyleDirty → StyleClean**: Style recalculation completes
3. **StyleClean → BoxTreeDirty**: Style changes affect box tree
4. **BoxTreeDirty → BoxTreeClean**: Box tree construction completes
5. **BoxTreeClean → IntrinsicSizesDirty**: Box tree changes affect intrinsic sizes
6. **IntrinsicSizesDirty → IntrinsicSizesClean**: Size recalculation completes
7. **IntrinsicSizesClean → LayoutDirty**: Size changes affect layout
8. **LayoutDirty → LayoutClean**: Layout recalculation completes
9. **LayoutClean → PaintDirty**: Layout changes affect visual representation
10. **PaintDirty → PaintClean**: Visual update completes

The system enforces valid state transitions to ensure consistent operation.

## 7. Performance Optimization Strategies

The architecture includes several performance optimization strategies:

### 7.1 Minimal Recalculation

- Only elements affected by changes are recalculated
- Dependency tracking identifies precisely what needs updating
- Containment boundaries limit the scope of changes
- Box tree provides optimized representation for layout algorithms
- Separate intrinsic size and layout phases avoid unnecessary calculations

### 7.2 Efficient Caching

- Style, box tree, intrinsic size, and layout results are cached
- Box-based cache keys for precise invalidation
- Intelligent invalidation based on detailed dependency tracking
- Different cache entries for different contexts (e.g., viewport sizes)
- Box tree reuse when possible

### 7.3 Memory Optimization

- Box tree object pooling for frequently created/destroyed boxes
- Anonymous box reuse when possible
- Flyweight pattern for shared box properties
- Specialized text handling to minimize object allocation

### 7.4 Batched Processing

- Related mutations are processed together
- Updates are scheduled in batches
- Non-critical updates can be deferred

### 7.5 Formatting Context Optimizations

- Specialized algorithms for different layout types
- Fast paths for common layout scenarios
- Skip unnecessary phases when possible
- Reuse fragments when appropriate

### 7.6 Fragment Immutability Benefits

- Thread safety for concurrent operations
- Simpler memory management
- Easier debugging and testing
- Efficient change detection

## 8. Error Handling and Resilience

The system includes robust error handling:

### 8.1 Graceful Degradation

- If part of the system fails, it can fall back to simpler approaches
- Default styles and layouts are provided as fallbacks
- System continues to function even with partial failures

### 8.2 Error Recovery

- The system can recover from invalid states
- Timeouts prevent infinite loops or excessive computation
- Error boundaries contain failures to specific components

### 8.3 Logging and Diagnostics

- Comprehensive error logging helps diagnose issues
- Performance metrics identify bottlenecks
- Diagnostic tools help understand system behavior

## 9. Extension Points

The architecture includes several extension points:

### 9.1 Custom Box Types

- Additional box types can be implemented
- Specialized box handling can be added
- Custom box properties can be defined

### 9.2 Custom Formatting Contexts

- Additional formatting contexts can be implemented
- Specialized layout algorithms can be added
- Custom box types can be created

### 9.3 Layout Algorithm Extensions

- Custom layout algorithms can be added
- Special rendering modes can be implemented
- Domain-specific optimizations can be integrated

### 9.4 Constraint Space Extensions

- Custom constraints can be added
- Specialized constraint types for specific layouts
- Additional context information for algorithms

### 9.5 Rendering Integration (Future)

- The system can be extended to integrate with different rendering backends
- Custom visualization can be implemented
- Export capabilities can be added

## 10. Implementation Considerations

When implementing the system, consider the following:

### 10.1 Box Tree Implementation

- Implement box tree construction before layout algorithms
- Focus on proper anonymous box creation according to CSS spec
- Ensure efficient box property access for layout algorithms
- Consider object pooling for frequently created box types

### 10.2 Threading Model

- The system primarily operates on a single thread
- Long-running operations could use background processing
- Thread safety is important for shared state

### 10.3 Memory Management

- Cached results should use appropriate memory management
- Box tree and fragment pooling for memory efficiency
- Weak references may be appropriate for some caching
- Disposal of unused resources is important

### 10.4 Performance Benchmarking

- Create benchmarks for key operations
- Compare with browser performance where possible
- Use performance data to guide optimization

### 10.5 Progressive Enhancement

- Implement core functionality first
- Add advanced features incrementally
- Ensure system works well with partial implementation

## 11. Integration with AngleSharp

The system integrates with AngleSharp's existing components:

### 11.1 DOM Integration

- Uses AngleSharp's DOM implementation
- Extends functionality without modifying core AngleSharp classes
- Provides extension methods for easy access

### 11.2 CSS Integration

- Uses AngleSharp's CSS parser and model
- Extends styling capabilities
- Integrates with existing style sheet handling

### 11.3 Configuration Integration

- Adds layout engine to AngleSharp configuration
- Provides configuration options for performance tuning
- Allows selective enabling of features