
## Overview

The StyleComputationModule is responsible for computing CSS styles for DOM elements, following the CSS cascade, inheritance, and computation rules. This updated architecture incorporates enhancements needed to support the LayoutNG-inspired layout engine, including logical property resolution, efficient property access, and CSS variable integration.

## System Architecture

The module is designed with the following core components:

### 1. StyleComputationEngine

The main entry point that orchestrates the style computation process. It has been enhanced with new methods to support layout-optimized property access.

```csharp
public class StyleComputationEngine
{
    // Original method for computing element style
    public ICssStyleDeclaration ComputeElementStyle(
        IElement element,
        ICssStyleDeclaration parentStyle = null,
        string pseudoElement = null);
        
    // New methods for LayoutNG integration
    public T GetComputedValue<T>(
        IElement element, 
        string propertyName);
        
    public LogicalLength GetLogicalInlineSize(
        IElement element, 
        WritingMode writingMode);
        
    public LogicalLength GetLogicalBlockSize(
        IElement element, 
        WritingMode writingMode);
        
    public LogicalEdges GetLogicalMargins(
        IElement element, 
        WritingMode writingMode);
}
```

### 2. StyleSheetManager

Manages stylesheets from multiple sources and maintains their proper cascade order.

```csharp
public class StyleSheetManager
{
    public void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);
    public void UnregisterStylesheet(ICssStyleSheet stylesheet);
    public void SetDocument(IDocument document);
    public IEnumerable<StylesheetEntry> GetStylesheets();
}

public enum StylesheetOrigin
{
    UserAgent, // Browser default styles (lowest priority)
    User,      // User-defined styles
    Author     // Website/document styles (highest priority)
}
```

### 3. SelectorMatcher

Matches CSS selectors against elements and computes their specificity.

```csharp
public class SelectorMatcher
{
    public IEnumerable<MatchedRule> MatchRules(
        IElement element,
        IEnumerable<StylesheetEntry> stylesheets,
        string pseudoElement = null);
}

public class MatchedRule
{
    public ICssStyleRule Rule { get; }
    public Priority Specificity { get; }
    public StylesheetOrigin Origin { get; }
    public int OriginalIndex { get; }
}
```

### 4. CascadeResolver

Resolves the cascade by determining which style properties take precedence.

```csharp
public class CascadeResolver
{
    public ICssStyleDeclaration ResolveCascade(
        IEnumerable<MatchedRule> matchedRules,
        IElement element);
}
```

### 5. InheritanceProcessor

Applies inheritance rules to pass properties from parent to child elements. Leverages AngleSharp's existing property model for inheritance flags.

```csharp
public class InheritanceProcessor
{
    public ICssStyleDeclaration ApplyInheritance(
        ICssStyleDeclaration elementStyle,
        ICssStyleDeclaration parentStyle);
}
```

### 6. ValueComputer

Computes final absolute values by resolving relative units and special values. Enhanced with CSS variable support and logical property handling.

```csharp
public class ValueComputer
{
    public ICssStyleDeclaration ComputeValues(
        ICssStyleDeclaration inheritedStyle,
        IElement element,
        ICssStyleDeclaration computedParentStyle);
        
    // New methods for LayoutNG support
    public ICssValue ResolveLogicalValue(
        ICssValue value, 
        string propertyName, 
        WritingMode writingMode);
        
    public ICssValue ResolveUnitValue(
        ICssValue value, 
        ComputationContext context);
}
```

### 7. VariableResolver (NEW)

Resolves CSS custom property (variable) references.

```csharp
public class VariableResolver
{
    public ICssValue ResolveVariable(
        CssVarValue varValue, 
        IElement element, 
        ResolverContext context);
        
    public ICssValue ResolveFallback(
        ICssValue fallback, 
        IElement element, 
        ResolverContext context);
}
```

### 8. VariableRegistry (NEW)

Manages CSS custom properties from different origins.

```csharp
public class VariableRegistry
{
    public void RegisterVariable(
        string name, 
        ICssValue value, 
        StylesheetOrigin origin, 
        Priority specificity, 
        bool isImportant = false);
        
    public ICssValue GetVariableValue(string name);
}
```

### 9. StylePropertyResolver (NEW)

Provides logical property resolution based on writing mode.

```csharp
public class StylePropertyResolver
{
    public LogicalLength GetInlineSize(
        ICssStyleDeclaration style, 
        WritingMode writingMode);
        
    public LogicalLength GetBlockSize(
        ICssStyleDeclaration style, 
        WritingMode writingMode);
        
    public LogicalEdges GetMargins(
        ICssStyleDeclaration style, 
        WritingMode writingMode);
        
    public LogicalEdges GetBorders(
        ICssStyleDeclaration style, 
        WritingMode writingMode);
        
    public LogicalEdges GetPadding(
        ICssStyleDeclaration style, 
        WritingMode writingMode);
}
```

