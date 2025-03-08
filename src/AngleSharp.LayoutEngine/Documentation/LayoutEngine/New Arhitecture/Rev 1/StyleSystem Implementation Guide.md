# StyleSystem Implementation Guide

## Leveraging Existing AngleSharp Components

AngleSharp already provides a comprehensive set of CSS value implementations that should be used rather than reimplemented. The following components should be reused:

### CSS Value Types

Use these AngleSharp value types instead of creating custom ones:

|StyleSystem Type|AngleSharp Type to Use|
|---|---|
|`Color`|`AngleSharp.Css.Values.Primitives.CssColorValue`|
|`CssLengthValue`|`AngleSharp.Css.Values.Primitives.CssLengthValue`|
|`CssStringValue`|`AngleSharp.Css.Values.Primitives.CssStringValue`|

### Additional Value Types Available

AngleSharp provides many more value types that can be used:

- **Primitives**: `CssAngleValue`, `CssIntegerValue`, `CssPercentageValue`, `CssTimeValue`
- **Composites**: `CssFontValue`, `CssShadowValue`, `CssBorderRadiusValue`
- **Functions**: `CssCalcValue`, `CssVarValue`, `CssUrlValue`

### Calculation and Variable Resolution

For CSS calculation and variable resolution:

- Use `CssCalcValue` for calculation expressions
- Use `CssVarValue` for variable references
- Use `ICssValue.Compute()` for value computation

## Components to Keep and Extend

The following StyleSystem components provide functionality not available in core AngleSharp and should be kept:

### Core Components

1. **StyleEngine**: Main orchestrator for style computation
2. **PropertyTreeManager**: For efficient style storage and sharing
3. **StyleCache**: For caching computed styles
4. **StyleInvalidationTracker**: For tracking style dependencies

### Pipeline Components

1. **RuleCollector**: For matching elements to CSS rules
2. **CascadeResolver**: For resolving property precedence
3. **InheritanceProcessor**: For handling property inheritance
4. **ComputedStyleBuilder**: For building the final computed style

## Implementation Pattern

When extending AngleSharp, follow these patterns:

1. **Adapter Pattern**: Create adapters where necessary to convert between AngleSharp's value system and your layout-optimized representation.
    
2. **Extension Methods**: Extend AngleSharp's interfaces rather than recreating them.
    
3. **Composition Over Inheritance**: Use AngleSharp's objects as internal components.
    

## Example Pattern for ComputedStyle

```csharp
public class ComputedStyle : IComputedStyle
{
    private readonly Dictionary<string, ICssValue> _computedValues;
    
    // Use AngleSharp's value system internally
    public CssLengthValue FontSize => 
        _computedValues["font-size"] as CssLengthValue ?? CssLengthValue.Medium;
        
    public CssColorValue Color => 
        _computedValues["color"] as CssColorValue ?? CssColorValue.Black;
        
    // Convert to layout-optimized representation for performance-critical paths
    public float FontSizeInPixels => FontSize.ToPixel(null);
}
```

## Avoiding Duplication

Do not reimplement:

- CSS value parsing
- Selector matching (use AngleSharp's selector engine)
- CSS property definitions
- Media query evaluation

Instead, focus on extending AngleSharp with layout and rendering capabilities.