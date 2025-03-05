# CSS Variable Implementation Plan for ValueComputer

This document combines the ValueComputer Implementation Plan with the CSS Variable Resolution Architecture to create a comprehensive roadmap for implementing robust CSS variable support in the AngleSharp LayoutEngine.

## 1. Variable Resolution System

### 1.1 Variable Registry Implementation

**Purpose**: Track and manage all CSS variables defined in the document style context.

**Files to create/modify**: 
- `VariableRegistry.cs` (new)
- `ValueComputer.cs`

**Implementation details**:

```csharp
public class VariableRegistry
{
    private readonly Dictionary<string, VariableDefinition> _variables = new();

    // Register a variable with cascade information
    public void RegisterVariable(string name, ICssValue value, 
                                StylesheetOrigin origin, Priority specificity, 
                                bool isImportant = false)
    {
        var definition = new VariableDefinition(name, value, origin, specificity, isImportant);
        
        // Check if this definition should replace an existing one (cascade rules)
        if (_variables.TryGetValue(name, out var existing))
        {
            if (ShouldOverrideDefinition(existing, definition))
            {
                _variables[name] = definition;
            }
        }
        else
        {
            _variables[name] = definition;
        }
    }

    // Get a variable value
    public ICssValue? GetVariableValue(string name)
    {
        return _variables.TryGetValue(name, out var definition) 
            ? definition.Value 
            : null;
    }

    // Cascade rules for variables
    private bool ShouldOverrideDefinition(VariableDefinition existing, VariableDefinition newDef)
    {
        // Important flag has highest priority
        if (newDef.IsImportant && !existing.IsImportant)
            return true;
        if (existing.IsImportant && !newDef.IsImportant)
            return false;
            
        // Then compare origin (Author > User > UserAgent)
        if (newDef.Origin > existing.Origin)
            return true;
        if (existing.Origin > newDef.Origin)
            return false;
            
        // Then compare specificity
        if (newDef.Specificity > existing.Specificity)
            return true;
        if (existing.Specificity > newDef.Specificity)
            return false;
            
        // Finally, use document order
        return true; // Last one wins
    }
}

// Container for variable information
public class VariableDefinition
{
    public string Name { get; }
    public ICssValue Value { get; }
    public StylesheetOrigin Origin { get; }
    public Priority Specificity { get; }
    public bool IsImportant { get; }
    
    public VariableDefinition(string name, ICssValue value, 
                             StylesheetOrigin origin, Priority specificity, 
                             bool isImportant)
    {
        Name = name;
        Value = value;
        Origin = origin;
        Specificity = specificity;
        IsImportant = isImportant;
    }
}
```

### 1.2 Variable Resolver Implementation

**Purpose**: Resolve `var()` references to their computed values.

**Files to create/modify**:
- `VariableResolver.cs` (new)
- `ValueComputer.cs`

**Implementation details**:

