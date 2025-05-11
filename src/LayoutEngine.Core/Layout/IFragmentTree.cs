namespace LayoutEngine.Core.Layout;

using System.Collections.Generic;
using AngleSharp.Dom;

public interface IFragmentTree
{
    // Root fragment of the tree
    ILayoutFragment RootFragment { get; }

    // Find fragments for a specific element
    IReadOnlyList<ILayoutFragment> FindFragmentsForElement(IElement element);
}