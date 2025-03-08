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
    

## Initial Values and CSS Defaults

AngleSharp provides built-in support for CSS initial values through the `IDeclarationFactory`:

```csharp
// Get the factory from the browsing context
var factory = context.GetFactory<IDeclarationFactory>();

// Get declaration info for a specific property
var declarationInfo = factory.Create("color");

// Access the initial value as defined in CSS specifications
var initialValue = declarationInfo.InitialValue;
string initialColorText = initialValue.CssText; // "rgba(0, 0, 0, 1)"
```

### Best Practices for Initial Values

1. **Use IDeclarationFactory for initial values**:
    
    ```csharp
    // In ComputedStyle.GetPropertyValue:
    if (string.IsNullOrEmpty(value)) {
        var factory = _context.GetFactory<IDeclarationFactory>();
        var declarationInfo = factory.Create(propertyName);
        return declarationInfo?.InitialValue?.CssText ?? string.Empty;
    }
    ```
    
2. **Avoid hardcoding defaults** - let AngleSharp handle the defaults according to CSS spec
    
3. **Fall back gracefully** - always check for null values when using the factory
    
4. **Cache declaration information** for frequently accessed properties
    

## Capabilities to Leverage

1. **Shorthand/Longhand Handling**
    
    - Automatically expands shorthands (`margin`) to longhands (`margin-top`, etc.)
    - Can reconstruct shorthands from longhands
    - Implementation: `SetShorthand()`, `TryCreateShorthand()`
    - **Important Note**: When testing `Length` property, be aware that shorthand properties like `margin: 10px` are internally expanded to 4 longhand properties (`margin-top`, `margin-right`, `margin-bottom`, `margin-left`) but can still be accessed via the shorthand name
    - The `CssStyleDeclaration.Length` reflects the total number of longhand properties, not shorthand ones
2. **Importance Preservation**
    
    - Preserves `!important` flags during operations
    - Higher specificity rules in `ChangeDeclarations()`
    - Logic: `(o, n) => !o.IsImportant || n.IsImportant`
3. **Validation & Normalization**
    
    - Validates values before setting
    - Normalizes values (e.g., `red` → `rgba(255, 0, 0, 1)`)
    - Verification with `if (property.RawValue is not null)`
    - Color keywords are automatically converted to their RGBA representation
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
- Be aware of shorthand-to-longhand expansion when counting properties
- Get initial values from `IDeclarationFactory` rather than hardcoding defaults

## Style Computation and Retrieval

When implementing the style system, follow these patterns for handling values:

```csharp
// In ComputedStyle.GetPropertyValue
public string GetPropertyValue(string propertyName)
{
    // First try property tree (computed values)
    string value = _propertyTree.GetPropertyValue(propertyName);
    
    // If no value found, get the initial value from the declaration factory
    if (string.IsNullOrEmpty(value))
    {
        var factory = _context.GetFactory<IDeclarationFactory>();
        if (factory != null)
        {
            var declarationInfo = factory.Create(propertyName);
            if (declarationInfo?.InitialValue != null)
            {
                return declarationInfo.InitialValue.CssText;
            }
        }
    }
    
    return value;
}
```

This approach ensures proper handling of all CSS properties, including those not explicitly handled by your style system.

## Media Queries and Container Rules

When collecting style rules from stylesheets, be sure to recursively traverse container rules like `@media` and `@supports`:

```csharp
// Recursively collect rules from all stylesheets
private void CollectRulesRecursively(IEnumerable<ICssRule> rules, List<ICssRule> collectedRules)
{
    foreach (var rule in rules)
    {
        // Add the current rule
        collectedRules.Add(rule);
        
        // If this is a container rule (like @media or @supports), collect its nested rules
        if (rule is ICssGroupingRule groupingRule)
        {
            CollectRulesRecursively(groupingRule.Rules, collectedRules);
        }
    }
}
```

## Shorthand Property Behavior

AngleSharp implements the CSS specification's behavior for shorthand properties:

