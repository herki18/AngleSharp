namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Immutable computed style data holder using ICssStyleDeclaration
/// </summary>
public class ComputedStyle : IComputedStyle
{
    public IElement Element { get; }
    public ICssStyleDeclaration Declaration { get; }

    public ComputedStyle(IElement element, ICssStyleDeclaration declaration)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
    }

    public string GetPropertyValue(string propertyName)
    {
        return Declaration.GetPropertyValue(propertyName) ?? string.Empty;
    }

    public void SetProperty(string propertyName, string value)
    {
        Declaration.SetProperty(propertyName, value);
    }

    public DisplayMode Display
    {
        get
        {
            var displayValue = GetPropertyValue("display");
            return displayValue switch
            {
                "none" => DisplayMode.None,
                "block" => DisplayMode.Block,
                "inline" => DisplayMode.Inline,
                "inline-block" => DisplayMode.InlineBlock,
                "flex" => DisplayMode.Flex,
                "inline-flex" => DisplayMode.InlineFlex,
                "grid" => DisplayMode.Grid,
                "inline-grid" => DisplayMode.InlineGrid,
                "table" => DisplayMode.Table,
                "table-row" => DisplayMode.TableRow,
                "table-cell" => DisplayMode.TableCell,
                "list-item" => DisplayMode.ListItem,
                _ => DisplayMode.Block
            };
        }
    }
}