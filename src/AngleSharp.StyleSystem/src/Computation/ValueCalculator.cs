namespace AngleSharp.StyleSystem.Computation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Parser;
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;
    using AngleSharp.StyleSystem.Interfaces;
    using AngleSharp.StyleSystem.Properties;

    /// <summary>
    /// Calculates and resolves CSS property values by computing absolute values,
    /// handling units, and evaluating expressions like calc().
    /// </summary>
    public class ValueCalculator : IValueCalculator
    {
        #region Fields

        private readonly IDeclarationFactory _declarationFactory;
        private readonly ICssParser _cssParser;
        private readonly IRenderDevice _renderDevice;
        private readonly Dictionary<string, ICssValue> _computationCache = new Dictionary<string, ICssValue>();
        private readonly HashSet<string> _percentageDependentProperties;

        #endregion

        #region Constructor

        public ValueCalculator(IDeclarationFactory declarationFactory, ICssParser cssParser, IRenderDevice renderDevice)
        {
            _declarationFactory = declarationFactory;
            _cssParser = cssParser;
            _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));

            // Initialize percentage-dependent properties with PropertyNames constants
            _percentageDependentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PropertyNames.Width, PropertyNames.Height,
                PropertyNames.Margin, PropertyNames.MarginTop, PropertyNames.MarginRight, PropertyNames.MarginBottom, PropertyNames.MarginLeft,
                PropertyNames.Padding, PropertyNames.PaddingTop, PropertyNames.PaddingRight, PropertyNames.PaddingBottom, PropertyNames.PaddingLeft,
                PropertyNames.Top, PropertyNames.Right, PropertyNames.Bottom, PropertyNames.Left
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Computes the absolute value of a CSS property for an element.
        /// </summary>
        public ICssValue? Compute(ICssValue? value, IElement element, string propertyName)
        {
            if (value == null)
                return GetDefaultValue(propertyName);

            var cacheKey = $"{element.GetHashCode()}:{propertyName}:{value.CssText}";
            if (_computationCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            try
            {
                ICssValue? result = value switch
                {
                    CssLengthValue lengthValue => ComputeLength(lengthValue, element, propertyName),
                    CssPercentageValue percentageValue => ComputePercentage(percentageValue, element, propertyName),
                    CssCalcValue calcValue => EvaluateCalc(calcValue, element, propertyName),
                    CssColorValue colorValue => colorValue,
                    _ => ProcessSpecialValue(value, element, propertyName)
                };

                if (result != null)
                {
                    _computationCache[cacheKey] = result;
                }
                return result;
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error computing value for {propertyName}: {ex.Message}");
                return GetDefaultValue(propertyName);
            }
        }

        /// <summary>
        /// Converts a length value to pixels.
        /// </summary>
        public double ToPixels(CssLengthValue length, IElement element, string propertyName)
        {
            if (length.Equals(CssLengthValue.Auto) || length.CssText == CssKeywords.None)
                return 0;

            if (length.Type == CssLengthValue.Unit.Px)
                return length.Value;

            var elementFontSize = GetFontSizeInPixels(element);
            var rootFontSize = GetRootFontSizeInPixels(element);
            var dpi = GetDpi();

            return length.Type switch
            {
                CssLengthValue.Unit.In => length.Value * dpi,
                CssLengthValue.Unit.Cm => length.Value * dpi / 2.54,
                CssLengthValue.Unit.Mm => length.Value * dpi / 25.4,
                CssLengthValue.Unit.Pt => length.Value * dpi / 72,
                CssLengthValue.Unit.Pc => length.Value * dpi / 6,
                CssLengthValue.Unit.Em => length.Value * elementFontSize,
                CssLengthValue.Unit.Rem => length.Value * rootFontSize,
                CssLengthValue.Unit.Ex => length.Value * elementFontSize * 0.5,
                CssLengthValue.Unit.Ch => length.Value * elementFontSize * 0.5,
                CssLengthValue.Unit.Vw => length.Value * _renderDevice.ViewPortWidth / 100,
                CssLengthValue.Unit.Vh => length.Value * _renderDevice.ViewPortHeight / 100,
                CssLengthValue.Unit.Vmin => length.Value * Math.Min(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100,
                CssLengthValue.Unit.Vmax => length.Value * Math.Max(_renderDevice.ViewPortWidth, _renderDevice.ViewPortHeight) / 100,
                CssLengthValue.Unit.Percent => ResolvePercentage(length.Value / 100, element, propertyName),
                _ => length.Value
            };
        }

        /// <summary>
        /// Evaluates a calc() expression to determine its computed value.
        /// </summary>
        public ICssValue? EvaluateCalc(CssCalcValue? calc, IElement element, string propertyName)
        {
            if (calc == null)
                return GetDefaultValue(propertyName);

            try
            {
                if (calc.Expression == null)
                {
                    return HandleSimpleCalcExpression(calc, element, propertyName);
                }

                var resolvedCalc = ResolveNestedCalcExpressions(calc, element, propertyName);

                if (resolvedCalc is CssLengthValue length)
                {
                    return ComputeLength(length, element, propertyName);
                }
                else if (resolvedCalc is CssPercentageValue percentage)
                {
                    return ComputePercentage(percentage, element, propertyName);
                }
                else if (resolvedCalc is CssNumberValue number && IsLengthProperty(propertyName))
                {
                    return new CssLengthValue(number.Value, CssLengthValue.Unit.Px);
                }
                else if (resolvedCalc is CssNumberValue)
                {
                    return resolvedCalc;
                }

                var pixelValue = EvaluateCalcToPixels(calc, element, propertyName);
                return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating calc() expression: {ex.Message}");
                return new CssLengthValue(0, CssLengthValue.Unit.Px);
            }
        }

        /// <summary>
        /// Resolves a relative value against a base value.
        /// </summary>
        public ICssValue ResolveRelative(ICssValue value, IElement element, ICssValue baseValue, string propertyName)
        {
            // Handle percentage-to-length conversion
            if (value is CssPercentageValue percentageValue && baseValue is CssLengthValue baseLengthValue)
            {
                var basePixels = ToPixels(baseLengthValue, element, propertyName);
                var computedValue = basePixels * (percentageValue.Value / 100);
                return new CssLengthValue(computedValue, CssLengthValue.Unit.Px);
            }

            // Handle font-weight relative keywords
            if (propertyName.Equals(PropertyNames.FontWeight, StringComparison.OrdinalIgnoreCase))
            {
                var fontWeightValue = value.CssText.ToLowerInvariant();
                if (fontWeightValue == "lighter" || fontWeightValue == "bolder")
                {
                    int parentWeight = GetParentFontWeight(baseValue);

                    return new CssIntegerValue(
                        fontWeightValue == "lighter"
                            ? GetLighterFontWeight(parentWeight)
                            : GetBolderFontWeight(parentWeight));
                }
            }

            return value;
        }

        /// <summary>
        /// Clears the computation cache to force recalculation of values.
        /// </summary>
        public void ClearCache()
        {
            _computationCache.Clear();
        }

        #endregion

        #region Private Methods - Value Processing

        private ICssValue? ProcessSpecialValue(ICssValue value, IElement element, string propertyName)
        {
            if (IsGlobalKeyword(value))
            {
                return ComputeGlobalKeyword(value, element, propertyName);
            }
            else if (IsPropertySpecificKeyword(value, propertyName))
            {
                return ComputePropertyKeyword(value, element, propertyName);
            }

            return value;
        }

        private ICssValue? ComputeLength(CssLengthValue length, IElement element, string propertyName)
        {
            if (IsAbsoluteLength(length) || length.Equals(CssLengthValue.Auto))
                return length;

            if (propertyName.Equals(PropertyNames.FontSize, StringComparison.OrdinalIgnoreCase) &&
                (length.Type == CssLengthValue.Unit.Em || length.Type == CssLengthValue.Unit.Rem))
            {
                var baseFontSize = length.Type == CssLengthValue.Unit.Rem
                    ? GetRootFontSizeInPixels(element)
                    : GetParentFontSizeInPixels(element);
                var computed = baseFontSize * length.Value;
                return new CssLengthValue(computed, CssLengthValue.Unit.Px);
            }

            var pixelValue = ToPixels(length, element, propertyName);
            return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
        }

        private ICssValue? ComputePercentage(CssPercentageValue percentage, IElement element, string propertyName)
        {
            if (_percentageDependentProperties.Contains(propertyName))
            {
                var pixelValue = ResolvePercentage(percentage.Value / 100, element, propertyName);
                return new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
            }

            if (propertyName.Equals(PropertyNames.FontSize, StringComparison.OrdinalIgnoreCase))
            {
                var parentFontSize = GetParentFontSizeInPixels(element);
                var computedSize = parentFontSize * (percentage.Value / 100);
                return new CssLengthValue(computedSize, CssLengthValue.Unit.Px);
            }

            return percentage;
        }

        private ICssValue? ComputeGlobalKeyword(ICssValue value, IElement element, string propertyName)
        {
            var keyword = value.CssText.ToLowerInvariant();

            return keyword switch
            {
                "inherit" => GetInheritedValue(element, propertyName),
                "initial" => GetInitialValue(propertyName),
                "unset" => IsInherited(propertyName)
                    ? GetInheritedValue(element, propertyName)
                    : GetInitialValue(propertyName),
                "revert" => GetUserAgentValue(propertyName),
                _ => value
            };
        }

        private ICssValue? ComputePropertyKeyword(ICssValue value, IElement element, string propertyName)
        {
            if (!propertyName.Equals(PropertyNames.FontSize, StringComparison.OrdinalIgnoreCase))
                return value;

            var keyword = value.CssText.ToLowerInvariant();

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

        #endregion

        #region Private Methods - Calc Expression Evaluation

        private ICssValue? HandleSimpleCalcExpression(CssCalcValue calc, IElement element, string propertyName)
        {
            var cssText = calc.CssText;
            if (cssText.StartsWith("calc(") && cssText.EndsWith(")"))
            {
                var expression = cssText.Substring(5, cssText.Length - 6).Trim();

                // Try to handle simple addition
                if (expression.Contains("+"))
                {
                    return HandleSimpleOperation(expression, '+', element, propertyName);
                }
                // Try to handle simple subtraction
                else if (expression.Contains("-"))
                {
                    return HandleSimpleOperation(expression, '-', element, propertyName);
                }
            }

            // Default value for unparseable expressions
            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? HandleSimpleOperation(string expression, char operation, IElement element, string propertyName)
        {
            var parts = expression.Split(operation);
            if (parts.Length == 2 &&
                TryParseLengthValue(parts[0].Trim(), out var left) &&
                TryParseLengthValue(parts[1].Trim(), out var right))
            {
                var leftPx = ToPixels(left, element, propertyName);
                var rightPx = ToPixels(right, element, propertyName);

                var result = operation == '+' ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(result, CssLengthValue.Unit.Px);
            }

            return null;
        }

        private ICssValue? ResolveNestedCalcExpressions(CssCalcValue calc, IElement element, string propertyName)
        {
            if (calc.Expression == null)
                return calc;

            if (calc.Expression is CssCalcAddExpression add)
            {
                return ResolveCalcOperation(add.Left, add.Right, true, element, propertyName);
            }
            else if (calc.Expression is CssCalcSubExpression sub)
            {
                return ResolveCalcOperation(sub.Left, sub.Right, false, element, propertyName);
            }
            else if (calc.Expression is CssCalcMulExpression mul)
            {
                return ResolveCalcMultiplication(mul.Left, mul.Right, element, propertyName);
            }
            else if (calc.Expression is CssCalcDivExpression div)
            {
                return ResolveCalcDivision(div.Left, div.Right, element, propertyName);
            }
            else if (calc.Expression is CssCalcBracketExpression bracket)
            {
                var innerCalc = new CssCalcValue(bracket.Value);
                return ResolveNestedCalcExpressions(innerCalc, element, propertyName);
            }
            else if (calc.Expression is CssLengthValue length)
            {
                return ComputeLength(length, element, propertyName);
            }
            else if (calc.Expression is CssPercentageValue percentage)
            {
                return ComputePercentage(percentage, element, propertyName);
            }

            return calc.Expression;
        }

        private ICssValue? ResolveCalcOperation(ICssValue? left, ICssValue? right, bool isAddition, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

            if (leftValue is CssLengthValue leftLength && rightValue is CssLengthValue rightLength)
            {
                var leftPx = ToPixels(leftLength, element, propertyName);
                var rightPx = ToPixels(rightLength, element, propertyName);
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssPercentageValue leftPercentage && rightValue is CssPercentageValue rightPercentage)
            {
                var resultPercentage = isAddition
                    ? leftPercentage.Value + rightPercentage.Value
                    : leftPercentage.Value - rightPercentage.Value;
                return new CssPercentageValue(resultPercentage);
            }
            else if (leftValue is CssPercentageValue percentage && rightValue is CssLengthValue length)
            {
                var leftPx = ResolvePercentage(percentage.Value / 100, element, propertyName);
                var rightPx = ToPixels(length, element, propertyName);
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }
            else if (leftValue is CssLengthValue length2 && rightValue is CssPercentageValue percentage2)
            {
                var leftPx = ToPixels(length2, element, propertyName);
                var rightPx = ResolvePercentage(percentage2.Value / 100, element, propertyName);
                var resultPx = isAddition ? leftPx + rightPx : leftPx - rightPx;
                return new CssLengthValue(resultPx, CssLengthValue.Unit.Px);
            }

            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ResolveCalcMultiplication(ICssValue? left, ICssValue? right, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

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

            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ResolveCalcDivision(ICssValue? left, ICssValue? right, IElement element, string propertyName)
        {
            if (left == null || right == null)
                return null;

            var leftValue = ComputeCalcOperand(left, element, propertyName);
            var rightValue = ComputeCalcOperand(right, element, propertyName);

            if (rightValue is CssNumberValue rightNumber)
            {
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
                    var result = leftNumber.Value / rightNumber.Value;
                    return new CssNumberValue(result);
                }
            }

            return new CssLengthValue(0, CssLengthValue.Unit.Px);
        }

        private ICssValue? ComputeCalcOperand(ICssValue operand, IElement element, string propertyName)
        {
            if (operand is CssCalcValue nestedCalc)
            {
                return ResolveNestedCalcExpressions(nestedCalc, element, propertyName);
            }
            else if (operand is CssLengthValue length)
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
            // Handle specific calc expression types
            else if (operand is CssCalcAddExpression add)
                return ResolveCalcOperation(add.Left, add.Right, true, element, propertyName);
            else if (operand is CssCalcSubExpression sub)
                return ResolveCalcOperation(sub.Left, sub.Right, false, element, propertyName);
            else if (operand is CssCalcMulExpression mul)
                return ResolveCalcMultiplication(mul.Left, mul.Right, element, propertyName);
            else if (operand is CssCalcDivExpression div)
                return ResolveCalcDivision(div.Left, div.Right, element, propertyName);
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
                return ToPixels(lengthValue, element, propertyName);
            else if (resolvedValue is CssPercentageValue percentageValue)
                return ResolvePercentage(percentageValue.Value / 100, element, propertyName);

            return 0.0;
        }

        #endregion

        #region Private Methods - Helper Functions

        private double ResolvePercentage(double percentageValue, IElement element, string propertyName)
        {
            var containingBlock = GetContainingBlockElement(element);
            if (containingBlock == null)
                return 0;

            // Horizontal properties depend on container width
            if (IsHorizontalProperty(propertyName))
            {
                var containerWidth = GetElementComputedWidth(containingBlock);
                return containerWidth * percentageValue;
            }
            // Vertical properties depend on container height
            else if (IsVerticalProperty(propertyName))
            {
                var containerHeight = GetElementComputedHeight(containingBlock);
                return containerHeight * percentageValue;
            }

            // Default to viewport width for other percentage values
            return _renderDevice.ViewPortWidth * percentageValue;
        }

        private bool IsHorizontalProperty(string propertyName)
        {
            return propertyName.Contains("width") ||
                   propertyName.Contains("left") ||
                   propertyName.Contains("right") ||
                   propertyName.Contains("margin-left") ||
                   propertyName.Contains("margin-right") ||
                   propertyName.Contains("padding-left") ||
                   propertyName.Contains("padding-right");
        }

        private bool IsVerticalProperty(string propertyName)
        {
            return propertyName.Contains("height") ||
                   propertyName.Contains("top") ||
                   propertyName.Contains("bottom") ||
                   propertyName.Contains("margin-top") ||
                   propertyName.Contains("margin-bottom") ||
                   propertyName.Contains("padding-top") ||
                   propertyName.Contains("padding-bottom");
        }

        private ICssValue? GetInheritedValue(IElement element, string propertyName)
        {
            var parent = element.ParentElement;
            if (parent == null)
                return GetDefaultValue(propertyName);

            var computedStyle = GetElementComputedStyle(parent);
            if (computedStyle == null)
                return GetDefaultValue(propertyName);

            var value = computedStyle.GetPropertyValue(propertyName);
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(propertyName);

            var cssValue = ParseCssValue(propertyName, value);
            return cssValue ?? GetDefaultValue(propertyName);
        }

        private ICssValue? GetInitialValue(string propertyName)
        {
            var declaration = _declarationFactory?.Create(propertyName);
            return declaration?.InitialValue ?? GetDefaultValue(propertyName);
        }

        private ICssValue? GetUserAgentValue(string propertyName)
        {
            return GetInitialValue(propertyName);
        }

        private ICssValue? GetDefaultValue(string propertyName)
        {
            // First try to get initial value from declaration factory
            var declaration = _declarationFactory?.Create(propertyName);
            if (declaration?.InitialValue != null)
                return declaration.InitialValue;

            // Fall back to common default values
            if (propertyName.Equals(PropertyNames.FontSize, StringComparison.OrdinalIgnoreCase))
                return new CssLengthValue(16, CssLengthValue.Unit.Px);
            else if (propertyName.Equals(PropertyNames.Width, StringComparison.OrdinalIgnoreCase) ||
                     propertyName.Equals(PropertyNames.Height, StringComparison.OrdinalIgnoreCase))
                return CssLengthValue.Auto;
            else if (propertyName.Equals(PropertyNames.Color, StringComparison.OrdinalIgnoreCase))
                return CssColorValue.Black;
            else if (propertyName.Equals(PropertyNames.BackgroundColor, StringComparison.OrdinalIgnoreCase))
                return CssColorValue.Transparent;
            else if (propertyName.StartsWith("margin", StringComparison.OrdinalIgnoreCase) ||
                     propertyName.StartsWith("padding", StringComparison.OrdinalIgnoreCase))
                return CssLengthValue.Zero;
            else if (propertyName.Contains("border-width"))
                return new CssLengthValue(0, CssLengthValue.Unit.Px);

            return null;
        }

        private bool IsInherited(string propertyName)
        {
            // First try to check property flags from declaration factory
            var declaration = _declarationFactory?.Create(propertyName);
            if (declaration != null)
            {
                return (declaration.Flags & PropertyFlags.Inherited) == PropertyFlags.Inherited;
            }

            // Fall back to known inherited properties
            var inheritedProperties = new[]
            {
                PropertyNames.Color, PropertyNames.Font, PropertyNames.FontFamily, PropertyNames.FontSize,
                PropertyNames.FontStyle, PropertyNames.FontVariant, PropertyNames.FontWeight, PropertyNames.FontStretch,
                PropertyNames.LineHeight, PropertyNames.LetterSpacing, PropertyNames.TextAlign, PropertyNames.TextIndent,
                PropertyNames.TextTransform, PropertyNames.WhiteSpace, PropertyNames.WordSpacing, PropertyNames.Visibility,
                PropertyNames.Cursor
            };

            return Array.Exists(inheritedProperties, p => p.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
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

            var fontSize = style.GetPropertyValue(PropertyNames.FontSize);
            if (string.IsNullOrEmpty(fontSize))
                return 16.0;

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
            var document = element.OwnerDocument;
            if (document?.DocumentElement == null)
                return 16.0;

            return GetFontSizeInPixels(document.DocumentElement);
        }

        private int GetParentFontWeight(ICssValue baseValue)
        {
            if (baseValue is CssIntegerValue intValue)
            {
                return intValue.IntValue;
            }
            else if (int.TryParse(baseValue.CssText, out var parsedWeight))
            {
                return parsedWeight;
            }

            return 400; // Default font-weight if unparseable
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

            var width = style.GetPropertyValue(PropertyNames.Width);
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

            var height = style.GetPropertyValue(PropertyNames.Height);
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
                return _cssParser?.ParseDeclaration(styleAttr);
            }

            return null;
        }

        private ICssValue? ParseCssValue(string propertyName, string cssText)
        {
            var declaration = _cssParser?.ParseDeclaration($"{propertyName}: {cssText}");
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

        private bool TryParseLengthValue(string text, out CssLengthValue result)
        {
            if (CssLengthValue.TryParse(text, out result))
            {
                return true;
            }

            if (double.TryParse(text, out var number))
            {
                result = new CssLengthValue(number, CssLengthValue.Unit.Px);
                return true;
            }

            result = CssLengthValue.Zero;
            return false;
        }

        private bool IsGlobalKeyword(ICssValue value)
        {
            var keyword = value.CssText.ToLowerInvariant();
            return keyword == "inherit" || keyword == "initial" || keyword == "unset" || keyword == "revert";
        }

        private bool IsPropertySpecificKeyword(ICssValue value, string propertyName)
        {
            if (!propertyName.Equals(PropertyNames.FontSize, StringComparison.OrdinalIgnoreCase))
                return false;

            var keyword = value.CssText.ToLowerInvariant();
            return keyword == "xx-small" || keyword == "x-small" || keyword == "small" ||
                   keyword == "medium" || keyword == "large" || keyword == "x-large" ||
                   keyword == "xx-large" || keyword == "xxx-large" ||
                   keyword == "smaller" || keyword == "larger";
        }

        private bool IsLengthProperty(string propertyName)
        {
            var lengthProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PropertyNames.Width, PropertyNames.Height,
                PropertyNames.MinWidth, PropertyNames.MinHeight,
                PropertyNames.MaxWidth, PropertyNames.MaxHeight,
                PropertyNames.Margin, PropertyNames.MarginTop, PropertyNames.MarginRight,
                PropertyNames.MarginBottom, PropertyNames.MarginLeft,
                PropertyNames.Padding, PropertyNames.PaddingTop, PropertyNames.PaddingRight,
                PropertyNames.PaddingBottom, PropertyNames.PaddingLeft,
                PropertyNames.Top, PropertyNames.Right, PropertyNames.Bottom, PropertyNames.Left,
                PropertyNames.BorderWidth, PropertyNames.BorderTopWidth, PropertyNames.BorderRightWidth,
                PropertyNames.BorderBottomWidth, PropertyNames.BorderLeftWidth,
                PropertyNames.FontSize, PropertyNames.LineHeight,
                PropertyNames.TextIndent, PropertyNames.LetterSpacing, PropertyNames.WordSpacing
            };

            return lengthProperties.Contains(propertyName);
        }

        private double GetDpi()
        {
            return _renderDevice.Resolution > 0 ? _renderDevice.Resolution : 96.0;
        }

        #endregion
    }
}