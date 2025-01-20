namespace AngleSharp.Renderer
{
    using System.Collections.Generic;
    using AngleSharp.Dom;

    /// <summary>
    /// Represents a render node.
    /// </summary>
    public interface IRenderNode
    {
        /// <summary>
        /// References the original DOM node.
        /// </summary>
        INode Ref { get; }

        /// <summary>
        /// References the contained render children.
        /// </summary>
        IEnumerable<IRenderNode> Children { get; }
    }
}