namespace AngleSharp.StyleSystem.Core.Interfaces
{
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
        /// <param name="variableName">The name of the variable.</param>
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
        /// Resolves any CSS variables within a CSS value.
        /// </summary>
        /// <param name="value">The CSS value potentially containing variables.</param>
        /// <param name="element">The element context.</param>
        /// <param name="propertyName">The property name being processed.</param>
        /// <returns>The CSS value with variables resolved.</returns>
        ICssValue ResolveVariablesInValue(ICssValue value, IElement element, string propertyName);

        /// <summary>
        /// Registers a variable on an element.
        /// </summary>
        /// <param name="element">The element to register the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="value">The variable value.</param>
        void RegisterVariable(IElement element, string variableName, ICssValue value);

        /// <summary>
        /// Extracts all CSS variables from a style declaration and registers them on the element.
        /// </summary>
        /// <param name="element">The element owning the style.</param>
        /// <param name="style">The style declaration containing variables.</param>
        void ExtractVariablesFromStyle(IElement element, ICssStyleDeclaration style);

        /// <summary>
        /// Removes a specific variable from an element.
        /// </summary>
        /// <param name="element">The element containing the variable.</param>
        /// <param name="variableName">The name of the variable to remove.</param>
        void RemoveVariable(IElement element, string variableName);

        /// <summary>
        /// Removes all variables associated with an element.
        /// </summary>
        /// <param name="element">The element to clear variables for.</param>
        void ClearElementVariables(IElement element);
    }
}