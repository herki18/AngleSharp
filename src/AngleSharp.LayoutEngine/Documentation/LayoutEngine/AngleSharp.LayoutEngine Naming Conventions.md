# AngleSharp.LayoutEngine Naming Conventions

## Project Name

- **AngleSharp.LayoutEngine** - Main project name and root namespace

## System Names

The LayoutEngine is composed of four primary systems, each with its own responsibility and namespace:

- **StyleSystem** - Computes CSS styles for DOM elements
    
    - _Previously: "StyleComputation Module"_
    - Namespace: `AngleSharp.LayoutEngine.StyleSystem`
- **LayoutSystem** - Computes element layout and positioning
    
    - _Previously: "Layout Engine Module"_
    - Namespace: `AngleSharp.LayoutEngine.LayoutSystem`
- **LifecycleSystem** - Manages document state, DOM mutations, and invalidation
    
    - _Previously: "Document Lifecycle Module"_
    - Namespace: `AngleSharp.LayoutEngine.LifecycleSystem`
- **CacheSystem** - Provides efficient caching with dependency tracking
    
    - _Previously: "Cache Module"_
    - Namespace: `AngleSharp.LayoutEngine.CacheSystem`

## Namespace Structure

Each system should follow a consistent namespace structure:

```
AngleSharp.LayoutEngine.{SystemName}
├── AngleSharp.LayoutEngine.{SystemName}.Abstractions
├── AngleSharp.LayoutEngine.{SystemName}.Core
├── AngleSharp.LayoutEngine.{SystemName}.Extensions
```

## Terminology

- Use "System" instead of "Module" in all documentation and naming
- Class names should reflect their parent system (e.g., StyleComputationEngine -> StyleEngine)
- Interface names should use the "I" prefix (e.g., IStyleEngine)
- Abstract base classes should use the "Base" suffix (e.g., FormattingContextBase)

## File Organization

- Files should be organized in folders corresponding to system names
- Common utilities and shared components should be in AngleSharp.LayoutEngine.Common
- Integration code should be in AngleSharp.LayoutEngine.Integration