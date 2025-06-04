namespace LayoutEngine.NG.Layout.Geometry;

using System.Diagnostics;
using LayoutEngine.NG.Layout;
using LayoutEngine.NG.Layout.Fragments;
using Renderer.Platform.Text;

/// <summary>
/// This class represents CSS property values to convert between logical and
/// physical coordinate systems. See:
/// https://drafts.csswg.org/css-writing-modes-3/#logical-to-physical
/// </summary>
public class WritingModeConverter
{
    private WritingDirectionMode _writingDirection;
    private PhysicalSize _outerSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="WritingModeConverter"/> class.
    /// </summary>
    /// <param name="writingDirection">The writing mode and text direction.</param>
    /// <param name="outerSize">The size of the rect (typically a fragment). Some
    /// combinations of WritingMode and TextDirection require the size of the
    /// container to make the offset relative to the right or the bottom edges.</param>
    public WritingModeConverter(WritingDirectionMode writingDirection, PhysicalSize outerSize)
    {
        _writingDirection = writingDirection;
        _outerSize = outerSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WritingModeConverter"/> class.
    /// </summary>
    /// <param name="writingDirection">The writing mode and text direction.</param>
    /// <param name="outerSize">The logical size of the rect.</param>
    public WritingModeConverter(WritingDirectionMode writingDirection, LogicalSize outerSize)
    {
        _writingDirection = writingDirection;
        _outerSize = ToPhysicalSize(outerSize, writingDirection.GetWritingMode());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WritingModeConverter"/> class
    /// without an outer size. The caller should call SetOuterSize before conversions.
    /// </summary>
    /// <param name="writingDirection">The writing mode and text direction.</param>
    public WritingModeConverter(WritingDirectionMode writingDirection)
    {
        _writingDirection = writingDirection;
        // _outerSize will be default (0,0) or should be set via SetOuterSize
    }

    /// <summary>
    /// Gets the writing direction mode.
    /// </summary>
    public WritingDirectionMode GetWritingDirection() => _writingDirection;

    /// <summary>
    /// Gets the writing mode.
    /// </summary>
    public WritingMode GetWritingMode() => _writingDirection.GetWritingMode();

    /// <summary>
    /// Gets the text direction.
    /// </summary>
    public TextDirection Direction() => _writingDirection.Direction;

    /// <summary>
    /// Gets a value indicating whether the text direction is left-to-right.
    /// </summary>
    public bool IsLtr() => _writingDirection.IsLtr();

    /// <summary>
    /// Gets the outer physical size of the container.
    /// </summary>
    public PhysicalSize OuterSize() => _outerSize;

    /// <summary>
    /// Sets the outer physical size of the container.
    /// </summary>
    /// <param name="outerSize">The new outer physical size.</param>
    public void SetOuterSize(PhysicalSize outerSize)
    {
        _outerSize = outerSize;
    }

    /// <summary>
    /// Converts a physical offset and inner size to a logical offset.
    /// PhysicalOffset will be the physical top left point of the rectangle
    /// described by offset + inner_size. Setting inner_size to 0,0 will return
    /// the same point.
    /// </summary>
    /// <param name="offset">The physical offset.</param>
    /// <param name="innerSize">The physical size of the inner rect (typically a child fragment).</param>
    /// <returns>The logical offset.</returns>
    public LogicalOffset ToLogical(PhysicalOffset offset, PhysicalSize innerSize)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
            return new LogicalOffset(offset.Left, offset.Top);
        return SlowToLogical(offset, innerSize);
    }

    /// <summary>
    /// Converts a logical offset and inner size to a physical offset.
    /// </summary>
    /// <param name="offset">The logical offset.</param>
    /// <param name="innerSize">The physical size of the inner rect (typically a child fragment).</param>
    /// <returns>The physical offset.</returns>
    public PhysicalOffset ToPhysical(LogicalOffset offset, PhysicalSize innerSize)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
            return new PhysicalOffset(offset.InlineOffset, offset.BlockOffset);
        return SlowToPhysical(offset, innerSize);
    }

    /// <summary>
    /// Converts a physical size to a logical size.
    /// </summary>
    /// <param name="size">The physical size.</param>
    /// <returns>The logical size.</returns>
    public LogicalSize ToLogical(PhysicalSize size)
    {
        return ToLogicalSize(size, GetWritingMode());
    }

    /// <summary>
    /// Converts a logical size to a physical size.
    /// </summary>
    /// <param name="size">The logical size.</param>
    /// <returns>The physical size.</returns>
    public PhysicalSize ToPhysical(LogicalSize size)
    {
        return ToPhysicalSize(size, GetWritingMode());
    }

