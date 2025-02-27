namespace AngleSharp.Renderer;

#pragma warning disable CS0219, CS0162
/// <summary>
/// Holds the parent coordinates, available space, or any pass-specific info.
/// You can expand this as needed (e.g. storing pass count or partial reflow flags).
/// </summary>
public struct LayoutContext
{
    public float ParentX;
    public float ParentY;
    public float AvailableWidth;

    public bool IsFirstChild;
    public float ParentMarginBottom;

    public float PreviousMarginBottom; // For margin collapsing, if desired.

    // Sibling margin collapse tracking
    public float PreviousSiblingMarginBottom;
    public bool HasPreviousSibling;
    public float CurrentSiblingMarginTop;

    public float ParentGlobalPositionX;
    public float ParentGlobalPositionY;

    public bool IsCollapsedMarginWithParentTop;
    public float ChildMarginTop;

    public bool IsCollapsedMarginWithParentBottom;
    public float ChildMarginBottom;

    public float EmptyBlockCollapsedMargin;
}