```csharp
public class VariableResolver
{
    private readonly VariableRegistry _registry;
    private readonly IBrowsingContext _context;
    private readonly IRenderDevice _device;
    
    public VariableResolver(VariableRegistry registry, IBrowsingContext context, IRenderDevice device)
    {
        _registry = registry;
        _context = context;
        _device = device;
    }
    
    // Resolve a variable reference
    public ICssValue? ResolveVariable(CssVarValue varValue, IElement element, ResolverContext context)
    {
        // Extract variable name
        string name = varValue.Name;
        
        // Check for circular reference
        if (!context.TryEnterVariable(name))
        {
            // Circular reference detected, use fallback if available
            return ResolveFallback(varValue.Fallback, element, context);
        }
        
        try
        {
            // Try to get value from element's own style
            var value = TryGetOwnValue(name, element);
            
            // If not found in element's style, look in inheritance chain
            if (value == null)
            {
                value = TryGetInheritedValue(name, element);
            }
            
            // If still not found, use fallback if available
            if (value == null)
            {
                return ResolveFallback(varValue.Fallback, element, context);
            }
            
            // Resolve any nested variables in the value
            return ResolveNestedReferences(value, element, context);
        }
        finally
        {
            context.ExitVariable(name);
        }
    }
    
    // Resolve fallback value
    private ICssValue? ResolveFallback(ICssValue? fallback, IElement element, ResolverContext context)
    {
        if (fallback == null)
            return null;
            
        // If fallback is another var(), resolve it
        if (fallback is CssVarValue nestedVar)
        {
            return ResolveVariable(nestedVar, element, context);
        }
        
        // Otherwise use as is (after resolving any nested references)
        return ResolveNestedReferences(fallback, element, context);
    }
    
    // Resolve nested variables within a value
    private ICssValue? ResolveNestedReferences(ICssValue value, IElement element, ResolverContext context)
    {
        // Handle different value types
        if (value is CssVarValue nestedVar)
        {
            return ResolveVariable(nestedVar, element, context);
        }
        
        if (value is CssCalcValue calcValue)
        {
            return ResolveCalcExpression(calcValue, element, context);
        }
        
        // For other value types, return as is
        return value;
    }
    
    // Resolve variables in calc() expressions
    private ICssValue? ResolveCalcExpression(CssCalcValue calcValue, IElement element, ResolverContext context)
    {
        // Implementation for resolving variables in calc() expressions
        // This would involve traversing the expression tree and resolving each var() reference
        
        // Simplified example - full implementation would be more complex
        if (calcValue.Expression is CssVarValue varInCalc)
        {
            var resolved = ResolveVariable(varInCalc, element, context);
            // Create a new calc value with the resolved variable
            return CreateCalcWithResolvedVariable(calcValue, varInCalc, resolved);
        }
        
        return calcValue;
    }
    
    // Try to get variable value from element's own style
    private ICssValue? TryGetOwnValue(string name, IElement element)
    {
        // In a full implementation, this would look at the matched CSS rules for the element
        // For simplicity, we're using the registry directly here
        return _registry.GetVariableValue(name);
    }
    
    // Get variable value from inheritance chain
    private ICssValue? TryGetInheritedValue(string name, IElement element)
    {
        var parent = element.ParentElement;
        if (parent == null)
            return null;
            
        // Try to get value from parent's own style
        var value = TryGetOwnValue(name, parent);
        if (value != null)
            return value;
            
        // Recursively check further up the tree
        return TryGetInheritedValue(name, parent);
    }
}
```

### 1.3 Resolution Context Implementation

**Purpose**: Maintain state during the variable resolution process.

**Files to create/modify**:
- `ResolverContext.cs` (new)
- `VariableResolver.cs`

**Implementation details**:

```csharp
public class ResolverContext
{
    private readonly HashSet<string> _resolutionChain = new();
    private readonly Dictionary<string, ICssValue> _cache = new();
    private const int MaxResolutionDepth = 32; // Prevent excessive recursion
    
    // Current resolution depth tracker
    public int CurrentDepth { get; private set; } = 0;
    
    // Try to enter variable resolution (with cycle detection)
    public bool TryEnterVariable(string name)
    {
        // Check for circular reference
        if (_resolutionChain.Contains(name))
            return false;
            
        // Check for excessive resolution depth
        if (CurrentDepth >= MaxResolutionDepth)
            return false;
            
        // Enter variable resolution
        _resolutionChain.Add(name);
        CurrentDepth++;
        return true;
    }
    
    // Exit variable resolution
    public void ExitVariable(string name)
    {
        _resolutionChain.Remove(name);
        CurrentDepth--;
    }
    
    // Try to get cached value
    public bool TryGetCachedValue(string key, out ICssValue? value)
    {
        return _cache.TryGetValue(key, out value);
    }
    
    // Cache resolved value
    public void CacheValue(string key, ICssValue value)
    {
        _cache[key] = value;
    }
    
    // Generate cache key
    public string GenerateCacheKey(string name, IElement element)
    {
        // Unique key combining variable name and element
        return $"{name}_{element.GetHashCode()}";
    }
}
```

## 2. Integration with ValueComputer

### 2.1 Enhance ComputationContext class

**Purpose**: Extend the existing ComputationContext to support variable resolution.

