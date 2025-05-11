namespace LayoutEngine.Core.Render;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using Layout;
using LayoutInvalidatedEvent = Events.LayoutInvalidatedEvent;

/// <summary>
/// Interface for render commands - these are platform-agnostic
/// instructions that will be implemented by the specific UI toolkit
/// </summary>
public interface IRenderCommand
{
    /// <summary>
    /// The fragment this command is associated with
    /// </summary>
    ILayoutFragment Fragment { get; }

    /// <summary>
    /// The type of render command
    /// </summary>
    RenderCommandType CommandType { get; }
}

/// <summary>
/// Enumeration of render command types
/// </summary>
public enum RenderCommandType
{
    Create,
    Update,
    Delete,
    SetProperty,
    SetChildren,
    SetText,
    SetImage,
    SetLayout
}

/// <summary>
/// Create command for creating a new visual element
/// </summary>
public class CreateElementCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.Create;
    public string ElementType { get; }

    public CreateElementCommand(ILayoutFragment fragment, string elementType)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
    }
}

/// <summary>
/// Command for setting properties on a visual element
/// </summary>
public class SetPropertyCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.SetProperty;
    public string PropertyName { get; }
    public object PropertyValue { get; }

    public SetPropertyCommand(ILayoutFragment fragment, string propertyName, object propertyValue)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        PropertyValue = propertyValue;
    }
}

/// <summary>
/// Command for setting the layout properties of a visual element
/// </summary>
public class SetLayoutCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.SetLayout;
    public Rect Bounds { get; }

    public SetLayoutCommand(ILayoutFragment fragment, Rect bounds)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        Bounds = bounds;
    }
}

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

/// <summary>
/// Default implementation of the render system
/// </summary>
public class RenderSystem : IRenderSystem
{
    private readonly IEventAggregator _eventAggregator;
    private readonly HashSet<IElement> _elementsNeedingRender = new();
    private readonly Dictionary<ILayoutFragment, string> _fragmentIdMap = new();
    private IRenderer? _renderer;

    /// <summary>
    /// Initializes a new instance of the RenderSystem
    /// </summary>
    public RenderSystem(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Subscribe to layout invalidation events
        _eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated);
    }

    /// <summary>
    /// Processes a fragment tree and generates render commands
    /// </summary>
    public IReadOnlyList<IRenderCommand> ProcessFragmentTree(IFragmentTree fragmentTree)
    {
        var commands = new List<IRenderCommand>();
        var processedIds = new HashSet<string>();

        // Process the root fragment and all its descendants
        ProcessFragment(fragmentTree.RootFragment, commands, processedIds);

        // Clean up any fragments that are no longer in the tree
        CleanupRemovedFragments(processedIds, commands);

        // Clear invalidated elements
        _elementsNeedingRender.Clear();

        // If we have a renderer, execute the commands
        _renderer?.Execute(commands);

        // Signal that rendering is complete
        _eventAggregator.Publish(new RenderCompletedEvent());

        return commands;
    }

    /// <summary>
    /// Processes a single fragment and generates render commands for it
    /// </summary>
    private void ProcessFragment(ILayoutFragment fragment, List<IRenderCommand> commands, HashSet<string> processedIds)
    {
        // Get or create a unique ID for this fragment
        var fragmentId = GetOrCreateFragmentId(fragment);
        processedIds.Add(fragmentId);

        // Determine what kind of element to create based on the fragment
        string elementType = DetermineElementType(fragment);

        // Add create command if this is a new fragment
        if (!_fragmentIdMap.ContainsKey(fragment))
        {
            commands.Add(new CreateElementCommand(fragment, elementType));
        }

        // Add layout command
        commands.Add(new SetLayoutCommand(fragment, fragment.Bounds));

        // Add visual property commands
        foreach (var prop in GetVisualPropertyCommands(fragment))
        {
            commands.Add(prop);
        }

        // Process children
        foreach (var child in fragment.Children)
        {
            ProcessFragment(child, commands, processedIds);
        }
    }

    /// <summary>
    /// Determines the element type based on the fragment
    /// </summary>
    private string DetermineElementType(ILayoutFragment fragment)
    {
        if (fragment.Element == null)
        {
            return "container";
        }

        // Determine element type based on tag name or display property
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
    /// Generates visual property commands for a fragment
    /// </summary>
    private IEnumerable<IRenderCommand> GetVisualPropertyCommands(ILayoutFragment fragment)
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

        // Text properties
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

        // Text content for text nodes
        if (fragment.Element?.NodeType == (int)NodeType.Text)
        {
            commands.Add(new SetPropertyCommand(fragment, "textContent", fragment.Element.TextContent));
        }

        return commands;
    }

    /// <summary>
    /// Gets or creates a unique ID for a fragment
    /// </summary>
    private string GetOrCreateFragmentId(ILayoutFragment fragment)
    {
        if (_fragmentIdMap.TryGetValue(fragment, out var id))
        {
            return id;
        }

        id = Guid.NewGuid().ToString();
        _fragmentIdMap[fragment] = id;
        return id;
    }

    /// <summary>
    /// Cleans up any fragments that are no longer in the tree
    /// </summary>
    private void CleanupRemovedFragments(HashSet<string> processedIds, List<IRenderCommand> commands)
    {
        var toRemove = new List<ILayoutFragment>();

        foreach (var kvp in _fragmentIdMap)
        {
            if (!processedIds.Contains(kvp.Value))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var fragment in toRemove)
        {
            _fragmentIdMap.Remove(fragment);
        }
    }

    /// <summary>
    /// Marks an element as needing to be re-rendered
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
    /// Recursively marks an element and its descendants as needing to be re-rendered
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
    /// Checks if an element needs to be re-rendered
    /// </summary>
    public bool NeedsRender(IElement element)
    {
        return _elementsNeedingRender.Contains(element);
    }

    /// <summary>
    /// Attaches a renderer to the render system
    /// </summary>
    public void AttachRenderer(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Gets the attached renderer
    /// </summary>
    public IRenderer GetRenderer()
    {
        return _renderer ?? throw new InvalidOperationException("No renderer attached to the render system.");
    }

    /// <summary>
    /// Handles layout invalidation events
    /// </summary>
    private void OnLayoutInvalidated(LayoutInvalidatedEvent @event)
    {
        foreach (var element in @event.Elements)
        {
            InvalidateRender(element, false);
        }
    }
}

/// <summary>
/// Event fired when rendering is completed
/// </summary>
public class RenderCompletedEvent : EventBase
{
}

/// <summary>
/// Event fired when rendering is invalidated
/// </summary>
public class RenderInvalidatedEvent : EventBase
{
    public IReadOnlyList<IElement> AffectedElements { get; }

    public RenderInvalidatedEvent(IReadOnlyList<IElement> affectedElements)
    {
        AffectedElements = affectedElements ?? throw new ArgumentNullException(nameof(affectedElements));
    }
}