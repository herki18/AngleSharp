# CSS Variable Resolution Architecture

## Overview

This document outlines the architecture for implementing CSS custom property (variable) resolution in the AngleSharp LayoutEngine. CSS variables (custom properties) provide a powerful mechanism for value reuse and dynamic styling, but require a comprehensive resolution approach that respects inheritance, cascading, and handles value substitution correctly.

## System Components

### 1. Variable Registry

**Purpose**: Track and manage all CSS variables defined in the document style context.

**Responsibilities**:
- Store variable definitions by name (`--variable-name`)
- Maintain source information (element, specificity, origin)
- Handle cascading through rule prioritization
- Support variable inheritance across the DOM tree

**Implementation Approach**:
```csharp
public class VariableRegistry
{
    private readonly Dictionary<string, VariableDefinition> _variables = new();

    public void RegisterVariable(string name, ICssValue value, StylesheetOrigin origin, Priority specificity);
    public ICssValue ResolveVariable(string name, IElement context);
}
```

### 2. Variable Resolver

**Purpose**: Resolve `var()` references to their computed values.

**Responsibilities**:
- Extract variable names from `var()` functions
- Detect and prevent circular references
- Handle fallback values
- Coordinate with the Variable Registry to obtain values
- Convert resolved values to appropriate CSS value types

**Implementation Approach**:
```csharp
public class VariableResolver
{
    private readonly VariableRegistry _registry;
    private readonly IRenderDevice _device;

    public ICssValue Resolve(CssVarValue varValue, IElement element, ResolverContext context);
}
```

### 3. Resolution Context

**Purpose**: Maintain state during the resolution process.

**Responsibilities**:
- Track variable reference chains to detect cycles
- Manage resolution depth to prevent stack overflows
- Provide access to element, parent, and root styles
- Cache intermediate computed values

**Implementation Approach**:
```csharp
public class ResolverContext
{
    private readonly HashSet<string> _resolutionChain = new();
    private readonly Dictionary<string, ICssValue> _cache = new();

    public bool TryEnterVariable(string name);
    public void ExitVariable(string name);
    public bool TryGetCachedValue(string key, out ICssValue value);
    public void CacheValue(string key, ICssValue value);
}
```

## Resolution Process

### 1. Variable Registration Phase

During style cascade resolution:
1. For each rule that matches an element:
   - Extract all custom properties (`--*` properties)
   - Register these variables with the Variable Registry
   - Store their values along with specificity and origin information
2. Build a complete variable set by combining:
   - User agent stylesheets (lowest priority)
   - User stylesheets
   - Author stylesheets (highest priority)
   - Inline styles (highest priority unless overridden with `!important`)

### 2. Variable Resolution Phase

When computing styles for an element:
1. For each property value containing a `var()` reference:
   - Initiate the resolution process via Variable Resolver
   - Resolve all nested variable references recursively
   - Apply appropriate type conversion based on property expectations
   - Return the fully computed value

### 3. Resolution Algorithm

For each `var()` reference:
1. Extract the variable name (e.g., `--main-color`)
2. Check if this variable is already being resolved (cycle detection)
3. Look up the variable's value in the Registry
   - Search in current element style
   - If not found, search parent element style (variables inherit by default)
   - Continue up to the root element
4. If found, resolve any nested variables in the value
5. If not found (or resolution fails), use the fallback value if provided
6. Cache the resolved value to prevent repeated resolution
7. Return the final computed value

## Implementation Considerations

### Circular Reference Detection

```csharp
// Example of circular reference detection
public ICssValue Resolve(CssVarValue varValue, ResolverContext context)
{
    string name = varValue.Name;

    // Check for circular reference
    if (!context.TryEnterVariable(name))
    {
        // Circular reference detected
        return ResolveCircularReference(varValue, context);
    }

    try
    {
        // Perform resolution
        return DoResolve(varValue, context);
    }
    finally
    {
        context.ExitVariable(name);
    }
}
```

### Nested Variable Resolution

```csharp
// Example of nested variable resolution
private ICssValue ResolveNestedVariables(ICssValue value, ResolverContext context)
{
    if (value is CssVarValue varValue)
    {
        // Recursively resolve this variable
        return Resolve(varValue, context);
    }

    // Handle complex values that might contain variables
    if (value is CssCalcValue calcValue)
    {
        // Resolve variables in calc() expressions
        return ResolveCalcExpression(calcValue, context);
    }

    // Value doesn't need resolution
    return value;
}
```

### Type Conversion

