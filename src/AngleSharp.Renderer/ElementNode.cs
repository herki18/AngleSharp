#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Renderer
{
    using System;
    using System.Collections.Generic;
    using Css.Dom;
    using Dom;

    public sealed class ElementNode : IRenderNode
    {
        public ElementNode(IElement reference, IEnumerable<IRenderNode> children, ICssStyleDeclaration specifiedStyle, ICssStyleDeclaration? computedStyle)
        {
            Ref = reference;
            Children = children;
            SpecifiedStyle = specifiedStyle;
            ComputedStyle = computedStyle;
        }

        public IElement Ref { get; }

        INode IRenderNode.Ref => Ref;

        public IEnumerable<IRenderNode> Children { get; }

        public IRenderNode? Parent { get; set; }

        public ICssStyleDeclaration SpecifiedStyle { get; }

        public ICssStyleDeclaration? ComputedStyle { get; }

        public LayoutBox? Layout { get; set; }

        public IRenderElement? Wrapper { get; set; } // This is a Unity-specific field
    }

    public class NonRenderableNode : IRenderNode
    {
        public NonRenderableNode(INode @ref, IEnumerable<IRenderNode> children)
        {
            Ref = @ref;
            Children = children;
        }

        public INode Ref { get; }
        public IEnumerable<IRenderNode> Children { get; }
        public IRenderNode? Parent { get; set; }
    }

    /// <summary>
    /// The final (used) layout geometry for an element or text node.
    /// </summary>
    public sealed class LayoutBox
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public LayoutBox(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public override String ToString()
        {
            return $"LayoutBox(X: {X}, Y: {Y}, Width: {Width}, Height: {Height})";
        }
    }
}