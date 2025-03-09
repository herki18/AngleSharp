using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

namespace AngleSharp.StyleSystem.Core
{
    using Css;

    /// <summary>
    /// Resolves CSS custom properties (variables) in style declarations.
    /// </summary>
    public class VariableResolver : IVariableResolver
    {
        private readonly IBrowsingContext _context;
        private readonly Dictionary<IElement, Dictionary<string, ICssValue>> _variableRegistry = new Dictionary<IElement, Dictionary<string, ICssValue>>();
        private readonly HashSet<string> _processingVariables = new HashSet<string>();
        private const int MaxVariableResolutionDepth = 32; // Prevents infinite recursion

        public VariableResolver(IBrowsingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Resolves a CSS variable reference.
        /// </summary>
        public ICssValue? ResolveVariable(string variableName, IElement element, ICssValue? defaultValue = null)
        {
            // CSS variables must start with -- prefix
            if (!variableName.StartsWith("--"))
            {
                return defaultValue;
            }

            // Check for circular references
            if (_processingVariables.Contains(variableName))
            {
                return defaultValue; // Circular reference detected
            }

            try
            {
                _processingVariables.Add(variableName);

                // Look up the variable in the current element first
                if (TryGetVariableFromElement(element, variableName, out var value))
                {
                    return value;
                }

                // If not found, look in parent elements (CSS variables inherit)
                var parent = element.ParentElement;
                while (parent != null)
                {
                    if (TryGetVariableFromElement(parent, variableName, out value))
                    {
                        return value;
                    }
                    parent = parent.ParentElement;
                }
            }
            finally
            {
                _processingVariables.Remove(variableName);
            }

            // If variable not found, return the default value
            return defaultValue;
        }

        /// <summary>
        /// Resolves a var() function value.
        /// </summary>
        public ICssValue? ResolveVarFunction(CssVarValue varValue, IElement element)
        {
            // Extract the variable name from the var() function
            var variableName = varValue.CssText;

            // In AngleSharp, var() is likely represented as "var(--name)" in CssText,
            // so we need to extract just the variable name
            if (variableName.StartsWith("var(") && variableName.EndsWith(")"))
            {
                // Extract content between var( and )
                variableName = variableName.Substring(4, variableName.Length - 5).Trim();

                // Handle potential fallback value if present (separated by comma)
                string? fallbackText = null;
                var commaIndex = variableName.IndexOf(',');

                if (commaIndex > -1)
                {
                    fallbackText = variableName.Substring(commaIndex + 1).Trim();
                    variableName = variableName.Substring(0, commaIndex).Trim();
                }

                // Resolve the variable
                var value = ResolveVariable(variableName, element);

                // If not found and we have a fallback, parse and use it
                if (value == null && !string.IsNullOrEmpty(fallbackText))
                {
                    var parser = _context.GetService<ICssParser>();
                    // Try to parse the fallback as a property value
                    var dummyProperty = $"dummy: {fallbackText}";
                    var parsed = parser?.ParseDeclaration(dummyProperty);

                    if (parsed != null && parsed.Any())
                    {
                        value = parsed.First().RawValue;

                        // If the fallback also contains var() references, resolve those too
                        if (value != null && fallbackText.Contains("var("))
                        {
                            value = ResolveVariablesInValue(value, element, "dummy");
                        }
                    }
                }

                return value;
            }

            return null;
        }

        /// <summary>
        /// Resolves all variable references in a CSS value.
        /// </summary>
        public ICssValue ResolveVariablesInValue(ICssValue value, IElement element, string propertyName)
        {
            // Handle var() directly
            if (value is CssVarValue varValue)
            {
                var resolved = ResolveVarFunction(varValue, element);
                return resolved ?? GetInitialValue(propertyName);
            }

            // For more complex values that might contain var() references
            // In a real implementation, we would need to traverse composite values,
            // calc expressions, etc. to find and resolve any var() references

            // Simplified implementation - just return the value if not a var()
            return value;
        }

        /// <summary>
        /// Registers a CSS variable for an element.
        /// </summary>
        public void RegisterVariable(IElement element, string variableName, ICssValue value)
        {
            if (!_variableRegistry.TryGetValue(element, out var variables))
            {
                variables = new Dictionary<string, ICssValue>();
                _variableRegistry[element] = variables;
            }

            variables[variableName] = value;
        }

        /// <summary>
        /// Extracts and registers all variables from a style declaration.
        /// </summary>
        public void ExtractVariablesFromStyle(IElement element, ICssStyleDeclaration style)
        {
            foreach (var property in style)
            {
                if (property.Name.StartsWith("--") && property.RawValue != null)
                {
                    RegisterVariable(element, property.Name, property.RawValue);
                }
            }
        }

        /// <summary>
        /// Tries to get a variable value from a specific element.
        /// </summary>
        private bool TryGetVariableFromElement(IElement element, string variableName, out ICssValue? value)
        {
            if (_variableRegistry.TryGetValue(element, out var variables) &&
                variables.TryGetValue(variableName, out value))
            {
                // If the value contains var() references, resolve those too
                if (value is CssVarValue varValue)
                {
                    value = ResolveVarFunction(varValue, element);
                }

                return value != null;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Gets the initial value for a property.
        /// </summary>
        private ICssValue GetInitialValue(string propertyName)
        {
            // Try to get from AngleSharp's declaration factory
            var factory = _context.GetFactory<IDeclarationFactory>();
            var declaration = factory?.Create(propertyName);

            if (declaration?.InitialValue != null)
                return declaration.InitialValue;

            // Fallback to a default value
            return new CssStringValue(string.Empty);
        }
    }
}