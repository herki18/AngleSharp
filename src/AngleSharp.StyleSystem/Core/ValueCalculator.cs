using System;
using System.Collections.Generic;
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
        /// Converts a CSS length value to pixels, taking into account the element context.
        /// </summary>
        public double ToPixels(CssLengthValue length, IElement element, string propertyName)
        {
            // Handle absolute units directly
            if (length.Type == CssLengthValue.Unit.Px)
                return length.Value;

            switch (length.Type)
            {
                case CssLengthValue.Unit.In:
                    return length.Value * 96; // 1in = 96px
                case CssLengthValue.Unit.Cm:
                    return length.Value * 37.8; // 1cm = 37.8px
                case CssLengthValue.Unit.Mm:
                    return length.Value * 3.78; // 1mm = 3.78px
                case CssLengthValue.Unit.Pt:
                    return length.Value * 1.33; // 1pt = 1.33px
                case CssLengthValue.Unit.Pc:
                    return length.Value * 16; // 1pc = 16px

                // Relative to font size
                case CssLengthValue.Unit.Em:
                    return length.Value * GetFontSizeInPixels(element);
                case CssLengthValue.Unit.Rem:
                    return length.Value * GetRootFontSizeInPixels(element);
                case CssLengthValue.Unit.Ex:
                    return length.Value * GetFontSizeInPixels(element) * 0.5; // Approximation: 1ex ≈ 0.5em
                case CssLengthValue.Unit.Ch:
                    return length.Value * GetFontSizeInPixels(element) * 0.5; // Approximation: 1ch ≈ 0.5em

                // Viewport-relative units
                case CssLengthValue.Unit.Vw:
                    return length.Value * _renderDevice.ViewPortWidth / 100;
                case CssLengthValue.Unit.Vh:
                    return length.Value * _renderDevice.ViewPortHeight / 100;
                case CssLengthValue.Unit.Vmin:
                    return length.Value * Math.Min(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100;
                case CssLengthValue.Unit.Vmax:
                    return length.Value * Math.Max(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100;

                // Percentage (requires context)
                case CssLengthValue.Unit.Percent:
                    return ResolvePercentage(length.Value / 100, element, propertyName);

                default:
                    return length.Value; // For unknown units, return as is
            }
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
                // First, try to resolve any nested expressions
                var resolvedCalc = ResolveNestedCalcExpressions(calc, element, propertyName);

                // If it's a simple length value or percentage now, convert to absolute
                if (resolvedCalc is CssLengthValue length)
                {
                    return ComputeLength(length, element, propertyName);
                }
                else if (resolvedCalc is CssPercentageValue percentage)
                {
                    return ComputePercentage(percentage, element, propertyName);
                }

                // For complex expressions, evaluate numeric result in pixels
                var pixelValue = EvaluateCalcToPixels(calc, element, propertyName);
                return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
            }
            catch
            {
                // If calculation fails, use 0px
                return new CssLengthValue(0, CssLengthValue.Unit.Px);
            }
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

            // If resolved to null, use the fallback or initial value
            if (resolvedValue == null)
            {
                if (varValue.Fallback != null)
                {
                    return Compute(varValue.Fallback, element, propertyName);
                }
                return GetDefaultValue(propertyName);
            }

            // Compute the resolved value in the context of this property
            return Compute(resolvedValue, element, propertyName);
        }

        private ICssValue? ComputeGlobalKeyword(ICssValue value, IElement element, string propertyName)
        {
            var keyword = value.CssText.ToLowerInvariant();

            // Handle global keywords
            if (keyword == "inherit")
            {
                return GetInheritedValue(element, propertyName);
            }
            else if (keyword == "initial")
            {
                return GetDefaultValue(propertyName);
            }
            else if (keyword == "unset")
            {
                return IsInherited(propertyName)
                    ? GetInheritedValue(element, propertyName)
                    : GetDefaultValue(propertyName);
            }

            return value;
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
            // This would recursively resolve nested calc expressions
            // In a real implementation, this would handle CssCalcAddExpression, CssCalcSubExpression, etc.
            return calc;
        }

        private double EvaluateCalcToPixels(CssCalcValue calc, IElement element, string propertyName)
        {
            // In a real implementation, this would evaluate the calc expression to a pixel value
            // It would handle operations on different units after converting to a common unit

            // For now, let's provide a simplified implementation that returns 0
            return 0.0;
        }

        public void ClearCache()
        {
            _computationCache.Clear();
        }

        #endregion
    }
}