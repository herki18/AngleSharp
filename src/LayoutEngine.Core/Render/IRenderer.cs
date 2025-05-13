namespace LayoutEngine.Core.Render;

using System.Collections.Generic;
using Commands;

/// <summary>
/// Interface for a renderer that can process render commands
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Executes a list of render commands to update the visual tree
    /// </summary>
    void Execute(IReadOnlyList<IRenderCommand> commands);

    /// <summary>
    /// Gets the root element of the rendered visual tree
    /// </summary>
    object GetRootElement();
}