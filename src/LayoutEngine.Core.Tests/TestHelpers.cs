namespace LayoutEngine.Core.Tests;

using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Style;
using NSubstitute;

public static class TestHelpers
{
    public static IElement CreateMockElement(string tagName = "div", IElement? parent = null)
    {
        var element = Substitute.For<IElement>();
        element.TagName.Returns(tagName.ToUpperInvariant());
        element.Parent.Returns(parent);

        var children = Substitute.For<IHtmlCollection<IElement>>();
        children.Length.Returns(0);
        element.Children.Returns(children);

        return element;
    }

    public static IDocument CreateMockDocument(IElement? documentElement = null)
    {
        var document = Substitute.For<IDocument>();
        document.DocumentElement.Returns(documentElement ?? CreateMockElement("html"));
        return document;
    }

    public static IComputedStyle CreateMockComputedStyle(IElement element, string display = "block")
    {
        var style = Substitute.For<IComputedStyle>();
        style.Element.Returns(element);
        style.Display.Returns(GetDisplayType(display));

        style.GetValue(Arg.Any<string>()).Returns(string.Empty);
        style.GetValue("display").Returns(display);

        return style;
    }

    private static DisplayType GetDisplayType(string display)
    {
        return display switch
        {
            "block" => DisplayType.Block,
            "flex" => DisplayType.Flex,
            "inline" => DisplayType.Inline,
            "inline-block" => DisplayType.InlineBlock,
            "none" => DisplayType.None,
            _ => DisplayType.Block
        };
    }

    public static ILayoutFragment CreateMockLayoutFragment(IElement? element = null, Rect bounds = default)
    {
        var fragment = Substitute.For<ILayoutFragment>();
        fragment.Element.Returns(element);
        fragment.Bounds.Returns(bounds.Equals(default) ? new Rect(0, 0, 100, 100) : bounds);
        fragment.Children.Returns(new List<ILayoutFragment>());
        fragment.VisualProperties.Returns(new VisualProperties());
        return fragment;
    }

    public static ILayoutInfo CreateMockLayoutInfo(IElement? element = null, Rect contentRect = default)
    {
        var layoutInfo = Substitute.For<ILayoutInfo>();
        layoutInfo.ContentRect.Returns(contentRect.Equals(default) ? new Rect(0, 0, 100, 100) : contentRect);
        layoutInfo.PaddingRect.Returns(new Rect(0, 0, 110, 110));
        layoutInfo.BorderRect.Returns(new Rect(0, 0, 120, 120));
        layoutInfo.MarginRect.Returns(new Rect(0, 0, 130, 130));
        layoutInfo.Position.Returns(new Point(0, 0));

        var fragments = new List<ILayoutFragment>();
        if (element != null)
        {
            var fragment = CreateMockLayoutFragment(element);
            fragments.Add(fragment);
        }

        layoutInfo.Fragments.Returns(fragments);
        return layoutInfo;
    }

    public static IFragmentTree CreateMockFragmentTree(ILayoutFragment? rootFragment = null)
    {
        var fragmentTree = Substitute.For<IFragmentTree>();
        var root = rootFragment ?? CreateMockLayoutFragment();
        fragmentTree.RootFragment.Returns(root);
        fragmentTree.FindFragmentsForElement(Arg.Any<IElement>()).Returns(new List<ILayoutFragment>());
        return fragmentTree;
    }
}