**Files to modify**:
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Updated ComputationContext class
private class ComputationContext : ICssComputeContext
{
    // Existing properties
    private readonly IRenderDevice _device;
    private readonly IBrowsingContext _context;
    private readonly double _fontSize;
    private readonly double _rootFontSize;
    private readonly ICssStyleDeclaration _style;
    private readonly ICssStyleDeclaration _parentStyle;
    private readonly ICssStyleDeclaration _rootStyle;
    
    // New properties for variable support
    private readonly VariableRegistry _variableRegistry;
    private readonly VariableResolver _variableResolver;
    private readonly ResolverContext _resolverContext;
    
    // Updated constructor
    public ComputationContext(
        IRenderDevice device,
        IBrowsingContext context,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration style,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle,
        IElement element,
        VariableRegistry variableRegistry)
    {
        _device = device;
        _context = context;
        _fontSize = fontSize;
        _rootFontSize = rootFontSize;
        _style = style;
        _parentStyle = parentStyle;
        _rootStyle = rootStyle;
        
        // Initialize variable resolution components
        _variableRegistry = variableRegistry;
        _variableResolver = new VariableResolver(variableRegistry, context, device);
        _resolverContext = new ResolverContext();
    }
    
    // Implement ICssComputeContext
    public IRenderDevice Device => _device;
    public IBrowsingContext Context => _context;
    public IValueConverter Converter => null;
    
    // Enhanced Resolve method handling both property and variable references
    public ICssValue? Resolve(string name)
    {
        // If it's a CSS variable, resolve it
        if (name.StartsWith("--"))
        {
            // Create a var() reference
            var varValue = new CssVarValue(name, null);
            return ResolveVarReference(varValue);
        }

        // For other property references
        var property = _style?.GetProperty(name);
        return property?.RawValue;
    }
    
    // Method to resolve var() references
    public ICssValue? ResolveVarReference(CssVarValue varValue)
    {
        // Try to get from cache first
        string cacheKey = _resolverContext.GenerateCacheKey(varValue.Name, _currentElement);
        if (_resolverContext.TryGetCachedValue(cacheKey, out var cachedValue))
            return cachedValue;
            
        // Resolve the reference
        var resolved = _variableResolver.ResolveVariable(varValue, _currentElement, _resolverContext);
            
        // Cache the result
        if (resolved != null)
            _resolverContext.CacheValue(cacheKey, resolved);
            
        return resolved;
    }
    
    // Gets the inherited value for a property from parent style
    public ICssValue? GetInheritedValue(string propertyName)
    {
        if (_parentStyle == null)
            return null;

        var property = _parentStyle.GetProperty(propertyName);
        return property?.RawValue;
    }
}
```

### 2.2 Update ValueComputer to Register Variables

**Purpose**: Collect and register all CSS variables before property computation.

**Files to modify**:
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Method to register variables from style declarations
private void RegisterVariables(
    ICssStyleDeclaration style,
    VariableRegistry registry,
    StylesheetOrigin origin = StylesheetOrigin.Author,
    Priority specificity = null)
{
    if (style == null)
        return;
        
    // Create default specificity if none provided
    specificity ??= new Priority(0, 0, 0, 0);
    
    // Extract and register all custom properties (--*)
    foreach (var property in style)
    {
        if (property.Name.StartsWith("--") && property.RawValue != null)
        {
            registry.RegisterVariable(
                property.Name,
                property.RawValue,
                origin,
                specificity,
                property.IsImportant);
        }
    }
}

// Updated ComputeValues method to incorporate variable resolution
public ICssStyleDeclaration ComputeValues(
    ICssStyleDeclaration declaration,
    IElement element,
    ICssStyleDeclaration parentStyle,
    ICssStyleDeclaration rootStyle)
{
    // Step 1: Create a variable registry
    var variableRegistry = new VariableRegistry();
    
    // Step 2: Register variables from all style sources
    // Note: In a real implementation, these would come with proper specificity values
    RegisterVariables(rootStyle, variableRegistry, StylesheetOrigin.UserAgent);
    RegisterVariables(parentStyle, variableRegistry, StylesheetOrigin.Author);
    RegisterVariables(declaration, variableRegistry, StylesheetOrigin.Author);
    
    // Continue with existing computation steps...
    // Calculate root and parent font sizes
    var rootFontSize = ExtractFontSizeInPixels(rootStyle);
    var parentFontSize = ExtractFontSizeInPixels(parentStyle);
    
    // Compute element's font-size first
    var elementFontSize = ComputeFontSize(declaration, parentFontSize, rootFontSize);
    
    // Create context with variable support
    var computeContext = new ComputationContext(
        _device,
        _context,
        elementFontSize,
        rootFontSize,
        declaration,
        parentStyle,
        rootStyle,
        element,
        variableRegistry);
    
    // Process all properties with variable resolution
    var result = new CssStyleDeclaration(_context);
    
    // Process properties
    foreach (var property in declaration)
    {
        ICssValue? computedValue = null;
        
        try
        {
            // Compute the property value (with variable resolution)
            computedValue = ComputePropertyValue(
                property.Name,
                property.RawValue,
                elementFontSize,
                rootFontSize,
                computeContext);
                
            if (computedValue != null)
            {
                // Set the computed property
                result.SetProperty(
                    property.Name,
                    computedValue.ToCss(),
                    property.IsImportant ? "important" : null);
            }
        }
        catch (Exception ex)
        {
            // Log error and use original value as fallback
            System.Diagnostics.Debug.WriteLine(
                $"Error computing value for {property.Name}: {ex.Message}");
                
            // Use original value as fallback
            result.SetProperty(
                property.Name,
                property.Value,
                property.IsImportant ? "important" : null);
        }
    }
    
    return result;
}
```

