using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Viewport;
using LayoutEngine.Core.Render.Commands;

namespace LayoutEngine.Core.Render;

using Layout.Public;
using Viewport = Viewport.Viewport;

public class RenderTreeWalker
{
    private readonly FragmentRegistry _fragmentRegistry;
    private readonly ViewportManager _viewportManager;
    private readonly RenderCommandGenerator _commandGenerator;

    public RenderTreeWalker(
        FragmentRegistry fragmentRegistry,
        ViewportManager viewportManager,
        RenderCommandGenerator commandGenerator)
    {
        _fragmentRegistry = fragmentRegistry;
        _viewportManager = viewportManager;
        _commandGenerator = commandGenerator;
    }

    public List<IRenderCommand> Walk(IFragmentTree fragmentTree)
    {
        var commands = new List<IRenderCommand>();
        var processedIds = new HashSet<string>();
        var rootViewport = _viewportManager.BuildViewportTree(fragmentTree);

        ProcessFragmentWithViewports(fragmentTree.RootFragment, rootViewport, commands, processedIds);

        // Optionally: handle removed fragments here

        return commands;
    }

    private void ProcessFragmentWithViewports(
        ILayoutFragment fragment,
        Viewport viewport,
        List<IRenderCommand> commands,
        HashSet<string> processedIds)
    {
        var fragmentStatus = _fragmentRegistry.RegisterFragment(fragment);
        processedIds.Add(fragmentStatus.Id);

        commands.AddRange(_commandGenerator.GenerateFragmentCommands(fragment, fragmentStatus));

        var fragmentViewport = _viewportManager.GetViewportForElement(fragment.Element);
        if (fragmentViewport != null)
        {
            commands.Add(new CreateViewportCommand(fragmentViewport));
            commands.Add(new SetScrollOffsetCommand(fragmentViewport.Id, fragmentViewport.ScrollOffset));

            foreach (var child in SortedChildren(fragment))
                ProcessFragmentWithViewports(child, fragmentViewport, commands, processedIds);

            commands.Add(new PopViewportCommand(fragmentViewport.Id));
        }
        else
        {
            foreach (var child in SortedChildren(fragment))
                ProcessFragmentWithViewports(child, viewport, commands, processedIds);
        }
    }

    private IEnumerable<ILayoutFragment> SortedChildren(ILayoutFragment fragment)
    {
        return fragment.Children.OrderBy(f => f.VisualProperties.ZIndex);
    }
}