## LayoutNG Integration Enhancements

### 1. Fast Property Access

To optimize layout performance, we've added specialized methods for retrieving commonly used CSS properties:

```csharp
public static class StyleExtensions
{
    // Fast property access for layout-critical properties
    public static Display GetDisplay(this ICssStyleDeclaration style)
    {
        // Optimized implementation with caching
    }
    
    public static Position GetPosition(this ICssStyleDeclaration style)
    {
        // Optimized implementation with caching
    }
    
    public static float GetOpacity(this ICssStyleDeclaration style)
    {
        // Optimized implementation with caching
    }
    
    // Additional fast property accessors
}
```

### 2. Logical Property Support

The layout engine requires support for logical properties that automatically adjust based on writing mode:

```csharp
// Logical dimensions
public struct LogicalSize
{
    public CssValue InlineSize { get; }
    public CssValue BlockSize { get; }
    
    public PhysicalSize ToPhysicalSize(WritingMode writingMode);
}

// Logical edges (margins, borders, padding)
public struct LogicalEdges
{
    public CssValue InlineStart { get; }
    public CssValue InlineEnd { get; }
    public CssValue BlockStart { get; }
    public CssValue BlockEnd { get; }
    
    public PhysicalEdges ToPhysicalEdges(WritingMode writingMode);
}

// Writing mode and direction information
public struct WritingMode
{
    public Direction Direction { get; }
    public WritingModeType Mode { get; }
    
    public bool IsHorizontal { get; }
    public bool IsVertical { get; }
    public bool IsRightToLeft { get; }
}
```

### 3. CSS Variable Integration

Layout-specific integration with CSS custom properties (variables):

```csharp
// Variable resolution in layout context
public static class VariableExtensions
{
    // Resolve a variable in layout context
    public static float? ResolveVariableAsFloat(
        this IVariableResolver resolver,
        IElement element,
        string variableName,
        float? defaultValue = null)
    {
        // Implementation that resolves variable and converts to float
    }
    
    // Resolve a variable as a color
    public static Color? ResolveVariableAsColor(
        this IVariableResolver resolver,
        IElement element,
        string variableName,
        Color? defaultValue = null)
    {
        // Implementation that resolves variable and converts to color
    }
    
    // Other type-specific variable resolution methods
}
```

### 4. Computation Context Enhancements

Enhanced computation context to support layout requirements:

```csharp
public class ComputationContext : ICssComputeContext
{
    // Original context properties
    public IRenderDevice Device { get; }
    public IBrowsingContext Context { get; }
    public double FontSize { get; }
    public double RootFontSize { get; }
    
    // New properties for layout integration
    public WritingMode WritingMode { get; }
    public VariableResolver VariableResolver { get; }
    public IElement CurrentElement { get; }
    
    // New methods for layout integration
    public float ResolveLength(CssValue length, LengthContext context);
    public LogicalSize ResolveLogicalSize(CssValue width, CssValue height);
}

// Length resolution contexts
public enum LengthContext
{
    FontRelative,   // em, ex, ch, rem
    ViewportRelative, // vw, vh, vmin, vmax
    ContainerRelative, // %, container units
    Absolute        // px, pt, in, cm, mm
}
```

## Style Computation Process

The style computation process has been enhanced to support the LayoutNG requirements:

1. **Collect Stylesheets**: Get all applicable stylesheets from StyleSheetManager
2. **Match Selectors**: Use SelectorMatcher to find all rules that match the element
3. **Collect Variables**: Extract and register all CSS variables from matched rules
4. **Resolve Cascade**: Use CascadeResolver to determine which properties take precedence
5. **Apply Inheritance**: Use InheritanceProcessor to inherit properties from parent
6. **Resolve Variables**: Resolve any `var()` references in property values
7. **Compute Values**: Use ValueComputer to resolve all relative values to absolute ones
8. **Perform Logical Conversion**: Convert logical properties to physical properties based on writing mode

This process ensures that all CSS properties are fully resolved, including variables, and properly adjusted for writing mode, providing the layout engine with the complete information needed for constraint-based layout.

## Integration with Layout Engine

The StyleComputationModule integrates with the Layout Engine through the following interfaces:

