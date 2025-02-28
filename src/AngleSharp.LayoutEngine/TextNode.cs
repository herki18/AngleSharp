namespace AngleSharp.LayoutEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AngleSharp.Dom;

    public sealed class TextNode : IRenderNode
    {
        public TextNode(INode reference)
        {
            Ref = reference;
        }

        public String? Id => null;

        public INode Ref { get; }

        public IEnumerable<IRenderNode> Children => Enumerable.Empty<IRenderNode>();

        public IRenderNode? Parent { get; set; }
        public LayoutBox? Layout { get; set; }
    }
}