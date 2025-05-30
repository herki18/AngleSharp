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

        // Generate commands for this fragment (create element, set properties, etc.)
        commands.AddRange(_commandGenerator.GenerateFragmentCommands(fragment, fragmentStatus));

        // Check if this fragment creates a viewport
        var fragmentViewport = _viewportManager.GetViewportForElement(fragment.Element);

        if (fragmentViewport != null)
        {
            // This element creates a viewport
            commands.Add(new CreateViewportCommand(fragmentViewport));
            commands.Add(new SetScrollOffsetCommand(fragmentViewport.Id, fragmentViewport.ScrollOffset));

            // Process children within this viewport
            foreach (var child in SortedChildren(fragment))
                ProcessFragmentWithViewports(child, fragmentViewport, commands, processedIds);

            commands.Add(new PopViewportCommand(fragmentViewport.Id));
        }
        else if (fragment.Children.Any())
        {
            // This element has children but doesn't create a viewport
            // Push this element as a container for its children
            commands.Add(new PushContainerCommand(fragment));

            // Process children within this container
            foreach (var child in SortedChildren(fragment))
                ProcessFragmentWithViewports(child, viewport, commands, processedIds);

            // Pop back to parent container
            commands.Add(new PopContainerCommand(fragment));
        }
        // If no children and no viewport, element is just added to current container
    }

    private IEnumerable<ILayoutFragment> SortedChildren(ILayoutFragment fragment)
    {
        return fragment.Children.OrderBy(f => f.VisualProperties.ZIndex);
    }
}