```csharp
// Style interface used by LayoutEngine
public interface IStyleProvider
{
    // Get computed style for an element
    ICssStyleDeclaration GetComputedStyle(IElement element, string pseudoElement = null);
    
    // Get specific computed value
    T GetComputedValue<T>(IElement element, string propertyName);
    
    // Get logical properties
    LogicalSize GetLogicalSize(IElement element, WritingMode writingMode);
    LogicalEdges GetLogicalMargins(IElement element, WritingMode writingMode);
    LogicalEdges GetLogicalBorders(IElement element, WritingMode writingMode);
    LogicalEdges GetLogicalPadding(IElement element, WritingMode writingMode);
}

// Implementation using StyleComputationEngine
public class StyleComputationProvider : IStyleProvider
{
    private readonly StyleComputationEngine _engine;
    
    public StyleComputationProvider(StyleComputationEngine engine)
    {
        _engine = engine;
    }
    
    // Implementation of IStyleProvider methods using the engine
}
```

## Caching Integration

The updated StyleComputationModule includes enhanced caching integration:

```csharp
// Enhanced style cache for layout properties
public class StylePropertyCache
{
    // Cache specific property values
    private readonly Dictionary<(IElement, string), object> _propertyCache = 
        new Dictionary<(IElement, string), object>();
    
    // Get or compute a property value
    public T GetOrComputeValue<T>(
        IElement element, 
        string propertyName, 
        Func<IElement, string, T> computeFunc)
    {
        var key = (element, propertyName);
        
        if (_propertyCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
        {
            return typedValue;
        }
        
        var value = computeFunc(element, propertyName);
        _propertyCache[key] = value;
        return value;
    }
    
    // Invalidate cache for an element
    public void InvalidateElement(IElement element)
    {
        // Remove all entries for this element
    }
}
```

## Style Invalidation

The module supports fine-grained invalidation for LayoutNG integration:

```csharp
// Enhanced style invalidation
public class StyleInvalidationTracker
{
    // Track which elements need style recalculation
    private readonly HashSet<IElement> _styleDirtyElements = new HashSet<IElement>();
    
    // Track which elements need specific property recalculation
    private readonly Dictionary<IElement, HashSet<string>> _propertyDirtyMap = 
        new Dictionary<IElement, HashSet<string>>();
    
    // Mark an element as needing full style recalculation
    public void MarkStyleDirty(IElement element)
    {
        _styleDirtyElements.Add(element);
    }
    
    // Mark specific properties as needing recalculation
    public void MarkPropertiesDirty(IElement element, IEnumerable<string> properties)
    {
        if (!_propertyDirtyMap.TryGetValue(element, out var dirtyProps))
        {
            dirtyProps = new HashSet<string>();
            _propertyDirtyMap[element] = dirtyProps;
        }
        
        foreach (var prop in properties)
        {
            dirtyProps.Add(prop);
        }
    }
    
    // Check if an element needs style recalculation
    public bool IsStyleDirty(IElement element)
    {
        return _styleDirtyElements.Contains(element);
    }
    
    // Check if a specific property needs recalculation
    public bool IsPropertyDirty(IElement element, string property)
    {
        return IsStyleDirty(element) || 
               (_propertyDirtyMap.TryGetValue(element, out var dirtyProps) && 
                dirtyProps.Contains(property));
    }
}
```

## Writing Mode Support

Comprehensive support for writing modes is essential for LayoutNG-style layout:

```csharp
// Writing mode properties
public enum WritingModeType
{
    HorizontalTopToBottom,    // Default left-to-right
    VerticalRightToLeft,      // Traditional East Asian
    VerticalLeftToRight,      // Non-traditional
    SidewaysRightToLeft,      // Rotated horizontal text
    SidewaysLeftToRight       // Rotated horizontal text
}

public enum Direction
{
    Ltr,
    Rtl
}

// Conversion between logical and physical properties
public static class LogicalPropertyResolver
{
    // Convert logical to physical based on writing mode
    public static (string Physical, string Value) ToPhysical(
        string logicalProperty, 
        string value, 
        WritingMode writingMode)
    {
        // Conversion implementation
    }
    
    // Convert physical to logical based on writing mode
    public static (string Logical, string Value) ToLogical(
        string physicalProperty, 
        string value, 
        WritingMode writingMode)
    {
        // Conversion implementation
    }
}
```

## Conclusion

The enhanced StyleComputationModule provides comprehensive support for the LayoutNG-inspired layout engine. The additions include:

1. Efficient property access for layout-critical properties
2. Logical property resolution based on writing mode
3. Full CSS variable resolution
4. Enhanced caching for specific style properties
5. Fine-grained style invalidation
6. Integration interfaces for the layout engine

These enhancements ensure that the StyleComputationModule can provide the layout engine with all the information it needs for modern, constraint-based layout calculations while maintaining optimal performance.