# StyleSystem Architecture Overview

## Introduction

This document outlines the redesigned StyleSystem architecture for AngleSharp.LayoutEngine, aligned with modern browser engines like Blink. The new architecture prioritizes performance, modularity, and modern CSS capabilities while maintaining compatibility with AngleSharp's core.

## Design Principles

1. **Clear Phase Separation**: Style computation is divided into distinct phases with well-defined inputs and outputs
2. **Memory Efficiency**: Property trees and style sharing reduce memory consumption
3. **Performance Optimization**: Multi-level caching and minimal recalculation improve speed
4. **Layout Integration**: Clean interfaces between style and layout systems
5. **Modularity**: Loosely coupled components with clear responsibilities
6. **Threading Support**: Parallel computation of styles where possible
7. **Lifecycle Integration**: Alignment with document lifecycle phases

## Core Components Overview

### 1. Document Integration Layer

- **DocumentLifecycleCoordinator**: Coordinates style computation with document lifecycle
- **StyleRecalcScheduler**: Schedules style computation based on priority and visibility
- **DisplayLockManager**: Defers processing for non-visible content
- **AnimationStyleEngine**: Optimizes style computation for animated elements

### 2. StyleEngine Core

- **StyleEngine**: The main entry point that orchestrates the style computation process
- **StyleTreeResolver**: Handles element tree traversal and style computation scheduling
- **StyleInvalidationTracker**: Tracks element dependencies for minimal recalculation
- **StyleCache**: Multi-level caching system for computed styles

### 3. Style Computation Pipeline

- **RuleCollector**: Gathers and matches style rules
- **CascadeResolver**: Resolves conflicting style declarations
- **InheritanceProcessor**: Handles property inheritance
- **ComputedStyleBuilder**: Creates optimized ComputedStyle objects

### 4. Value Processing Components

- **VariableResolver**: Efficiently resolves CSS custom properties (variables) with handling for circular references and fallbacks
- **ValueCalculator**: Performs unit conversion and computes values (independent of variable resolution)
- **PropertyTreeManager**: Manages shared property storage through tree structures for memory efficiency
- **StylePropertyMapper**: Handles logical-to-physical property mapping based on writing mode context

These components operate independently and do not directly depend on each other. Instead, they are orchestrated by the ComputedStyleBuilder, which coordinates the processing sequence:

1. First, CSS variables are resolved by the VariableResolver
2. Then computed values are calculated by the ValueCalculator
3. Logical properties are mapped to physical properties by the StylePropertyMapper
4. Finally, values are stored efficiently by the PropertyTreeManager

This orchestration-based approach follows modern browser engines like Blink and provides clear separation of concerns, better testability, and focused optimization opportunities.

### Component Processing Flow

The StyleSystem follows a specific processing sequence when computing styles:

```
CSS Declaration → Variable Resolution → Value Calculation → Logical-to-Physical Mapping → Property Storage
```

The ComputedStyleBuilder acts as the coordinator for this process:

1. For each property in a declaration:
   - First resolves any CSS variables in the property value
   - Then computes absolute values through unit conversion and calculation
   - For logical properties, maps them to their physical equivalents based on writing mode
   - Finally stores the computed values in the property tree for efficient storage

This clearly defined flow ensures that:
- Variables are always resolved before calculations are performed
- Unit conversion happens after variable resolution
- Logical properties are correctly mapped based on writing mode
- The property tree efficiently stores and shares values between similar elements

The flow ensures proper isolation between components while maintaining the correct processing order required for CSS.

### 5. Output Layer

- **ComputedStyle**: Layout-optimized representation of element styles
    - SurrogateBitfields: Flags and enums stored efficiently
    - BoxProperties: Dimensions, margins, borders, etc.
    - TextProperties: Font, text alignment, etc.
    - RareProperties: Less commonly used properties
    - NonInheritedProperties: Properties that don't inherit

## Key Architectural Enhancements

### Multi-Threading Support

The architecture supports parallel style computation across multiple threads:

- Critical-path elements processed on the main thread
- Off-critical-path elements processed by worker threads
- Thread-safe data structures for style sharing

### Display Locking

Performance is optimized by deferring work for non-visible content:

- Viewport visibility tracking
- Prioritization of visible element styling
- Just-in-time computation as elements scroll into view

### Animation Optimization

Animated styles receive special handling:

- Compositor-friendly property storage
- Minimal recalculation for animated properties
- Separation of compositor-only animations from main thread animations

### Property Trees

Memory efficiency is achieved through property sharing:

- Similar elements share style data when possible
- Common property values stored once and referenced
- Optimized immutable data structures

### Containment Awareness

CSS containment is respected for style calculations:

- Style containment boundaries limit invalidation scope
- Subtree isolation for independent style calculation
- Improved performance through reduced recalculation scope

## Layout System Integration

The StyleSystem provides optimized interfaces for layout consumption:

- Fast property accessors for layout-critical values
- Logical property resolution based on writing mode
- Specialized layout-oriented property groups
- Clear boundaries between style and layout responsibilities

## Caching Strategy

Multi-level caching improves performance:

- Element-level style caching
- Property-level caching for custom properties
- Intelligent invalidation based on dependencies
- Hash-based identity for style sharing opportunities

## Conclusion

This architecture provides a solid foundation for a high-performance style system aligned with modern browser engines. It enables efficient processing of complex stylesheets while supporting modern CSS features and maintaining compatibility with AngleSharp's existing interfaces.