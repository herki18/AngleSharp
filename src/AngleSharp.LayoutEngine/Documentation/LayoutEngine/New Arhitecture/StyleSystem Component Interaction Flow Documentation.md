# StyleSystem Component Interaction Flow

## Overview

This document clarifies the interaction patterns and processing sequence between key components of the StyleSystem architecture, particularly focusing on the Value Processing Components and their orchestration.

## Value Processing Component Independence

The Value Processing Components (VariableResolver, ValueCalculator, PropertyTreeManager, StylePropertyMapper) are designed as independent modules with clearly defined responsibilities:

- **VariableResolver**: Resolves CSS custom properties (variables) to their computed values
- **ValueCalculator**: Performs unit conversions and calculates computed values
- **PropertyTreeManager**: Manages efficient property value storage and deduplication
- **StylePropertyMapper**: Maps between logical and physical properties based on writing mode

These components do not directly depend on each other. Instead, they are orchestrated by the ComputedStyleBuilder.

## Processing Sequence

The processing of CSS values follows a specific sequence, orchestrated by the ComputedStyleBuilder:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │     │                 │
│ CSS Declaration │ ──▶ │ Variable        │ ──▶ │ Value           │ ──▶ │ Property        │
│                 │     │ Resolution      │     │ Calculation     │     │ Storage         │
│                 │     │                 │     │                 │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘     └─────────────────┘
```

1. **Variable Resolution Phase**:
   - Input: Raw CSS declaration with potential var() references
   - Process: VariableResolver resolves all CSS custom properties
   - Output: CSS values with variables resolved to their actual values

2. **Value Calculation Phase**:
   - Input: CSS values with resolved variables
   - Process: ValueCalculator converts units and evaluates expressions
   - Output: Computed values in absolute units (where applicable)

3. **Logical-to-Physical Mapping Phase** (for applicable properties):
   - Input: Computed values that may be logical properties
   - Process: StylePropertyMapper converts logical properties to physical ones
   - Output: Physical property values ready for storage

4. **Property Storage Phase**:
   - Input: Computed property values
   - Process: PropertyTreeManager optimizes storage through sharing
   - Output: Efficiently stored property values in the property tree

## Orchestration by ComputedStyleBuilder

The ComputedStyleBuilder acts as the coordinator for these phases:

```csharp
// In ComputedStyleBuilder:
public IComputedStyle BuildComputedStyle(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
{
    // Phase 1: Variable Resolution
    foreach (var property in declaration)
    {
        if (property.RawValue == null)
            continue;
            
        var propertyName = property.Name;
        var propertyValue = property.RawValue;
        
        // Resolve any CSS variables in the value
        var resolvedValue = _variableResolver.ResolveVariablesInValue(propertyValue, element, propertyName);
        
        // Phase 2: Value Calculation
        var computedValue = _valueCalculator.Compute(resolvedValue, element, propertyName);
        
        // Phase 3: Logical-to-Physical Mapping (if needed)
        if (_stylePropertyMapper.IsLogicalProperty(propertyName))
        {
            // Map logical properties to physical ones based on writing mode
            var physicalProps = _stylePropertyMapper.MapLogicalToPhysical(
                propertyName, computedValue, writingMode);
                
            // Store each physical property
            foreach (var physicalProp in physicalProps)
            {
                // Phase 4: Property Storage
                _propertyTreeManager.StoreProperty(element, physicalProp.Key, physicalProp.Value);
            }
        }
        else
        {
            // Phase 4: Property Storage for non-logical properties
            _propertyTreeManager.StoreProperty(element, propertyName, computedValue);
        }
    }
    
    // Create and return the computed style
    return CreateComputedStyle(element, parentStyle);
}
```

## Benefits of This Architecture

This orchestration-based architecture, similar to Blink's, provides several advantages:

1. **Separation of Concerns**: Each component has a clear, focused responsibility
2. **Maintainability**: Components can be developed, tested, and modified independently
3. **Performance Optimization**: Each phase can be optimized without affecting others
4. **Testability**: Components can be tested in isolation with well-defined inputs/outputs
5. **Flexibility**: New CSS features can be added by extending specific components

## Implementation Guidelines

When implementing these components:

1. **Avoid Direct Dependencies**: Components should not directly reference each other
2. **Use Clear Interfaces**: Define clean, minimal interfaces for each component
3. **Follow the Processing Sequence**: Always process in the defined order
4. **Cache Intelligently**: Cache results at appropriate phase boundaries
5. **Handle Errors at Each Phase**: Each phase should handle its own error cases

By following these principles, the StyleSystem will maintain alignment with modern browser architecture while providing optimal performance and maintainability.