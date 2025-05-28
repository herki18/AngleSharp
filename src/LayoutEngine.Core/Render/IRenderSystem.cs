namespace LayoutEngine.Core.Render;

using System.Collections.Generic;
using AngleSharp.Dom;
using Commands;
using Layout;
using Layout.Public;

/// <summary>
/// Interface for the render system that generates render commands from a fragment tree
/// </summary>
public interface IRenderSystem
{
    /// <summary>
    /// Processes a fragment tree and generates render commands
    /// </summary>
    IReadOnlyList<IRenderCommand> ProcessFragmentTree(IFragmentTree fragmentTree);

    /// <summary>
    /// Marks an element as needing to be re-rendered
    /// </summary>
    void InvalidateRender(IElement element, bool recursive = true);

    /// <summary>
    /// Checks if an element needs to be re-rendered
    /// </summary>
    bool NeedsRender(IElement element);

    /// <summary>
    /// Attaches a renderer to the render system
    /// </summary>
    void AttachRenderer(IRenderer renderer);

    /// <summary>
    /// Gets the attached renderer
    /// </summary>
    IRenderer GetRenderer();
}