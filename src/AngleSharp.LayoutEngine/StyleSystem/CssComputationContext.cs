namespace AngleSharp.LayoutEngine.StyleSystem;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Enhanced implementation of ICssComputeContext that supports CSS variable resolution
/// and provides context for value computation.
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

    // Variable resolution components
    private readonly VariableRegistry _variableRegistry;
    private readonly VariableResolver _variableResolver;
    private readonly ResolverContext _resolverContext;

    /// <summary>
    /// Creates a new computation context for CSS value resolution.
    /// </summary>
    /// <param name="device">The render device for unit conversions</param>
    /// <param name="context">The browsing context</param>
    /// <param name="fontSize">The current element's font size</param>
    /// <param name="rootFontSize">The root element's font size</param>
    /// <param name="style">The element's style declaration</param>
    /// <param name="parentStyle">The parent element's style</param>
    /// <param name="rootStyle">The root element's style</param>
    /// <param name="element">The element being styled</param>
    /// <param name="variableRegistry">The variable registry with defined custom properties</param>
    public CssComputationContext(
        IRenderDevice device,
        IBrowsingContext context,
        double fontSize,
        double rootFontSize,
        ICssStyleDeclaration style,
        ICssStyleDeclaration parentStyle,
        ICssStyleDeclaration rootStyle,
        IElement element,
        VariableRegistry variableRegistry)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _fontSize = fontSize;
        _rootFontSize = rootFontSize;
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _parentStyle = parentStyle;
        _rootStyle = rootStyle;
        _element = element ?? throw new ArgumentNullException(nameof(element));

        // Initialize variable resolution components
        _variableRegistry = variableRegistry ?? throw new ArgumentNullException(nameof(variableRegistry));
        _variableResolver = new VariableResolver(variableRegistry, context, device);
        _resolverContext = new ResolverContext();
    }

    /// <summary>
    /// Gets the render device for unit conversions.
    /// </summary>
    public IRenderDevice Device => _device;

    /// <summary>
    /// Gets the browsing context.
    /// </summary>
    public IBrowsingContext Context => _context;

    /// <summary>
    /// Gets the element's font size.
    /// </summary>
    public double FontSize => _fontSize;

    /// <summary>
    /// Gets the root element's font size.
    /// </summary>
    public double RootFontSize => _rootFontSize;

    /// <summary>
    /// Gets the element being styled.
    /// </summary>
    public IElement Element => _element;

    /// <summary>
    /// Gets the value converter (not implemented in this context).
    /// </summary>
    public IValueConverter? Converter => null;

    /// <summary>
    /// Resolves a CSS variable or property reference.
    /// </summary>
    /// <param name="name">The variable or property name</param>
    /// <returns>The resolved value, or null if not found</returns>
    public ICssValue? Resolve(string name)
    {
        // If it's a CSS variable, resolve it
        if (name.StartsWith("--"))
        {
            // Create a var() reference
            var varValue = new CssVarValue(name, null);
            return ResolveVarReference(varValue);
        }

        // For other property references
        var property = _style?.GetProperty(name);
        return property?.RawValue;
    }

    /// <summary>
    /// Resolves a var() reference to its computed value.
    /// </summary>
    /// <param name="varValue">The var() function to resolve</param>
    /// <returns>The resolved value, or null if unresolvable</returns>
    public ICssValue? ResolveVarReference(CssVarValue varValue)
    {
        // Try to get from cache first
        string cacheKey = _resolverContext.GenerateCacheKey(varValue.VariableName, _element);
        if (_resolverContext.TryGetCachedValue(cacheKey, out var cachedValue))
            return cachedValue;

        // Resolve the reference
        var resolved = _variableResolver.ResolveVariable(varValue, _element, _resolverContext);

        // Cache the result
        if (resolved != null)
            _resolverContext.CacheValue(cacheKey, resolved);

        return resolved;
    }

    /// <summary>
    /// Gets the inherited value for a property from parent style.
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <returns>The inherited value, or null if no parent or property not found</returns>
    public ICssValue? GetInheritedValue(string propertyName)
    {
        if (_parentStyle == null)
            return null;

        var property = _parentStyle.GetProperty(propertyName);
        return property?.RawValue;
    }

    /// <summary>
    /// Safely resolves a var() reference with error handling.
    /// </summary>
    /// <param name="varValue">The var() function to resolve</param>
    /// <returns>The resolved value, or null if resolution failed</returns>
    public ICssValue? SafeResolveVarReference(CssVarValue varValue)
    {
        try
        {
            return ResolveVarReference(varValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resolving var() reference: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clears the variable resolution cache.
    /// </summary>
    public void ClearCache()
    {
        _resolverContext.ClearCache();
    }
}