1. **Expansion during parsing**: Shorthand properties (`margin`, `padding`, `border`, etc.) are automatically expanded into their component longhand properties
2. **Internal representation**: Properties are stored as longhands in the internal collection
3. **Property access**: Both shorthand and longhand names can be used with `GetPropertyValue()`
4. **Length calculation**: `CssStyleDeclaration.Length` returns the count of longhand properties
5. **Setting values**: Setting a shorthand property clears and sets all related longhands
6. **Testing considerations**: When asserting counts, account for shorthand expansion

### Example of Shorthand Expansion

```csharp
var style = new CssStyleDeclaration();
style.SetProperty("margin", "10px");

// The following is true:
style.Length == 4; // Not 1, because margin expands to 4 properties
style.GetPropertyValue("margin") == "10px";
style.GetPropertyValue("margin-top") == "10px";
style.GetPropertyValue("margin-right") == "10px";
style.GetPropertyValue("margin-bottom") == "10px";
style.GetPropertyValue("margin-left") == "10px";
```

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
    private readonly IBrowsingContext _context;
    
    // Use AngleSharp's value system internally
    public CssLengthValue FontSize => 
        _computedValues["font-size"] as CssLengthValue ?? CssLengthValue.Medium;
        
    public CssColorValue Color => 
        _computedValues["color"] as CssColorValue ?? CssColorValue.Black;
        
    // Convert to layout-optimized representation for performance-critical paths
    public float FontSizeInPixels => FontSize.ToPixel(null);
    
    // Rely on AngleSharp's IDeclarationFactory for initial values
    public string GetPropertyValue(string propertyName)
    {
        // First try property tree (computed values)
        string value = _propertyTree.GetPropertyValue(propertyName);
        
        // If no value found, get the initial value from the declaration factory
        if (string.IsNullOrEmpty(value))
        {
            var factory = _context.GetFactory<IDeclarationFactory>();
            if (factory != null)
            {
                var declarationInfo = factory.Create(propertyName);
                if (declarationInfo?.InitialValue != null)
                {
                    return declarationInfo.InitialValue.CssText;
                }
            }
        }
        
        return value;
    }
}
```

## Testing StyleSystem Components

When writing tests for StyleSystem components that interact with AngleSharp's CSS handling, keep in mind:

1. **Property count expectations**: When asserting the length of a style declaration that involves shorthand properties, account for their expansion to longhand properties
2. **Color normalization**: Color values are normalized to RGBA format
3. **Value comparison**: Use `GetPropertyValue()` rather than direct property access for consistent results
4. **Mock with care**: When mocking CSS interfaces, ensure they mimic AngleSharp's shorthand/longhand behavior
5. **Recursive rule collection**: Remember to recursively collect rules from container rules like `@media` when testing
6. **Default/initial values**: Use AngleSharp's `IDeclarationFactory` instead of hardcoding expected values when possible

## StyleSheetManager Configuration

When working with the `StyleSheetManager`, consider providing initialization options:

```csharp
// Create a StyleSheetManager with options
public StyleSheetManager(
    IBrowsingContext context, 
    bool loadUserAgentStylesheets = true)
{
    _context = context;
    _loadUserAgentStylesheets = loadUserAgentStylesheets;
    
    // Only load user agent stylesheets if enabled
    if (_loadUserAgentStylesheets)
    {
        LoadUserAgentStylesheets();
    }
}
```

This allows for testing scenarios and specialized applications where you want complete control over styling without browser defaults.

## Avoid Duplication

Do not reimplement:

- CSS value parsing
- Selector matching (use AngleSharp's selector engine)
- CSS property definitions
- Media query evaluation
- Initial values (use `IDeclarationFactory`)

Instead, focus on extending AngleSharp with layout and rendering capabilities.

## Advantages of Using AngleSharp Types

1. **Consistency**: Maintains consistent representation with AngleSharp's parser
2. **Compatibility**: Ensures seamless integration with AngleSharp's infrastructure
3. **Maintenance**: Reduces duplication and potential versioning conflicts
4. **Functionality**: Leverages AngleSharp's existing computation and conversion functions