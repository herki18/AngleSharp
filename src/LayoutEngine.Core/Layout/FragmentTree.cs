namespace LayoutEngine.Core.Layout;

using System.Collections.Generic;
using AngleSharp.Dom;

public class FragmentTree : IFragmentTree
{
    private readonly ILayoutResult _layoutResult;

    public FragmentTree(ILayoutResult layoutResult)
    {
        _layoutResult = layoutResult;
        RootFragment = layoutResult.RootFragment;
    }

    public FragmentTree(ILayoutFragment rootFragment)
    {
        RootFragment = rootFragment;
        _layoutResult = null!;
    }

    public ILayoutFragment RootFragment { get; }

    public IReadOnlyList<ILayoutFragment> FindFragmentsForElement(IElement element)
    {
        if (_layoutResult != null)
        {
            var layoutInfo = _layoutResult.GetLayoutInfo(element);
            if (layoutInfo != null)
            {
                return layoutInfo.Fragments;
            }
        }

        // Fall back to searching the tree
        var fragments = new List<ILayoutFragment>();
        FindFragmentsForElementRecursive(RootFragment, element, fragments);
        return fragments;
    }

    private void FindFragmentsForElementRecursive(ILayoutFragment fragment, IElement element, List<ILayoutFragment> results)
    {
        if (fragment.Element == element)
        {
            results.Add(fragment);
        }

        foreach (var child in fragment.Children)
        {
            FindFragmentsForElementRecursive(child, element, results);
        }
    }
}