### 2.3 Enhance ComputePropertyValue for Variable Support

**Purpose**: Update the property computation to handle variables.

**Files to modify**:
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Enhanced ComputePropertyValue method
private ICssValue? ComputePropertyValue(
    string propertyName,
    ICssValue value,
    double fontSize,
    double rootFontSize,
    ComputationContext context)
{
    // Handle CSS variables
    if (value is CssVarValue varValue)
    {
        // Resolve the variable
        var resolvedValue = context.ResolveVarReference(varValue);
        if (resolvedValue == null)
        {
            // If resolution fails, return initial value or null
            return null;
        }
        
        // Compute the resolved value
        return ComputePropertyValue(
            propertyName,
            resolvedValue,
            fontSize,
            rootFontSize,
            context);
    }
    
    // Rest of the existing ComputePropertyValue implementation
    // ...
}
```

## 3. calc() Expression Evaluation

### 3.1 Enhanced calc() Expression Parser

**Purpose**: Handle complex calc() expressions with variables.

**Files to create/modify**:
- `CalcExpressionEvaluator.cs` (new)
- `ValueComputer.cs`

**Implementation details**:

```csharp
public class CalcExpressionEvaluator
{
    private readonly IElement _element;
    private readonly double _fontSize;
    private readonly double _rootFontSize;
    private readonly IRenderDevice _device;
    private readonly ComputationContext _context;
    
    public CalcExpressionEvaluator(
        IElement element,
        double fontSize,
        double rootFontSize,
        IRenderDevice device,
        ComputationContext context)
    {
        _element = element;
        _fontSize = fontSize;
        _rootFontSize = rootFontSize;
        _device = device;
        _context = context;
    }
    
    // Evaluate a calc expression
    public ICssValue? EvaluateCalc(CssCalcValue calcValue)
    {
        // Parse the expression
        var expression = calcValue.Expression;
        
        // Resolve any variables in the expression first
        expression = ResolveVariablesInExpression(expression);
        
        // Evaluate the expression
        var result = EvaluateExpression(expression);
        
        // Return result in appropriate unit
        return CreateResultValue(result);
    }
    
    // Resolve variables in an expression
    private ICssValue ResolveVariablesInExpression(ICssValue expression)
    {
        if (expression is CssVarValue varValue)
        {
            // Resolve variable reference
            var resolved = _context.ResolveVarReference(varValue);
            return resolved ?? expression;
        }
        
        // For complex expressions (would need proper expression tree handling)
        // This is a simplified example
        return expression;
    }
    
