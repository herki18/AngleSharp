namespace LayoutEngine.LayoutSystem;

using System.Collections.Generic;
using AngleSharp.Dom;
using Contracts.LayoutSystem;
using Contracts.StyleSystem;

/// <summary>
/// Mock implementation of ILayoutBox to provide dummy layout data.
/// </summary>
public class LayoutBox : ILayoutBox
{
    private readonly List<ILayoutBox> _children = new List<ILayoutBox>();

    public IElement? Element { get; }
    public BoxType BoxType { get; }
    public IComputedStyle? Style { get; }
    public ILayoutBox? Parent { get; set; }
    public IReadOnlyList<ILayoutBox> Children => _children;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public BoxEdges Margin { get; set; }
    public BoxEdges Border { get; set; }
    public BoxEdges Padding { get; set; }

    public Rect ContentRect => new Rect(X, Y, Width, Height);

    public LayoutBox(IElement? element, BoxType boxType = BoxType.Block, IComputedStyle? style = null)
    {
        Element = element;
        BoxType = boxType;
        Style = style;

        // Default values
        X = 0;
        Y = 0;
        Width = 100;
        Height = 20;
        Margin = new BoxEdges(0);
        Border = new BoxEdges(0);
        Padding = new BoxEdges(0);

        // Customize based on element type
        if (element != null)
        {
            var tagName = element.TagName.ToLowerInvariant();

            switch (tagName)
            {
                case "body":
                    Width = 800;
                    Height = 600;
                    Margin = new BoxEdges(8);
                    Padding = new BoxEdges(8);
                    break;

                case "div":
                    Width = 780;
                    Height = 100;
                    Margin = new BoxEdges(5);

                    // If element has a "container" class, adjust margins
                    if (element.ClassList.Contains("container"))
                    {
                        Margin = new BoxEdges(0, 20, 0, 20); // 0 auto
                        Width = 800;
                        Height = 400;
                    }
                    break;

                case "h1":
                    Width = 760;
                    Height = 40; // Taller for h1
                    Margin = new BoxEdges(10, 0, 16, 0);
                    break;

                case "h2":
                    Width = 760;
                    Height = 30;
                    Margin = new BoxEdges(8, 0, 12, 0);
                    break;

                case "p":
                    Width = 760;
                    Height = 60; // Account for multiple lines
                    Margin = new BoxEdges(0, 0, 16, 0);
                    break;

                case "span":
                    Width = 100; // Width based on content
                    Height = 18;
                    BoxType = BoxType.Inline;
                    break;

                case "a":
                    Width = 100;
                    Height = 18;
                    BoxType = BoxType.Inline;
                    break;

                case "strong":
                case "b":
                    Width = 80;
                    Height = 18;
                    BoxType = BoxType.Inline;
                    break;

                case "img":
                    Width = 200;
                    Height = 150;
                    break;
            }

            // Set width based on parent if available
            if (element.ParentElement != null)
            {
                var parentTag = element.ParentElement.TagName.ToLowerInvariant();
                if (parentTag == "body" && (tagName == "div" || tagName == "p" || tagName.StartsWith("h")))
                {
                    Width = 760; // Adjust for body margin
                }
            }

            // Check for inline styles
            var inlineStyle = element.GetAttribute("style");
            if (!string.IsNullOrEmpty(inlineStyle))
            {
                ApplyInlineStyles(inlineStyle);
            }
        }
    }

    public Rect GetAbsoluteRect()
    {
        var absX = X;
        var absY = Y;

        // Traverse parent chain to calculate absolute position
        var currentParent = Parent;
        while (currentParent != null)
        {
            absX += currentParent.X;
            absY += currentParent.Y;
            currentParent = currentParent.Parent;
        }

        return new Rect(absX, absY, Width, Height);
    }

    public bool ContainsPoint(float x, float y)
    {
        var absoluteRect = GetAbsoluteRect();
        return x >= absoluteRect.X && x <= absoluteRect.X + absoluteRect.Width &&
               y >= absoluteRect.Y && y <= absoluteRect.Y + absoluteRect.Height;
    }

    public void AddChild(LayoutBox child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    private void ApplyInlineStyles(string inlineStyle)
    {
        // Simple inline style parser for common layout properties
        var styles = inlineStyle.Split(';');

        foreach (var styleItem in styles)
        {
            var parts = styleItem.Split(':');
            if (parts.Length != 2)
                continue;

            var property = parts[0].Trim();
            var value = parts[1].Trim();

            if (property == "width" && value.EndsWith("px"))
            {
                if (float.TryParse(value.Replace("px", ""), out float width))
                {
                    Width = width;
                }
            }
            else if (property == "height" && value.EndsWith("px"))
            {
                if (float.TryParse(value.Replace("px", ""), out float height))
                {
                    Height = height;
                }
            }
            else if (property == "margin" && value.EndsWith("px"))
            {
                if (float.TryParse(value.Replace("px", ""), out float margin))
                {
                    Margin = new BoxEdges(margin);
                }
            }
            else if (property == "padding" && value.EndsWith("px"))
            {
                if (float.TryParse(value.Replace("px", ""), out float padding))
                {
                    Padding = new BoxEdges(padding);
                }
            }
        }
    }
}