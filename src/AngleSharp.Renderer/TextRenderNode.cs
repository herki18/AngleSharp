namespace AngleSharp.Renderer
{
    using System.Collections.Generic;
    using System.Linq;
    using AngleSharp.Dom;

    sealed class TextRenderNode : IRenderNode
    {
        public TextRenderNode(INode reference)
        {
            Ref = reference;
        }

        public INode Ref { get; }

        public IEnumerable<IRenderNode> Children => Enumerable.Empty<IRenderNode>();

        public IRenderNode? Parent { get; set; }
        public LayoutBox? Layout { get; set; }
    }
}