using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

namespace AngleSharp.StyleSystem.Core;

public class VariableResolver : IVariableResolver
{
    private readonly IBrowsingContext _context;
    private readonly Dictionary<IElement, Dictionary<string, ICssValue>> _elementVariables;
    private readonly Dictionary<string, ICssValue> _resolvedVariableCache;
    private readonly HashSet<string> _processingVariables;
    private const int MaxResolutionDepth = 50;

    public VariableResolver(IBrowsingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _elementVariables = new Dictionary<IElement, Dictionary<string, ICssValue>>();
        _resolvedVariableCache = new Dictionary<string, ICssValue>();
        _processingVariables = new HashSet<string>();
    }

    public ICssValue? ResolveVariable(string variableName, IElement? element, ICssValue? defaultValue = null)
    {
        // Handle null element
        if (element is null)
            return defaultValue;

        if (!variableName.StartsWith("--"))
            return defaultValue;

        // Check for circular references
        string cacheKey = $"{element.GetHashCode()}:{variableName}";
        if (_processingVariables.Contains(cacheKey))
            return defaultValue; // Circular reference detected

        try
        {
            _processingVariables.Add(cacheKey);

            // Check cache first
            if (_resolvedVariableCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            // Try to find the variable in the current element
            if (TryGetElementVariable(element, variableName, out var value))
            {
                // If the value is a var() function, we need to resolve it
                if (value is CssVarValue varValue)
                {
                    var resolvedValue = ResolveVarFunction(varValue, element);
                    if (resolvedValue != null)
                    {
                        _resolvedVariableCache[cacheKey] = resolvedValue;
                        return resolvedValue;
                    }
                }
                else
                {
                    _resolvedVariableCache[cacheKey] = value;
                    return value;
                }
            }

            // If not found, try parent element
            if (element.ParentElement != null)
            {
                var parentValue = ResolveVariable(variableName, element.ParentElement, null);
                if (parentValue != null)
                {
                    _resolvedVariableCache[cacheKey] = parentValue;
                    return parentValue;
                }
            }

            // If still not found, try the root element (if we're not already there)
            var root = element.OwnerDocument?.DocumentElement;
            if (root != null && root != element && element.ParentElement != root)
            {
                var rootValue = ResolveVariable(variableName, root, null);
                if (rootValue != null)
                {
                    _resolvedVariableCache[cacheKey] = rootValue;
                    return rootValue;
                }
            }

            // If all else fails, use the fallback value
            return defaultValue;
        }
        finally
        {
            _processingVariables.Remove(cacheKey);
        }
    }

    public ICssValue? ResolveVarFunction(CssVarValue varValue, IElement? element)
    {
        // Handle null element
        if (element is null)
            return null;

        // Try to resolve the variable
        var resolved = ResolveVariable(varValue.VariableName, element, null);

        // If successful, return the resolved value
        if (resolved != null)
            return resolved;

        // If not, try the fallback if provided
        if (varValue.DefaultValue != null)
        {
            // If the fallback is itself a var() function, resolve it recursively
            if (varValue.DefaultValue is CssVarValue nestedVarValue)
            {
                return ResolveVarFunction(nestedVarValue, element);
            }

            return varValue.DefaultValue;
        }

        // No resolution and no fallback
        return null;
    }

    public ICssValue ResolveVariablesInValue(ICssValue value, IElement? element, string propertyName)
    {
        // Handle null element
        if (element is null)
            return value;

        // If it's a var() function, resolve it
        if (value is CssVarValue varValue)
        {
            var resolved = ResolveVarFunction(varValue, element);
            return resolved ?? value;
        }

        // For complex values that might contain var() references
        // This is a simplified implementation - in a real scenario, we'd need to
        // parse complex values and replace var() references within them

        // Just return the value as is for now
        return value;
    }

    public void RegisterVariable(IElement element, string variableName, ICssValue value)
    {
        if (string.IsNullOrEmpty(variableName))
            return;

        if (!_elementVariables.TryGetValue(element, out var variables))
        {
            variables = new Dictionary<string, ICssValue>();
            _elementVariables[element] = variables;
        }

        variables[variableName] = value;

        // Clear cache entries related to this variable
        var keysToRemove = _resolvedVariableCache.Keys
            .Where(k => k.EndsWith(variableName))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resolvedVariableCache.Remove(key);
        }
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

    private bool TryGetElementVariable(IElement element, string variableName, out ICssValue value)
    {
        if (_elementVariables.TryGetValue(element, out var variables) &&
            variables.TryGetValue(variableName, out value))
        {
            return true;
        }

        value = null!;
        return false;
    }
}