namespace AngleSharp.StyleSystem.Computation;
using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Integration;
using Interfaces;
using Storage;

public class ComputedStyleFactory : IComputedStyleFactory
{
    private readonly StyleEngine _engine;
    private readonly Dictionary<string, ComputedStyle> _emptyStylePrototypes = new();

    public ComputedStyleFactory(StyleEngine engine)
    {
        _engine = engine;
    }

    public IComputedStyle CreateComputedStyle()
    {
        throw new InvalidOperationException("Cannot create a computed style without context");
    }

    public IComputedStyle CopyComputedStyle(IComputedStyle source)
    {
        if (source is ComputedStyle sourceStyle)
        {
            var element = sourceStyle._element;
            var parentStyle = sourceStyle._parentStyle;

            // Create a new property tree node based on the source node
            var sourceNode = sourceStyle.PropertyTreeNode;
            var newNode = _engine.PropertyTreeManager.CreateNode(element);

            // Copy all properties from the source node to the new node
            var properties = sourceNode.GetAllProperties();
            foreach (var prop in properties)
            {
                newNode.SetProperty(prop.Key, prop.Value);
            }

            // Create a declaration that reflects the computed values
            var declaration = CreateDeclarationFromPropertyTree(newNode);

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

    internal IComputedStyle CreateComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration, IPropertyTreeNode? node = null)
    {
        // Handle empty style case with style sharing optimization
        if (declaration.Length == 0 && parentStyle != null)
        {
            string prototypeKey = element.NodeName;
            if (!_emptyStylePrototypes.TryGetValue(prototypeKey, out var prototype))
            {
                var parentNode = parentStyle is ComputedStyle parentComputed
                    ? parentComputed.PropertyTreeNode
                    : null;
                var propertyNode = _engine.PropertyTreeManager.GetOrCreateNode(element, parentNode);

                // Create a new empty declaration
                var emptyDeclaration = new CssStyleDeclaration(_engine.Context);

                prototype = new ComputedStyle(
                    element,
                    parentStyle,
                    emptyDeclaration,
                    propertyNode,
                    _engine.RenderDevice,
                    _engine.InvalidationTracker,
                    _engine.Context);

                _emptyStylePrototypes[prototypeKey] = prototype;
            }

            return CopyComputedStyle(prototype);
        }

        // Get the parent node for inheritance
        var styleParentNode = parentStyle is ComputedStyle styleParentComputed
            ? styleParentComputed.PropertyTreeNode
            : null;

        // Get or create the property tree node
        node ??= _engine.PropertyTreeManager.GetOrCreateNode(element, styleParentNode);

        // If we've already processed styles for this node (via the ComputedStyleBuilder),
        // create a declaration that reflects those computed values
        if (node.GetPropertyCount() > 0)
        {
            // Create a declaration from the property tree to ensure consistency
            var computedDeclaration = CreateDeclarationFromPropertyTree(node);

            // Merge with the original declaration to preserve any properties not in the node
            MergeDeclarations(computedDeclaration, declaration);

            return new ComputedStyle(
                element,
                parentStyle,
                computedDeclaration,
                node,
                _engine.RenderDevice,
                _engine.InvalidationTracker,
                _engine.Context);
        }

        // Otherwise, use the original declaration
        return new ComputedStyle(
            element,
            parentStyle,
            declaration,
            node,
            _engine.RenderDevice,
            _engine.InvalidationTracker,
            _engine.Context);
    }

    // Create a declaration that reflects the values in the property tree
    private CssStyleDeclaration CreateDeclarationFromPropertyTree(IPropertyTreeNode node)
    {
        var declaration = new CssStyleDeclaration(_engine.Context);
        var properties = node.GetAllProperties();

        foreach (var property in properties)
        {
            var propertyName = property.Key;
            var value = property.Value;

            // Set the property in the declaration
            if (value != null)
            {
                declaration.SetProperty(propertyName, value.CssText);
            }
        }

        return declaration;
    }

    // Merge declarations to preserve properties not in the computed declaration
    private void MergeDeclarations(CssStyleDeclaration target, ICssStyleDeclaration source)
    {
        foreach (var property in source)
        {
            if (string.IsNullOrEmpty(target.GetPropertyValue(property.Name)))
            {
                target.SetProperty(
                    property.Name,
                    property.Value,
                    property.IsImportant ? "important" : null);
            }
        }
    }
}