    /// <summary>
    /// Converts a physical rectangle to a logical rectangle.
    /// </summary>
    /// <param name="rect">The physical rectangle.</param>
    /// <returns>The logical rectangle.</returns>
    public LogicalRect ToLogical(PhysicalRect rect)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
            return new LogicalRect(rect.X, rect.Y, rect.Width, rect.Height);
        return SlowToLogical(rect);
    }

    /// <summary>
    /// Converts a logical rectangle to a physical rectangle.
    /// </summary>
    /// <param name="rect">The logical rectangle.</param>
    /// <returns>The physical rectangle.</returns>
    public PhysicalRect ToPhysical(LogicalRect rect)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
        {
            return new PhysicalRect(rect.Offset.InlineOffset, rect.Offset.BlockOffset,
                rect.Size.InlineSize, rect.Size.BlockSize);
        }
        return SlowToPhysical(rect);
    }

    // gfx variants - Using System.Drawing for .NET equivalents
    // Consider using System.Numerics.Vector2 for PointF and SizeF if more appropriate for your project.

    /// <summary>
    /// Converts a physical point (gfx::PointF) to a logical point.
    /// </summary>
    /// <param name="offset">The physical point.</param>
    /// <returns>The logical point.</returns>
    public System.Drawing.PointF ToLogical(System.Drawing.PointF offset)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
        {
            return offset;
        }
        return SlowToLogical(offset, System.Drawing.SizeF.Empty);
    }

    /// <summary>
    /// Converts a physical size (gfx::SizeF) to a logical size.
    /// </summary>
    /// <param name="size">The physical size.</param>
    /// <returns>The logical size.</returns>
    public System.Drawing.SizeF ToLogical(System.Drawing.SizeF size)
    {
        return _writingDirection.IsHorizontal() ? size : new System.Drawing.SizeF(size.Height, size.Width);
    }

    /// <summary>
    /// Converts a physical rectangle (gfx::RectF) to a logical rectangle.
    /// </summary>
    /// <param name="rect">The physical rectangle.</param>
    /// <returns>The logical rectangle.</returns>
    public System.Drawing.RectangleF ToLogical(System.Drawing.RectangleF rect)
    {
        if (_writingDirection.IsHorizontalLtr()) // IsHorizontalLtr needs to be added to WritingDirectionMode
        {
            return rect;
        }
        return SlowToLogical(rect);
    }


    private LogicalOffset SlowToLogical(PhysicalOffset offset, PhysicalSize innerSize)
    {
        switch (GetWritingMode())
        {
            case WritingMode.HorizontalTb:
                Debug.Assert(!IsLtr()); // LTR is in the fast code path.
                return new LogicalOffset(_outerSize.Width - offset.Left - innerSize.Width,
                    offset.Top);
            case WritingMode.VerticalRl:
            case WritingMode.SidewaysRl: // Assuming SidewaysRl behaves like VerticalRl for offset conversion
                if (IsLtr())
                {
                    return new LogicalOffset(
                        offset.Top, _outerSize.Width - offset.Left - innerSize.Width);
                }
                return new LogicalOffset(_outerSize.Height - offset.Top - innerSize.Height,
                    _outerSize.Width - offset.Left - innerSize.Width);
            case WritingMode.VerticalLr:
                if (IsLtr())
                    return new LogicalOffset(offset.Top, offset.Left);
                return new LogicalOffset(_outerSize.Height - offset.Top - innerSize.Height,
                    offset.Left);
            case WritingMode.SidewaysLr: // Assuming SidewaysLr behaves like VerticalLr for offset conversion
                if (IsLtr())
                {
                    return new LogicalOffset(
                        _outerSize.Height - offset.Top - innerSize.Height, offset.Left);
                }
                return new LogicalOffset(offset.Top, offset.Left);
        }
        Debug.Fail("Unhandled WritingMode in SlowToLogical");
        return default; // Should not be reached
    }

    private System.Drawing.PointF SlowToLogical(System.Drawing.PointF offset, System.Drawing.SizeF innerSize)
    {
        switch (GetWritingMode())
        {
            case WritingMode.HorizontalTb:
                Debug.Assert(!IsLtr()); // LTR is in the fast code path.
                return new System.Drawing.PointF(_outerSize.Width - offset.X - innerSize.Width, offset.Y);
            case WritingMode.VerticalRl:
            case WritingMode.SidewaysRl:
                if (IsLtr())
                {
                    return new System.Drawing.PointF(offset.Y,
                        _outerSize.Width - offset.X - innerSize.Width);
                }
                return new System.Drawing.PointF(_outerSize.Height - offset.Y - innerSize.Height,
                    _outerSize.Width - offset.X - innerSize.Width);
            case WritingMode.VerticalLr:
                if (IsLtr())
                {
                    return new System.Drawing.PointF(offset.Y, offset.X);
                }
                return new System.Drawing.PointF(_outerSize.Height - offset.Y - innerSize.Height,
                    offset.X);
            case WritingMode.SidewaysLr:
                if (IsLtr())
                {
                    return new System.Drawing.PointF(_outerSize.Height - offset.Y - innerSize.Height,
                        offset.X);
                }
                return new System.Drawing.PointF(offset.Y, offset.X);
        }
        Debug.Fail("Unhandled WritingMode in SlowToLogical (PointF)");
        return default;
    }


    private PhysicalOffset SlowToPhysical(LogicalOffset offset, PhysicalSize innerSize)
    {
        switch (GetWritingMode())
        {
            case WritingMode.HorizontalTb:
                Debug.Assert(!IsLtr()); // LTR is in the fast code path.
                return new PhysicalOffset(
                    _outerSize.Width - offset.InlineOffset - innerSize.Width,
                    offset.BlockOffset);
            case WritingMode.VerticalRl:
            case WritingMode.SidewaysRl:
                if (IsLtr())
                {
                    return new PhysicalOffset(
                        _outerSize.Width - offset.BlockOffset - innerSize.Width,
                        offset.InlineOffset);
                }
                return new PhysicalOffset(
                    _outerSize.Width - offset.BlockOffset - innerSize.Width,
                    _outerSize.Height - offset.InlineOffset - innerSize.Height);
            case WritingMode.VerticalLr:
                if (IsLtr())
                    return new PhysicalOffset(offset.BlockOffset, offset.InlineOffset);
                return new PhysicalOffset(
                    offset.BlockOffset,
                    _outerSize.Height - offset.InlineOffset - innerSize.Height);
            case WritingMode.SidewaysLr:
                if (IsLtr())
                {
                    return new PhysicalOffset(
                        offset.BlockOffset,
                        _outerSize.Height - offset.InlineOffset - innerSize.Height);
                }
                return new PhysicalOffset(offset.BlockOffset, offset.InlineOffset);
        }
        Debug.Fail("Unhandled WritingMode in SlowToPhysical");
        return default; // Should not be reached
    }

    private LogicalRect SlowToLogical(PhysicalRect rect)
    {
        return new LogicalRect(SlowToLogical(rect.Offset, rect.Size),
            ToLogical(rect.Size));
    }

    private System.Drawing.RectangleF SlowToLogical(System.Drawing.RectangleF rect)
    {
        System.Drawing.PointF logicalOrigin = SlowToLogical(new System.Drawing.PointF(rect.X, rect.Y), rect.Size);
        System.Drawing.SizeF logicalSize = ToLogical(rect.Size);
        return new System.Drawing.RectangleF(logicalOrigin, logicalSize);
    }


    private PhysicalRect SlowToPhysical(LogicalRect rect)
    {
        PhysicalSize size = ToPhysical(rect.Size);
        return new PhysicalRect(SlowToPhysical(rect.Offset, size), size);
    }

    // Helper methods from C++ (usually static in Blink, made instance methods here or could be static)
    // These are simplified versions. Blink's actual ToPhysicalSize/ToLogicalSize might be more complex
    // or part of a different utility class.
    private static PhysicalSize ToPhysicalSize(LogicalSize logicalSize, WritingMode writingMode)
    {
        if (writingMode == WritingMode.HorizontalTb ||
            writingMode == WritingMode.LrTb || // Assuming LrTb is horizontal
            writingMode == WritingMode.RlTb)   // Assuming RlTb is horizontal
        {
            return new PhysicalSize(logicalSize.InlineSize, logicalSize.BlockSize);
        }
        else
        {
            return new PhysicalSize(logicalSize.BlockSize, logicalSize.InlineSize);
        }
    }

    private static LogicalSize ToLogicalSize(PhysicalSize physicalSize, WritingMode writingMode)
    {
        if (writingMode == WritingMode.HorizontalTb ||
            writingMode == WritingMode.LrTb ||
            writingMode == WritingMode.RlTb)
        {
            return new LogicalSize(physicalSize.Width, physicalSize.Height);
        }
        else
        {
            return new LogicalSize(physicalSize.Height, physicalSize.Width);
        }
    }
}