# StyleSystem Architecture Overview

## Introduction

The StyleSystem for AngleSharp.LayoutEngine provides a modern CSS styling implementation aligned with browser engines like Blink. The architecture has been enhanced with a dedicated dependency injection system that operates independently from AngleSharp's core services while maintaining full compatibility.

## Design Principles

1. **Clear Phase Separation**: Style computation is divided into distinct phases with well-defined inputs and outputs
2. **Memory Efficiency**: Property trees and style sharing reduce memory consumption
3. **Performance Optimization**: Multi-level caching and minimal recalculation improve speed
4. **Layout Integration**: Clean interfaces between style and layout systems
5. **Modularity**: Loosely coupled components with clear responsibilities
6. **Threading Support**: Parallel computation of styles where possible
7. **Lifecycle Integration**: Alignment with document lifecycle phases
8. **Dependency Injection**: Independent DI system that integrates with existing AngleSharp services

## Dependency Injection System

The StyleSystem introduces a dedicated dependency injection framework that provides several key benefits:

1. **Separation of Concerns**: Decouples the StyleSystem from AngleSharp's internal service management
2. **Configurability**: Allows fine-grained configuration of StyleSystem components
3. **Extensibility**: Makes it easy to replace or extend individual components
4. **Integration**: Seamlessly bridges with AngleSharp's existing service architecture
5. **Testing**: Facilitates unit testing by allowing mock implementations to be injected

Key elements of the DI system include:

- **StyleSystemServiceCollectionExtensions**: Registers all StyleSystem components
- **AngleSharpServiceCollectionExtensions**: Adapts AngleSharp services for use with StyleSystem
- **StyleSystemDependencyExtensions**: Connects StyleSystem with AngleSharp context
- **StyleSystemService**: Acts as the central orchestrator for StyleSystem components

## Core Components Overview

### 1. Document Integration Layer

- **DocumentLifecycleCoordinator**: Coordinates style computation with document lifecycle
- **DomMutationTracker**: Monitors DOM changes and triggers style invalidation
- **StyleRecalcScheduler**: Schedules style computation based on priority and visibility
- **DisplayLockManager**: Defers processing for non-visible content
- **AnimationStyleEngine**: Optimizes style computation for animated elements

### 2. StyleEngine Core

- **StyleEngine**: The main entry point that orchestrates the style computation process
- **StyleTreeResolver**: Handles element tree traversal and style computation scheduling
- **StyleInvalidationTracker**: Tracks element dependencies for minimal recalculation
- **StyleCache**: Multi-level caching system for computed styles
- **StyleSheetManager**: Manages stylesheets from different origins (user agent, author, etc.)

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

### 5. Output Layer

- **ComputedStyle**: Layout-optimized representation of element styles
    - SurrogateBitfields: Flags and enums stored efficiently
    - BoxProperties: Dimensions, margins, borders, etc.
    - TextProperties: Font, text alignment, etc.
    - RareProperties: Less commonly used properties
    - PropertyTreeNode: Efficient storage for property values with sharing capabilities

### 6. Threading Components

- **MainThreadStyleWork**: Handles critical-path elements on the main thread
- **WorkerThreadStylePool**: Manages worker threads for parallel style computation
- **StyleTaskScheduler**: Schedules and executes style-related tasks
- **StyleRecalcScheduler**: Coordinates style recalculation across threads

## Component Processing Flow

The StyleSystem follows a specific processing sequence when computing styles:

```
DOM Mutation → Style Invalidation → Style Recalc → Rule Matching → Cascade → Inheritance → Computed Style
```

The StyleEngine orchestrates this process while the ComputedStyleBuilder acts as the coordinator:

1. First, DOM mutations are tracked by the DomMutationTracker
2. Affected elements are marked by the StyleInvalidationTracker
3. Style recalculation is scheduled by the StyleRecalcScheduler based on priority
4. The StyleEngine processes invalidated elements through the style computation pipeline
5. ComputedStyle objects are created and cached for future use

## Key Architectural Enhancements

### Service Registration and Initialization

The StyleSystem now supports standard dependency injection patterns:

```csharp
// Register StyleSystem with services collection
services.AddStyleSystem(options => {
    options.EnableOptimization = true;
    options.MaxWorkerThreads = 4;
    options.BatchSize = 100;
});

// Register AngleSharp services for use with StyleSystem
services.AddAngleSharpServices(context);
```

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

### Property Trees

Memory efficiency is achieved through property sharing:

- Similar elements share style data when possible
- Common property values stored once and referenced
- Optimized immutable data structures

### Style Containment

CSS containment is respected for improved performance:

- **Style Containment**: Limits style invalidation scope to contained subtrees, preventing style changes from propagating beyond containment boundaries
- **Layout Containment**: Creates independent layout contexts that don't affect parent layout calculations, allowing for more efficient layout updates
- **Paint Containment**: Creates new stacking contexts and containing blocks, enabling optimizations like subtree skipping for off-screen content
- **Size Containment**: Elements' size doesn't depend on their descendants, enabling early layout optimization
- **Content Containment**: Combines layout, style, and paint containment for maximum optimization

## Layout System Integration

The StyleSystem provides optimized interfaces for layout consumption:

- Fast property accessors for layout-critical values
- Logical property resolution based on writing mode
- Specialized layout-oriented property groups
- Clear boundaries between style and layout responsibilities

## Conclusion

The enhanced architecture with dedicated dependency injection provides a solid foundation for a high-performance style system aligned with modern browser engines. It enables efficient processing of complex stylesheets while supporting modern CSS features and maintaining compatibility with AngleSharp's existing interfaces.