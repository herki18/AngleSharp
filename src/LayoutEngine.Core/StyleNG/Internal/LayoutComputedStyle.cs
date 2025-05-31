// namespace LayoutEngine.Core.LayoutStyle.Internal;
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using LayoutEngine.Core.LayoutNG.Public;
// using LayoutEngine.Core.LayoutStyle.Public;
// using AngleSharp.Css.Dom;
//
// /// <summary>
// /// Computed style associated with a layout object.
// /// </summary>
// public class LayoutComputedStyle : ILayoutComputedStyle
// {
//     private readonly ICssStyleDeclaration _declaration;
//     private string? _cachedDisplay;
//     private string? _cachedPosition;
//     private bool? _cachedCreatesBlockFormattingContext;
//     private bool? _cachedCreatesStackingContext;
//
//     public LayoutComputedStyle(
//         ILayoutObject layoutObject,
//         ICssStyleDeclaration declaration,
//         bool isSynthesized)
//     {
//         LayoutObject = layoutObject ?? throw new ArgumentNullException(nameof(layoutObject));
//         _declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
//         IsSynthesized = isSynthesized;
//     }
//
//     public ILayoutObject LayoutObject { get; }
//
//     public ICssStyleDeclaration Declaration => _declaration;
//
//     public bool IsSynthesized { get; }
//
//     public string Display
//     {
//         get
//         {
//             if (_cachedDisplay == null)
//             {
//                 _cachedDisplay = GetPropertyValue("display");
//                 if (string.IsNullOrEmpty(_cachedDisplay))
//                 {
//                     _cachedDisplay = "inline"; // CSS initial value
//                 }
//             }
//             return _cachedDisplay;
//         }
//     }
//
//     public string Position
//     {
//         get
//         {
//             if (_cachedPosition == null)
//             {
//                 _cachedPosition = GetPropertyValue("position");
//                 if (string.IsNullOrEmpty(_cachedPosition))
//                 {
//                     _cachedPosition = "static"; // CSS initial value
//                 }
//             }
//             return _cachedPosition;
//         }
//     }
//
//     public bool CreatesBlockFormattingContext
//     {
//         get
//         {
//             if (_cachedCreatesBlockFormattingContext == null)
//             {
//                 _cachedCreatesBlockFormattingContext = CalculateCreatesBlockFormattingContext();
//             }
//             return _cachedCreatesBlockFormattingContext.Value;
//         }
//     }
//
//     public bool CreatesStackingContext
//     {
//         get
//         {
//             if (_cachedCreatesStackingContext == null)
//             {
//                 _cachedCreatesStackingContext = CalculateCreatesStackingContext();
//             }
//             return _cachedCreatesStackingContext.Value;
//         }
//     }
//
//     public string GetPropertyValue(string propertyName)
//     {
//         return _declaration.GetPropertyValue(propertyName) ?? string.Empty;
//     }
//
//     public void SetProperty(string propertyName, string value)
//     {
//         _declaration.SetProperty(propertyName, value);
//
//         // Invalidate cached values if relevant properties change
//         switch (propertyName)
//         {
//             case "display":
//                 _cachedDisplay = null;
//                 _cachedCreatesBlockFormattingContext = null;
//                 break;
//             case "position":
//                 _cachedPosition = null;
//                 _cachedCreatesBlockFormattingContext = null;
//                 _cachedCreatesStackingContext = null;
//                 break;
//             case "float":
//             case "overflow":
//             case "contain":
//                 _cachedCreatesBlockFormattingContext = null;
//                 break;
//             case "opacity":
//             case "transform":
//             case "filter":
//             case "z-index":
//                 _cachedCreatesStackingContext = null;
//                 break;
//         }
//     }
//
//     public ILayoutComputedStyle Clone()
//     {
//         // Create a new declaration with all properties copied
//         var newDeclaration = new CssStyleDeclaration();
//
//         foreach (var property in _declaration)
//         {
//             newDeclaration.SetProperty(
//                 property.Name,
//                 property.Value,
//                 property.IsImportant ? "important" : null);
//         }
//
//         return new LayoutComputedStyle(LayoutObject, newDeclaration, IsSynthesized);
//     }
//
//     private bool CalculateCreatesBlockFormattingContext()
//     {
//         // According to CSS spec, a box creates a BFC if:
//
//         // 1. It's the root element
//         if (LayoutObject.Parent == null)
//             return true;
//
//         // 2. It's a float
//         var float_ = GetPropertyValue("float");
//         if (float_ != "none" && !string.IsNullOrEmpty(float_))
//             return true;
//
//         // 3. It's absolutely positioned
//         if (Position == "absolute" || Position == "fixed")
//             return true;
//
//         // 4. It has overflow other than visible
//         var overflow = GetPropertyValue("overflow");
//         if (!string.IsNullOrEmpty(overflow) && overflow != "visible")
//             return true;
//
//         // 5. It's a table cell or table caption
//         if (Display == "table-cell" || Display == "table-caption")
//             return true;
//
//         // 6. It's a table, inline-table, table-row, table-row-group, etc.
//         if (Display.StartsWith("table"))
//             return true;
//
//         // 7. It's a block container that contains block-level children
//         if (Display == "inline-block" || Display == "flow-root")
//             return true;
//
//         // 8. It's a flex or grid container
//         if (Display == "flex" || Display == "inline-flex" ||
//             Display == "grid" || Display == "inline-grid")
//             return true;
//
//         // 9. It has contain: layout, content, or paint
//         var contain = GetPropertyValue("contain");
//         if (!string.IsNullOrEmpty(contain))
//         {
//             var containValues = contain.Split(' ');
//             if (containValues.Contains("layout") ||
//                 containValues.Contains("content") ||
//                 containValues.Contains("paint"))
//                 return true;
//         }
//
//         return false;
//     }
//
//     private bool CalculateCreatesStackingContext()
//     {
//         // According to CSS spec, an element creates a stacking context if:
//
//         // 1. It's the root element
//         if (LayoutObject.Parent == null)
//             return true;
//
//         // 2. It's positioned (absolute or relative) with z-index other than auto
//         if (Position == "absolute" || Position == "relative" || Position == "fixed" || Position == "sticky")
//         {
//             var zIndex = GetPropertyValue("z-index");
//             if (!string.IsNullOrEmpty(zIndex) && zIndex != "auto")
//                 return true;
//         }
//
//         // 3. It's a flex/grid item with z-index other than auto
//         if (LayoutObject.Parent?.Style != null)
//         {
//             var parentDisplay = LayoutObject.Parent.Style.GetPropertyValue("display");
//             if (parentDisplay == "flex" || parentDisplay == "inline-flex" ||
//                 parentDisplay == "grid" || parentDisplay == "inline-grid")
//             {
//                 var zIndex = GetPropertyValue("z-index");
//                 if (!string.IsNullOrEmpty(zIndex) && zIndex != "auto")
//                     return true;
//             }
//         }
//
//         // 4. It has opacity less than 1
//         var opacity = GetPropertyValue("opacity");
//         if (!string.IsNullOrEmpty(opacity) && opacity != "1")
//         {
//             if (float.TryParse(opacity, out var opacityValue) && opacityValue < 1)
//                 return true;
//         }
//
//         // 5. It has a transform other than none
//         var transform = GetPropertyValue("transform");
//         if (!string.IsNullOrEmpty(transform) && transform != "none")
//             return true;
//
//         // 6. It has a filter other than none
//         var filter = GetPropertyValue("filter");
//         if (!string.IsNullOrEmpty(filter) && filter != "none")
//             return true;
//
//         // 7. It has isolation: isolate
//         var isolation = GetPropertyValue("isolation");
//         if (isolation == "isolate")
//             return true;
//
//         // 8. It has will-change with specific values
//         var willChange = GetPropertyValue("will-change");
//         if (!string.IsNullOrEmpty(willChange) && willChange != "auto")
//         {
//             var willChangeValues = willChange.Split(',').Select(v => v.Trim());
//             if (willChangeValues.Any(v => v == "transform" || v == "opacity" || v == "filter"))
//                 return true;
//         }
//
//         return false;
//     }
// }