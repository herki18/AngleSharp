namespace AngleSharp.LayoutEngine.DOM;

using System;
using System.Collections.Generic;
using Box;
using Dom;

#pragma warning disable CS1591
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