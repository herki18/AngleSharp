namespace AngleSharp.StyleSystem.Computation;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;

/// <summary>
/// Resolves CSS custom properties (variables) and handles var() functions.
/// </summary>
public class VariableResolver : IVariableResolver
{
    // Constants
    private const string CssVariablePrefix = "--";
    private const int MaxResolutionDepth = 50;
    private const string CacheKeySeparator = ":";

    // Fields storing variables and caches
    private readonly Dictionary<IElement, Dictionary<string, ICssValue>> _elementVariables;
    private readonly Dictionary<string, ICssValue> _resolvedVariableCache;
    private readonly HashSet<string> _processingVariables;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableResolver"/> class.
    /// </summary>
    public VariableResolver()
    {
        _elementVariables = new Dictionary<IElement, Dictionary<string, ICssValue>>();
        _resolvedVariableCache = new Dictionary<string, ICssValue>();
        _processingVariables = new HashSet<string>();
    }

    /// <inheritdoc />
    public ICssValue? ResolveVariable(string variableName, IElement? element, ICssValue? defaultValue = null)
    {
        return ResolveVariableInternal(variableName, element, defaultValue, 0);
    }

    private ICssValue? ResolveVariableInternal(string variableName, IElement? element, ICssValue? defaultValue, int depth)
    {
        // Handle null element
        if (element is null)
            return defaultValue;

        // Hard limit on recursion depth - immediately return the default
        if (depth >= MaxResolutionDepth)
            return defaultValue;

        // Ensure the variable name is a proper CSS custom property
        if (!IsValidCssVariableName(variableName))
            return defaultValue;

        // Create cache key for this variable lookup
        string cacheKey = GetCacheKey(element, variableName);

        // Detect circular references
        if (_processingVariables.Contains(cacheKey))
            return defaultValue;

        try
        {
            // Track that we're currently processing this variable
            _processingVariables.Add(cacheKey);

            // Check cache first
            if (_resolvedVariableCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            // Try to get the variable from the current element
            if (TryGetElementVariable(element, variableName, out var value))
            {
                if (value is CssVarValue varValue)
                {
                    // Important: Increment the depth for the nested call
                    // This is critical to prevent infinite recursion
                    var resolvedValue = ResolveVarFunctionInternal(varValue, element, depth + 1);
                    if (resolvedValue != null)
                    {
                        // Cache the resolved value
                        _resolvedVariableCache[cacheKey] = resolvedValue;
                        return resolvedValue;
                    }
                }
                else
                {
                    // Cache and return the direct value
                    _resolvedVariableCache[cacheKey] = value;
                    return value;
                }
            }

            // Try to get the variable from parent element (inheritance)
            if (element.ParentElement != null)
            {
                var parentValue = ResolveVariableInternal(variableName, element.ParentElement, null, depth + 1);
                if (parentValue != null)
                {
                    _resolvedVariableCache[cacheKey] = parentValue;
                    return parentValue;
                }
            }

            // Try to get the variable from the root element (if not already checked)
            var root = element.OwnerDocument?.DocumentElement;
            if (root != null && root != element && element.ParentElement != root)
            {
                var rootValue = ResolveVariableInternal(variableName, root, null, depth + 1);
                if (rootValue != null)
                {
                    _resolvedVariableCache[cacheKey] = rootValue;
                    return rootValue;
                }
            }

            return defaultValue;
        }
        finally
        {
            // Always remove from processing set, even if exception occurs
            _processingVariables.Remove(cacheKey);
        }
    }

    /// <inheritdoc />
    public ICssValue? ResolveVarFunction(CssVarValue varValue, IElement? element)
    {
        return ResolveVarFunctionInternal(varValue, element, 0);
    }

    private ICssValue? ResolveVarFunctionInternal(CssVarValue varValue, IElement? element, int depth)
    {
        // Early exit conditions - null checks
        if (element is null || varValue is null)
            return null;

        // Hard depth limit to prevent stack overflow
        if (depth >= MaxResolutionDepth)
        {
            // When we hit the depth limit, extract the final non-var value
            return ExtractFinalFallbackValue(varValue);
        }

        // Resolve the variable name
        var resolved = ResolveVariableInternal(varValue.VariableName, element, null, depth);
        if (resolved != null)
            return resolved;

        // Try to use the default value if provided
        if (varValue.DefaultValue != null)
        {
            if (varValue.DefaultValue is CssVarValue nestedVarValue)
            {
                // Handle nested var() function with incremented depth
                return ResolveVarFunctionInternal(nestedVarValue, element, depth + 1);
            }
            return varValue.DefaultValue;
        }

        return null;
    }

    // Helper method to extract the final fallback value from nested var() functions
    private ICssValue? ExtractFinalFallbackValue(CssVarValue varValue)
    {
        ICssValue? currentValue = varValue.DefaultValue;
        int safetyCounter = 0;
        const int maxIterations = 100; // Prevent infinite loops

        // Keep unwrapping nested var() functions until we get a non-var value
        while (currentValue is CssVarValue nestedVar && safetyCounter < maxIterations)
        {
            currentValue = nestedVar.DefaultValue;
            safetyCounter++;
        }

        return currentValue;
    }

    /// <inheritdoc />
    public ICssValue ResolveVariablesInValue(ICssValue value, IElement? element, string propertyName)
    {
        if (element is null || value is null)
            return value!;

        // Handle var() functions in value
        if (value is CssVarValue varValue)
        {
            var resolved = ResolveVarFunction(varValue, element);
            return resolved ?? value;
        }

        // For other CSS values, return as-is
        return value;
    }

    /// <inheritdoc />
    public void RegisterVariable(IElement element, string variableName, ICssValue value)
    {
        if (element is null || value is null || string.IsNullOrEmpty(variableName))
            return;

        // Ensure we have a variables dictionary for this element
        if (!_elementVariables.TryGetValue(element, out var variables))
        {
            variables = new Dictionary<string, ICssValue>();
            _elementVariables[element] = variables;
        }

        // Store the variable
        variables[variableName] = value;

        // Invalidate relevant cache entries
        InvalidateCache(variableName);
    }

    /// <inheritdoc />
    public void ExtractVariablesFromStyle(IElement element, ICssStyleDeclaration style)
    {
        if (element is null || style is null)
            return;

        // Extract all CSS variables from the style declaration
        foreach (var property in style)
        {
            if (IsValidCssVariableName(property.Name) && property.RawValue != null)
            {
                RegisterVariable(element, property.Name, property.RawValue);
            }
        }
    }

    /// <inheritdoc />
    public void RemoveVariable(IElement element, string variableName)
    {
        if (element is null || string.IsNullOrEmpty(variableName))
            return;

        if (_elementVariables.TryGetValue(element, out var variables))
        {
            if (variables.Remove(variableName))
            {
                // Invalidate relevant cache entries
                string cacheKey = GetCacheKey(element, variableName);
                InvalidateCacheEntriesContaining(cacheKey);
            }
        }
    }

    /// <inheritdoc />
    public void ClearElementVariables(IElement element)
    {
        if (element is null)
            return;

        if (_elementVariables.Remove(element))
        {
            // Invalidate all cache entries for this element
            string prefix = $"{element.GetHashCode()}{CacheKeySeparator}";
            InvalidateCacheEntriesStartingWith(prefix);
        }
    }

    /// <summary>
    /// Clears all cached resolved variables.
    /// </summary>
    public void ClearCache()
    {
        _resolvedVariableCache.Clear();
        _processingVariables.Clear();
    }

    // Private helper methods

    private bool IsValidCssVariableName(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && propertyName.StartsWith(CssVariablePrefix, StringComparison.Ordinal);
    }

    private string GetCacheKey(IElement element, string variableName)
    {
        return $"{element.GetHashCode()}{CacheKeySeparator}{variableName}";
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

    private void InvalidateCache(string variableName)
    {
        var keysToRemove = _resolvedVariableCache.Keys
            .Where(k => k.EndsWith(variableName, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resolvedVariableCache.Remove(key);
        }
    }

    private void InvalidateCacheEntriesContaining(string pattern)
    {
        var keysToRemove = _resolvedVariableCache.Keys
            .Where(k => k.Equals(pattern, StringComparison.Ordinal) ||
                        k.Contains(pattern, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resolvedVariableCache.Remove(key);
        }
    }

    private void InvalidateCacheEntriesStartingWith(string prefix)
    {
        var keysToRemove = _resolvedVariableCache.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resolvedVariableCache.Remove(key);
        }
    }
}