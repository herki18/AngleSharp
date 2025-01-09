#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Renderer
{
    using System.Collections.Generic;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;
    using Css.RenderTree;

    public sealed class ElementRenderNode : IRenderNode
    {
        public ElementRenderNode(IElement reference, IEnumerable<IRenderNode?> children, ICssStyleDeclaration specifiedStyle, ICssStyleDeclaration? computedStyle)
        {
            Ref = reference;
            Children = children;
            SpecifiedStyle = specifiedStyle;
            ComputedStyle = computedStyle;
        }

        public IElement Ref { get; }

        INode IRenderNode.Ref => Ref;

        public IEnumerable<IRenderNode?> Children { get; }

        public IRenderNode? Parent { get; set; }

        public ICssStyleDeclaration SpecifiedStyle { get; }

        public ICssStyleDeclaration? ComputedStyle { get; }
    }
}