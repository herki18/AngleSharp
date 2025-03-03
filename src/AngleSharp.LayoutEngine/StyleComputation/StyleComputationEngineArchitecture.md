# StyleComputationEngine Architecture

## Overview

The StyleComputationEngine is a self-contained system designed to work alongside AngleSharp, leveraging its DOM and CSS models while maintaining clear boundaries. It computes CSS styles for HTML elements following the CSS cascade, inheritance, and computation rules.

## System Architecture

The engine is designed with the following core components:

### 1. StyleComputationEngine

The main entry point that orchestrates the style computation process. It:
- Takes an element, optional parent style, and optional pseudo-element as input
- Returns a fully computed style declaration
- Delegates to specialized components for each step of the computation

```csharp
public class StyleComputationEngine
{
    public ICssStyleDeclaration ComputeElementStyle(
        IElement element,
        ICssStyleDeclaration parentStyle = null,
        string pseudoElement = null);
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

Key responsibilities:
- Handles properties marked with `PropertyFlags.Inherited` in AngleSharp
- Manages explicit inheritance via the `inherit` keyword
- Supports CSS custom properties (variables) which always inherit
- Manages the parent-child inheritance chain
- Handles special cases like the root element and missing parent styles

### 6. ValueComputer

Computes final values by resolving relative units and handling special values.

```csharp
public class ValueComputer
{
    public ICssStyleDeclaration ComputeValues(
        ICssStyleDeclaration declaration,
        IElement element,
        ICssStyleDeclaration parentStyle);
}
```

## Computation Flow

1. **Collect Stylesheets**: Get all applicable stylesheets from StyleSheetManager
2. **Match Selectors**: Use SelectorMatcher to find all rules that match the element
3. **Resolve Cascade**: Use CascadeResolver to determine which properties take precedence
4. **Apply Inheritance**: Use InheritanceProcessor to inherit properties from parent
5. **Compute Values**: Use ValueComputer to resolve all relative values to absolute ones

## Integration with AngleSharp

The engine uses the following AngleSharp interfaces:
- `IElement` and `IDocument` for DOM access
- `ICssStyleSheet` for stylesheet representation
- `ICssStyleRule` for style rules
- `ICssStyleDeclaration` for style declarations
- `ICssProperty` for individual properties with inheritance flags
- `ICssValue` for property values and computation

### Inheritance Model

Our inheritance system builds upon AngleSharp's property model:
- AngleSharp's `CssProperty` class already maintains inheritance flags through `PropertyFlags.Inherited`
- Properties expose `CanBeInherited` and `IsInherited` properties for inheritance decisions
- The `Compute` method handles value transformations
- Our InheritanceProcessor focuses on cross-element inheritance not handled by AngleSharp

## Device Information

Device information is used for:
- Media query evaluation
- Viewport-relative unit calculations
- Device-dependent property calculations

## Special Features

- **Pseudo-element support**: Computing styles for `::before`, `::after`, etc.
- **Media query evaluation**: Filtering stylesheets based on device characteristics
- **CSS variable resolution**: Handling custom properties (`--*`) which always inherit
- **Nested rules support**: Handling rules inside `@media`, `@supports`, etc.
- **Important flag handling**: Proper handling of the `!important` flag
- **Parent-child inheritance chain**: Computing parent styles when needed for inheritance
- **Root element handling**: Special case for elements without parents