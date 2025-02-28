#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.LayoutEngine
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;

    public sealed class ElementNode : IRenderNode
    {
        public ElementNode(IElement reference, IEnumerable<IRenderNode> children, ICssStyleDeclaration specifiedStyle, ICssStyleDeclaration? computedStyle)
        {
            Ref = reference;
            Children = children;
            SpecifiedStyle = specifiedStyle;
            ComputedStyle = computedStyle;
            Id = reference.Id;
        }

        public String? Id { get; }

        public IElement Ref { get; }

        INode IRenderNode.Ref => Ref;

        public IEnumerable<IRenderNode> Children { get; }

        public IRenderNode? Parent { get; set; }

        public ICssStyleDeclaration SpecifiedStyle { get; }

        public ICssStyleDeclaration? ComputedStyle { get; }

        public LayoutBox? Layout { get; set; }
    }

    public class NonRenderableNode : IRenderNode
    {
        public NonRenderableNode(INode @ref, IEnumerable<IRenderNode> children)
        {
            Ref = @ref;
            Children = children;
        }

        public String? Id => null;

        public INode Ref { get; }
        public IEnumerable<IRenderNode> Children { get; }
        public LayoutBox? Layout { get; set; }
        public IRenderNode? Parent { get; set; }
    }
}