namespace AngleSharp.StyleSystem.Computation
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;
    using AngleSharp.StyleSystem.Interfaces;

    public class ComputedStyleFactory : IComputedStyleFactory
    {
        private readonly IRenderDevice _renderDevice;
        private readonly IStyleInvalidationTracker _invalidationTracker;
        private readonly IPropertyTreeManager _propertyTreeManager;
        private readonly IDeclarationFactory _declarationFactory;
        private readonly ICssStyleDeclarationFactory _cssStyleDeclarationFactory;
        private readonly Dictionary<string, ComputedStyle> _emptyStylePrototypes = new();

        public ComputedStyleFactory(
            IRenderDevice renderDevice,
            IStyleInvalidationTracker invalidationTracker,
            IPropertyTreeManager propertyTreeManager,
            IDeclarationFactory declarationFactory,
            ICssStyleDeclarationFactory cssStyleDeclarationFactory)
        {
            _renderDevice = renderDevice;
            _invalidationTracker = invalidationTracker;
            _propertyTreeManager = propertyTreeManager;
            _declarationFactory = declarationFactory;
            _cssStyleDeclarationFactory = cssStyleDeclarationFactory;
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
                var sourceNode = sourceStyle.PropertyTreeNode;
                var newNode = _propertyTreeManager.CreateNode(element);
                var properties = sourceNode.GetAllProperties();
                foreach (var prop in properties)
                {
                    newNode.SetProperty(prop.Key, prop.Value);
                }
                var declaration = CreateDeclarationFromPropertyTree(newNode);
                return new ComputedStyle(
                    element,
                    parentStyle,
                    declaration,
                    newNode,
                    _renderDevice,
                    _invalidationTracker,
                    _declarationFactory);
            }
            throw new ArgumentException("Source style must be a ComputedStyle instance", nameof(source));
        }

        public IComputedStyle CreateComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration, IPropertyTreeNode? node = null)
        {
            if (declaration.Length == 0 && parentStyle != null)
            {
                string prototypeKey = element.NodeName;
                if (!_emptyStylePrototypes.TryGetValue(prototypeKey, out var prototype))
                {
                    var parentNode = parentStyle is ComputedStyle parentComputed
                        ? parentComputed.PropertyTreeNode
                        : null;
                    var propertyNode = _propertyTreeManager.GetOrCreateNode(element, parentNode);
                    var emptyDeclaration = _cssStyleDeclarationFactory.Create();
                    prototype = new ComputedStyle(
                        element,
                        parentStyle,
                        emptyDeclaration,
                        propertyNode,
                        _renderDevice,
                        _invalidationTracker,
                        _declarationFactory);
                    _emptyStylePrototypes[prototypeKey] = prototype;
                }
                return CopyComputedStyle(prototype);
            }

            var styleParentNode = parentStyle is ComputedStyle styleParentComputed
                ? styleParentComputed.PropertyTreeNode
                : null;
            node ??= _propertyTreeManager.GetOrCreateNode(element, styleParentNode);

            if (node.GetPropertyCount() > 0)
            {
                var computedDeclaration = CreateDeclarationFromPropertyTree(node);
                MergeDeclarations(computedDeclaration, declaration);
                return new ComputedStyle(
                    element,
                    parentStyle,
                    computedDeclaration,
                    node,
                    _renderDevice,
                    _invalidationTracker,
                    _declarationFactory);
            }

            return new ComputedStyle(
                element,
                parentStyle,
                declaration,
                node,
                _renderDevice,
                _invalidationTracker,
                _declarationFactory);
        }

        private ICssStyleDeclaration CreateDeclarationFromPropertyTree(IPropertyTreeNode node)
        {
            var declaration = _cssStyleDeclarationFactory.Create();
            var properties = node.GetAllProperties();
            foreach (var property in properties)
            {
                var propertyName = property.Key;
                var value = property.Value;
                if (value != null)
                {
                    declaration.SetProperty(propertyName, value.CssText);
                }
            }
            return declaration;
        }

        private void MergeDeclarations(ICssStyleDeclaration target, ICssStyleDeclaration source)
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
}