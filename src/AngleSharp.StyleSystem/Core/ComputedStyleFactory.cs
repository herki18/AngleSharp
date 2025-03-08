namespace AngleSharp.StyleSystem.Core;

using System;
using Css.Dom;
using Dom;
using Interfaces;

/// <summary>
/// Factory for creating computed style objects.
/// </summary>
public class ComputedStyleFactory : IComputedStyleFactory
{
    private readonly StyleEngine _engine;

    public ComputedStyleFactory(StyleEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Creates a new computed style object.
    /// </summary>
    public IComputedStyle CreateComputedStyle()
    {
        // In practice, you always need an element and style declaration to create a computed style
        throw new InvalidOperationException("Cannot create a computed style without context");
    }

    /// <summary>
    /// Creates a computed style by copying another.
    /// </summary>
    public IComputedStyle CopyComputedStyle(IComputedStyle source)
    {
        // In a real implementation, we would create a deep copy
        throw new NotImplementedException("Copying computed styles is not implemented yet");
    }

    internal IComputedStyle CreateComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration)
    {
        var parentNode = parentStyle is ComputedStyle parentComputed
            ? parentComputed.PropertyTreeNode
            : null;

        var propertyNode = _engine.PropertyTreeManager.GetOrCreateNode(element, parentNode);

        // Pass the render device to the ComputedStyle constructor
        return new ComputedStyle(element, parentStyle, declaration, propertyNode, _engine.RenderDevice, _engine.InvalidationTracker);
    }
}