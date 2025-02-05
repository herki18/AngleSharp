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
        public LayoutBox? Layout { get; set; }
        public IRenderNode? Parent { get; set; }
    }

    /// <summary>
    /// The final (used) layout geometry for an element or text node.
    /// </summary>
    public class LayoutBox
    {
        // Global position
        public float X { get; set; }
        public float Y { get; set; }

        // Size of the entire box (including padding and borders)
        public float BoxWidth { get; set; }
        public float BoxHeight { get; set; }

        // Relative position (e.g., offset within parent's coordinate system)
        public float RelativeX { get; set; }
        public float RelativeY { get; set; }

        // Box model metrics
        public float MarginTop { get; set; }
        public float MarginRight { get; set; }
        public float MarginBottom { get; set; }
        public float MarginLeft { get; set; }

        public float BorderTop { get; set; }
        public float BorderRight { get; set; }
        public float BorderBottom { get; set; }
        public float BorderLeft { get; set; }

        public float PaddingTop { get; set; }
        public float PaddingRight { get; set; }
        public float PaddingBottom { get; set; }
        public float PaddingLeft { get; set; }

        // Derived measurements
        public float ContentWidth => BoxWidth - (PaddingLeft + PaddingRight + BorderLeft + BorderRight);
        public float ContentHeight => BoxHeight - (PaddingTop + PaddingBottom + BorderTop + BorderBottom);

        public LayoutBox(
            float x, float y,
            float width, float height)
        {
            X = x;
            Y = y;
            BoxWidth = width;
            BoxHeight = height;
        }

        public override string ToString()
        {
            return $"LayoutBox(Global: ({X}, {Y}), Size: ({BoxWidth} x {BoxHeight}), " +
                   $"Margins: (T:{MarginTop}, R:{MarginRight}, B:{MarginBottom}, L:{MarginLeft}))";
        }
    }
}