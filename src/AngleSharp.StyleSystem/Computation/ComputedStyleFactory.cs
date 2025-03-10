namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using Css.Dom;
using Dom;
using Interfaces;

/// <summary>
/// Factory for creating computed style objects.
/// </summary>
public class ComputedStyleFactory : IComputedStyleFactory
{
    private readonly StyleEngine _engine;
    private readonly Dictionary<string, ComputedStyle> _emptyStylePrototypes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedStyleFactory"/> class.
    /// </summary>
    /// <param name="engine">The style engine.</param>
    public ComputedStyleFactory(StyleEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Creates a new computed style.
    /// </summary>
    /// <returns>A new computed style.</returns>
    public IComputedStyle CreateComputedStyle()
    {
        throw new InvalidOperationException("Cannot create a computed style without context");
    }

    /// <summary>
    /// Copies a computed style.
    /// </summary>
    /// <param name="source">The source style.</param>
    /// <returns>A copy of the computed style.</returns>
    public IComputedStyle CopyComputedStyle(IComputedStyle source)
    {
        if (source is ComputedStyle sourceStyle)
        {
            // For optimal sharing, we should create a new property tree node
            // that references the same shared nodes from the source
            var element = sourceStyle._element;
            var parentStyle = sourceStyle._parentStyle;
            var declaration = new CssStyleDeclaration(_engine.Context);

            // Copy all properties from the source style's declaration
            foreach (var property in sourceStyle.Declaration)
            {
                declaration.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : string.Empty);
            }

            // Create a property tree node that shares with the source
            var sourceNode = sourceStyle.PropertyTreeNode;
            var newNode = _engine.PropertyTreeManager.CreateNode(element);

            // Share all properties from the source node
            var properties = sourceNode.GetAllProperties();
            foreach (var prop in properties)
            {
                newNode.SetProperty(prop.Key, prop.Value);
            }

            // Create a new computed style with the shared property tree
            return new ComputedStyle(
                element,
                parentStyle,
                declaration,
                newNode,
                _engine.RenderDevice,
                _engine.InvalidationTracker,
                _engine.Context);
        }

        throw new ArgumentException("Source style must be a ComputedStyle instance", nameof(source));
    }

    /// <summary>
    /// Creates a computed style for an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="parentStyle">The parent style.</param>
    /// <param name="declaration">The CSS declaration.</param>
    /// <returns>A new computed style.</returns>
    internal IComputedStyle CreateComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration)
    {
        // Check if this is an empty declaration and we can reuse a prototype
        if (declaration.Length == 0 && parentStyle != null)
        {
            string prototypeKey = element.NodeName;
            if (!_emptyStylePrototypes.TryGetValue(prototypeKey, out var prototype))
            {
                // Create a prototype for this element type with empty styles
                var parentNode = parentStyle is ComputedStyle parentComputed
                    ? parentComputed.PropertyTreeNode
                    : null;

                var propertyNode = _engine.PropertyTreeManager.GetOrCreateNode(element, parentNode);

                prototype = new ComputedStyle(
                    element,
                    parentStyle,
                    declaration,
                    propertyNode,
                    _engine.RenderDevice,
                    _engine.InvalidationTracker,
                    _engine.Context);

                _emptyStylePrototypes[prototypeKey] = prototype;
            }

            // Clone the prototype but with the specific element
            // This reuses the property tree node which is optimized for sharing
            return CopyComputedStyle(prototype);
        }

        // For non-empty styles, create a new optimized property tree
        var styleParentNode = parentStyle is ComputedStyle styleParentComputed
            ? styleParentComputed.PropertyTreeNode
            : null;

        var node = _engine.PropertyTreeManager.GetOrCreateNode(element, styleParentNode);

        return new ComputedStyle(
            element,
            parentStyle,
            declaration,
            node,
            _engine.RenderDevice,
            _engine.InvalidationTracker,
            _engine.Context);
    }
}