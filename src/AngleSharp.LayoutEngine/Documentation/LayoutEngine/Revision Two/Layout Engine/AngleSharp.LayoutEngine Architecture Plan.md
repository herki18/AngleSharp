# AngleSharp.LayoutEngine Architecture Plan

## Overview

The AngleSharp.LayoutEngine is designed as a natural extension to the AngleSharp framework, specifically building upon the StyleSystem to transform styled DOM elements into visual layouts. Following the architecture of modern browsers like Blink, this system converts computed styles into geometrical box models that represent the visual layout of HTML documents.

## Core Design Principles

### 1. Architectural Alignment

- **Blink-Inspired Design**: Follows Blink's layout architecture patterns for proven performance
- **Clear Component Boundaries**: Well-defined interfaces between components
- **Extension Pattern**: Builds naturally on top of AngleSharp core and StyleSystem

### 2. Performance Focus

- **Minimal Recalculation**: Only recalculate what's necessary when styles or DOM change
- **Tree Optimization**: Layout tree is optimized for layout operations
- **Efficient Caching**: Layout results are cached and reused when possible
- **Incremental Processing**: Process layout in chunks to avoid blocking the main thread

### 3. Standard Compliance

- **CSS Box Model**: Correct implementation of CSS box model calculations
- **Positioning Schemes**: Full support for static, relative, absolute, fixed, and sticky positioning
- **Layout Algorithms**: Specialized algorithms for block, inline, flex, grid, and table layouts

### 4. Extensibility

- **Algorithm Pluggability**: New layout algorithms can be added without changing core components
- **Rendering Abstraction**: Layout system outputs abstract geometry that can be consumed by various rendering systems
- **Customization Points**: Clear extension points for specialized layout needs

## System Components

### Core Components

1. **LayoutEngine**
    
    - Central orchestrator for layout operations
    - Manages the transformation from DOM+styles to layout results
    - Coordinates with StyleSystem for style information
    - Provides the public API for controlling layout operations
2. **LayoutTreeBuilder**
    
    - Transforms styled DOM elements into a specialized layout tree
    - Creates appropriate layout node types based on display property
    - Builds parent-child relationships reflecting DOM structure
    - Applies optimizations for layout tree construction
3. **LayoutTreeNode**
    
    - Base representation of an element in the layout tree
    - Holds reference to the DOM element and its computed style
    - Contains layout-specific properties and geometry
    - Delegates layout calculations to specialized layout algorithms
4. **LayoutInvalidationTracker**
    
    - Tracks which elements need layout recalculation
    - Optimizes by only invalidating affected nodes
    - Creates dependency graphs to handle cascading layout changes
    - Coordinates with StyleInvalidationTracker for efficient updates

### Layout Algorithms

1. **LayoutResolutionEngine**
    
    - Selects appropriate layout algorithm based on display property
    - Coordinates the application of layout algorithms
    - Resolves conflicts between different layout systems
    - Provides unified interface for all layout algorithms
2. **Block Layout Algorithm**
    
    - Handles normal flow block-level elements
    - Implements vertical stacking of elements
    - Manages margin collapsing between adjacent blocks
    - Supports width constraints and height calculations
3. **Inline Layout Algorithm**
    
    - Handles inline-level elements and text
    - Implements line breaking algorithm
    - Manages text flow, justification, and alignment
    - Supports bidirectional text and mixed content
4. **Flex Layout Algorithm**
    
    - Implements CSS Flexible Box Layout
    - Calculates flex item sizes and positions
    - Handles flex direction, wrap, and alignment properties
    - Supports nested flex contexts
5. **Grid Layout Algorithm**
    
    - Implements CSS Grid Layout
    - Calculates grid track sizes and positions
    - Places items according to grid placement rules
    - Handles alignment and spanning of items
6. **Table Layout Algorithm**
    
    - Implements CSS Table Layout
    - Handles fixed and auto table layouts
    - Calculates row and column sizes
    - Manages spanning cells and alignment

### Box Model Components

1. **BoxModelCalculator**
    
    - Calculates dimensions based on CSS box model
    - Handles content, padding, border, and margin boxes
    - Applies box-sizing rules (content-box vs. border-box)
    - Computes constraints and min/max dimensions
2. **MarginCollapseCalculator**
    
    - Implements CSS margin collapsing rules
    - Calculates collapsed margins between adjacent elements
    - Handles special cases like empty blocks and nested margins
    - Provides clean abstraction for margin calculations
