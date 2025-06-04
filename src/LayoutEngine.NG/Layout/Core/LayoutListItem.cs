namespace LayoutEngine.NG.Layout.Core;

/// <summary>
/// Represents a list item element in the layout tree.
/// In LayoutNG, list items handle marker positioning and numbering.
/// </summary>
public class LayoutListItem : LayoutBlockFlow
{
    private LayoutOutsideListMarker? _marker;

    public override bool IsLayoutListItem() => true;

    /// <summary>
    /// Gets the marker for this list item if it has an outside marker.
    /// </summary>
    public LayoutObject? Marker() => _marker;

    /// <summary>
    /// Sets the marker for this list item.
    /// </summary>
    public void SetMarker(LayoutOutsideListMarker? marker)
    {
        _marker = marker;
        if (marker != null)
        {
            marker.ListItem = this;
        }
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Block;
    }
}

/// <summary>
/// Represents an outside list marker in the layout tree.
/// In LayoutNG, outside markers are positioned outside the list item's content.
/// </summary>
public class LayoutOutsideListMarker : LayoutBlockFlow
{
    /// <summary>
    /// The list item this marker belongs to.
    /// </summary>
    public LayoutListItem? ListItem { get; set; }

    public override bool IsLayoutOutsideListMarker() => true;

    /// <summary>
    /// Whether this marker needs to occupy the whole line.
    /// In LayoutNG, this affects line box construction.
    /// </summary>
    public virtual bool NeedsOccupyWholeLine()
    {
        // Simplified logic - in real LayoutNG this depends on list-style-position
        // and whether the marker would fit on the line
        return false;
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Block;
    }
}