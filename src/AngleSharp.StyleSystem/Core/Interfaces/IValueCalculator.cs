namespace AngleSharp.StyleSystem.Core.Interfaces;

using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Calculates computed values from CSS values with full unit conversion support.
/// </summary>
public interface IValueCalculator
{
    /// <summary>
    /// Computes the value of a CSS property.
    /// </summary>
    /// <param name="value">The CSS value to compute.</param>
    /// <param name="element">The element context for the computation.</param>
    /// <param name="propertyName">The name of the property being computed.</param>
    /// <returns>The computed value.</returns>
    ICssValue? Compute(ICssValue? value, IElement element, string propertyName);

    /// <summary>
    /// Converts a length value to pixels.
    /// </summary>
    /// <param name="length">The length value to convert.</param>
    /// <param name="element">The element context for relative units.</param>
    /// <param name="propertyName">The property name for context-specific conversions.</param>
    /// <returns>The length in pixels.</returns>
    double ToPixels(CssLengthValue length, IElement element, string propertyName);

    /// <summary>
    /// Evaluates a calc() expression.
    /// </summary>
    /// <param name="calc">The calc expression to evaluate.</param>
    /// <param name="element">The element context.</param>
    /// <param name="propertyName">The property name for context-specific calculations.</param>
    /// <returns>The computed value.</returns>
    ICssValue? EvaluateCalc(CssCalcValue? calc, IElement element, string propertyName);

    /// <summary>
    /// Resolves a value that might be relative to another value.
    /// </summary>
    /// <param name="value">The value to resolve.</param>
    /// <param name="element">The element context.</param>
    /// <param name="baseValue">The base value for relative calculations.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The resolved value.</returns>
    ICssValue ResolveRelative(ICssValue value, IElement element, ICssValue baseValue, string propertyName);
}