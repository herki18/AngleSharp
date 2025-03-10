namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Defines a calculator for CSS values.
/// </summary>
public interface IValueCalculator
{
    /// <summary>
    /// Computes the absolute value of a CSS value in the context of an element and property.
    /// </summary>
    /// <param name="value">The CSS value to compute.</param>
    /// <param name="element">The element context.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The computed value.</returns>
    ICssValue? Compute(ICssValue? value, IElement element, string propertyName);

    /// <summary>
    /// Converts a CSS length value to pixels.
    /// </summary>
    /// <param name="length">The length value to convert.</param>
    /// <param name="element">The element context.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The length in pixels.</returns>
    double ToPixels(CssLengthValue length, IElement element, string propertyName);

    /// <summary>
    /// Evaluates a CSS calc() expression.
    /// </summary>
    /// <param name="calc">The calc expression to evaluate.</param>
    /// <param name="element">The element context.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The computed value.</returns>
    ICssValue? EvaluateCalc(CssCalcValue? calc, IElement element, string propertyName);

    /// <summary>
    /// Resolves a relative CSS value based on a base value.
    /// </summary>
    /// <param name="value">The value to resolve.</param>
    /// <param name="element">The element context.</param>
    /// <param name="baseValue">The base value for relative calculations.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The resolved value.</returns>
    ICssValue ResolveRelative(ICssValue value, IElement element, ICssValue baseValue, string propertyName);

    /// <summary>
    /// Clears the computation cache.
    /// </summary>
    void ClearCache();
}