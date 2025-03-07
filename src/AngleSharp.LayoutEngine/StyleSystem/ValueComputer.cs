namespace AngleSharp.LayoutEngine.StyleSystem;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Computes final CSS property values by resolving relative units,
/// special values, and CSS variables.
/// </summary>
public class ValueComputer
{
    private readonly IRenderDevice _device;
    private readonly IBrowsingContext _context;
    private readonly IDeclarationFactory _factory;
    private readonly VariableRegistry _variableRegistry;
    private readonly VariableResolver _variableResolver;
    private const string LogPrefix = "[ValueComputer] ";

    /// <summary>
    /// Creates a new value computer for CSS property computation.
    /// </summary>
    /// <param name="device">The render device providing dimensions and other context.</param>
    /// <param name="context">The browsing context.</param>
    public ValueComputer(IRenderDevice device, IBrowsingContext context)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _factory = context.GetService<IDeclarationFactory>() ?? throw new ArgumentNullException(nameof(context));
        _variableRegistry = new VariableRegistry();
        _variableResolver = new VariableResolver(_variableRegistry, context, device);
        Console.WriteLine($"{LogPrefix}Initialized with device {device.GetType().Name} and context {context.GetType().Name}");
    }

    /// <summary>
    /// Computes the final CSS values for a declaration.
    /// </summary>
    /// <param name="declaration">The CSS style declaration to compute values for.</param>
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
        Console.WriteLine($"{LogPrefix}Computing values for element {element.NodeName}#{element.Id ?? ""}");

        if (declaration == null)
            throw new ArgumentNullException(nameof(declaration));
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var computedStyle = new CssStyleDeclaration(_context);
        var rootFontSize = ExtractFontSizeInPixels(rootStyle);
        var parentFontSize = ExtractFontSizeInPixels(parentStyle);

        Console.WriteLine($"{LogPrefix}Root font size: {rootFontSize}px, Parent font size: {parentFontSize}px");

        var elementFontSize = ComputeFontSize(declaration, parentFontSize, rootFontSize);
        Console.WriteLine($"{LogPrefix}Computed element font size: {elementFontSize}px");

        var resolverContext = new ResolverContext();

        // Extract and register variables
        Console.WriteLine($"{LogPrefix}Extracting and registering CSS variables");
        ExtractAndRegisterVariables(declaration, element);

        // Log all registered variables
        var variables = _variableRegistry.GetVariableNames().ToList();
        Console.WriteLine($"{LogPrefix}Registered {variables.Count} variables: {string.Join(", ", variables)}");

        // Create the computation context
        Console.WriteLine($"{LogPrefix}Creating computation context");
        var computeContext = CreateComputationContext(
            declaration,
            element,
            elementFontSize,
            rootFontSize,
            parentStyle,
            rootStyle,
            resolverContext);

        // Compute all properties
        Console.WriteLine($"{LogPrefix}Computing properties for declaration with {declaration.Length} properties");
        var computedProperties = ComputeAllProperties(
            declaration,
            element,
            elementFontSize,
            rootFontSize,
            parentStyle,
            computeContext,
            resolverContext);

        Console.WriteLine($"{LogPrefix}Setting {computedProperties.Count()} computed properties to result");
        computedStyle.SetDeclarations(computedProperties);

        // Log summary of computed properties
        Console.WriteLine($"{LogPrefix}Computed style now has {computedStyle.Length} properties");
        return computedStyle;
    }

    private double ExtractFontSizeInPixels(ICssStyleDeclaration style)
    {
        const double defaultFontSize = 16.0;
        Console.WriteLine($"{LogPrefix}Extracting font size from style");

        try {
            var fontSizeValue = style.GetPropertyValue(PropertyNames.FontSize);
            Console.WriteLine($"{LogPrefix}Font size value string: '{fontSizeValue}'");

            if (!string.IsNullOrEmpty(fontSizeValue) && fontSizeValue.EndsWith("px"))
            {
                if (double.TryParse(fontSizeValue.Substring(0, fontSizeValue.Length - 2),
                    out var fontSize))
                {
                    Console.WriteLine($"{LogPrefix}Parsed pixel font size: {fontSize}px");
                    return fontSize;
                }
                Console.WriteLine($"{LogPrefix}Failed to parse pixel font size from '{fontSizeValue}'");
            }

            var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
            if (fontSizeProperty?.RawValue is CssLengthValue lengthValue &&
                lengthValue.Type == CssLengthValue.Unit.Px)
            {
                Console.WriteLine($"{LogPrefix}Found CssLengthValue font size: {lengthValue.Value}px");
                return lengthValue.Value;
            }
            Console.WriteLine($"{LogPrefix}No direct pixel font size found, using default");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{LogPrefix}Error extracting font size: {ex.Message}");
        }

        Console.WriteLine($"{LogPrefix}Using default font size: {defaultFontSize}px");
        return defaultFontSize;
    }

    private double ComputeFontSize(ICssStyleDeclaration style, double parentFontSize, double rootFontSize)
    {
        Console.WriteLine($"{LogPrefix}Computing font size");

        try
        {
            var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
            if (fontSizeProperty == null || fontSizeProperty.RawValue == null)
            {
                Console.WriteLine($"{LogPrefix}No font-size property found, using parent font size: {parentFontSize}px");
                return parentFontSize;
            }

            var value = fontSizeProperty.RawValue;
            Console.WriteLine($"{LogPrefix}Font size raw value type: {value.GetType().Name}, value: {value.CssText}");

            if (value is CssConstantValue<CssLengthValue> constValue)
            {
                var result = constValue.CssText switch
                {
                    CssKeywords.XxSmall => 9.0 / 16.0 * rootFontSize,
                    CssKeywords.XSmall => 10.0 / 16.0 * rootFontSize,
                    CssKeywords.Small => 13.0 / 16.0 * rootFontSize,
                    CssKeywords.Medium => rootFontSize,
                    CssKeywords.Large => 18.0 / 16.0 * rootFontSize,
                    CssKeywords.XLarge => 24.0 / 16.0 * rootFontSize,
                    CssKeywords.XxLarge => 32.0 / 16.0 * rootFontSize,
                    CssKeywords.XxxLarge => 48.0 / 16.0 * rootFontSize,
                    CssKeywords.Smaller => parentFontSize / 1.2,
                    CssKeywords.Larger => parentFontSize * 1.2,
                    CssKeywords.Inherit => parentFontSize,
                    CssKeywords.Initial => rootFontSize,
                    CssKeywords.Unset => parentFontSize,
                    _ => rootFontSize
                };
                Console.WriteLine($"{LogPrefix}Computed font size from keyword '{constValue.CssText}': {result}px");
                return result;
            }

            if (value is CssLengthValue lengthValue)
            {
                var result = lengthValue.Type switch
                {
                    CssLengthValue.Unit.Px => lengthValue.Value,
                    CssLengthValue.Unit.Pt => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.In => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.Cm => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.Mm => lengthValue.ToPixel(_device),
                    CssLengthValue.Unit.Em => lengthValue.Value * parentFontSize,
                    CssLengthValue.Unit.Rem => lengthValue.Value * rootFontSize,
                    CssLengthValue.Unit.Ex => lengthValue.Value * parentFontSize * 0.5,
                    CssLengthValue.Unit.Vh => lengthValue.Value * _device.ViewPortHeight / 100.0,
                    CssLengthValue.Unit.Vw => lengthValue.Value * _device.ViewPortWidth / 100.0,
                    CssLengthValue.Unit.Vmin => lengthValue.Value * Math.Min(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0,
                    CssLengthValue.Unit.Vmax => lengthValue.Value * Math.Max(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0,
                    CssLengthValue.Unit.Percent => lengthValue.Value * parentFontSize / 100.0,
                    _ => parentFontSize
                };
                Console.WriteLine($"{LogPrefix}Computed font size from {lengthValue.Type}: {result}px");
                return result;
            }

            Console.WriteLine($"{LogPrefix}Unhandled font size value type, using parent font size: {parentFontSize}px");
            return parentFontSize;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{LogPrefix}Error computing font size: {ex.Message}");
            Console.WriteLine($"{LogPrefix}Using parent font size: {parentFontSize}px");
            return parentFontSize;
        }
    }

    private void ExtractAndRegisterVariables(ICssStyleDeclaration declaration, IElement element)
    {
        Console.WriteLine($"{LogPrefix}Extracting variables from declaration");
        int count = 0;

        foreach (var property in declaration)
        {
            if (property.Name.StartsWith("--") && property.RawValue != null)
            {
                var specificity = GetSpecificityForElement(element, property.Name);
                Console.WriteLine($"{LogPrefix}Registering variable {property.Name} = {property.RawValue.CssText} (important: {property.IsImportant})");

                _variableRegistry.RegisterVariable(
                    property.Name,
                    property.RawValue,
                    StylesheetOrigin.Author,
                    specificity,
                    property.IsImportant);
                count++;
            }
        }

        Console.WriteLine($"{LogPrefix}Registered {count} variables from declaration");
    }

    private Priority GetSpecificityForElement(IElement element, string propertyName)
    {
        // This is a simplification - in a real implementation, we would calculate
        // the specificity based on the selector that set the variable
        return new Priority(0, 0, 0, 1);
    }

    private CssComputationContext CreateComputationContext(
        ICssStyleDeclaration style,
        IElement element,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle,
        ResolverContext resolverContext)
    {
        Console.WriteLine($"{LogPrefix}Creating CssComputationContext");

        return new CssComputationContext(
            _device,
            _context,
            fontSize,
            rootFontSize,
            style,
            parentStyle,
            rootStyle,
            element,
            _variableRegistry,
            resolverContext);
    }

    private IEnumerable<ICssProperty> ComputeAllProperties(
        ICssStyleDeclaration style,
        IElement element,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration parentStyle,
        CssComputationContext context,
        ResolverContext resolverContext)
    {
        Console.WriteLine($"{LogPrefix}Computing all properties (total: {style.Length})");
        List<ICssProperty> computedProperties = new List<ICssProperty>();

        // Handle font-size first as other em-based properties depend on it
        var fontSizeProperty = style.GetProperty(PropertyNames.FontSize);
        if (fontSizeProperty != null)
        {
            var fontSizeValue = CreatePixelLengthValue(fontSize);
            Console.WriteLine($"{LogPrefix}Creating computed font-size property: {fontSizeValue.CssText}");

            var computedFontSize = CreateComputedProperty(
                PropertyNames.FontSize,
                fontSizeValue,
                fontSizeProperty.IsImportant);

            computedProperties.Add(computedFontSize);
            Console.WriteLine($"{LogPrefix}Added computed font-size property");
        }

        // Process all other properties
        foreach (var property in style)
        {
            // Skip CSS variables and font-size (already handled)
            if (property.Name.StartsWith("--"))
            {
                Console.WriteLine($"{LogPrefix}Skipping CSS variable: {property.Name}");
                continue;
            }

            if (property.Name == PropertyNames.FontSize)
            {
                Console.WriteLine($"{LogPrefix}Skipping font-size (already processed)");
                continue;
            }

            if (property.RawValue == null)
            {
                Console.WriteLine($"{LogPrefix}Skipping property with null value: {property.Name}");
                continue;
            }

            Console.WriteLine($"{LogPrefix}Computing property: {property.Name} = {property.Value} ({property.RawValue.GetType().Name})");

            try
            {
                ICssValue? computedValue = ComputePropertyValue(
                    property.Name,
                    property.RawValue,
                    fontSize,
                    rootFontSize,
                    context,
                    resolverContext);

                if (computedValue != null)
                {
                    Console.WriteLine($"{LogPrefix}Property {property.Name} computed to: {computedValue.CssText}");

                    var computedProperty = CreateComputedProperty(
                        property.Name,
                        computedValue,
                        property.IsImportant);

                    computedProperties.Add(computedProperty);
                    Console.WriteLine($"{LogPrefix}Added computed property: {property.Name}");
                }
                else
                {
                    Console.WriteLine($"{LogPrefix}Property {property.Name} computed to null, keeping original");
                    computedProperties.Add(property);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}ERROR computing property {property.Name}: {ex.Message}");
                Console.WriteLine($"{LogPrefix}Stack trace: {ex.StackTrace}");
                Console.WriteLine($"{LogPrefix}Keeping original property: {property.Name}");
                computedProperties.Add(property);
            }
        }

        Console.WriteLine($"{LogPrefix}Computed {computedProperties.Count} properties");
        return computedProperties;
    }

    private ICssValue? ComputePropertyValue(
        string propertyName,
        ICssValue value,
        double fontSize,
        double rootFontSize,
        CssComputationContext context,
        ResolverContext resolverContext)
    {
        if (resolverContext.CurrentDepth >= ResolverContext.MaxResolutionDepth)
        {
            Console.WriteLine($"{LogPrefix}Maximum variable resolution depth reached for {propertyName}");
            return value;
        }

        Console.WriteLine($"{LogPrefix}Computing value for property {propertyName}, value: {value.CssText} ({value.GetType().Name})");

        try
        {
            if (value is CssVarValue varValue)
            {
                Console.WriteLine($"{LogPrefix}Property {propertyName} has var() reference: {varValue.VariableName}");
                var resolvedValue = _variableResolver.SafeResolveVariable(varValue, context.Element, resolverContext);

                if (resolvedValue != null)
                {
                    Console.WriteLine($"{LogPrefix}Var reference {varValue.VariableName} resolved to: {resolvedValue.CssText}");

                    if (!ReferenceEquals(resolvedValue, value))
                    {
                        Console.WriteLine($"{LogPrefix}Recursively computing resolved value");
                        return ComputePropertyValue(propertyName, resolvedValue, fontSize, rootFontSize, context, resolverContext);
                    }
                }
                else
                {
                    Console.WriteLine($"{LogPrefix}Var reference {varValue.VariableName} resolved to null");
                }

                return resolvedValue ?? value;
            }

            if (value is CssCalcValue calcValue)
            {
                Console.WriteLine($"{LogPrefix}Property {propertyName} has calc() expression");
                var resolvedCalc = _variableResolver.ResolveCalcExpression(calcValue, context.Element, resolverContext);

                if (resolvedCalc is not CssCalcValue && !ReferenceEquals(resolvedCalc, value))
                {
                    Console.WriteLine($"{LogPrefix}Calc expression resolved to non-calc value: {resolvedCalc.CssText}");
                    return ComputePropertyValue(propertyName, resolvedCalc, fontSize, rootFontSize, context, resolverContext);
                }

                Console.WriteLine($"{LogPrefix}Evaluating calc expression: {resolvedCalc.CssText}");
                var calculator = new CalcExpressionEvaluator(context.Element, fontSize, rootFontSize, _device, context);

                if (resolvedCalc is CssCalcValue cssCalcValue)
                {
                    var result = calculator.EvaluateCalc(cssCalcValue);
                    Console.WriteLine($"{LogPrefix}Calc expression evaluated to: {result.CssText}");
                    return result;
                }

                Console.WriteLine($"{LogPrefix}Using resolved calc value: {resolvedCalc.CssText}");
                return resolvedCalc;
            }

            // Special property-specific handling
            switch (propertyName)
            {
                case PropertyNames.LineHeight:
                    if (value is CssNumberValue numberValue)
                    {
                        var result = CreatePixelLengthValue(numberValue.Value * fontSize);
                        Console.WriteLine($"{LogPrefix}Unitless line-height {numberValue.Value} computed to: {result.CssText}");
                        return result;
                    }
                    break;

                case PropertyNames.FontWeight:
                    if (value is CssNumberValue fontWeightValue)
                    {
                        Console.WriteLine($"{LogPrefix}Font weight kept as is: {fontWeightValue.CssText}");
                        return value;
                    }
                    break;

                case PropertyNames.LetterSpacing:
                case PropertyNames.WordSpacing:
                    // Special handling could be added here
                    break;
            }

            // Handle special values (initial, inherit, etc.)
            if (value is ICssSpecialValue specialValue)
            {
                Console.WriteLine($"{LogPrefix}Processing special value: {specialValue.CssText}");

                switch (specialValue.CssText)
                {
                    case CssKeywords.Initial:
                        Console.WriteLine($"{LogPrefix}Getting initial value for {propertyName}");
                        var declarationInfo = _factory.Create(propertyName);
                        var initialValue = declarationInfo.InitialValue;
                        Console.WriteLine($"{LogPrefix}Initial value for {propertyName}: {initialValue?.CssText ?? "null"}");
                        return initialValue;

                    case CssKeywords.Inherit:
                        Console.WriteLine($"{LogPrefix}Getting inherited value for {propertyName}");
                        var inheritedValue = context.GetInheritedValue(propertyName);
                        Console.WriteLine($"{LogPrefix}Inherited value for {propertyName}: {inheritedValue?.CssText ?? "null"}");
                        return inheritedValue;

                    case CssKeywords.Unset:
                        if (IsInheritable(propertyName))
                        {
                            Console.WriteLine($"{LogPrefix}Property {propertyName} is inheritable, getting inherited value for 'unset'");
                            var unsetValue = context.GetInheritedValue(propertyName);
                            Console.WriteLine($"{LogPrefix}Unset value for {propertyName}: {unsetValue?.CssText ?? "null"}");
                            return unsetValue;
                        }
                        Console.WriteLine($"{LogPrefix}Property {propertyName} is not inheritable, 'unset' behaves like 'initial'");
                        return value; // Will be handled by initial value in real cases
                }
            }

            // Handle length values
            if (value is CssLengthValue lengthValue)
            {
                Console.WriteLine($"{LogPrefix}Converting length value {lengthValue.CssText} to pixels");
                var result = ConvertLengthToPixels(lengthValue, propertyName, fontSize, rootFontSize);
                Console.WriteLine($"{LogPrefix}Length value converted to: {result.CssText}");
                return result;
            }

            // Handle multiple values (e.g., border: 1px solid black)
            if (value is ICssMultipleValue multiValue)
            {
                Console.WriteLine($"{LogPrefix}Processing multiple value with {multiValue.Count} items");
                var resolvedItems = new List<ICssValue>();

                for (var i = 0; i < multiValue.Count; i++)
                {
                    var item = multiValue[i];
                    Console.WriteLine($"{LogPrefix}Computing item {i}: {item.CssText}");

                    var resolvedItem = ComputePropertyValue(propertyName, item, fontSize, rootFontSize, context, resolverContext);

                    if (resolvedItem != null)
                    {
                        Console.WriteLine($"{LogPrefix}Item {i} computed to: {resolvedItem.CssText}");
                        resolvedItems.Add(resolvedItem);
                    }
                    else
                    {
                        Console.WriteLine($"{LogPrefix}Item {i} computed to null, keeping original");
                        resolvedItems.Add(item);
                    }
                }

                var result = new CssListValue(resolvedItems.ToArray());
                Console.WriteLine($"{LogPrefix}Multiple value computed to: {result.CssText}");
                return result;
            }

            // Use ICssValue's own computation method if available
            if (value is ICssValue cssValue && cssValue.Compute != null)
            {
                try
                {
                    Console.WriteLine($"{LogPrefix}Using value's own Compute method");
                    var result = cssValue.Compute(context);
                    Console.WriteLine($"{LogPrefix}Value computed itself to: {result?.CssText ?? "null"}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{LogPrefix}Error using value's own Compute method: {ex.Message}");
                    Console.WriteLine($"{LogPrefix}Keeping original value");
                    return value;
                }
            }

            Console.WriteLine($"{LogPrefix}No special computation needed for {propertyName}, keeping original: {value.CssText}");
            return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{LogPrefix}ERROR computing value for {propertyName}: {ex.Message}");
            Console.WriteLine($"{LogPrefix}Stack trace: {ex.StackTrace}");
            Console.WriteLine($"{LogPrefix}Keeping original value: {value.CssText}");
            return value;
        }
    }

    private ICssValue ConvertLengthToPixels(
        CssLengthValue lengthValue,
        string propertyName,
        double fontSize,
        double rootFontSize)
    {
        Console.WriteLine($"{LogPrefix}Converting {lengthValue.Type} value {lengthValue.Value} to pixels");

        if (lengthValue.Type == CssLengthValue.Unit.Px)
        {
            Console.WriteLine($"{LogPrefix}Already in pixels: {lengthValue.Value}px");
            return lengthValue;
        }

        double pixelValue;

        switch (lengthValue.Type)
        {
            case CssLengthValue.Unit.Pt:
            case CssLengthValue.Unit.In:
            case CssLengthValue.Unit.Cm:
            case CssLengthValue.Unit.Mm:
            case CssLengthValue.Unit.Pc:
                pixelValue = lengthValue.ToPixel(_device);
                Console.WriteLine($"{LogPrefix}Absolute unit {lengthValue.Type} converted to {pixelValue}px");
                break;

            case CssLengthValue.Unit.Em:
                pixelValue = lengthValue.Value * fontSize;
                Console.WriteLine($"{LogPrefix}em value {lengthValue.Value} converted to {pixelValue}px using font size {fontSize}px");
                break;

            case CssLengthValue.Unit.Rem:
                pixelValue = lengthValue.Value * rootFontSize;
                Console.WriteLine($"{LogPrefix}rem value {lengthValue.Value} converted to {pixelValue}px using root font size {rootFontSize}px");
                break;

            case CssLengthValue.Unit.Ex:
                pixelValue = lengthValue.Value * fontSize * 0.5;
                Console.WriteLine($"{LogPrefix}ex value {lengthValue.Value} converted to {pixelValue}px using font size {fontSize}px * 0.5");
                break;

            case CssLengthValue.Unit.Ch:
                pixelValue = lengthValue.Value * fontSize * 0.5;
                Console.WriteLine($"{LogPrefix}ch value {lengthValue.Value} converted to {pixelValue}px using font size {fontSize}px * 0.5");
                break;

            case CssLengthValue.Unit.Vh:
                pixelValue = lengthValue.Value * _device.ViewPortHeight / 100.0;
                Console.WriteLine($"{LogPrefix}vh value {lengthValue.Value} converted to {pixelValue}px using viewport height {_device.ViewPortHeight}");
                break;

            case CssLengthValue.Unit.Vw:
                pixelValue = lengthValue.Value * _device.ViewPortWidth / 100.0;
                Console.WriteLine($"{LogPrefix}vw value {lengthValue.Value} converted to {pixelValue}px using viewport width {_device.ViewPortWidth}");
                break;

            case CssLengthValue.Unit.Vmin:
                pixelValue = lengthValue.Value * Math.Min(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0;
                Console.WriteLine($"{LogPrefix}vmin value {lengthValue.Value} converted to {pixelValue}px");
                break;

            case CssLengthValue.Unit.Vmax:
                pixelValue = lengthValue.Value * Math.Max(_device.ViewPortHeight, _device.ViewPortWidth) / 100.0;
                Console.WriteLine($"{LogPrefix}vmax value {lengthValue.Value} converted to {pixelValue}px");
                break;

            case CssLengthValue.Unit.Percent:
                pixelValue = HandlePercentageValue(lengthValue.Value, propertyName, fontSize);
                Console.WriteLine($"{LogPrefix}Percentage value {lengthValue.Value}% converted to {pixelValue}px");
                break;

            default:
                Console.WriteLine($"{LogPrefix}Unhandled unit type {lengthValue.Type}, keeping original");
                return lengthValue;
        }

        var result = CreatePixelLengthValue(pixelValue);
        Console.WriteLine($"{LogPrefix}Final converted value: {result.CssText}");
        return result;
    }

    private double HandlePercentageValue(double percentValue, string propertyName, double fontSize)
    {
        Console.WriteLine($"{LogPrefix}Handling percentage value {percentValue}% for property {propertyName}");

        double fraction = percentValue / 100.0;

        switch (propertyName)
        {
            case PropertyNames.FontSize:
            case PropertyNames.LineHeight:
            case PropertyNames.VerticalAlign:
                var result = fraction * fontSize;
                Console.WriteLine($"{LogPrefix}Font-relative percentage: {percentValue}% = {result}px");
                return result;

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
                Console.WriteLine($"{LogPrefix}Box model percentage: keeping as {percentValue}%");
                return percentValue; // Keep as percentage for layout properties

            default:
                var defaultResult = fraction * fontSize;
                Console.WriteLine($"{LogPrefix}Default percentage handling: {percentValue}% = {defaultResult}px");
                return defaultResult;
        }
    }

    private CssLengthValue CreatePixelLengthValue(double pixelValue)
    {
        var result = new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
        Console.WriteLine($"{LogPrefix}Created pixel length value: {result.CssText}");
        return result;
    }

    private ICssProperty CreateComputedProperty(
        string name,
        ICssValue value,
        bool important)
    {
        Console.WriteLine($"{LogPrefix}Creating computed property: {name} = {value.CssText}" + (important ? " !important" : ""));

        var property = _context.CreateProperty(name);
        property.RawValue = value;
        property.IsImportant = important;
        return property;
    }

    private bool IsInheritable(string propertyName)
    {
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

        var result = inheritableProperties.Contains(propertyName);
        Console.WriteLine($"{LogPrefix}Property {propertyName} is{(result ? "" : " not")} inheritable");
        return result;
    }
}