    // Evaluate a calc expression (simplified)
    private double EvaluateExpression(ICssValue expression)
    {
        // Handle different expression types
        if (expression is CssLengthValue length)
        {
            return ConvertLengthToPixels(length);
        }
        
        if (expression is CssNumberValue number)
        {
            return number.Value;
        }
        
        // Handle operators (add, subtract, multiply, divide)
        // This would require proper expression tree traversal
        
        // Default fallback
        return 0;
    }
    
    // Convert length to pixels considering context
    private double ConvertLengthToPixels(CssLengthValue length)
    {
        // Similar to existing ConvertLengthToPixels in ValueComputer
        // ...
    }
    
    // Create result value with appropriate unit
    private ICssValue CreateResultValue(double value)
    {
        // For now, return pixels
        return new CssLengthValue(value, CssLengthValue.Unit.Px);
    }
}
```

### 3.2 Mixed Unit Operations

**Purpose**: Handle operations between different units in calc().

**Files to modify**:
- `CalcExpressionEvaluator.cs`

**Implementation details**:

```csharp
// Add to CalcExpressionEvaluator class
// Handle operations with mixed units
private double CombineUnits(double value1, string unit1, double value2, string unit2, string operation)
{
    // Convert to common unit (usually pixels)
    var pixels1 = ConvertToPixels(value1, unit1);
    var pixels2 = ConvertToPixels(value2, unit2);
    
    // Perform operation
    return operation switch
    {
        "+" => pixels1 + pixels2,
        "-" => pixels1 - pixels2,
        "*" => pixels1 * pixels2,
        "/" => pixels1 / pixels2,
        _ => throw new NotSupportedException($"Operation {operation} not supported")
    };
}

// Convert value to pixels based on unit
private double ConvertToPixels(double value, string unit)
{
    switch (unit.ToLowerInvariant())
    {
        case "px":
            return value;
        case "em":
            return value * _fontSize;
        case "rem":
            return value * _rootFontSize;
        case "vh":
            return value * _device.ViewPortHeight / 100.0;
        case "vw":
            return value * _device.ViewPortWidth / 100.0;
        case "%":
            // Percentage requires context
            // For simplicity, we're assuming font-size context
            return value * _fontSize / 100.0;
        // Other units...
        default:
            return value;
    }
}
```

## 4. Error Handling and Robustness

### 4.1 Improved Error Recovery

**Purpose**: Add robust error handling to the variable resolution system.

**Files to modify**:
- `VariableResolver.cs`
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Add to VariableResolver class
// Safe variable resolution with error handling
public ICssValue? SafeResolveVariable(CssVarValue varValue, IElement element, ResolverContext context)
{
    try
    {
        return ResolveVariable(varValue, element, context);
    }
    catch (Exception ex)
    {
        // Log error
        System.Diagnostics.Debug.WriteLine($"Error resolving variable {varValue.Name}: {ex.Message}");
        
        // Try fallback if available
        if (varValue.Fallback != null)
        {
            try
            {
                return ResolveFallback(varValue.Fallback, element, context);
            }
            catch
            {
                // If fallback fails too, return null
                return null;
            }
        }
        
        return null;
    }
}

// Add to ValueComputer class
// Safe computation with error recovery
private ICssValue? SafeComputeValue(string propertyName, ICssValue value, Func<ICssValue?> computeFunc)
{
    try
    {
        return computeFunc() ?? GetDefaultForProperty(propertyName);
    }
    catch (Exception ex)
    {
        // Log error
        System.Diagnostics.Debug.WriteLine($"Error computing {propertyName}: {ex.Message}");
        
        // Return a safe default
        return GetDefaultForProperty(propertyName);
    }
}

// Get default value for a property
private ICssValue? GetDefaultForProperty(string propertyName)
{
    // Return appropriate default based on property
    switch (propertyName)
    {
        case PropertyNames.Color:
            return new CssColorValue(0, 0, 0, 1); // black
        case PropertyNames.FontSize:
            return new CssLengthValue(16, CssLengthValue.Unit.Px);
        // Other properties...
        default:
            return null;
    }
}
```

