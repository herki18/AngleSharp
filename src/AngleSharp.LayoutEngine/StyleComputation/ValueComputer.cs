namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Collections.Generic;
using Css;
using Css.Dom;
using Css.Values;
using Dom;

/// <summary>
/// Computes final CSS values by resolving relative units and handling special values.
/// </summary>
public class ValueComputer
{
    private readonly IRenderDevice _device;
    private readonly IBrowsingContext _context;

    /// <summary>
    /// Creates a new ValueComputer.
    /// </summary>
    /// <param name="device">The render device used for viewport-relative units.</param>
    /// <param name="context">The browsing context for CSS operations.</param>
    public ValueComputer(IRenderDevice device, IBrowsingContext context)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Computes absolute values for all properties in the provided style declaration.
    /// </summary>
    /// <param name="declaration">The cascaded and inherited style declaration.</param>
    /// <param name="element">The element being styled.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <param name="rootStyle">The root element's computed style.</param>
    /// <returns>A new style declaration with computed values.</returns>
    public ICssStyleDeclaration ComputeValues(
        ICssStyleDeclaration declaration,
        IElement element,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle)
    {
        if (declaration == null)
            throw new ArgumentNullException(nameof(declaration));
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (parentStyle == null)
            throw new ArgumentNullException(nameof(parentStyle));
        if (rootStyle == null)
            throw new ArgumentNullException(nameof(rootStyle));

        // Step 1: Create a new declaration to hold computed values
        var computedStyle = new CssStyleDeclaration(_context);

        // Step 2: Calculate root and parent font sizes
        var rootFontSize = ExtractFontSizeInPixels(rootStyle);
        var parentFontSize = ExtractFontSizeInPixels(parentStyle);

        // Step 3: Compute element's font-size first as other properties may depend on it
        var elementFontSize = ComputeFontSize(declaration, parentFontSize, rootFontSize);

        // Step 4: Create a compute context with all needed information
        var computeContext = CreateComputeContext(declaration, element, elementFontSize, rootFontSize, parentStyle, rootStyle);

        // Step 5: Process all properties
        var computedProperties = ComputeAllProperties(
            declaration,
            element,
            elementFontSize,
            rootFontSize,
            parentStyle,
            computeContext);

        // Step 6: Apply the computed properties to our result
        computedStyle.SetDeclarations(computedProperties);

        return computedStyle;
    }

    /// <summary>
    /// Extracts the font size from a style declaration in pixels.
    /// </summary>
    private double ExtractFontSizeInPixels(ICssStyleDeclaration style)
    {
        const double defaultFontSize = 16.0;

        try {
            var fontSizeValue = style.GetPropertyValue(PropertyNames.FontSize);
            if (!string.IsNullOrEmpty(fontSizeValue) && fontSizeValue.EndsWith("px"))
            {
                if (double.TryParse(fontSizeValue.Substring(0, fontSizeValue.Length - 2),
                    out var fontSize))
                {
                    return fontSize;
                }
            }

            // Try accessing the raw value
            var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
            if (fontSizeProperty?.RawValue is CssLengthValue lengthValue &&
                lengthValue.Type == CssLengthValue.Unit.Px)
            {
                return lengthValue.Value;
            }
        }
        catch (Exception)
        {
            // Fall back to default in case of any error
        }

        return defaultFontSize;
    }

