using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Layout;

namespace LayoutEngine.Core.Render;

using Commands;

/// <summary>
/// Manages the rendering process by generating and executing render commands.
/// </summary>
public class RenderSystem : IRenderSystem
{
    private readonly IEventAggregator _eventAggregator;
    private readonly HashSet<IElement> _elementsNeedingRender = new();
    private readonly FragmentRegistry _fragmentRegistry = new();
    private IRenderer? _renderer;

    public RenderSystem(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated);
    }

    /// <summary>
    /// Processes a fragment tree and generates corresponding render commands.
    /// </summary>
    public IReadOnlyList<IRenderCommand> ProcessFragmentTree(IFragmentTree fragmentTree)
    {
        var commands = new List<IRenderCommand>();
        var processedIds = new HashSet<string>();

        // Process the root fragment and all its children recursively
        ProcessFragmentHierarchy(fragmentTree.RootFragment, commands, processedIds);

        // Handle cleanup of fragments that no longer exist
        HandleRemovedFragments(processedIds, commands);

        // Clear invalidated state
        _elementsNeedingRender.Clear();

        // Execute commands if renderer is attached
        _renderer?.Execute(commands);

        // Notify completion
        _eventAggregator.Publish(new RenderCompletedEvent());

        return commands;
    }

    /// <summary>
    /// Process a fragment and all its children to generate render commands.
    /// </summary>
    private void ProcessFragmentHierarchy(
        ILayoutFragment fragment,
        List<IRenderCommand> commands,
        HashSet<string> processedIds)
    {
        // Register fragment and determine if it's new
        var fragmentStatus = _fragmentRegistry.RegisterFragment(fragment);
        processedIds.Add(fragmentStatus.Id);

        // Generate appropriate commands based on fragment status
        GenerateFragmentCommands(fragment, fragmentStatus, commands);

        // Process all children recursively
        foreach (var child in fragment.Children)
        {
            ProcessFragmentHierarchy(child, commands, processedIds);
        }
    }

    /// <summary>
    /// Generate all commands needed for a specific fragment.
    /// </summary>
    private void GenerateFragmentCommands(
        ILayoutFragment fragment,
        FragmentStatus fragmentStatus,
        List<IRenderCommand> commands)
    {
        // If this is a new fragment, create it
        if (fragmentStatus.IsNew)
        {
            string elementType = DetermineElementType(fragment);
            commands.Add(new CreateElementCommand(fragment, elementType));
        }

        // Always update layout
        commands.Add(new SetLayoutCommand(fragment, fragment.Bounds));

        // Add all visual property commands
        foreach (var command in GenerateVisualPropertyCommands(fragment))
        {
            commands.Add(command);
        }
    }

    /// <summary>
    /// Generate commands to handle any removed fragments.
    /// </summary>
    private void HandleRemovedFragments(HashSet<string> processedIds, List<IRenderCommand> commands)
    {
        // Get IDs that were previously registered but not seen in this process
        var removedIds = _fragmentRegistry.GetRemovedFragmentIds(processedIds);

        // Clean up registry and generate delete commands if needed
        foreach (var id in removedIds)
        {
            // If we have a renderer and want to generate deletion commands, we would do it here
            // commands.Add(new DeleteElementCommand(id));

            _fragmentRegistry.UnregisterFragmentById(id);
        }
    }

    /// <summary>
    /// Determine the type of element to create based on the fragment.
    /// </summary>
    private string DetermineElementType(ILayoutFragment fragment)
    {
        if (fragment.Element == null)
        {
            return "container";
        }

        switch (fragment.Element.TagName?.ToUpperInvariant())
        {
            case "DIV":
                return "container";
            case "SPAN":
                return "text";
            case "IMG":
                return "image";
            case "INPUT":
                var type = fragment.Element.GetAttribute("type") ?? "text";
                return $"input-{type}";
            case "BUTTON":
                return "button";
            default:
                return "container";
        }
    }

    /// <summary>
    /// Generate commands for all visual properties of a fragment.
    /// </summary>
    private IEnumerable<IRenderCommand> GenerateVisualPropertyCommands(ILayoutFragment fragment)
    {
        var commands = new List<IRenderCommand>();
        var props = fragment.VisualProperties;

        // Background color
        if (!string.IsNullOrEmpty(props.BackgroundColor) && props.BackgroundColor != "transparent")
        {
            commands.Add(new SetPropertyCommand(fragment, "backgroundColor", props.BackgroundColor));
        }

        // Border properties
        if (props.BorderTopWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderTopWidth", props.BorderTopWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderTopColor", props.BorderTopColor));
        }

        if (props.BorderRightWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderRightWidth", props.BorderRightWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderRightColor", props.BorderRightColor));
        }

        if (props.BorderBottomWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderBottomWidth", props.BorderBottomWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderBottomColor", props.BorderBottomColor));
        }

        if (props.BorderLeftWidth > 0)
        {
            commands.Add(new SetPropertyCommand(fragment, "borderLeftWidth", props.BorderLeftWidth));
            commands.Add(new SetPropertyCommand(fragment, "borderLeftColor", props.BorderLeftColor));
        }

        // Text and font properties
        commands.Add(new SetPropertyCommand(fragment, "color", props.Color));
        commands.Add(new SetPropertyCommand(fragment, "fontSize", props.FontSize));

        if (!string.IsNullOrEmpty(props.FontFamily))
        {
            commands.Add(new SetPropertyCommand(fragment, "fontFamily", props.FontFamily));
        }

        if (!string.IsNullOrEmpty(props.FontWeight))
        {
            commands.Add(new SetPropertyCommand(fragment, "fontWeight", props.FontWeight));
        }

        // Text content
        if (fragment.Element?.NodeType == (int)NodeType.Text)
        {
            commands.Add(new SetPropertyCommand(fragment, "textContent", fragment.Element.TextContent));
        }

        return commands;
    }

    /// <summary>
    /// Invalidates rendering for the specified element.
    /// </summary>
    public void InvalidateRender(IElement element, bool recursive = true)
    {
        var affectedElements = new List<IElement>();
        _elementsNeedingRender.Add(element);
        affectedElements.Add(element);

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateRenderRecursive(child, affectedElements);
            }
        }

        _eventAggregator.Publish(new RenderInvalidatedEvent(affectedElements));
    }

    /// <summary>
    /// Recursively invalidates rendering for an element and its children.
    /// </summary>
    private void InvalidateRenderRecursive(IElement element, List<IElement> affectedElements)
    {
        _elementsNeedingRender.Add(element);
        affectedElements.Add(element);

        foreach (var child in element.Children.OfType<IElement>())
        {
            InvalidateRenderRecursive(child, affectedElements);
        }
    }

    /// <summary>
    /// Checks if an element needs rendering.
    /// </summary>
    public bool NeedsRender(IElement element)
    {
        return _elementsNeedingRender.Contains(element);
    }

    /// <summary>
    /// Attaches a renderer to the render system.
    /// </summary>
    public void AttachRenderer(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Gets the attached renderer.
    /// </summary>
    public IRenderer GetRenderer()
    {
        return _renderer ?? throw new InvalidOperationException("No renderer attached to the render system.");
    }

    /// <summary>
    /// Handles layout invalidation events by invalidating rendering.
    /// </summary>
    private void OnLayoutInvalidated(LayoutInvalidatedEvent @event)
    {
        foreach (var element in @event.Elements)
        {
            InvalidateRender(element, false);
        }
    }
}