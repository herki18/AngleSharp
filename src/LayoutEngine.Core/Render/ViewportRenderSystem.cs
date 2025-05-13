namespace LayoutEngine.Core.Render;

using System;
using System.Collections.Generic;
using Commands;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Layout;
using Viewport;

public class ViewportRenderSystem : IRenderSystem
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ViewportManager _viewportManager;
    private readonly FragmentRegistry _fragmentRegistry = new();
    private IRenderer? _renderer;

    public ViewportRenderSystem(
        IEventAggregator eventAggregator,
        ViewportManager viewportManager)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _viewportManager = viewportManager ?? throw new ArgumentNullException(nameof(viewportManager));
    }

    public IReadOnlyList<IRenderCommand> ProcessFragmentTree(IFragmentTree fragmentTree)
    {
        var commands = new List<IRenderCommand>();
        var processedIds = new HashSet<string>();

        // Save existing scroll positions before processing
        _viewportManager.SaveAllScrollPositions();

        // Generate viewports from the fragment tree
        var rootViewport = _viewportManager.BuildViewportTree(fragmentTree, null); // Needs styleSystem

        // Process the fragment tree based on viewports
        ProcessFragmentWithViewports(fragmentTree.RootFragment, rootViewport, commands, processedIds);

        // Execute the commands
        _renderer?.Execute(commands);

        _eventAggregator.Publish(new RenderCompletedEvent());
        return commands;
    }

    private void ProcessFragmentWithViewports(
        ILayoutFragment fragment,
        Core.Viewport.Viewport viewport,
        List<IRenderCommand> commands,
        HashSet<string> processedIds)
    {
        // Register this fragment
        var fragmentStatus = _fragmentRegistry.RegisterFragment(fragment);
        processedIds.Add(fragmentStatus.Id);

        // Generate basic commands for this fragment
        GenerateFragmentCommands(fragment, fragmentStatus, commands);

        // Find the viewport for this fragment, if any
        var fragmentViewport = FindViewportForFragment(fragment);

        if (fragmentViewport != null)
        {
            // This fragment is a scroll container - treat specially
            commands.Add(new CreateViewportCommand(fragmentViewport));

            // Apply scroll offset
            commands.Add(new SetScrollOffsetCommand(fragmentViewport.Id, fragmentViewport.ScrollOffset));

            // Process children in the context of this viewport
            foreach (var child in fragment.Children)
            {
                ProcessFragmentWithViewports(child, fragmentViewport, commands, processedIds);
            }

            // Pop back to parent viewport
            commands.Add(new PopViewportCommand(fragmentViewport.Id));
        }
        else
        {
            // Regular fragment - process children normally
            foreach (var child in fragment.Children)
            {
                ProcessFragmentWithViewports(child, viewport, commands, processedIds);
            }
        }
    }

    private Core.Viewport.Viewport? FindViewportForFragment(ILayoutFragment fragment)
    {
        // Find a viewport that corresponds to this fragment's element (if any)
        if (fragment.Element != null)
        {
            return _viewportManager.GetViewportForElement(fragment.Element);
        }
        return null;
    }

    private void GenerateFragmentCommands(
        ILayoutFragment fragment,
        FragmentStatus fragmentStatus,
        List<IRenderCommand> commands)
    {
        if (fragmentStatus.IsNew)
        {
            string elementType = DetermineElementType(fragment);
            commands.Add(new CreateElementCommand(fragment, elementType));
        }

        commands.Add(new SetLayoutCommand(fragment, fragment.Bounds));

        // Property commands remain the same
        foreach (var command in GenerateVisualPropertyCommands(fragment))
        {
            commands.Add(command);
        }
    }

    // Other methods like DetermineElementType and GenerateVisualPropertyCommands
    // remain largely the same as in the original RenderSystem

    // Add these viewport-specific render commands

    public void AttachRenderer(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public IRenderer GetRenderer()
    {
        return _renderer ?? throw new InvalidOperationException("No renderer attached to the render system.");
    }
}