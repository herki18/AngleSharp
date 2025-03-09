namespace AngleSharp.StyleSystem.Core.Interfaces;

using Css.Dom;
using Css.Values;
using Dom;

/// <summary>
/// Resolves CSS custom property (variable) values.
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    /// Resolves a CSS variable reference.
    /// </summary>
    /// <param name="variableName">The name of the variable (including -- prefix).</param>
    /// <param name="element">The element context.</param>
    /// <param name="defaultValue">Optional default value if the variable is not defined.</param>
    /// <returns>The resolved value or default.</returns>
    ICssValue? ResolveVariable(string variableName, IElement element, ICssValue? defaultValue = null);

    /// <summary>
    /// Resolves a var() function value.
    /// </summary>
    /// <param name="varValue">The var() function value.</param>
    /// <param name="element">The element context.</param>
    /// <returns>The resolved value.</returns>
    ICssValue? ResolveVarFunction(CssVarValue varValue, IElement element);

    /// <summary>
    /// Resolves all variables in a property value.
    /// </summary>
    /// <param name="value">The value that might contain variables.</param>
    /// <param name="element">The element context.</param>
    /// <param name="propertyName">The property name for context.</param>
    /// <returns>The value with all variables resolved.</returns>
    ICssValue ResolveVariablesInValue(ICssValue value, IElement element, string propertyName);

    /// <summary>
    /// Registers a variable value for an element.
    /// </summary>
    /// <param name="element">The element that defines the variable.</param>
    /// <param name="variableName">The variable name (including -- prefix).</param>
    /// <param name="value">The variable value.</param>
    void RegisterVariable(IElement element, string variableName, ICssValue value);
}