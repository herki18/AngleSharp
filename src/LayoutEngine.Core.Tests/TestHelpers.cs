namespace LayoutEngine.Core.Tests;
using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Style;
using NSubstitute;
using Style.Public;

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

        SetupInvalidationFlags(element);

        return element;
    }

    private static void SetupInvalidationFlags(IElement element)
    {
        // Set up default return values
        element.NeedsStyleRecalc().Returns(false);
        element.ChildNeedsStyleRecalc().Returns(false);
        element.NeedsLayout().Returns(false);
        element.ChildNeedsLayout().Returns(false);
        element.NeedsPaintInvalidation().Returns(false);
        element.HasAnyInvalidation().Returns(false);

        // Set up behavior: SetNeedsLayout should also call SetNeedsPaintInvalidation
        element.When(x => x.SetNeedsLayout()).Do(callInfo =>
        {
            // When layout is invalidated, paint should also be invalidated
            element.SetNeedsPaintInvalidation();
        });

        // Set up other flag behaviors
        element.When(x => x.SetNeedsStyleRecalc()).Do(_ => { });
        element.When(x => x.ClearNeedsStyleRecalc()).Do(_ => { });
        element.When(x => x.ClearNeedsLayout()).Do(_ => { });
        element.When(x => x.SetNeedsPaintInvalidation()).Do(_ => { });
        element.When(x => x.ClearNeedsPaintInvalidation()).Do(_ => { });
        element.When(x => x.ClearAllInvalidation()).Do(_ => { });
    }

    public static IDocument CreateMockDocument(IElement? documentElement = null)
    {
        var document = Substitute.For<IDocument>();
        var docElement = documentElement ?? CreateMockElement("html");
        document.DocumentElement.Returns(docElement);
        return document;
    }

    public static IComputedStyle CreateMockComputedStyle(IElement element, string display = "block")
    {
        var style = Substitute.For<IComputedStyle>();
        style.Element.Returns(element);
        style.GetPropertyValue(Arg.Any<string>()).Returns(string.Empty);
        style.GetPropertyValue("display").Returns(display);
        return style;
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

    public static void SetupElementInvalidationFlags(IElement element,
        bool needsStyle = false,
        bool childNeedsStyle = false,
        bool needsLayout = false,
        bool childNeedsLayout = false,
        bool needsPaint = false)
    {
        element.NeedsStyleRecalc().Returns(needsStyle);
        element.ChildNeedsStyleRecalc().Returns(childNeedsStyle);
        element.NeedsLayout().Returns(needsLayout);
        element.ChildNeedsLayout().Returns(childNeedsLayout);
        element.NeedsPaintInvalidation().Returns(needsPaint);

        bool hasAny = needsStyle || childNeedsStyle || needsLayout || childNeedsLayout || needsPaint;
        element.HasAnyInvalidation().Returns(hasAny);
    }

    public static IElement CreateMockElementWithChildren(string tagName, params IElement[] children)
    {
        var element = CreateMockElement(tagName);
        var htmlCollection = new TestHtmlCollection(children);
        element.Children.Returns(htmlCollection);

        foreach (var child in children)
        {
            child.Parent.Returns(element);
        }

        return element;
    }

    public static void VerifyInvalidationFlagsSet(IElement element,
        bool shouldHaveSetStyle = false,
        bool shouldHaveSetLayout = false,
        bool shouldHaveSetPaint = false)
    {
        if (shouldHaveSetStyle)
        {
            element.Received(1).SetNeedsStyleRecalc();
        }
        else
        {
            element.DidNotReceive().SetNeedsStyleRecalc();
        }

        if (shouldHaveSetLayout)
        {
            element.Received(1).SetNeedsLayout();
        }
        else
        {
            element.DidNotReceive().SetNeedsLayout();
        }

        if (shouldHaveSetPaint)
        {
            element.Received(1).SetNeedsPaintInvalidation();
        }
        else
        {
            element.DidNotReceive().SetNeedsPaintInvalidation();
        }
    }

    public static void VerifyInvalidationFlagsCleared(IElement element,
        bool shouldHaveClearedStyle = false,
        bool shouldHaveClearedLayout = false,
        bool shouldHaveClearedPaint = false)
    {
        if (shouldHaveClearedStyle)
        {
            element.Received(1).ClearNeedsStyleRecalc();
        }
        else
        {
            element.DidNotReceive().ClearNeedsStyleRecalc();
        }

        if (shouldHaveClearedLayout)
        {
            element.Received(1).ClearNeedsLayout();
        }
        else
        {
            element.DidNotReceive().ClearNeedsLayout();
        }

        if (shouldHaveClearedPaint)
        {
            element.Received(1).ClearNeedsPaintInvalidation();
        }
        else
        {
            element.DidNotReceive().ClearNeedsPaintInvalidation();
        }
    }
}