```csharp
// Example of type conversion after resolution
private ICssValue ConvertToExpectedType(ICssValue resolved, string propertyName)
{
    // For color properties, ensure we have a color value
    if (IsColorProperty(propertyName) && resolved is CssIdentifierValue identifier)
    {
        // Convert named color to RGBA
        return ConvertNamedColorToRgba(identifier.Value);
    }

    // For length properties, ensure we have a pixel value
    if (IsLengthProperty(propertyName) && resolved is CssLengthValue length)
    {
        // Convert to pixels if needed
        return ConvertToPixels(length);
    }

    return resolved;
}
```

## Performance Optimizations

1. **Caching**: Cache variable values after initial resolution.
2. **Lazy Evaluation**: Only resolve variables when their values are actually needed.
3. **Resolution Batching**: Batch variable resolutions to minimize redundant lookups.
4. **Early Termination**: Exit early when circular references or invalid values are detected.
5. **Variable Invalidation**: Invalidate cached variables selectively when style changes affect them.

## Edge Cases and Challenges

### 1. Inheritance vs. Cascading

Custom properties cascade like regular properties but also inherit by default, requiring careful tracking of the inheritance chain.

```csharp
// Solution: Track both cascading and inheritance
public ICssValue ResolveVariable(string name, IElement element)
{
    // First try to find the variable in the element's own style
    if (TryGetVariableFromElementStyle(name, element, out var value))
        return value;

    // If not found, look in parent element (inheritance)
    if (element.ParentElement != null)
        return ResolveVariable(name, element.ParentElement);

    // Not found anywhere in the tree
    return null;
}
```

### 2. Initial and Computed Values

Variables might reference properties that themselves have initial or computed values, requiring different resolution strategies.

```csharp
// Solution: Handle special property values
private ICssValue ResolveSpecialValue(string propertyName, ICssIdentifierValue identifier)
{
    switch (identifier.Value)
    {
        case "inherit":
            return GetInheritedValue(propertyName);
        case "initial":
            return GetInitialValue(propertyName);
        case "unset":
            return IsInheritable(propertyName) ? GetInheritedValue(propertyName) : GetInitialValue(propertyName);
        default:
            return identifier;
    }
}
```

### 3. Media Queries and Conditional Variables

Variables might be defined within media queries or conditional rules, requiring context-sensitive resolution.

```csharp
// Solution: Consider media context during registration
public void RegisterVariable(string name, ICssValue value, RuleContext ruleContext)
{
    if (!IsRuleApplicable(ruleContext))
        return;

    _variables[name] = value;
}
```

### 4. Environment Variables

CSS has environment variables (`env()`) in addition to custom properties, requiring similar but distinct handling.

```csharp
// Solution: Dedicated environment variable handling
public ICssValue ResolveEnvironmentVariable(string name)
{
    switch (name)
    {
        case "safe-area-inset-top":
            return new CssLengthValue(_device.SafeAreaInsetTop, CssLengthValue.Unit.Px);
        // Other environment variables...
        default:
            return null;
    }
}
```

## Integration with ValueComputer

The Variable Resolution system integrates with the ValueComputer in the property computation pipeline:

```csharp
public ICssStyleDeclaration ComputeValues(...)
{
    // Create variable registry and resolver
    var registry = new VariableRegistry();
    var resolver = new VariableResolver(registry, _device);

    // Register all variables from styles
    RegisterVariables(declaration, registry);
    RegisterVariables(parentStyle, registry);
    RegisterVariables(rootStyle, registry);

    // Create context for variable resolution
    var context = new ComputationContext(
        _device,
        _context,
        fontSize,
        rootFontSize,
        style,
        parentStyle,
        rootStyle,
        resolver);

    // Process properties with computed values
    foreach (var property in style)
    {
        if (property.RawValue is CssVarValue varValue)
        {
            // Resolve and compute the variable
            var resolved = context.Resolve(varValue.Name);
            var computed = ComputePropertyValue(property.Name, resolved, ...);

            // Add to computed style
            computedStyle.SetProperty(property.Name, computed.ToCss(), property.IsImportant ? "important" : null);
        }
        // Other property processing...
    }
}
```

## Implementation Phases

The variable resolution system can be implemented in phases:

### Phase 1: Basic Variable Support
- Variable definition collection
- Simple variable lookup
- Basic inheritance support
- No deep variable resolution

### Phase 2: Complete Variable Resolution
- Nested variable resolution
- Fallback value handling
- Circular reference detection
- Caching for performance

### Phase 3: Advanced Features
- Type conversion for computed values
- Integration with calc() expressions
- Media query conditional variables
- Environment variable support

## Conclusion

CSS variable resolution requires a systematic approach that respects the CSS cascade, handles inheritance properly, and correctly processes nested references. By following this architecture, the AngleSharp LayoutEngine can provide robust support for CSS custom properties that matches browser behavior.

This implementation will enable the complete style computation pipeline to handle modern CSS with variables, leading to accurate style resolution for web document rendering.