### 4.2 Validation and Circular Reference Handling

**Purpose**: Add validation to prevent invalid variable usage.

**Files to modify**:
- `ResolverContext.cs`
- `VariableResolver.cs`

**Implementation details**:

```csharp
// Add to ResolverContext class
// Enhanced cycle detection with debug information
public (bool HasCycle, IEnumerable<string> Path) DetectCycle(string variableName)
{
    if (_resolutionChain.Contains(variableName))
    {
        // Create path for debugging
        var cyclePath = new List<string>(_resolutionChain);
        cyclePath.Add(variableName);
        return (true, cyclePath);
    }
    
    return (false, Enumerable.Empty<string>());
}

// Add to VariableResolver class
// Enhanced circular reference detection
private ICssValue? HandleCircularReference(CssVarValue varValue, ResolverContext context)
{
    var (hasCycle, path) = context.DetectCycle(varValue.Name);
    
    if (hasCycle)
    {
        // Log the cycle for debugging
        var pathString = string.Join(" -> ", path);
        System.Diagnostics.Debug.WriteLine($"Circular reference detected: {pathString}");
        
        // Use fallback if available
        if (varValue.Fallback != null)
        {
            return ResolveFallback(varValue.Fallback, context);
        }
        
        // Otherwise return null (or could use initial value)
        return null;
    }
    
    return null;
}
```

## 5. Integration with StyleComputationEngine

### 5.1 Update ComputeElementStyle Method

**Purpose**: Integrate variable resolution into the main StyleComputationEngine.

**Files to modify**:
- `StyleComputationEngine.cs`

**Implementation details**:

```csharp
// Update StyleComputationEngine.ComputeElementStyle method
public ICssStyleDeclaration ComputeElementStyle(
    IElement element,
    ICssStyleDeclaration? parentStyle = null,
    string? pseudoElement = null)
{
    if (element is null) throw new ArgumentNullException(nameof(element));

    var window = element.OwnerDocument?.DefaultView;
    if (window is null)
        throw new InvalidOperationException("Element must be part of a document with a default view");

    if (parentStyle is null && element.ParentElement is not null)
    {
        parentStyle = ComputeElementStyle(element.ParentElement, null, pseudoElement);
    }

    // 1. Get stylesheets
    var stylesheets = _stylesheetManager.GetStylesheets();

    // 2. Match selectors
    var matchedRules = _selectorMatcher.MatchRules(element, stylesheets, pseudoElement);

    // 3. Extract and register all CSS variables from matched rules
    var variableRegistry = new VariableRegistry();
    foreach (var rule in matchedRules)
    {
        RegisterVariablesFromRule(rule, variableRegistry);
    }

    // 4. Resolve cascade
    var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);

    // 5. Apply inheritance
    var inheritedStyle = _inheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);

    // 6. Compute values with variable resolution
    var computedStyle = _valueComputer.ComputeValues(
        inheritedStyle, 
        element, 
        parentStyle!, 
        GetRootStyle(element),
        variableRegistry);

    return computedStyle;
}

// Helper to register variables from a matched rule
private void RegisterVariablesFromRule(MatchedRule rule, VariableRegistry registry)
{
    foreach (var property in rule.Rule.Style)
    {
        if (property.Name.StartsWith("--") && property.RawValue != null)
        {
            registry.RegisterVariable(
                property.Name,
                property.RawValue,
                rule.Origin,
                rule.Specificity,
                property.IsImportant);
        }
    }
}

// Helper to get root element style
private ICssStyleDeclaration GetRootStyle(IElement element)
{
    var document = element.OwnerDocument;
    if (document == null || document.DocumentElement == null)
        return new CssStyleDeclaration(_browsingContext);
        
    return ComputeElementStyle(document.DocumentElement);
}
```

### 5.2 Update ValueComputer Interface

**Purpose**: Update the ValueComputer interface to support variable registry.

