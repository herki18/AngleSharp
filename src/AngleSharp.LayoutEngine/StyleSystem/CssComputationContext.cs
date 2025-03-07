namespace AngleSharp.LayoutEngine.StyleSystem;

using System;
using System.Diagnostics;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// The CSS computation context used for resolving CSS values, including CSS variables.
/// This is the canonical implementation of ICssComputeContext for the layout engine.
/// </summary>
public class CssComputationContext : ICssComputeContext
{
    private readonly IRenderDevice _device;
    private readonly IBrowsingContext _context;
    private readonly ICssStyleDeclaration _style;
    private readonly ICssStyleDeclaration _parentStyle;
    private readonly ICssStyleDeclaration _rootStyle;
    private readonly double _fontSize;
    private readonly double _rootFontSize;
    private readonly IElement _element;
    private readonly VariableRegistry _variableRegistry;
    private readonly VariableResolver _variableResolver;
    private readonly ResolverContext _resolverContext;

    /// <summary>
    /// Creates a new CSS computation context with full variable resolution support.
    /// </summary>
    /// <param name="device">The render device providing dimensions and other context.</param>
    /// <param name="context">The browsing context.</param>
    /// <param name="fontSize">The current element's font size in pixels.</param>
    /// <param name="rootFontSize">The root element's font size in pixels.</param>
    /// <param name="style">The element's style declaration.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <param name="rootStyle">The root element's computed style.</param>
    /// <param name="element">The element being styled.</param>
    /// <param name="variableRegistry">The registry of CSS variables.</param>
    /// <param name="resolverContext">Optional resolver context for caching and cycle detection.</param>
    public CssComputationContext(
        IRenderDevice device,
        IBrowsingContext context,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration style,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle,
        IElement element,
        VariableRegistry variableRegistry,
        ResolverContext? resolverContext = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _fontSize = fontSize;
        _rootFontSize = rootFontSize;
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _parentStyle = parentStyle;
        _rootStyle = rootStyle;
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _variableRegistry = variableRegistry ?? throw new ArgumentNullException(nameof(variableRegistry));
        _variableResolver = new VariableResolver(variableRegistry, context, device);
        _resolverContext = resolverContext ?? new ResolverContext();
    }

    /// <summary>
    /// Gets the render device.
    /// </summary>
    public IRenderDevice Device => _device;

    /// <summary>
    /// Gets the browsing context.
    /// </summary>
    public IBrowsingContext Context => _context;

    /// <summary>
    /// Gets the element's font size in pixels.
    /// </summary>
    public double FontSize => _fontSize;

    /// <summary>
    /// Gets the root element's font size in pixels.
    /// </summary>
    public double RootFontSize => _rootFontSize;

    /// <summary>
    /// Gets the element being styled.
    /// </summary>
    public IElement Element => _element;

    /// <summary>
    /// Gets the value converter (not currently used).
    /// </summary>
    public IValueConverter? Converter => null;

    /// <summary>
    /// Resolves a CSS property or variable reference by name.
    /// </summary>
    /// <param name="name">The name of the property or variable to resolve.</param>
    /// <returns>The resolved CSS value, or null if not found.</returns>
    public ICssValue? Resolve(string name)
    {
        if (name.StartsWith("--"))
        {
            var varValue = new CssVarValue(name, null);
            return ResolveVarReference(varValue);
        }

        var property = _style?.GetProperty(name);
        return property?.RawValue;
    }

    /// <summary>
    /// Resolves a CSS variable reference.
    /// </summary>
    /// <param name="varValue">The variable reference to resolve.</param>
    /// <returns>The resolved CSS value, or null if not found.</returns>
    public ICssValue? ResolveVarReference(CssVarValue varValue)
    {
        string cacheKey = _resolverContext.GenerateCacheKey(varValue.VariableName, _element);
        if (_resolverContext.TryGetCachedValue(cacheKey, out var cachedValue))
            return cachedValue;

        try
        {
            var resolved = _variableResolver.SafeResolveVariable(varValue, _element, _resolverContext);
            if (resolved != null)
                _resolverContext.CacheValue(cacheKey, resolved);
            return resolved;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error resolving var() reference: {ex.Message}");
            return varValue.DefaultValue;
        }
    }

    /// <summary>
    /// Gets the inherited value for a property from the parent style.
    /// </summary>
    /// <param name="propertyName">The name of the property to inherit.</param>
    /// <returns>The inherited CSS value, or null if not found.</returns>
    public ICssValue? GetInheritedValue(string propertyName)
    {
        if (_parentStyle == null)
            return null;

        var property = _parentStyle.GetProperty(propertyName);
        return property?.RawValue;
    }

    /// <summary>
    /// Resolves a CSS variable reference with error handling.
    /// </summary>
    /// <param name="varValue">The variable reference to resolve.</param>
    /// <returns>The resolved CSS value, the fallback, or null if resolution fails.</returns>
    public ICssValue? SafeResolveVarReference(CssVarValue varValue)
    {
        try
        {
            return ResolveVarReference(varValue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in SafeResolveVarReference: {ex.Message}");
            return varValue.DefaultValue;
        }
    }

    /// <summary>
    /// Directly resolves a variable by name from the available styles.
    /// </summary>
    /// <param name="name">The variable name to resolve.</param>
    /// <returns>The resolved CSS value, or null if not found.</returns>
    public ICssValue? ResolveVariable(string name)
    {
        // First check in registry
        var value = _variableRegistry.GetVariableValue(name);
        if (value != null)
            return value;

        // Then check element's style
        var variable = _style?.GetProperty(name);
        if (variable?.RawValue != null)
            return variable.RawValue;

        // Then check parent style
        if (_parentStyle != null)
        {
            variable = _parentStyle.GetProperty(name);
            if (variable?.RawValue != null)
                return variable.RawValue;
        }

        // Finally check root style
        if (_rootStyle != null && !ReferenceEquals(_parentStyle, _rootStyle))
        {
            variable = _rootStyle.GetProperty(name);
            if (variable?.RawValue != null)
                return variable.RawValue;
        }

        return null;
    }

    /// <summary>
    /// Clears the variable resolution cache.
    /// </summary>
    public void ClearCache()
    {
        _resolverContext.ClearCache();
    }
}