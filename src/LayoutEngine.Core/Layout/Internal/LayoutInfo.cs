namespace LayoutEngine.Core.Layout.Internal;

using System.Collections.Generic;
using LayoutEngine.Core.Layout.Public;

public class LayoutInfo : ILayoutInfo
{
    private readonly List<ILayoutFragment> _fragments = new();

    public Rect ContentRect { get; set; }
    public Rect PaddingRect { get; set; }
    public Rect BorderRect { get; set; }
    public Rect MarginRect { get; set; }
    public Point Position { get; set; }

    public IReadOnlyList<ILayoutFragment> Fragments => _fragments;

    public void AddFragment(ILayoutFragment fragment)
    {
        _fragments.Add(fragment);
    }
}