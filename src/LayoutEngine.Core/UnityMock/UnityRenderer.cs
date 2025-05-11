// namespace LayoutEngine.Core.UnityMock;
//
// using System;
// using System.Collections.Generic;
// using System.Net.Mime;
// using System.Reflection.Emit;
// using AngleSharp.Css.Dom;
// using AngleSharp.Dom;
// using Layout;
// using Render;
//
// /// <summary>
// /// Implementation of IRenderer that uses Unity UI Toolkit
// /// </summary>
// public class UnityRenderer : IRenderer
// {
//     private readonly VisualElement _rootElement;
//     private readonly Dictionary<ILayoutFragment, VisualElement> _fragmentToElement = new Dictionary<ILayoutFragment, VisualElement>();
//
//     public UnityRenderer(VisualElement rootElement)
//     {
//         _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
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
//     public object GetRootElement()
//     {
//         return _rootElement;
//     }
//
//     private void ExecuteCommand(IRenderCommand command)
//     {
//         switch (command.CommandType)
//         {
//             case RenderCommandType.Create:
//                 ExecuteCreateCommand(command as CreateElementCommand);
//                 break;
//
//             case RenderCommandType.SetProperty:
//                 ExecuteSetPropertyCommand(command as SetPropertyCommand);
//                 break;
//
//             case RenderCommandType.SetLayout:
//                 ExecuteSetLayoutCommand(command as SetLayoutCommand);
//                 break;
//
//             case RenderCommandType.Delete:
//                 // Implement deletion logic
//                 break;
//
//             case RenderCommandType.SetChildren:
//                 // Implement children updating logic
//                 break;
//
//             default:
//                 Debug.LogWarning($"Unsupported command type: {command.CommandType}");
//                 break;
//         }
//     }
//
//     private void ExecuteCreateCommand(CreateElementCommand command)
//     {
//         if (command == null) return;
//
//         // Create the appropriate Unity UI Toolkit element based on the element type
//         VisualElement element = command.ElementType switch
//         {
//             "container" => new VisualElement(),
//             "text" => new Label(),
//             "image" => new MediaTypeNames.Image(),
//             "input-text" => new TextField(),
//             "button" => new Button(),
//             _ => new VisualElement()
//         };
//
//         // Set default styles
//         element.style.position = Position.Absolute;
//
//         // Store the element for later use
//         _fragmentToElement[command.Fragment] = element;
//
//         // Find the parent element
//         VisualElement parent = _rootElement;
//         if (command.Fragment.Element?.ParentElement != null)
//         {
//             var parentFragment = GetFragmentForElement(command.Fragment.Element.ParentElement);
//             if (parentFragment != null && _fragmentToElement.TryGetValue(parentFragment, out var parentElement))
//             {
//                 parent = parentElement;
//             }
//         }
//
//         // Add the element to the parent
//         parent.Add(element);
//     }
//
//     private void ExecuteSetPropertyCommand(SetPropertyCommand command)
//     {
//         if (command == null) return;
//
//         if (!_fragmentToElement.TryGetValue(command.Fragment, out var element))
//         {
//             return;
//         }
//
//         switch (command.PropertyName)
//         {
//             case "backgroundColor":
//                 element.style.backgroundColor = ParseColor(command.PropertyValue.ToString());
//                 break;
//
//             case "color":
//                 if (element is Label label)
//                 {
//                     label.style.color = ParseColor(command.PropertyValue.ToString());
//                 }
//                 break;
//
//             case "fontSize":
//                 if (element is TextElement textElement)
//                 {
//                     textElement.style.fontSize = ParseLength(command.PropertyValue);
//                 }
//                 break;
//
//             case "fontFamily":
//                 if (element is TextElement textElement2)
//                 {
//                     // Handle font family - you might need to load fonts from Resources
//                     var fontName = command.PropertyValue.ToString();
//                     var font = Resources.Load<Font>(fontName);
//                     if (font != null)
//                     {
//                         textElement2.style.unityFont = font;
//                     }
//                 }
//                 break;
//
//             case "fontWeight":
//                 if (element is TextElement textElement3)
//                 {
//                     var fontWeight = command.PropertyValue.ToString();
//                     textElement3.style.unityFontStyleAndWeight = fontWeight == "bold"
//                         ? FontStyle.Bold
//                         : FontStyle.Normal;
//                 }
//                 break;
//
//             case "borderTopWidth":
//                 element.style.borderTopWidth = ParseLength(command.PropertyValue);
//                 break;
//
//             case "borderRightWidth":
//                 element.style.borderRightWidth = ParseLength(command.PropertyValue);
//                 break;
//
//             case "borderBottomWidth":
//                 element.style.borderBottomWidth = ParseLength(command.PropertyValue);
//                 break;
//
//             case "borderLeftWidth":
//                 element.style.borderLeftWidth = ParseLength(command.PropertyValue);
//                 break;
//
//             case "borderTopColor":
//                 element.style.borderTopColor = ParseColor(command.PropertyValue.ToString());
//                 break;
//
//             case "borderRightColor":
//                 element.style.borderRightColor = ParseColor(command.PropertyValue.ToString());
//                 break;
//
//             case "borderBottomColor":
//                 element.style.borderBottomColor = ParseColor(command.PropertyValue.ToString());
//                 break;
//
//             case "borderLeftColor":
//                 element.style.borderLeftColor = ParseColor(command.PropertyValue.ToString());
//                 break;
//
//             case "textContent":
//                 if (element is Label label2)
//                 {
//                     label2.text = command.PropertyValue.ToString();
//                 }
//                 else if (element is Button button)
//                 {
//                     button.text = command.PropertyValue.ToString();
//                 }
//                 break;
//         }
//     }
//
//     private void ExecuteSetLayoutCommand(SetLayoutCommand command)
//     {
//         if (command == null) return;
//
//         if (!_fragmentToElement.TryGetValue(command.Fragment, out var element))
//         {
//             return;
//         }
//
//         // Set position and size
//         element.style.left = command.Bounds.X;
//         element.style.top = command.Bounds.Y;
//         element.style.width = command.Bounds.Width;
//         element.style.height = command.Bounds.Height;
//     }
//
//     private ILayoutFragment GetFragmentForElement(IElement element)
//     {
//         foreach (var kvp in _fragmentToElement)
//         {
//             if (kvp.Key.Element == element)
//             {
//                 return kvp.Key;
//             }
//         }
//
//         return null;
//     }
//
//     private Color ParseColor(string colorString)
//     {
//         if (ColorUtility.TryParseHtmlString(colorString, out var color))
//         {
//             return color;
//         }
//
//         // Handle named colors
//         return colorString.ToLowerInvariant() switch
//         {
//             "black" => Color.black,
//             "white" => Color.white,
//             "red" => Color.red,
//             "green" => Color.green,
//             "blue" => Color.blue,
//             "transparent" => new Color(0, 0, 0, 0),
//             _ => Color.black
//         };
//     }
//
//     private float ParseLength(object value)
//     {
//         if (value is float floatValue)
//         {
//             return floatValue;
//         }
//
//         if (value is int intValue)
//         {
//             return intValue;
//         }
//
//         if (float.TryParse(value.ToString(), out var result))
//         {
//             return result;
//         }
//
//         return 0;
//     }
// }