**Files to modify**:
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Add overload to ComputeValues method
public ICssStyleDeclaration ComputeValues(
    ICssStyleDeclaration declaration,
    IElement element,
    ICssStyleDeclaration parentStyle,
    ICssStyleDeclaration rootStyle,
    VariableRegistry variableRegistry)
{
    // Similar to existing method but uses provided variable registry
    // ...
}
```

## 6. Performance Optimization

### 6.1 Caching Strategy

**Purpose**: Optimize variable resolution performance with caching.

**Files to modify**:
- `VariableResolver.cs`
- `ResolverContext.cs`

**Implementation details**:

```csharp
// Enhanced caching in ResolverContext
public class ResolverContext
{
    // Existing fields
    private readonly HashSet<string> _resolutionChain = new();
    
    // Two-level cache strategy
    // 1. Element-specific cache (variables resolved in specific element context)
    private readonly Dictionary<(string Name, int ElementHash), ICssValue> _elementCache = new();
    
    // 2. Global cache (variables with fixed values regardless of context)
    private readonly Dictionary<string, ICssValue> _globalCache = new();
    
    // Check element-specific cache
    public bool TryGetElementCachedValue(string name, IElement element, out ICssValue? value)
    {
        return _elementCache.TryGetValue((name, element.GetHashCode()), out value);
    }
    
    // Cache element-specific value
    public void CacheElementValue(string name, IElement element, ICssValue value)
    {
        _elementCache[(name, element.GetHashCode())] = value;
    }
    
    // Check global cache
    public bool TryGetGlobalCachedValue(string name, out ICssValue? value)
    {
        return _globalCache.TryGetValue(name, out value);
    }
    
    // Cache global value
    public void CacheGlobalValue(string name, ICssValue value)
    {
        _globalCache[name] = value;
    }
    
    // Determine if variable can be globally cached
    public bool CanGloballyCache(string name, ICssValue value)
    {
        // Variables that don't reference other variables or context-dependent values
        // can be globally cached
        
        // Simple check - if it's a simple value type
        return value is CssColorValue || 
               (value is CssLengthValue length && length.Type == CssLengthValue.Unit.Px);
    }
}
```

### 6.2 Variable Resolution Batching

**Purpose**: Optimize performance by batching variable resolutions.

**Files to modify**:
- `ValueComputer.cs`

**Implementation details**:

```csharp
// Add to ValueComputer class
// Pre-resolve common variables
private void PreresolveCommonVariables(
    IDictionary<string, CssVarValue> variables,
    IElement element,
    ComputationContext context)
{
    // Group variables by dependencies to resolve efficiently
    var independentVars = new List<string>();
    var dependentVars = new Dictionary<string, HashSet<string>>();
    
    // Identify independent variables (no var() references)
    foreach (var (name, varValue) in variables)
    {
        if (!ContainsVarReferences(varValue))
        {
            independentVars.Add(name);
        }
        else
        {
            // Track dependencies
            var dependencies = ExtractVarDependencies(varValue);
            dependentVars[name] = dependencies;
        }
    }
    
    // First resolve independent variables
    foreach (var name in independentVars)
    {
        context.ResolveVarReference(variables[name]);
    }
    
    // Then resolve dependent variables in dependency order
    // This would use topological sort for proper ordering
    // Simplified version for demonstration
    while (dependentVars.Count > 0)
    {
        bool progress = false;
        
        foreach (var (name, dependencies) in dependentVars.ToList())
        {
            // If all dependencies are resolved, resolve this variable
            if (dependencies.All(dep => !dependentVars.ContainsKey(dep)))
            {
                context.ResolveVarReference(variables[name]);
                dependentVars.Remove(name);
                progress = true;
            }
        }
        
        // If no progress made, we have circular dependencies
        if (!progress && dependentVars.Count > 0)
        {
            // Handle remaining variables with potential circular dependencies
            foreach (var (name, _) in dependentVars)
            {
                context.ResolveVarReference(variables[name]);
            }
            break;
        }
    }
}

// Helper to check if value contains var() references
private bool ContainsVarReferences(ICssValue value)
{
    if (value is CssVarValue)
        return true;
        
    // For complex values, would need to check all components
    // Simplified for demonstration
    return false;
}

