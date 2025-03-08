# AngleSharp CSS Reference

## Key Declaration Handling Methods

- **SetDeclarations**: Replaces existing properties based on importance
    
    ```csharp
    // Use for explicitly inherited properties
    cssResult.SetDeclarations(inheritPropertiesFromParent);
    ```
    
- **UpdateDeclarations**: Updates only compatible properties
    
    ```csharp
    // Use for natural inheritance
    cssResult.UpdateDeclarations(inheritableProperties);
    ```
    

## Capabilities to Leverage

1. **Shorthand/Longhand Handling**
    
    - Automatically expands shorthands (`margin`) to longhands (`margin-top`, etc.)
    - Can reconstruct shorthands from longhands
    - Implementation: `SetShorthand()`, `TryCreateShorthand()`
2. **Importance Preservation**
    
    - Preserves `!important` flags during operations
    - Higher specificity rules in `ChangeDeclarations()`
    - Logic: `(o, n) => !o.IsImportant || n.IsImportant`
3. **Validation & Normalization**
    
    - Validates values before setting
    - Normalizes values (e.g., `red` → `rgba(255, 0, 0, 1)`)
    - Verification with `if (property.RawValue is not null)`
4. **Property Dependencies**
    
    - Understands relationships between properties
    - Manages complex properties like `font`
    - Uses `GetMappings()` to find related properties

## Implementation Strategy

- Use `SetDeclarations` for explicit `inherit` keyword properties
- Use `UpdateDeclarations` for natural inheritance
- Delegate complex CSS behaviors to AngleSharp
- Focus implementation on inheritance algorithm
- Avoid reimplementing CSS specification details

## Existing AngleSharp Enum Types to Use

The StyleSystem should use these existing AngleSharp enumeration types instead of defining duplicate versions:

### Display and Positioning

- `AngleSharp.Css.Dom.DisplayMode` - Defines display types (None, Block, Inline, Flex, Grid, etc.)
- `AngleSharp.Css.Dom.PositionMode` - Position types (Static, Relative, Absolute, Fixed, Sticky)
- `AngleSharp.Css.Dom.OverflowMode` - Overflow types (Visible, Hidden, Scroll, Auto, Clip)

### Text Formatting

- `AngleSharp.Css.Dom.TextAlign` - Text alignment options (Left, Right, Center, Justify, Start, End, JustifyAll, MatchParent)
- `AngleSharp.Dom.DirectionMode` - Text direction (Ltr, Rtl)

### CSS Values

- `AngleSharp.Css.Values.Primitives.CssLengthValue` - For dimensions, sizes, etc.
- `AngleSharp.Css.Values.Primitives.CssColorValue` - For colors
- `AngleSharp.Css.Values.Primitives.CssNumberValue` - For numeric values
- `AngleSharp.Css.Values.Primitives.CssIntegerValue` - For integer values

## CSS Value Types to Reuse

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

## Implementation Patterns

When extending AngleSharp, follow these patterns:

1. **Adapter Pattern**: Create adapters where necessary to convert between AngleSharp's value system and your layout-optimized representation.
    
2. **Extension Methods**: Extend AngleSharp's interfaces rather than recreating them.
    
3. **Composition Over Inheritance**: Use AngleSharp's objects as internal components.
    

### Example Pattern for ComputedStyle

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

## Avoid Duplication

Do not reimplement:

- CSS value parsing
- Selector matching (use AngleSharp's selector engine)
- CSS property definitions
- Media query evaluation

Instead, focus on extending AngleSharp with layout and rendering capabilities.

## Advantages of Using AngleSharp Types

1. **Consistency**: Maintains consistent representation with AngleSharp's parser
2. **Compatibility**: Ensures seamless integration with AngleSharp's infrastructure
3. **Maintenance**: Reduces duplication and potential versioning conflicts
4. **Functionality**: Leverages AngleSharp's existing computation and conversion functions