namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Collections.Generic;
using Css;
using Css.Dom;
using Dom;

/// <summary>
/// Computes final CSS values by resolving relative units and handling special values.
/// </summary>
public class ValueComputer
{
    private readonly IRenderDevice _device;
    private readonly IBrowsingContext _context;

    /// <summary>
    /// Creates a new ValueComputer.
    /// </summary>
    /// <param name="device">The render device used for viewport-relative units.</param>
    /// <param name="context">The browsing context for CSS operations.</param>
    public ValueComputer(IRenderDevice device, IBrowsingContext context)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Computes absolute values for all properties in the provided style declaration.
    /// </summary>
    /// <param name="declaration">The cascaded style declaration.</param>
    /// <param name="element">The element being styled.</param>
    /// <param name="parentStyle">The parent element's computed style, if available.</param>
    /// <returns>A new style declaration with computed values.</returns>
    public ICssStyleDeclaration ComputeValues(
        ICssStyleDeclaration declaration,
        IElement element,
        ICssStyleDeclaration? parentStyle = null)
    {
        // 1. Compute font-size first as other properties may depend on it
        var rootFontSize = GetRootFontSize(element);
        var parentFontSize = GetParentFontSize(parentStyle, rootFontSize);
        var elementFontSize = ComputeFontSize(declaration, parentFontSize, rootFontSize);

        // 2. Compute all other properties using the computed font-size
        var computedProperties = ComputeAllProperties(declaration, element,
            elementFontSize, rootFontSize, parentStyle);

        computedStyle.SetDeclarations(computedProperties);
        return computedStyle;
    }

    // Additional methods for specific value types...
}