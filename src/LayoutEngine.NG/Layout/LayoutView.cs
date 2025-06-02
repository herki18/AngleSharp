namespace LayoutEngine.NG.Layout;

using AngleSharp.Dom;
using Core;

/// <summary>
/// The root of the layout tree, representing the document's viewport.
/// In LayoutNG, LayoutView is always the root layout object and handles
/// the initial containing block and viewport-related calculations.
/// Following Blink's actual hierarchy, LayoutView inherits from LayoutBlockFlow.
/// </summary>
public class LayoutView : LayoutBlockFlow
{
    /// <summary>
    /// The viewport width.
    /// In LayoutNG, this is the initial containing block width.
    /// </summary>
    public float ViewportWidth { get; set; }

    /// <summary>
    /// The viewport height.
    /// In LayoutNG, this is the initial containing block height.
    /// </summary>
    public float ViewportHeight { get; set; }

    /// <summary>
    /// The document this view is rendering.
    /// In LayoutNG, the LayoutView is associated with a document.
    /// </summary>
    public IDocument? Document { get; set; }

    /// <summary>
    /// Whether the document is in quirks mode.
    /// In LayoutNG, this affects various layout calculations.
    /// </summary>
    public bool InQuirksMode { get; set; }

    /// <summary>
    /// The current scroll offset.
    /// In LayoutNG, the view tracks the document's scroll position.
    /// </summary>
    public PhysicalOffset ScrollOffset { get; set; }

    /// <summary>
    /// The layout overflow (scrollable area).
    /// In LayoutNG, this is the total area that can be scrolled.
    /// </summary>
    public PhysicalSize LayoutOverflowSize { get; set; }

    /// <summary>
    /// Whether layout is currently being performed.
    /// In LayoutNG, prevents re-entrancy during layout.
    /// </summary>
    public bool IsInLayout { get; set; }

    /// <summary>
    /// The frame that owns this view.
    /// In LayoutNG, each frame has one LayoutView.
    /// </summary>
    public LocalFrame? OwnerFrame { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.View;
    }

    /// <summary>
    /// The view always establishes the initial containing block.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        return true;
    }

    /// <summary>
    /// Updates the viewport size.
    /// In LayoutNG, this triggers relayout when the viewport changes.
    /// </summary>
    public void UpdateViewportSize(float width, float height)
    {
        if (ViewportWidth != width || ViewportHeight != height)
        {
            ViewportWidth = width;
            ViewportHeight = height;

            // Update the view's own dimensions
            ContentSize = new PhysicalSize(width, height);

            // Mark for layout
            NeedsLayout = true;
        }
    }

    /// <summary>
    /// Performs layout starting from this LayoutView.
    /// In LayoutNG, this is UpdateLayout() or similar.
    /// This is the main entry point for layout calculation.
    /// </summary>
    public void Layout()
    {
        if (!NeedsLayout)
            return;

        if (IsInLayout)
        {
            // Prevent re-entrancy
            return;
        }

        IsInLayout = true;
        try
        {
            // Set the view's own size from viewport
            ContentSize = new PhysicalSize(ViewportWidth, ViewportHeight);
            Location = PhysicalOffset.Zero;

            // Create initial constraint space
            var constraintSpace = new LayoutConstraintSpace
            {
                AvailableWidth = ViewportWidth,
                AvailableHeight = ViewportHeight,
                IsFixedWidth = true,
                IsFixedHeight = true,
                IsNewFormattingContext = true
            };

            // Layout children
            LayoutChildren(constraintSpace);

            // Clear the needs layout flag
            NeedsLayout = false;

            // Update overflow after layout
            UpdateLayoutOverflow();

            // Create fragment for this view
            Fragment = CreateFragment();
        }
        finally
        {
            IsInLayout = false;
        }
    }

    /// <summary>
    /// Layouts the children of this view.
    /// </summary>
    private void LayoutChildren(LayoutConstraintSpace constraintSpace)
    {
        float currentY = 0;
        var child = FirstChild;

        while (child != null)
        {
            if (child is LayoutBox box)
            {
                // Simplified block layout
                // In real LayoutNG, this would use the appropriate layout algorithm

                // Set position
                box.Location = new PhysicalOffset(0, currentY);

                // Layout the child
                if (box is LayoutBlockFlow blockFlow)
                {
                    blockFlow.LayoutBlock(constraintSpace);
                }
                else
                {
                    // Simple box layout
                    box.ContentSize = new PhysicalSize(
                        constraintSpace.AvailableWidth,
                        100 // Default height
                    );
                }

                // Update current Y position
                currentY += box.BorderBoxSize.Height;

                // Clear needs layout
                box.NeedsLayout = false;

                // Create fragment for child
                box.Fragment = box.CreateFragment();
            }

            child = child.NextSibling;
        }
    }

    /// <summary>
    /// Gets the initial containing block size.
    /// In LayoutNG, this is used for percentage calculations on the root element.
    /// </summary>
    public PhysicalSize GetInitialContainingBlockSize()
    {
        return new PhysicalSize(ViewportWidth, ViewportHeight);
    }

    /// <summary>
    /// Computes the scrollable overflow area.
    /// In LayoutNG, this determines the scrollbar presence and range.
    /// </summary>
    public void UpdateLayoutOverflow()
    {
        // Start with viewport size
        LayoutOverflowSize = new PhysicalSize(ViewportWidth, ViewportHeight);

        // Compute union of all child overflow rectangles
        var child = FirstChild;
        while (child != null)
        {
            if (child.Fragment != null)
            {
                var childBounds = child.Fragment;
                LayoutOverflowSize = new PhysicalSize(
                    System.Math.Max(LayoutOverflowSize.Width,
                        childBounds.Offset.Left + childBounds.Size.Width),
                    System.Math.Max(LayoutOverflowSize.Height,
                        childBounds.Offset.Top + childBounds.Size.Height)
                );
            }
            child = child.NextSibling;
        }
    }

    /// <summary>
    /// Sets the scroll offset.
    /// In LayoutNG, scrolling doesn't require layout but may require paint.
    /// </summary>
    public void SetScrollOffset(float x, float y)
    {
        var newOffset = new PhysicalOffset(x, y);
        if (ScrollOffset != newOffset)
        {
            ScrollOffset = newOffset;
            // In real LayoutNG, this would trigger paint invalidation
            NeedsPaint = true;
        }
    }
}

/// <summary>
/// Represents layout constraints passed during layout.
/// In LayoutNG, this is NGConstraintSpace.
/// </summary>
public class LayoutConstraintSpace
{
    public float AvailableWidth { get; set; }
    public float AvailableHeight { get; set; }
    public bool IsFixedWidth { get; set; }
    public bool IsFixedHeight { get; set; }
    public bool IsNewFormattingContext { get; set; }

    // Simplified - in real LayoutNG this would have many more properties
}