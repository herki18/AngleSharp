# AngleSharp.LayoutEngine Architecture Plan

## Overview

The AngleSharp.LayoutEngine is designed as a natural extension to the AngleSharp framework, specifically building upon the StyleSystem to transform styled DOM elements into visual layouts. Following the architecture of modern browsers, particularly Blink's LayoutNG, this system converts computed styles into a fragment-based layout model that represents the visual layout of HTML documents.

## Core Design Principles

### 1. Fragment-Based Layout

- **Immutable Layout Results**: Layout fragments are immutable, improving predictability and enabling caching
- **Single-Pass Layout**: Minimize recalculations with a more predictable approach
- **Constraint-Based Sizing**: Use flexible constraints rather than absolute dimensions
- **Independent Processing**: Break down layout into fragments that can be independently processed

### 2. Document Lifecycle Coordination

- **Central Orchestration**: Document Lifecycle coordinates all update operations
- **Phase Management**: Clear separation of update phases (style, layout, paint)
- **State Transitions**: Well-defined lifecycle states prevent invalid operations
- **Batched Processing**: Changes are collected and processed efficiently in appropriate phases

### 3. Performance Focus

- **Dirty Bit Propagation**: Efficient marking of elements needing layout
- **Fragment Caching**: Layout results are cached and reused when possible
- **Parallel Processing**: Design for concurrent computation of independent subtrees
- **Memory Optimization**: Efficient data structures and fragment recycling

### 4. Standard Compliance

- **CSS Box Model**: Correct implementation of CSS box model calculations
- **Layout Algorithms**: Full support for block, inline, flex, grid, and table layouts
- **Positioning Schemes**: Complete support for all CSS positioning modes
- **International Text**: Support for bidirectional text and complex scripts

## System Components

### Document Lifecycle Components

1. **DocumentLifecycleCoordinator**
    
    - Central entry point for all layout operations
    - Manages transitions between document states
    - Ensures operations occur in correct sequence
    - Coordinates style, layout, and rendering phases
2. **DirtyBitPropagator**
    
    - Efficiently marks affected elements for update
    - Propagates "needs layout" flags up and down the tree
    - Minimizes scope of recalculation
    - Tracks layout containment boundaries
3. **LayoutScheduler**
    
    - Manages timing of layout operations
    - Prioritizes visible content
    - Handles deferred and incremental layout
    - Coordinates with animation frames

### BoxTree Components

1. **BoxTreeBuilder**
    
    - Constructs the box tree from DOM and computed styles
    - Creates appropriate box types based on display property
    - Inserts anonymous boxes as needed
    - Provides foundation for layout calculations
2. **LayoutObject**
    
    - Represents a basic layout object (block, inline, flex, etc.)
    - Contains style information relevant to layout
    - Stores dirty bits and layout state
    - Serves as input to layout algorithms
3. **AnonymousBox**
    
    - Handles cases where layout requires boxes not directly tied to DOM
    - Supports special formatting cases
    - Maintains logical connections to DOM elements

### Layout Algorithm Components

1. **LayoutInputNode**
    
    - Transforms LayoutObject into input for layout algorithms
    - Contains necessary style and layout properties
    - Caches layout results
    - Tracks invalidation state
2. **ConstraintSpace**
    
    - Defines available space for layout
    - Contains sizing constraints
    - Provides context for layout calculations
    - Supports nested constraint propagation
3. **LayoutAlgorithms**
    
    - Specialized implementations for different display types
    - Processes input nodes with constraint spaces
    - Produces layout results
    - Handles specialized layout models (block, flex, grid, etc.)

### Fragment System

1. **LayoutResult**
    
    - Immutable output from layout algorithms
    - Contains geometric information
    - Stores positioned children
    - Provides data for painting and hit testing
2. **PhysicalFragment**
    
    - Final positioned layout element
    - Contains absolute coordinates and dimensions
    - Stores painting properties
    - Forms fragment tree for rendering
3. **FragmentCache**
    
    - Stores fragments for reuse
    - Maps input parameters to cached results
    - Manages cache invalidation
    - Optimizes memory usage

## Update Flow

The layout update flow follows a clearly defined path through the system:

1. **Trigger Detection**
    
    - Style changes provide affected elements and property types
    - DOM mutations provide affected nodes and mutation types
    - Resize events provide new dimensions and containment info
    - Media query changes provide affected queries and elements
2. **Document Lifecycle Processing**
    
    - `setNeedsLayout()` and similar methods mark objects for update
    - Dirty bits are propagated through appropriate hierarchies
    - Updates are batched for efficient processing
3. **Layout Phase**
    
    - During layout phase, marked objects are processed
    - Input nodes are created from dirty layout objects
    - Cached fragments are invalidated
    - New constraint spaces are generated
    - Layout algorithms produce fresh results
    - Physical fragments are created from layout results
4. **Result Utilization**
    
    - Fragment tree is used for painting and hit testing
    - Layout results are cached for future use
    - Position and size information is made available to scripts

## Integration Points

1. **StyleSystem Integration**
    
    - Style changes notify DocumentLifecycleCoordinator
    - Style invalidation sets guide layout invalidation
    - Layout property changes trigger appropriate updates
2. **DOM Integration**
    
    - DOM mutations flow through MutationObserver to DocumentLifecycleCoordinator
    - DOM structure changes update the box tree
    - Layout containment is respected for change propagation
3. **Rendering Integration**
    
    - Fragment tree provides input to painting system
    - Layout results determine composition needs
    - Visual updates are synchronized with layout completion

## Performance Considerations

1. **Selective Invalidation**
    
    - Use dirty bits to limit update scope
    - Respect layout containment for isolation
    - Cache fragments based on input stability
2. **Parallel Processing**
    
    - Process independent subtrees concurrently
    - Use multiple threads for layout algorithms
    - Ensure thread-safe fragment access
3. **Memory Management**
    
    - Recycle fragments to reduce allocation pressure
    - Share common data structures
    - Use compact representations where possible

## Implementation Plan

The LayoutEngine will be implemented in phases:

### Phase 1: Core Infrastructure (3 months)

- Document lifecycle coordination
- Box tree and dirty bit system
- Simple block layout algorithm
- Integration with AngleSharp and StyleSystem

### Phase 2: Basic Layout Capabilities (3 months)

- Block layout with margin collapsing
- Simple inline layout with basic text metrics
- Basic positioning (relative/absolute)
- Initial fragment caching

### Phase 3: Advanced Layout Models (4 months)

- Flexbox layout
- Grid layout
- Improved inline layout with line breaking
- Table layout

### Phase 4: Performance Optimization (2 months)

- Advanced fragment caching
- Parallelization
- Layout invalidation refinement
- Memory optimization

### Phase 5: Complex Features (3 months)

- Multi-column layout
- Fragmentation (page breaks)
- Complex international text
- SVG layout integration

### Phase 6: Final Integration (2 months)

- Complete renderer integration
- Performance benchmarking
- Edge case handling
- Compatibility testing

## Conclusion

This architecture provides a solid foundation for implementing a modern, efficient layout system based on Blink's LayoutNG approach. By centering coordination around the Document Lifecycle and using fragment-based layout with immutable results, the system can deliver accurate layouts while enabling advanced optimization techniques such as caching, parallelization, and incremental processing.