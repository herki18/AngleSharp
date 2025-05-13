// using System;
// using System.Collections.Generic;
// using System.Numerics;
// using Infrastructure.EventAggregator.API.Aggregation;
// using LayoutEngine.Core.Layout;
// using LayoutEngine.Core.Render;
// using LayoutEngine.Core.Render.Commands;
// using LayoutEngine.Core.Viewport;
//
// namespace LayoutEngine.Core.UnityMock;
//
// public class UnityViewportRenderer : IRenderer
// {
//     private readonly VisualElement _rootElement;
//     private readonly IEventAggregator _eventAggregator;
//     private readonly Dictionary<ILayoutFragment, VisualElement> _fragmentToElement = new();
//     private readonly Dictionary<string, ScrollView> _viewportScrollViews = new();
//     private readonly Stack<VisualElement> _contentContainerStack = new();
//
//     public UnityViewportRenderer(VisualElement rootElement, IEventAggregator eventAggregator)
//     {
//         _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
//         _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
//         _contentContainerStack.Push(_rootElement);
//     }
//
//     public void Execute(IReadOnlyList<IRenderCommand> commands)
//     {
//         foreach (var command in commands)
//         {
//             ExecuteCommand(command);
//         }
//     }
//
//     private void ExecuteCommand(IRenderCommand command)
//     {
//         switch (command.CommandType)
//         {
//             case RenderCommandType.Create:
//                 ExecuteCreateCommand(command as CreateElementCommand);
//                 break;
//             case RenderCommandType.SetProperty:
//                 ExecuteSetPropertyCommand(command as SetPropertyCommand);
//                 break;
//             case RenderCommandType.SetLayout:
//                 ExecuteSetLayoutCommand(command as SetLayoutCommand);
//                 break;
//             case RenderCommandType.CreateViewport:
//                 ExecuteCreateViewportCommand(command as CreateViewportCommand);
//                 break;
//             case RenderCommandType.PopViewport:
//                 ExecutePopViewportCommand(command as PopViewportCommand);
//                 break;
//             case RenderCommandType.SetScrollOffset:
//                 ExecuteSetScrollOffsetCommand(command as SetScrollOffsetCommand);
//                 break;
//             // Handle other commands...
//         }
//     }
//
//     private void ExecuteCreateViewportCommand(CreateViewportCommand command)
//     {
//         if (command == null) return;
//
//         var viewport = command.Viewport;
//
//         // Create a new scroll view for this viewport
//         var scrollView = new ScrollView();
//
//         // Configure the scroll view based on viewport properties
//         scrollView.style.width = new StyleLength(viewport.ViewportRect.Width);
//         scrollView.style.height = new StyleLength(viewport.ViewportRect.Height);
//         scrollView.style.position = Position.Absolute;
//         scrollView.style.left = viewport.ViewportRect.X;
//         scrollView.style.top = viewport.ViewportRect.Y;
//
//         // Set scroll directions
//         scrollView.horizontalScrollerVisibility = viewport.CanScrollHorizontally
//             ? ScrollerVisibility.Auto
//             : ScrollerVisibility.Hidden;
//
//         scrollView.verticalScrollerVisibility = viewport.CanScrollVertically
//             ? ScrollerVisibility.Auto
//             : ScrollerVisibility.Hidden;
//
//         // Set content size
//         var contentContainer = new VisualElement();
//         contentContainer.style.width = viewport.ContentSize.Width;
//         contentContainer.style.height = viewport.ContentSize.Height;
//         contentContainer.style.position = Position.Relative;
//
//         scrollView.contentContainer.Add(contentContainer);
//
//         // Add to current content container
//         var currentContainer = _contentContainerStack.Peek();
//         currentContainer.Add(scrollView);
//
//         // Register for scroll events
//         scrollView.RegisterCallback<ScrollEvent>(evt => {
//             var viewportId = viewport.Id;
//             var newOffset = new Point(scrollView.scrollOffset.x, scrollView.scrollOffset.y);
//             _eventAggregator.Publish(new UnityScrollEvent(viewportId, newOffset));
//         });
//
//         // Store for later reference
//         _viewportScrollViews[viewport.Id] = scrollView;
//         _contentContainerStack.Push(contentContainer);
//
//         // Apply optimizations
//         OptimizeScrollView(scrollView);
//     }
//
//     private void ExecutePopViewportCommand(PopViewportCommand command)
//     {
//         if (command == null || _contentContainerStack.Count <= 1) return;
//
//         _contentContainerStack.Pop();
//     }
//
//     private void ExecuteSetScrollOffsetCommand(SetScrollOffsetCommand command)
//     {
//         if (command == null) return;
//
//         if (_viewportScrollViews.TryGetValue(command.ViewportId, out var scrollView))
//         {
//             // Set scroll position with or without animation
//             var viewport = _viewportManager.GetViewport(command.ViewportId);
//             bool useSmooth = viewport?.UseSmoothScrolling ?? false;
//
//             if (useSmooth)
//             {
//                 scrollView.smoothScrollTo(new Vector2(command.ScrollOffset.X, command.ScrollOffset.Y));
//             }
//             else
//             {
//                 scrollView.scrollOffset = new Vector2(command.ScrollOffset.X, command.ScrollOffset.Y);
//             }
//         }
//     }
//
//     // Other execute methods (CreateElement, SetProperty, SetLayout) remain mostly the same
//     // but may need adjustments to work with the content container stack
//
//     private void OptimizeScrollView(ScrollView scrollView)
//     {
//         // Enable virtualization for better performance with large content
//         scrollView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
//
//         // Adjust scroll deceleration for more natural feel
//         scrollView.scrollDecelerationRate = 0.135f;
//
//         // Enable elastic scrolling
//         scrollView.elasticity = 0.1f;
//
//         // Configure scroll snapping if available
//         // (UI Toolkit doesn't directly support this, would need custom implementation)
//     }
//
//     public object GetRootElement()
//     {
//         return _rootElement;
//     }
// }