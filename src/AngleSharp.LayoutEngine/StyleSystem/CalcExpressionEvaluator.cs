namespace AngleSharp.LayoutEngine.StyleSystem;

using System;
using System.Diagnostics;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Evaluates CSS calc() expressions, handling different units and variable references.
/// </summary>
public class CalcExpressionEvaluator
{
    private readonly IElement _element;
    private readonly double _fontSize;
    private readonly double _rootFontSize;
    private readonly IRenderDevice _device;
    private readonly CssComputationContext _context;

    /// <summary>
    /// Creates a new calculator for evaluating calc() expressions.
    /// </summary>
    /// <param name="element">The element being styled</param>
    /// <param name="fontSize">The element's font size</param>
    /// <param name="rootFontSize">The root element's font size</param>
    /// <param name="device">The render device for unit conversions</param>
    /// <param name="context">The computation context</param>
    public CalcExpressionEvaluator(
        IElement element,
        double fontSize,
        double rootFontSize,
        IRenderDevice device,
        CssComputationContext context)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _fontSize = fontSize;
        _rootFontSize = rootFontSize;
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Evaluates a calc() expression.
    /// </summary>
    /// <param name="calcValue">The calc() expression to evaluate</param>
    /// <returns>The evaluated result as a CSS value</returns>
    public ICssValue EvaluateCalc(CssCalcValue calcValue)
    {
        // Resolve any variables in the expression first
        var resolvedExpression = ResolveVariablesInExpression(calcValue.Expression);

        // Check if we can simplify the expression immediately
        if (resolvedExpression is CssLengthValue length)
        {
            return length;
        }

        // If the expression is still a calc after variable resolution,
        // return a new calc with the resolved expression
        if (resolvedExpression != calcValue.Expression)
        {
            return new CssCalcValue(resolvedExpression);
        }

        // Otherwise, return the original calc
        return calcValue;
    }

    /// <summary>
    /// Resolves variables in an expression.
    /// </summary>
    /// <param name="expression">The expression potentially containing variables</param>
    /// <returns>The expression with variables resolved</returns>
    private ICssValue ResolveVariablesInExpression(ICssValue expression)
    {
        if (expression is CssVarValue varValue)
        {
            // Resolve variable reference
            var resolved = _context.ResolveVarReference(varValue);
            return resolved ?? expression;
        }

        // This is a simplified implementation - a real implementation would need
        // to handle complex expressions (operations between lengths)

        return expression;
    }

    /// <summary>
    /// Combines values with different units according to CSS calc() rules.
    /// </summary>
    /// <param name="value1">First value</param>
    /// <param name="unit1">First unit</param>
    /// <param name="value2">Second value</param>
    /// <param name="unit2">Second unit</param>
    /// <param name="operation">Mathematical operation</param>
    /// <returns>Result in pixels</returns>
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

    /// <summary>
    /// Converts a value from its unit to pixels based on the current context.
    /// </summary>
    /// <param name="value">The numeric value</param>
    /// <param name="unit">The CSS unit</param>
    /// <returns>Value in pixels</returns>
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
            case "vmin":
                return value * Math.Min(_device.ViewPortWidth, _device.ViewPortHeight) / 100.0;
            case "vmax":
                return value * Math.Max(_device.ViewPortWidth, _device.ViewPortHeight) / 100.0;
            case "%":
                // Percentage requires context - default to font-size
                return value * _fontSize / 100.0;
            case "cm":
                // 96dpi = 37.8 pixels per cm
                return value * 37.8;
            case "mm":
                // 96dpi = 3.78 pixels per mm
                return value * 3.78;
            case "in":
                // 96 pixels per inch at standard dpi
                return value * 96.0;
            case "pt":
                // 1pt = 1/72 of an inch, 96/72 = 1.333 pixels per point
                return value * 1.333;
            case "pc":
                // 1pc = 12pt = 12/72 inches = 1/6 inch, 96/6 = 16 pixels per pica
                return value * 16.0;
            default:
                // Unknown unit - log warning and return as is
                Debug.WriteLine($"Unknown CSS unit in calc(): {unit}");
                return value;
        }
    }

    /// <summary>
    /// Evaluates a calc expression involving one value and an operation.
    /// </summary>
    /// <param name="value">The value to evaluate</param>
    /// <param name="operation">The operation (negate, etc.)</param>
    /// <returns>The result value</returns>
    private ICssValue? EvaluateUnaryOperation(ICssValue value, string operation)
    {
        // Only implementation for now is negation
        if (operation == "-" && value is CssLengthValue length)
        {
            return new CssLengthValue(-length.Value, length.Type);
        }

        // Default: can't evaluate
        return null;
    }

    /// <summary>
    /// Creates a result value with appropriate unit after calculation.
    /// </summary>
    /// <param name="value">The calculated value</param>
    /// <param name="preferredUnit">The preferred unit, if any</param>
    /// <returns>A CSS value with the result</returns>
    private ICssValue CreateResultValue(double value, string preferredUnit = "px")
    {
        // For now, return pixels
        return new CssLengthValue(value, CssLengthValue.Unit.Px);
    }
}