3. **ContainingFormattingContext**
    
    - Determines the containing block for layout
    - Establishes formatting contexts for layout algorithms
    - Handles block formatting contexts (BFC) creation
    - Manages stacking contexts and positioning contexts

### Positioning System

1. **PositioningEngine**
    
    - Central manager for element positioning
    - Implements static, relative, absolute, fixed, and sticky positioning
    - Calculates final positions based on offset properties
    - Coordinates with the containing block system
2. **AbsoluteFixedPositioner**
    
    - Specialized positioning for out-of-flow elements
    - Calculates positions for absolute and fixed elements
    - Handles containing block and offset constraints
    - Manages viewport-relative positioning
3. **StickyPositionController**
    
    - Implements sticky positioning behavior
    - Calculates position adjustments based on scroll offset
    - Manages sticky constraints and limits
    - Provides efficient updates during scrolling
4. **FloatCollisionProcessor**
    
    - Handles float positioning and collision detection
    - Implements float clearance rules
    - Manages float placement around other content
    - Calculates line boxes with floats

### Output and Integration

1. **LayoutBoxGeometry**
    
    - Represents the final geometric output of layout
    - Contains content, padding, border, and margin boxes
    - Provides coordinate transformations
    - Supports efficient geometry operations
2. **VisualRenderRectangle**
    
    - Represents the visual area for rendering
    - Includes clipping and overflow information
    - Provides coordinate system for rendering
    - Maps layout coordinates to rendering space
3. **RenderLayerBuilder**
    
    - Bridges between layout and rendering systems
    - Transforms layout results into render instructions
    - Creates layer trees for compositing
    - Provides abstraction for different rendering backends

## Architecture Diagrams

The system follows a layered architecture with clear component boundaries:

1. **Layer Structure**: DOM → StyleSystem → LayoutSystem → Rendering System
2. **Component Relationships**: See the detailed component architecture diagram
3. **Data Flow**: See the layout system data flow diagram

## Integration Points

1. **StyleSystem Integration**:
    
    - Consumes ComputedStyle from StyleSystem
    - Coordinates with StyleInvalidationTracker
    - Shares lifecycle with DocumentLifecycleCoordinator
2. **DOM Integration**:
    
    - Maps layout tree to DOM structure
    - Listens for DOM mutations via DOMObserver
    - Provides hit testing for DOM interactions
3. **Rendering Integration**:
    
    - Outputs geometry for rendering pipeline
    - Creates render layers for compositing
    - Supports different rendering backends

## Key Processes

### Layout Calculation Process

1. DOM elements and computed styles are converted to layout tree nodes
2. Containing blocks and formatting contexts are established
3. Box model properties are calculated
4. Layout algorithm is selected based on display property
5. Layout algorithm calculates positions and sizes
6. Positioning adjustments are applied
7. Final layout geometry is produced
8. Render instructions are generated

### Layout Invalidation Process

1. DOM or style changes trigger invalidation
2. LayoutInvalidationTracker identifies affected nodes
3. Dependency graph is built to capture cascading effects
4. Layout is recalculated only for affected nodes
5. Layout results are cached for future use
6. Rendering system is notified of changes

## Performance Considerations

1. **Layout Tree Optimization**:
    
    - Specialized node types for different display modes
    - Memory-efficient representation of geometry
    - Smart references to avoid duplication
2. **Incremental Layout**:
    
    - Only recalculate what's necessary
    - Prioritize visible elements
    - Defer non-critical layout operations
3. **Caching Strategy**:
    
    - Cache layout results based on style inputs
    - Invalidate cache judiciously
    - Share geometry data when possible
4. **Containment Awareness**:
    
    - Respect CSS containment for isolation
    - Use containment to limit invalidation scope
    - Optimize layout for contained subtrees

## Future Extensions

1. **Painting Integration**:
    
    - Add paint ordering and stacking context handling
    - Support for composite effects and blend modes
    - Layer tree optimization for rendering
2. **Animation Support**:
    
    - Fast path for transform and opacity animations
    - Layout-aware animation system
    - Efficient animation-driven layout updates
3. **Advanced Text Layout**:
    
    - Enhanced international text support
    - Line grid and vertical text layout
    - Advanced typography features
4. **Layout-based API**:
    
    - Computed geometry API for applications
    - Layout-based element queries
    - Visual debugging tools

## Conclusion

The AngleSharp.LayoutEngine architecture provides a solid foundation for implementing a modern, efficient layout system that complements the existing AngleSharp framework. By following Blink's proven architecture patterns and focusing on performance, the system can deliver accurate layout results while maintaining good performance characteristics.