namespace LayoutEngine.Core.Layout;

using System.Collections.Generic;
using AngleSharp.Dom;

public class LayoutResult : ILayoutResult
{
    private readonly Dictionary<IElement, ILayoutInfo> _layoutInfo = new();
    private ILayoutFragment _rootFragment = null!;

    public ILayoutFragment RootFragment => _rootFragment;

    public void SetRootFragment(ILayoutFragment rootFragment)
    {
        _rootFragment = rootFragment;
    }

    public ILayoutInfo? GetLayoutInfo(IElement element)
    {
        _layoutInfo.TryGetValue(element, out var info);
        return info;
    }

    public void SetLayoutInfo(IElement element, ILayoutInfo info)
    {
        _layoutInfo[element] = info;
    }

    public void AddFragment(IElement element, ILayoutFragment fragment)
    {
        if (!_layoutInfo.TryGetValue(element, out var info))
        {
            info = new LayoutInfo();
            _layoutInfo[element] = info;
        }

        if (info is LayoutInfo layoutInfo)
        {
            layoutInfo.AddFragment(fragment);
        }
    }
}