    /// <summary>
    /// Computes the font-size property for an element, which needs special handling.
    /// </summary>
    private double ComputeFontSize(ICssStyleDeclaration style, double parentFontSize, double rootFontSize)
    {
        try
        {
            // Get the font-size property
            var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
            if (fontSizeProperty == null || fontSizeProperty.RawValue == null)
                return parentFontSize; // Inherit from parent if not specified

            var value = fontSizeProperty.RawValue;

            // Handle absolute size keywords
            if (value is CssConstantValue<CssLengthValue> constValue)
            {
                return constValue.CssText switch
                {
                    CssKeywords.XxSmall => 9.0 / 16.0 * rootFontSize,
                    CssKeywords.XSmall => 10.0 / 16.0 * rootFontSize,
                    CssKeywords.Small => 13.0 / 16.0 * rootFontSize,
                    CssKeywords.Medium => rootFontSize,
                    CssKeywords.Large => 18.0 / 16.0 * rootFontSize,
                    CssKeywords.XLarge => 24.0 / 16.0 * rootFontSize,
                    CssKeywords.XxLarge => 32.0 / 16.0 * rootFontSize,
                    CssKeywords.XxxLarge => 48.0 / 16.0 * rootFontSize,

                    // Relative keywords
                    CssKeywords.Smaller => parentFontSize / 1.2,
                    CssKeywords.Larger => parentFontSize * 1.2,

                    // Special values
                    CssKeywords.Inherit => parentFontSize,
                    CssKeywords.Initial => rootFontSize,
                    CssKeywords.Unset => parentFontSize, // font-size is inheritable

                    _ => rootFontSize // Default fallback
                };
            }

            // Handle length values
            if (value is CssLengthValue lengthValue)
            {
                return lengthValue.Type switch
                {
                    // Absolute units
                    CssLengthValue.Unit.Px => lengthValue.Value,
                    // Convert other absolute units using device DPI
                    CssLengthValue.Unit.Pt => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.In => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.Cm => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.Mm => lengthValue.ToPixel(_device),

                    // Relative units
                    CssLengthValue.Unit.Em => lengthValue.Value * parentFontSize,
                    CssLengthValue.Unit.Rem => lengthValue.Value * rootFontSize,
                    CssLengthValue.Unit.Ex => lengthValue.Value * parentFontSize * 0.5, // Approximation

                    // Viewport relative units
                    CssLengthValue.Unit.Vh => lengthValue.Value * _device.ViewPortHeight / 100.0,
                    CssLengthValue.Unit.Vw => lengthValue.Value * _device.ViewPortWidth / 100.0,
                    CssLengthValue.Unit.Vmin => lengthValue.Value * Math.Min(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0,
                    CssLengthValue.Unit.Vmax => lengthValue.Value * Math.Max(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0,

                    // Percentage
                    CssLengthValue.Unit.Percent => lengthValue.Value * parentFontSize / 100.0,

                    _ => parentFontSize // Default fallback
                };
            }

            // Handle fallback case
            return parentFontSize;
        }
        catch (Exception)
        {
            // If computation fails, return the parent font size
            return parentFontSize;
        }
    }

    /// <summary>
    /// Creates a computation context for resolving CSS values.
    /// </summary>
    private ComputationContext CreateComputeContext(
        ICssStyleDeclaration style,
        IElement element,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle)
    {
        return new ComputationContext(
            _device,
            _context,
            fontSize,
            rootFontSize,
            style,
            parentStyle,
            rootStyle);
    }

    /// <summary>
    /// Computes values for all properties in the style declaration.
    /// </summary>
    private IEnumerable<ICssProperty> ComputeAllProperties(
        ICssStyleDeclaration style,
        IElement element,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration parentStyle,
        ComputationContext context)
    {
        List<ICssProperty> computedProperties = new List<ICssProperty>();

        // First, compute the font-size property to add to the list
        var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
        if (fontSizeProperty != null)
        {
            var fontSizeValue = CreatePixelLengthValue(fontSize);
            var computedFontSize = CreateComputedProperty(
                PropertyNames.FontSize,
                fontSizeValue,
                fontSizeProperty.IsImportant);

            computedProperties.Add(computedFontSize);
        }

        // Process all other properties
        foreach (var property in style)
        {
            // Skip font-size as we've already handled it
            if (property.Name == PropertyNames.FontSize)
                continue;

            // Skip properties without values
            if (property.RawValue == null)
                continue;

            try
            {
                // Process the property value
                ICssValue? computedValue = ComputePropertyValue(
                    property.Name,
                    property.RawValue,
                    fontSize,
                    rootFontSize,
                    context);

                // Create a new property with the computed value
                if (computedValue != null)
                {
                    var computedProperty = CreateComputedProperty(
                        property.Name,
                        computedValue,
                        property.IsImportant);

                    computedProperties.Add(computedProperty);
                }
                else
                {
                    // If we couldn't compute the value, add the original property
                    computedProperties.Add(property);
                }
            }
            catch (Exception)
            {
                // If computation fails, add the original property
                computedProperties.Add(property);
            }
        }

        return computedProperties;
    }

    /// <summary>
    /// Computes the value for a specific CSS property.
    /// </summary>
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
            var resolvedValue = context.ResolveVariable(varValue.Name);
            if (resolvedValue != null)
            {
                return ComputePropertyValue(propertyName, resolvedValue, fontSize, rootFontSize, context);
            }
            return value; // Keep as is if can't resolve
        }

        // Handle calc() expressions
        if (value is CssCalcValue calcValue)
        {
            // We would evaluate the calc expression here
            // This is a complex topic - for now we'll keep it as is
            return value;
        }

        // Handle special values
        if (value is ICssSpecialValue specialValue)
        {
            switch (specialValue.CssText)
            {
                case CssKeywords.Initial:
                    // Would return the initial value for this property
                    return value;
                case CssKeywords.Inherit:
                    // Would use the parent value
                    return context.GetInheritedValue(propertyName);
                case CssKeywords.Unset:
                    // Inherit if inheritable, initial otherwise
                    if (IsInheritable(propertyName))
                        return context.GetInheritedValue(propertyName);
                    return value;
            }
        }

        // Handle length values
        if (value is CssLengthValue lengthValue)
        {
            // Convert relative units to absolute pixels
            return ConvertLengthToPixels(lengthValue, propertyName, fontSize, rootFontSize);
        }

        // For other value types (colors, etc.), let AngleSharp handle it
        // through its own computation mechanism if possible
        if (value is ICssValue cssValue && cssValue.Compute != null)
        {
            try
            {
                return cssValue.Compute(context);
            }
            catch
            {
                // If computation fails, return the original value
                return value;
            }
        }

        // Keep other values as they are
        return value;
    }

