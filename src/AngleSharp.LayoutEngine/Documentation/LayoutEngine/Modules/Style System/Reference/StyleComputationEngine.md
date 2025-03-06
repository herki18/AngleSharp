# StyleEngine: Input/Output Interface Guide

## Quick Integration Reference

This guide shows how to use the StyleEngine API in the simplest terms - what to provide as input and what you'll receive as output.

## Basic Usage

```csharp
// INPUTS:
// - AngleSharp document containing HTML elements
// - Optional render device settings

// OUTPUTS:
// - ICssStyleDeclaration containing computed CSS properties for elements

// Initialize AngleSharp and load a document
var context = BrowsingContext.New(Configuration.Default.WithCss());
var document = await context.OpenAsync(req => req.Content("<html><body><div id='target'>Test</div></body></html>"));

// Create the engine
var engine = new StyleEngine(context: context, document: document);

// Get an element to compute styles for
var element = document.GetElementById("target");

// Compute the element's style - this is the main API call
ICssStyleDeclaration computedStyle = engine.ComputeElementStyle(element);

// Access computed property values
string color = computedStyle.GetPropertyValue("color");
string fontSize = computedStyle.GetPropertyValue("font-size");
string display = computedStyle.GetPropertyValue("display");
```

## Input Parameters

### Constructor Parameters

```csharp
// All parameters are optional - shown with defaults
var engine = new StyleEngine(
    renderDevice: null,  // Provides screen dimensions and media capabilities
    context: null,       // AngleSharp browsing context
    document: null,      // Document containing elements to style
    cacheManager: null   // Style calculation cache
);
```

### ComputeElementStyle Method Parameters

```csharp
ICssStyleDeclaration style = engine.ComputeElementStyle(
    element,             // Required: The element to compute styles for
    parentStyle: null,   // Optional: Pre-computed parent style
    pseudoElement: null  // Optional: Pseudo-element like "::before", "::after"
);
```

## Output Format

The output is an `ICssStyleDeclaration` with these key methods:

```csharp
// Get a specific CSS property value
string value = computedStyle.GetPropertyValue("property-name");

// Check if a property has the !important flag
string priority = computedStyle.GetPropertyPriority("property-name");

// Enumerate all properties
foreach (var property in computedStyle)
{
    string name = property.Name;
    string value = property.Value;
    bool isImportant = property.IsImportant;
}
```

## Common Integration Patterns

### Basic HTML Renderer

```csharp
// Input: HTML document and engine
// Output: Rendering commands with styles applied

void RenderDocument(IDocument document, StyleEngine engine)
{
    // Process all elements in the document
    foreach (var element in document.QuerySelectorAll("*"))
    {
        // Get computed style
        var style = engine.ComputeElementStyle(element);

        // Extract needed CSS properties
        var display = style.GetPropertyValue("display");
        var color = style.GetPropertyValue("color");
        var backgroundColor = style.GetPropertyValue("background-color");
        var width = style.GetPropertyValue("width");
        var height = style.GetPropertyValue("height");

        // Apply to your rendering system
        RenderElement(element, display, color, backgroundColor, width, height);
    }
}
```

### Responsive Layout System

```csharp
// Input: Element, container dimensions
// Output: Positioned element with computed dimensions

LayoutBox CalculateLayout(IElement element, double containerWidth, double containerHeight)
{
    // Create device with specific dimensions
    var device = new MockRenderDevice {
        ViewPortWidth = (int)containerWidth,
        ViewPortHeight = (int)containerHeight
    };

    // Create engine with this device
    var engine = new StyleEngine(renderDevice: device);

    // Get computed style
    var style = engine.ComputeElementStyle(element);

    // Extract layout-related properties
    return new LayoutBox {
        Width = ParseLength(style.GetPropertyValue("width")),
        Height = ParseLength(style.GetPropertyValue("height")),
        MarginTop = ParseLength(style.GetPropertyValue("margin-top")),
        // etc.
    };
}
```

### Document Processor

```csharp
// Input: HTML content
// Output: Processed text with styles applied

string ProcessDocument(string html)
{
    // Parse HTML
    var context = BrowsingContext.New(Configuration.Default.WithCss());
    var document = context.OpenAsync(req => req.Content(html)).Result;

    // Create engine
    var engine = new StyleEngine(context: context, document: document);

    // Process each text-containing element
    var result = new StringBuilder();
    foreach (var element in document.QuerySelectorAll("p, h1, h2, h3, li"))
    {
        // Get computed style
        var style = engine.ComputeElementStyle(element);

        // Apply styling to text
        var text = element.TextContent;
        var fontWeight = style.GetPropertyValue("font-weight");
        var fontStyle = style.GetPropertyValue("font-style");

        // Format based on computed styles
        text = FormatText(text, fontWeight, fontStyle);
        result.AppendLine(text);
    }

    return result.ToString();
}
```

## Summary

### Inputs

- **IElement**: DOM element to compute styles for
- **IRenderDevice** (optional): Screen/device information for units and media queries
- **IBrowsingContext**: AngleSharp context for document processing
- **IDocument**: HTML document containing elements
- **string**: Optional pseudo-element selector

### Outputs

- **ICssStyleDeclaration**: Object containing all computed CSS properties
    - Property names are standard CSS kebab-case (e.g., "font-size")
    - Values are computed to their final form (e.g., "16px", "rgba(255, 0, 0, 1)")
    - Units are normalized according to CSS specification
    - Relative values are resolved to absolute values where possible