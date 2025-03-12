# Implementation Plan for CSS Logical Properties

## Overview

This plan outlines the implementation of CSS logical properties in AngleSharp.LayoutEngine's StyleSystem. Logical properties allow developers to specify layout in terms of flow-relative directions (block/inline) instead of physical directions (top/right/bottom/left), making them essential for internationalized layouts.

## Phase 1: Declaration Definitions

### Step 1: Add Property Name Constants

Add constants to `PropertyNames.cs` for all logical properties:

```csharp
// Border logical properties
public const string BorderBlock = "border-block";
public const string BorderBlockStart = "border-block-start";
public const string BorderBlockEnd = "border-block-end";
public const string BorderBlockWidth = "border-block-width";
public const string BorderBlockStartWidth = "border-block-start-width";
public const string BorderBlockEndWidth = "border-block-end-width";
// Continue with all other properties...
```

### Step 2: Create Declaration Classes

Create declaration classes for each logical property following AngleSharp's pattern:

Example for BorderBlockDeclaration.cs:

```csharp
namespace AngleSharp.Css.Declarations
{
    using System;
    using Dom;
    using static ValueConverters;
    
    static class BorderBlockDeclaration
    {
        public static String Name = PropertyNames.BorderBlock;
        public static IValueConverter Converter = WithBorderSide(
            InitialValues.BorderBlockStartWidthDecl,
            InitialValues.BorderBlockStartStyleDecl,
            InitialValues.BorderBlockStartColorDecl);
        public static ICssValue InitialValue = null;
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderBlockWidth,
            PropertyNames.BorderBlockStyle,
            PropertyNames.BorderBlockColor,
        };
    }
}
```

### Step 3: Define Initial Values

Add initial values for all logical properties in InitialValues.cs:

```csharp
// Border logical properties
public static readonly ICssValue BorderBlockStartWidthDecl = CssLengthValue.Medium; // Same as BorderTopWidthDecl
public static readonly ICssValue BorderBlockEndWidthDecl = CssLengthValue.Medium;
public static readonly ICssValue BorderInlineStartWidthDecl = CssLengthValue.Medium;
public static readonly ICssValue BorderInlineEndWidthDecl = CssLengthValue.Medium;
// Continue with all other properties...
```

### Step 4: Update Value Converters

Ensure all necessary value converters for logical properties are defined:

```csharp
// For BorderStartStartRadius, etc.
public static readonly IValueConverter LogicalBorderRadiusConverter = BorderRadiusLonghandConverter;

// For BlockSize, InlineSize
public static readonly IValueConverter BlockSizeConverter = AutoLengthOrPercentConverter;
```


Based on the implementation so far, we've covered the main logical properties for borders, insets, and sizing. However, there are several other CSS logical properties we should implement:

## Margin Logical Properties

1. `margin-block`
2. `margin-block-start`
3. `margin-block-end`
4. `margin-inline`
5. `margin-inline-start`
6. `margin-inline-end`

## Padding Logical Properties

1. `padding-block`
2. `padding-block-start`
3. `padding-block-end`
4. `padding-inline`
5. `padding-inline-start`
6. `padding-inline-end`

## Logical Border Radius Properties

1. `border-start-start-radius`
2. `border-start-end-radius`
3. `border-end-start-radius`
4. `border-end-end-radius`

## Text-related Logical Properties

1. `text-align` with logical values (`start`, `end`)

## Overflow Logical Properties

1. `overflow-block`
2. `overflow-inline`

## Logical Float and Clear Properties

1. `float` with logical values (`inline-start`, `inline-end`)
2. `clear` with logical values (`inline-start`, `inline-end`)

## Other Logical Properties

1. `resize` with logical values (`block`, `inline`)
2. `overscroll-behavior-block`
3. `overscroll-behavior-inline`

Let me know which of these you'd like me to implement next. I notice that we've already created the implementations for `margin-block`, `margin-inline`, `padding-block`, and `padding-inline` (as I can see these in your provided file list), but we might need to implement the individual properties.