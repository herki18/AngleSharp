namespace AngleSharp.StyleSystem.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Css.Dom;
    using Css.Values;
    using Dom;
    using Interfaces;

    public class VariableResolver : IVariableResolver
    {
        private readonly IBrowsingContext _context;
        private readonly Dictionary<IElement, Dictionary<string, ICssValue>> _variables = new();
        private readonly Dictionary<string, ICssValue> _resolutionCache = new();
        private const int MaxResolutionDepth = 50;

        public VariableResolver(IBrowsingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ICssValue? ResolveVariable(string variableName, IElement element, ICssValue? defaultValue = null)
        {
            if (element == null)
                return defaultValue;

            if (!variableName.StartsWith("--"))
                return defaultValue;

            return ResolveVariableInternal(variableName, element, defaultValue, new HashSet<string>(), 0);
        }

        private ICssValue? ResolveVariableInternal(string variableName, IElement element, ICssValue? defaultValue, HashSet<string> resolutionChain, int depth)
        {
            // Check for circular references and max depth
            if (resolutionChain.Contains(variableName) || depth >= MaxResolutionDepth)
                return defaultValue;

            resolutionChain.Add(variableName);

            // Cache key for the resolved variable
            string cacheKey = $"{element.GetHashCode()}:{variableName}";

            // Try to find in cache
            if (_resolutionCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            // Try to get the variable from the current element
            if (_variables.TryGetValue(element, out var elementVariables) &&
                elementVariables.TryGetValue(variableName, out var value))
            {
                // If the value is itself a var() function, resolve it recursively
                if (value is CssVarValue varValue)
                {
                    value = ResolveVarFunctionInternal(varValue, element, new HashSet<string>(resolutionChain), depth + 1);
                }

                if (value != null)
                {
                    _resolutionCache[cacheKey] = value;
                    return value;
                }
            }

            // Try parent element if available
            if (element.ParentElement != null)
            {
                var parentResult = ResolveVariableInternal(variableName, element.ParentElement, null, new HashSet<string>(resolutionChain), depth + 1);
                if (parentResult != null)
                {
                    _resolutionCache[cacheKey] = parentResult;
                    return parentResult;
                }
            }

            // Try document root element as a last resort for global variables
            if (element.OwnerDocument?.DocumentElement != null && element != element.OwnerDocument.DocumentElement)
            {
                var rootResult = ResolveVariableInternal(variableName, element.OwnerDocument.DocumentElement, null, new HashSet<string>(resolutionChain), depth + 1);
                if (rootResult != null)
                {
                    _resolutionCache[cacheKey] = rootResult;
                    return rootResult;
                }
            }

            // If not found anywhere, use the default value
            return defaultValue;
        }

        public ICssValue? ResolveVarFunction(CssVarValue varValue, IElement element)
        {
            if (element == null || varValue == null)
                return null;

            return ResolveVarFunctionInternal(varValue, element, new HashSet<string>(), 0);
        }

        private ICssValue? ResolveVarFunctionInternal(CssVarValue varValue, IElement element, HashSet<string> resolutionChain, int depth)
        {
            if (depth >= MaxResolutionDepth)
                return null;

            var variableName = varValue.VariableName;
            var fallbackValue = varValue.DefaultValue;

            // Handle nested var() in fallback
            if (fallbackValue is CssVarValue nestedFallback)
            {
                fallbackValue = ResolveVarFunctionInternal(nestedFallback, element, new HashSet<string>(resolutionChain), depth + 1);
            }

            // Resolve the variable
            var result = ResolveVariableInternal(variableName, element, fallbackValue, new HashSet<string>(resolutionChain), depth);

            // If we got a var() value back, resolve it recursively
            if (result is CssVarValue nestedVarValue)
            {
                return ResolveVarFunctionInternal(nestedVarValue, element, new HashSet<string>(resolutionChain), depth + 1);
            }

            return result;
        }

        public ICssValue ResolveVariablesInValue(ICssValue value, IElement element, string propertyName)
        {
            // If it's a var() function, resolve it
            if (value is CssVarValue varValue)
            {
                var resolved = ResolveVarFunction(varValue, element);
                return resolved ?? value;
            }

            // Handle complex values that might contain var() references
            if (value is ICssFunctionValue functionValue &&
                functionValue.Arguments != null &&
                functionValue.Arguments.Any(arg => arg is CssVarValue))
            {
                // This is a placeholder for complex value resolution
                // A real implementation would need to parse and process the function
                // with all its arguments, resolving any var() references inside
                return value;
            }

            return value;
        }

        public void RegisterVariable(IElement element, string variableName, ICssValue value)
        {
            if (!variableName.StartsWith("--"))
                return;

            if (!_variables.TryGetValue(element, out var elementVariables))
            {
                elementVariables = new Dictionary<string, ICssValue>(StringComparer.OrdinalIgnoreCase);
                _variables[element] = elementVariables;
            }

            elementVariables[variableName] = value;

            // Invalidate cache when variables change
            _resolutionCache.Clear();
        }

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
    }
}