    /// <summary>
    /// Converts a CSS length value to pixels based on context.
    /// </summary>
    private ICssValue ConvertLengthToPixels(
        CssLengthValue lengthValue,
        string propertyName,
        double fontSize,
        double rootFontSize)
    {
        // If already in pixels, return as is
        if (lengthValue.Type == CssLengthValue.Unit.Px)
            return lengthValue;

        double pixelValue;

        switch (lengthValue.Type)
        {
            // Absolute units
            case CssLengthValue.Unit.Pt:
            case CssLengthValue.Unit.In:
            case CssLengthValue.Unit.Cm:
            case CssLengthValue.Unit.Mm:
            case CssLengthValue.Unit.Pc:
                // Use device to convert absolute units to pixels
                pixelValue = lengthValue.ToPixel(_device);
                break;

            // Font-relative units
            case CssLengthValue.Unit.Em:
                pixelValue = lengthValue.Value * fontSize;
                break;
            case CssLengthValue.Unit.Rem:
                pixelValue = lengthValue.Value * rootFontSize;
                break;
            case CssLengthValue.Unit.Ex:
                // Approximate ex as 0.5em
                pixelValue = lengthValue.Value * fontSize * 0.5;
                break;
            case CssLengthValue.Unit.Ch:
                // Approximate ch as 0.5em
                pixelValue = lengthValue.Value * fontSize * 0.5;
                break;

            // Viewport-relative units
            case CssLengthValue.Unit.Vh:
                pixelValue = lengthValue.Value * _device.ViewPortHeight / 100.0;
                break;
            case CssLengthValue.Unit.Vw:
                pixelValue = lengthValue.Value * _device.ViewPortWidth / 100.0;
                break;
            case CssLengthValue.Unit.Vmin:
                pixelValue = lengthValue.Value * Math.Min(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0;
                break;
            case CssLengthValue.Unit.Vmax:
                pixelValue = lengthValue.Value * Math.Max(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0;
                break;

            // Percentage values - context dependent
            case CssLengthValue.Unit.Percent:
                pixelValue = HandlePercentageValue(lengthValue.Value, propertyName, fontSize);
                break;

            // For other or unknown units, keep the original value
            default:
                return lengthValue;
        }

        // Create and return a new length value in pixels
        return CreatePixelLengthValue(pixelValue);
    }

    /// <summary>
    /// Handles percentage values based on property context.
    /// </summary>
    private double HandlePercentageValue(double percentValue, string propertyName, double fontSize)
    {
        // Convert percentage to decimal
        double fraction = percentValue / 100.0;

        // Different properties use percentages differently
        switch (propertyName)
        {
            // Font-relative properties
            case PropertyNames.FontSize:
            case PropertyNames.LineHeight:
            case PropertyNames.VerticalAlign:
                return fraction * fontSize;

            // Width/height would be relative to containing block width/height
            // For simplicity, we'll return the raw percentage for these
            case PropertyNames.Width:
            case PropertyNames.Height:
            case PropertyNames.MinWidth:
            case PropertyNames.MinHeight:
            case PropertyNames.MaxWidth:
            case PropertyNames.MaxHeight:
            case PropertyNames.Margin:
            case PropertyNames.MarginLeft:
            case PropertyNames.MarginRight:
            case PropertyNames.MarginTop:
            case PropertyNames.MarginBottom:
            case PropertyNames.Padding:
            case PropertyNames.PaddingLeft:
            case PropertyNames.PaddingRight:
            case PropertyNames.PaddingTop:
            case PropertyNames.PaddingBottom:
                // These should ideally be computed based on containing block
                // For now, leave as percentage
                return percentValue;

            // Default to font-size-relative
            default:
                return fraction * fontSize;
        }
    }

    /// <summary>
    /// Creates a CssLengthValue with a pixel unit.
    /// </summary>
    private CssLengthValue CreatePixelLengthValue(double pixelValue)
    {
        return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
    }

    /// <summary>
    /// Creates a computed CSS property with the given value.
    /// </summary>
    private ICssProperty CreateComputedProperty(
        string name,
        ICssValue value,
        bool important)
    {
        // Create a new property using the context factory
        var property = _context.CreateProperty(name);
        property.RawValue = value;
        property.IsImportant = important;
        return property;
    }

    /// <summary>
    /// Determines if a CSS property is inheritable by default.
    /// </summary>
    private bool IsInheritable(string propertyName)
    {
        // This list is not exhaustive - would need to be expanded
        HashSet<string> inheritableProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PropertyNames.Color,
            PropertyNames.FontFamily,
            PropertyNames.FontSize,
            PropertyNames.FontStyle,
            PropertyNames.FontWeight,
            PropertyNames.LineHeight,
            PropertyNames.ListStyle,
            PropertyNames.ListStyleImage,
            PropertyNames.ListStylePosition,
            PropertyNames.ListStyleType,
            PropertyNames.TextAlign,
            PropertyNames.TextIndent,
            PropertyNames.TextTransform,
            PropertyNames.Visibility,
            PropertyNames.WhiteSpace,
            PropertyNames.WordSpacing
        };

        return inheritableProperties.Contains(propertyName);
    }

    /// <summary>
    /// Context class for CSS value computation.
    /// </summary>
    private class ComputationContext : ICssComputeContext
    {
        private readonly IRenderDevice _device;
        private readonly IBrowsingContext _context;
        private readonly double _fontSize;
        private readonly double _rootFontSize;
        private readonly ICssStyleDeclaration _style;
        private readonly ICssStyleDeclaration _parentStyle;
        private readonly ICssStyleDeclaration _rootStyle;

        public ComputationContext(
            IRenderDevice device,
            IBrowsingContext context,
            double fontSize,
            double rootFontSize,
            ICssStyleDeclaration style,
            ICssStyleDeclaration parentStyle,
            ICssStyleDeclaration rootStyle)
        {
            _device = device;
            _context = context;
            _fontSize = fontSize;
            _rootFontSize = rootFontSize;
            _style = style;
            _parentStyle = parentStyle;
            _rootStyle = rootStyle;
        }

        public IRenderDevice Device => _device;

        public IBrowsingContext Context => _context;

        public IValueConverter? Converter => null;

        public ICssValue? Resolve(string name)
        {
            // If it's a CSS variable, resolve it
            if (name.StartsWith("--"))
            {
                return ResolveVariable(name);
            }

            // For other property references
            var property = _style?.GetProperty(name);
            return property?.RawValue;
        }

        /// <summary>
        /// Resolves a CSS variable by name.
        /// </summary>
        public ICssValue? ResolveVariable(string name)
        {
            // Check current style first
            var variable = _style?.GetProperty(name);
            if (variable?.RawValue != null)
                return variable.RawValue;

            // Check parent style if variable not found
            if (_parentStyle != null)
            {
                variable = _parentStyle.GetProperty(name);
                if (variable?.RawValue != null)
                    return variable.RawValue;
            }

            // Check root style if different from parent
            if (_rootStyle != null && !ReferenceEquals(_parentStyle, _rootStyle))
            {
                variable = _rootStyle.GetProperty(name);
                if (variable?.RawValue != null)
                    return variable.RawValue;
            }

            return null;
        }

        /// <summary>
        /// Gets the inherited value for a property from parent style.
        /// </summary>
        public ICssValue? GetInheritedValue(string propertyName)
        {
            if (_parentStyle == null)
                return null;

            var property = _parentStyle.GetProperty(propertyName);
            return property?.RawValue;
        }
    }
}