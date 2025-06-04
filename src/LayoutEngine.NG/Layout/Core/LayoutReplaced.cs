namespace LayoutEngine.NG.Layout.Core;

/// <summary>
/// Represents a replaced element in the layout tree.
/// In LayoutNG, replaced elements are those that have intrinsic dimensions,
/// such as images, videos, iframes, etc.
/// </summary>
public class LayoutReplaced : LayoutBox
{
    public override bool IsLayoutReplaced() => true;

    /// <summary>
    /// Computes the natural sizing information for this replaced element.
    /// </summary>
    public virtual PhysicalNaturalSizingInfo ComputeNaturalSizingInfo()
    {
        // Default implementation - derived classes should override
        return new PhysicalNaturalSizingInfo
        {
            Size = new PhysicalSize(300, 150), // Default replaced element size
            HasWidth = true,
            HasHeight = true,
            AspectRatio = 2.0f // 300:150 = 2:1
        };
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box; // Replaced elements are boxes
    }
}

/// <summary>
/// Natural sizing information for replaced elements.
/// In LayoutNG, this encapsulates intrinsic dimensions and aspect ratio.
/// </summary>
public struct PhysicalNaturalSizingInfo
{
    /// <summary>
    /// The natural size of the replaced element.
    /// </summary>
    public PhysicalSize Size { get; set; }

    /// <summary>
    /// Whether the element has a natural width.
    /// </summary>
    public bool HasWidth { get; set; }

    /// <summary>
    /// Whether the element has a natural height.
    /// </summary>
    public bool HasHeight { get; set; }

    /// <summary>
    /// The aspect ratio (width/height) if applicable.
    /// </summary>
    public float AspectRatio { get; set; }
}