// Helper to extract variable dependencies
private HashSet<string> ExtractVarDependencies(CssVarValue varValue)
{
    var dependencies = new HashSet<string>();
    dependencies.Add(varValue.Name);
    
    // For nested references, would need recursive extraction
    // Simplified for demonstration
    return dependencies;
}
```

## 7. Testing Strategy

### 7.1 Unit Tests for Variable Resolution

**Purpose**: Verify correct variable resolution behavior.

**Test cases to implement**:
- Basic variable resolution
- Nested variable references
- Circular reference detection
- Fallback value handling
- Variable inheritance across DOM tree
- Cascade ordering of variable definitions

**Example test structure**:

```csharp
[Test]
public async Task VariableResolver_BasicVariableResolution_ResolvesCorrectly()
{
    // Arrange
    var html = @"
    <html>
    <head>
        <style>
            :root {
                --main-color: red;
            }
            div {
                color: var(--main-color);
            }
        </style>
    </head>
    <body>
        <div id='test'>Test</div>
    </body>
    </html>";

    var document = await _context.OpenAsync(req => req.Content(html));
    var element = document.GetElementById("test");

    // Act
    var style = _engine.ComputeElementStyle(element);

    // Assert
    Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgb(255, 0, 0)"));
}

[Test]
public async Task VariableResolver_NestedVariables_ResolvesCorrectly()
{
    // Arrange
    var html = @"
    <html>
    <head>
        <style>
            :root {
                --primary: blue;
                --theme-color: var(--primary);
            }
            div {
                color: var(--theme-color);
            }
        </style>
    </head>
    <body>
        <div id='test'>Test</div>
    </body>
    </html>";

    var document = await _context.OpenAsync(req => req.Content(html));
    var element = document.GetElementById("test");

    // Act
    var style = _engine.ComputeElementStyle(element);

    // Assert
    Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgb(0, 0, 255)"));
}
```

### 7.2 Integration Tests

**Purpose**: Verify variable resolution within the full style computation pipeline.

**Test cases to implement**:
- Variables in complex stylesheets
- Variables with media queries
- Variables with calc() expressions
- Performance benchmarks for variable resolution

**Example test structure**:

```csharp
[Test]
public async Task StyleComputation_VariablesWithCalc_ComputesCorrectly()
{
    // Arrange
    var html = @"
    <html>
    <head>
        <style>
            :root {
                --spacing: 10px;
            }
            div {
                margin: calc(var(--spacing) * 2);
                padding: calc(var(--spacing) / 2);
            }
        </style>
    </head>
    <body>
        <div id='test'>Test</div>
    </body>
    </html>";

    var document = await _context.OpenAsync(req => req.Content(html));
    var element = document.GetElementById("test");

    // Act
    var style = _engine.ComputeElementStyle(element);

    // Assert
    Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("20px"));
    Assert.That(style.GetPropertyValue("padding"), Is.EqualTo("5px"));
}
```

## 8. Implementation Phases

### Phase 1: Basic Variable Support
- Implement VariableRegistry
- Add basic variable resolution without handling nested references
- Create unit tests for basic functionality

### Phase 2: Complete Variable Resolution
- Add support for nested variable references
- Implement fallback value handling
- Add circular reference detection
- Implement variable inheritance
- Expand test coverage

### Phase 3: Advanced Features and Integration
- Integrate with calc() expression evaluation
- Add support for variables in other contexts
- Implement caching and performance optimizations
- Create comprehensive integration tests

### Phase 4: Performance Optimization and Refinement
- Optimize resolution algorithm
- Add debugging and error reporting
- Implement dependency tracking for cache invalidation
- Create performance benchmarks and optimization

## Conclusion

This implementation plan provides a comprehensive roadmap for implementing CSS variable support in the AngleSharp LayoutEngine's ValueComputer component. By following this architecture, the system will be able to correctly resolve CSS custom properties while respecting the CSS cascade, properly handling inheritance, and efficiently processing nested references.

The implementation follows a modular approach with clear separation of concerns, making it maintainable and testable. The phased implementation strategy allows for incremental development and testing, ensuring that each component works correctly before building on it.