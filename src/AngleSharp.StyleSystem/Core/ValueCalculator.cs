using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

namespace AngleSharp.StyleSystem.Core
{
    /// <summary>
    /// Calculates computed values for CSS properties based on element context and rendering device.
    /// </summary>
    public class ValueCalculator : IValueCalculator
    {
        private readonly IBrowsingContext _context;
        private readonly IRenderDevice _renderDevice;
        private readonly IVariableResolver _variableResolver;
        private readonly Dictionary<string, ICssValue> _computationCache = new Dictionary<string, ICssValue>();
        private readonly HashSet<string> _percentageDependentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "width", "height", "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "left", "right", "top", "bottom"
        };

        public ValueCalculator(IBrowsingContext context, IRenderDevice renderDevice, IVariableResolver variableResolver)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
            _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        }

        /// <summary>
        /// Computes the actual value of a CSS property based on its specified value and context.
        /// </summary>
        public ICssValue? Compute(ICssValue? value, IElement element, string propertyName)
        {
            if (value == null)
                return GetDefaultValue(propertyName);

            // Generate a stable cache key for this computation
            var cacheKey = $"{element.GetHashCode()}:{propertyName}:{value.CssText}";
            if (_computationCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            ICssValue? result;
            try
            {
                // Handle different value types
                if (value is CssVarValue varValue)
                {
                    result = ComputeVariable(varValue, element, propertyName);
                }
                else if (value is CssLengthValue lengthValue)
                {
                    result = ComputeLength(lengthValue, element, propertyName);
                }
                else if (value is CssPercentageValue percentageValue)
                {
                    result = ComputePercentage(percentageValue, element, propertyName);
                }
                else if (value is CssCalcValue calcValue)
                {
                    result = EvaluateCalc(calcValue, element, propertyName);
                }
                else if (value is CssColorValue colorValue)
                {
                    // Colors don't need computation
                    result = colorValue;
                }
                else if (IsGlobalKeyword(value))
                {
                    result = ComputeGlobalKeyword(value, element, propertyName);
                }
                else if (IsPropertySpecificKeyword(value, propertyName))
                {
                    result = ComputePropertyKeyword(value, element, propertyName);
                }
                else
                {
                    // For other value types, return as is
                    result = value;
                }
            }
            catch (Exception ex)
            {
                // In case of computational errors, return the initial value for the property
                result = GetDefaultValue(propertyName);
            }

            // Cache the result for future use
            if (result != null)
            {
                _computationCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Converts a CSS length value to pixels, taking into account the element context and property.
        /// </summary>
        public double ToPixels(CssLengthValue length, IElement element, string propertyName)
        {
            // Check for special length values
            if (length.Equals(CssLengthValue.Auto) || length.CssText == CssKeywords.None)
                return 0;

            // Handle absolute units directly
            if (length.Type == CssLengthValue.Unit.Px)
                return length.Value;

            // Get the base reference dimensions we'll need for various unit types
            var elementFontSize = GetFontSizeInPixels(element);
            var rootFontSize = GetRootFontSizeInPixels(element);
            var dpi = GetDpi();  // Default to 96dpi if not specified

            switch (length.Type)
            {
                // Absolute length units
                case CssLengthValue.Unit.In:
                    return length.Value * dpi; // 1in = 96px at 96dpi
                case CssLengthValue.Unit.Cm:
                    return length.Value * dpi / 2.54; // 1cm = 96px/2.54 ≈ 37.8px at 96dpi
                case CssLengthValue.Unit.Mm:
                    return length.Value * dpi / 25.4; // 1mm = 96px/25.4 ≈ 3.78px at 96dpi
                case CssLengthValue.Unit.Pt:
                    return length.Value * dpi / 72; // 1pt = 96px/72 ≈ 1.33px at 96dpi
                case CssLengthValue.Unit.Pc:
                    return length.Value * dpi / 6; // 1pc = 96px/6 = 16px at 96dpi

                // Font-relative length units
                case CssLengthValue.Unit.Em:
                    return length.Value * elementFontSize;
                case CssLengthValue.Unit.Rem:
                    return length.Value * rootFontSize;
                case CssLengthValue.Unit.Ex:
                    // ex is approximated as 0.5em (half the font's x-height)
                    return length.Value * elementFontSize * 0.5;
                case CssLengthValue.Unit.Ch:
                    // ch is approximated as 0.5em (width of the "0" glyph)
                    return length.Value * elementFontSize * 0.5;

                // Viewport-relative length units
                case CssLengthValue.Unit.Vw:
                    return length.Value * _renderDevice.ViewPortWidth / 100;
                case CssLengthValue.Unit.Vh:
                    return length.Value * _renderDevice.ViewPortHeight / 100;
                case CssLengthValue.Unit.Vmin:
                    return length.Value * Math.Min(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100;
                case CssLengthValue.Unit.Vmax:
                    return length.Value * Math.Max(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100;
                case CssLengthValue.Unit.Percent:
                    return ResolvePercentage(length.Value / 100, element, propertyName);

                // For unknown units, return the value directly (not ideal but safer than 0)
                default:
                    return length.Value;
            }
        }

        /// <summary>
        /// Gets the current DPI (dots per inch) from the render device.
        /// </summary>
        private double GetDpi()
        {
            // Ideally, this would come from the render device
            // Most browsers default to 96 DPI if not specified
            return _renderDevice.Resolution > 0 ? _renderDevice.Resolution : 96.0;
        }

        /// <summary>
        /// Evaluates a calc() expression to produce a computed value.
        /// </summary>
        public ICssValue? EvaluateCalc(CssCalcValue? calc, IElement element, string propertyName)
        {
            if (calc == null)
                return GetDefaultValue(propertyName);

            try
            {
                // First, resolve any CSS variables that might be in the calc expression
                var variableResolvedCalc = ResolveVariablesInCalc(calc, element, propertyName);

                // Then resolve any nested expressions
                var resolvedCalc = ResolveNestedCalcExpressions(variableResolvedCalc, element, propertyName);

                // If it's a simple length value or percentage now, convert to absolute
                if (resolvedCalc is CssLengthValue length)
                {
                    return ComputeLength(length, element, propertyName);
                }
                else if (resolvedCalc is CssPercentageValue percentage)
                {
                    return ComputePercentage(percentage, element, propertyName);
                }
                else if (resolvedCalc is CssNumberValue number)
                {
                    // For properties that expect lengths but get pure numbers, convert to px
                    if (IsLengthProperty(propertyName))
                    {
                        return new CssLengthValue(number.Value, CssLengthValue.Unit.Px);
                    }
                    return number;
                }

                // For complex expressions, evaluate numeric result in pixels
                var pixelValue = EvaluateCalcToPixels(variableResolvedCalc, element, propertyName);
                return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
            }
            catch (Exception ex)
            {
                // If calculation fails, use 0px
                return new CssLengthValue(0, CssLengthValue.Unit.Px);
            }
        }

        private CssCalcValue ResolveVariablesInCalc(CssCalcValue calc, IElement element, string propertyName)
        {
            // This would recursively traverse the calc expression tree and resolve any var() functions
            // Since we can't directly modify CssCalcValue, in a real implementation we would
            // create a new calc expression with variables resolved

            // For now, we'll just return the original calc
            // In a full implementation, this would be much more complex
            return calc;
        }

        private bool IsLengthProperty(string propertyName)
        {
            // Common CSS properties that expect length values
            return new[]
            {
                "width", "height", "min-width", "min-height", "max-width", "max-height",
                "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
                "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
                "top", "right", "bottom", "left",
                "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
                "font-size", "line-height", "text-indent", "letter-spacing", "word-spacing",
                "border-radius", "outline-width", "outline-offset",
                "column-width", "column-gap"
            }.Contains(propertyName.ToLowerInvariant());
        }

        /// <summary>
        /// Resolves relative values based on a reference value.
        /// </summary>
        public ICssValue ResolveRelative(ICssValue value, IElement element, ICssValue baseValue, string propertyName)
        {
            // Handle font-weight relative values
            if (propertyName.Equals("font-weight", StringComparison.OrdinalIgnoreCase))
            {
                var fontWeightValue = value.CssText.ToLowerInvariant();

                if (fontWeightValue == "lighter" || fontWeightValue == "bolder")
                {
                    int parentWeight = 400; // Default
                    if (baseValue is CssIntegerValue intValue)
                    {
                        parentWeight = intValue.Value;
                    }
                    else if (int.TryParse(baseValue.CssText, out var parsedWeight))
                    {
                        parentWeight = parsedWeight;
                    }

                    if (fontWeightValue == "lighter")
                    {
                        return new CssIntegerValue(GetLighterFontWeight(parentWeight));
                    }
                    else // bolder
                    {
                        return new CssIntegerValue(GetBolderFontWeight(parentWeight));
                    }
                }
            }

            // For other relative values (like em, % in font-size), they're already handled by Compute
            return value;
        }

        #region Helper Methods

        private ICssValue? ComputeLength(CssLengthValue length, IElement element, string propertyName)
        {
            // For absolute lengths or 'auto', return as is
            if (IsAbsoluteLength(length) || length.Equals(CssLengthValue.Auto))
                return length;

            // For font-relative lengths in font-size property, compute relative to parent's font size
            if (propertyName.Equals("font-size", StringComparison.OrdinalIgnoreCase) &&
                (length.Type == CssLengthValue.Unit.Em || length.Type == CssLengthValue.Unit.Rem))
            {
                var baseFontSize = length.Type == CssLengthValue.Unit.Rem
                    ? GetRootFontSizeInPixels(element)
                    : GetParentFontSizeInPixels(element);

                var computed = baseFontSize * length.Value;
                return new CssLengthValue(computed, CssLengthValue.Unit.Px);
            }

            // For other relative lengths, convert to pixels
            var pixelValue = ToPixels(length, element, propertyName);
            return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
        }

        private ICssValue? ComputePercentage(CssPercentageValue percentage, IElement element, string propertyName)
        {
            // Handle percentages for different property contexts
            // Width/height percentages are relative to containing block
            if (_percentageDependentProperties.Contains(propertyName))
            {
                var pixelValue = ResolvePercentage(percentage.Value / 100, element, propertyName);
                return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
            }

            // Font-size percentages are relative to parent font size
            if (propertyName.Equals("font-size", StringComparison.OrdinalIgnoreCase))
            {
                var parentFontSize = GetParentFontSizeInPixels(element);
                var computedSize = parentFontSize * (percentage.Value / 100);
                return new CssLengthValue(computedSize, CssLengthValue.Unit.Px);
            }

            // For properties where percentage doesn't convert to absolute, keep as percentage
            return percentage;
        }

        private ICssValue? ComputeVariable(CssVarValue varValue, IElement element, string propertyName)
        {
            // Use the variable resolver to get the actual value
            var resolvedValue = _variableResolver.ResolveVarFunction(varValue, element);

            // If resolved to null, use the initial value
            if (resolvedValue == null)
            {
                // Note: In AngleSharp, the variable fallback is likely handled in IVariableResolver.ResolveVarFunction
                // rather than exposed as a property on CssVarValue
                return GetDefaultValue(propertyName);
            }

            // Compute the resolved value in the context of this property
            return Compute(resolvedValue, element, propertyName);
        }

        private ICssValue? ComputeGlobalKeyword(ICssValue value, IElement element, string propertyName)
        {
            var keyword = value.CssText.ToLowerInvariant();

            // Handle global keywords according to CSS specifications
            switch (keyword)
            {
                case "inherit":
                    // 'inherit' takes the computed value from the parent element
                    return GetInheritedValue(element, propertyName);

                case "initial":
                    // 'initial' uses the default value defined by the CSS specification
                    return GetInitialValue(propertyName);

                case "unset":
                    // 'unset' behaves like 'inherit' for inherited properties and 'initial' for non-inherited properties
                    return IsInherited(propertyName)
                        ? GetInheritedValue(element, propertyName)
                        : GetInitialValue(propertyName);

                case "revert":
                    // 'revert' rolls back the cascade to the user-agent level
                    // This is a simplification as full implementation is complex
                    return GetUserAgentValue(propertyName);

                default:
                    return value;
            }
        }

        private ICssValue? GetInitialValue(string propertyName)
        {
            // Try to get the initial value from AngleSharp's declaration factory
            var factory = _context.GetFactory<IDeclarationFactory>();
            var declaration = factory?.Create(propertyName);

            if (declaration?.InitialValue != null)
                return declaration.InitialValue;

            // If not found, use our own defaults
            return GetDefaultValue(propertyName);
        }

        private ICssValue? GetUserAgentValue(string propertyName)
        {
            // In a real implementation, this would get the user agent's default style
            // For now, simplify by returning the initial value
            return GetInitialValue(propertyName);
        }

        private ICssValue? ComputePropertyKeyword(ICssValue value, IElement element, string propertyName)
        {
            var keyword = value.CssText.ToLowerInvariant();

            // Handle property-specific keywords
            if (propertyName.Equals("font-size", StringComparison.OrdinalIgnoreCase))
            {
                return keyword switch
                {
                    "xx-small" => new CssLengthValue(9, CssLengthValue.Unit.Px),
                    "x-small" => new CssLengthValue(10, CssLengthValue.Unit.Px),
                    "small" => new CssLengthValue(13, CssLengthValue.Unit.Px),
                    "medium" => new CssLengthValue(16, CssLengthValue.Unit.Px),
                    "large" => new CssLengthValue(18, CssLengthValue.Unit.Px),
                    "x-large" => new CssLengthValue(24, CssLengthValue.Unit.Px),
                    "xx-large" => new CssLengthValue(32, CssLengthValue.Unit.Px),
                    "xxx-large" => new CssLengthValue(48, CssLengthValue.Unit.Px),
                    "smaller" => ComputeRelativeFontSize(element, 0.8),
                    "larger" => ComputeRelativeFontSize(element, 1.2),
                    _ => value
                };
            }

            return value;
        }

        private bool IsGlobalKeyword(ICssValue value)
        {
            var keyword = value.CssText.ToLowerInvariant();
            return keyword == "inherit" || keyword == "initial" || keyword == "unset";
        }

        private bool IsPropertySpecificKeyword(ICssValue value, string propertyName)
        {
            var keyword = value.CssText.ToLowerInvariant();

            if (propertyName.Equals("font-size", StringComparison.OrdinalIgnoreCase))
            {
                return keyword == "xx-small" || keyword == "x-small" || keyword == "small" ||
                       keyword == "medium" || keyword == "large" || keyword == "x-large" ||
                       keyword == "xx-large" || keyword == "xxx-large" ||
                       keyword == "smaller" || keyword == "larger";
            }

            return false;
        }

        private double ResolvePercentage(double percentageValue, IElement element, string propertyName)
        {
            // Get the element's containing block
            var containingBlock = GetContainingBlockElement(element);
            if (containingBlock == null)
                return 0;

            // Width percentages are relative to containing block's width
            if (propertyName.Contains("width") ||
                propertyName.Contains("left") ||
                propertyName.Contains("right") ||
                propertyName.Contains("margin-left") ||
                propertyName.Contains("margin-right") ||
                propertyName.Contains("padding-left") ||
                propertyName.Contains("padding-right"))
            {
                var containerWidth = GetElementComputedWidth(containingBlock);
                return containerWidth * percentageValue;
            }

            // Height percentages are relative to containing block's height
            if (propertyName.Contains("height") ||
                propertyName.Contains("top") ||
                propertyName.Contains("bottom") ||
                propertyName.Contains("margin-top") ||
                propertyName.Contains("margin-bottom") ||
                propertyName.Contains("padding-top") ||
                propertyName.Contains("padding-bottom"))
            {
                var containerHeight = GetElementComputedHeight(containingBlock);
                return containerHeight * percentageValue;
            }

            // Default to viewport width if we can't determine the basis
            return _renderDevice.ViewPortWidth * percentageValue;
        }

        private ICssValue? GetInheritedValue(IElement element, string propertyName)
        {
            var parent = element.ParentElement;
            if (parent == null)
                return GetDefaultValue(propertyName);

            // Get the parent element's computed style
            var computedStyle = GetElementComputedStyle(parent);
            if (computedStyle == null)
                return GetDefaultValue(propertyName);

            var value = computedStyle.GetPropertyValue(propertyName);
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(propertyName);

            // Try to parse the value or use default
            var cssValue = ParseCssValue(propertyName, value);
            return cssValue ?? GetDefaultValue(propertyName);
        }

        private ICssValue? GetDefaultValue(string propertyName)
        {
            // Try to get from AngleSharp's declaration factory
            var factory = _context.GetFactory<IDeclarationFactory>();
            var declaration = factory?.Create(propertyName);

            if (declaration?.InitialValue != null)
                return declaration.InitialValue;

            // Fallback defaults for common properties
            return propertyName.ToLowerInvariant() switch
            {
                "font-size" => new CssLengthValue(16, CssLengthValue.Unit.Px),
                "width" => CssLengthValue.Auto,
                "height" => CssLengthValue.Auto,
                "color" => CssColorValue.Black,
                "background-color" => CssColorValue.Transparent,
                "margin" or "margin-top" or "margin-right" or "margin-bottom" or "margin-left" => CssLengthValue.Zero,
                "padding" or "padding-top" or "padding-right" or "padding-bottom" or "padding-left" => CssLengthValue.Zero,
                "border-width" => new CssLengthValue(0, CssLengthValue.Unit.Px),
                _ => null
            };
        }

        private bool IsInherited(string propertyName)
        {
            // Try AngleSharp's declaration factory first
            var factory = _context.GetFactory<IDeclarationFactory>();
            var declaration = factory?.Create(propertyName);

            if (declaration != null)
                return declaration.CanBeInherited;

            // Fallback for common inherited properties
            return new[]
            {
                "color", "font", "font-family", "font-size", "font-style", "font-variant",
                "font-weight", "font-stretch", "line-height", "letter-spacing",
                "text-align", "text-indent", "text-transform", "white-space", "word-spacing",
                "visibility", "cursor"
            }.Contains(propertyName.ToLowerInvariant());
        }

        private bool IsAbsoluteLength(CssLengthValue length)
        {
            return length.Type == CssLengthValue.Unit.Px ||
                   length.Type == CssLengthValue.Unit.In ||
                   length.Type == CssLengthValue.Unit.Cm ||
                   length.Type == CssLengthValue.Unit.Mm ||
                   length.Type == CssLengthValue.Unit.Pt ||
                   length.Type == CssLengthValue.Unit.Pc;
        }

        private double GetFontSizeInPixels(IElement element)
        {
            var style = GetElementComputedStyle(element);
            if (style == null)
                return 16.0;

            var fontSize = style.GetPropertyValue("font-size");
            if (string.IsNullOrEmpty(fontSize))
                return 16.0;

            // Parse the font size value
            if (double.TryParse(fontSize.Replace("px", "").Trim(), out var size))
                return size;

            return 16.0;
        }

        private double GetParentFontSizeInPixels(IElement element)
        {
            var parent = element.ParentElement;
            return parent != null ? GetFontSizeInPixels(parent) : 16.0;
        }

        private double GetRootFontSizeInPixels(IElement element)
        {
            var document = element.Owner;
            if (document?.DocumentElement == null)
                return 16.0;

            return GetFontSizeInPixels(document.DocumentElement);
        }

        private ICssValue ComputeRelativeFontSize(IElement element, double factor)
        {
            var parentSize = GetParentFontSizeInPixels(element);
            return new CssLengthValue(parentSize * factor, CssLengthValue.Unit.Px);
        }

        private IElement? GetContainingBlockElement(IElement element)
        {
            // Simplification: just use parent element as containing block
            // In a real implementation, this would consider positioning context
            return element.ParentElement;
        }

        private double GetElementComputedWidth(IElement element)
        {
            var style = GetElementComputedStyle(element);
            if (style == null)
                return _renderDevice.ViewPortWidth;

            var width = style.GetPropertyValue("width");
            if (string.IsNullOrEmpty(width) || width == "auto")
                return _renderDevice.ViewPortWidth;

            if (double.TryParse(width.Replace("px", "").Trim(), out var size))
                return size;

            return _renderDevice.ViewPortWidth;
        }

        private double GetElementComputedHeight(IElement element)
        {
            var style = GetElementComputedStyle(element);
            if (style == null)
                return _renderDevice.ViewPortHeight;

            var height = style.GetPropertyValue("height");
            if (string.IsNullOrEmpty(height) || height == "auto")
                return _renderDevice.ViewPortHeight;

            if (double.TryParse(height.Replace("px", "").Trim(), out var size))
                return size;

            return _renderDevice.ViewPortHeight;
        }

        private ICssStyleDeclaration? GetElementComputedStyle(IElement element)
        {
            // In a real implementation, this would get the element's computed style from StyleEngine
            // For now, we'll use the element's style attribute as a fallback
            var styleAttr = element.GetAttribute("style");
            if (!string.IsNullOrEmpty(styleAttr))
            {
                var parser = _context.GetService<ICssParser>();
                return parser?.ParseDeclaration(styleAttr);
            }
            return null;
        }

        private ICssValue? ParseCssValue(string propertyName, string cssText)
        {
            // Try to parse the CSS value from text
            var parser = _context.GetService<ICssParser>();
            var declaration = parser?.ParseDeclaration($"{propertyName}: {cssText}");

            if (declaration != null && declaration.Any())
            {
                var property = declaration.First();
                return property.RawValue;
            }

            return null;
        }

        private int GetLighterFontWeight(int weight)
        {
            if (weight >= 700) return 400;
            if (weight >= 600) return 400;
            if (weight >= 500) return 300;
            if (weight >= 400) return 300;
            if (weight >= 300) return 200;
            return 100;
        }

        private int GetBolderFontWeight(int weight)
        {
            if (weight >= 900) return 900;
            if (weight >= 800) return 900;
            if (weight >= 700) return 800;
            if (weight >= 600) return 700;
            if (weight >= 500) return 700;
            if (weight >= 400) return 700;
            if (weight >= 300) return 400;
            if (weight >= 200) return 300;
            if (weight >= 100) return 200;
            return 400;
        }

        private ICssValue? ResolveNestedCalcExpressions(CssCalcValue calc, IElement element, string propertyName)
        {
            // Recursively resolve the calc expression tree
            if (calc.Expression == null)
                return calc;

            ICssValue? resolvedExpression = null;

            if (calc.Expression is CssCalcAddExpression add)
            {
                resolvedExpression = ResolveCalcOperation(add.Left, add.Right, true, element, propertyName);
            }
            else if (calc.Expression is CssCalcSubExpression sub)
            {
                resolvedExpression = ResolveCalcOperation(sub.Left, sub.Right, false, element, propertyName);
            }
            else if (calc.Expression is CssCalcMulExpression mul)
            {
                resolvedExpression = ResolveCalcMultiplication(mul.Left, mul.Right, element, propertyName);
            }
            else if (calc.Expression is CssCalcDivExpression div)
            {
                resolvedExpression = ResolveCalcDivision(div.Left, div.Right, element, propertyName);
            }
            else if (calc.Expression is CssCalcBracketExpression bracket)
            {
                // Handle bracketed expressions by recursively resolving the inner expression
                var innerCalc = new CssCalcValue(bracket.Value);
                resolvedExpression = ResolveNestedCalcExpressions(innerCalc, element, propertyName);
            }
            else if (calc.Expression is CssLengthValue length)
            {
                resolvedExpression = ComputeLength(length, element, propertyName);
            }
            else if (calc.Expression is CssPercentageValue percentage)
            {
                resolvedExpression = ComputePercentage(percentage, element, propertyName);
            }
            else
            {
                // For other expression types, leave as is
                resolvedExpression = calc.Expression;
            }

            return resolvedExpression;
        }

        private ICssValue? ResolveCalcOperation(ICssValue? left, ICssValue? right, bool isAddition, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            // Compute values for both sides
            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

            if (leftValue is CssLengthValue leftLength && rightValue is CssLengthValue rightLength)
            {
                // Convert both to pixels for calculation
                var leftPx = ToPixels(leftLength, element, propertyName);
                var rightPx = ToPixels(rightLength, element, propertyName);

                // Perform the operation
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssPercentageValue leftPercentage && rightValue is CssPercentageValue rightPercentage)
            {
                // For percentages, we can add/subtract the values directly
                var resultPercentage = isAddition
                    ? leftPercentage.Value + rightPercentage.Value
                    : leftPercentage.Value - rightPercentage.Value;
                return new CssPercentageValue(resultPercentage);
            }
            else if (leftValue is CssPercentageValue percentage && rightValue is CssLengthValue length)
            {
                // For mixed percentage and length, convert both to pixels based on context
                var leftPx = ResolvePercentage(percentage.Value / 100, element, propertyName);
                var rightPx = ToPixels(length, element, propertyName);
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssLengthValue length2 && rightValue is CssPercentageValue percentage2)
            {
                // For mixed length and percentage, convert both to pixels based on context
                var leftPx = ToPixels(length2, element, propertyName);
                var rightPx = ResolvePercentage(percentage2.Value / 100, element, propertyName);
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }

            // If we couldn't resolve the operation, return a fallback value
            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ResolveCalcMultiplication(ICssValue? left, ICssValue? right, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            // Compute values for both sides
            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

            // One operand must be a number, the other a length or percentage
            if (leftValue is CssNumberValue leftNumber && rightValue is CssLengthValue rightLength)
            {
                var rightPx = ToPixels(rightLength, element, propertyName);
                var resultPx = leftNumber.Value * rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssLengthValue leftLength && rightValue is CssNumberValue rightNumber)
            {
                var leftPx = ToPixels(leftLength, element, propertyName);
                var resultPx = leftPx * rightNumber.Value;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssNumberValue leftNumber2 && rightValue is CssPercentageValue rightPercentage)
            {
                var resultPercentage = leftNumber2.Value * rightPercentage.Value;
                return new CssPercentageValue(resultPercentage);
            }
            else if (leftValue is CssPercentageValue leftPercentage && rightValue is CssNumberValue rightNumber2)
            {
                var resultPercentage = leftPercentage.Value * rightNumber2.Value;
                return new CssPercentageValue(resultPercentage);
            }

            // If we couldn't resolve the multiplication, return a fallback value
            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ResolveCalcDivision(ICssValue? left, ICssValue? right, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            // Compute values for both sides
            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

            // The right operand must be a number
            if (rightValue is CssNumberValue rightNumber)
            {
                // Avoid division by zero
                if (Math.Abs(rightNumber.Value) < 0.0001)
                    return new CssLengthValue(0, CssLengthValue.Unit.Px);

                if (leftValue is CssLengthValue leftLength)
                {
                    var leftPx = ToPixels(leftLength, element, propertyName);
                    var resultPx = leftPx / rightNumber.Value;
                    return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
                }
                else if (leftValue is CssPercentageValue leftPercentage)
                {
                    var resultPercentage = leftPercentage.Value / rightNumber.Value;
                    return new CssPercentageValue(resultPercentage);
                }
                else if (leftValue is CssNumberValue leftNumber)
                {
                    // Number divided by number gives a number
                    var result = leftNumber.Value / rightNumber.Value;
                    return new CssNumberValue(result);
                }
            }

            // If we couldn't resolve the division, return a fallback value
            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ComputeCalcOperand(ICssValue operand, IElement element, string propertyName)
        {
            // Handle nested calc expressions
            if (operand is CssCalcValue nestedCalc)
            {
                return ResolveNestedCalcExpressions(nestedCalc, element, propertyName);
            }

            // For simple values, compute them directly
            if (operand is CssLengthValue length)
            {
                return ComputeLength(length, element, propertyName);
            }
            else if (operand is CssPercentageValue percentage)
            {
                return ComputePercentage(percentage, element, propertyName);
            }
            else if (operand is CssNumberValue number)
            {
                return number;
            }

            // For other calc expressions, handle recursively
            if (operand is CssCalcAddExpression add)
            {
                return ResolveCalcOperation(add.Left, add.Right, true, element, propertyName);
            }
            else if (operand is CssCalcSubExpression sub)
            {
                return ResolveCalcOperation(sub.Left, sub.Right, false, element, propertyName);
            }
            else if (operand is CssCalcMulExpression mul)
            {
                return ResolveCalcMultiplication(mul.Left, mul.Right, element, propertyName);
            }
            else if (operand is CssCalcDivExpression div)
            {
                return ResolveCalcDivision(div.Left, div.Right, element, propertyName);
            }
            else if (operand is CssCalcBracketExpression bracket)
            {
                var innerCalc = new CssCalcValue(bracket.Value);
                return ResolveNestedCalcExpressions(innerCalc, element, propertyName);
            }

            return operand;
        }

        private double EvaluateCalcToPixels(CssCalcValue calc, IElement element, string propertyName)
        {
            var resolvedValue = ResolveNestedCalcExpressions(calc, element, propertyName);

            if (resolvedValue is CssLengthValue lengthValue)
            {
                return ToPixels(lengthValue, element, propertyName);
            }
            else if (resolvedValue is CssPercentageValue percentageValue)
            {
                return ResolvePercentage(percentageValue.Value / 100, element, propertyName);
            }

            // If we couldn't evaluate to pixels, return 0
            return 0.0;
        }

        public void ClearCache()
        {
            _computationCache.Clear();
        }

        #endregion
    }
}