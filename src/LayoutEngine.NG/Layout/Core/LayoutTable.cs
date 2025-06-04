namespace LayoutEngine.NG.Layout.Core;

using System;
using AngleSharp.Css.Dom;

/// <summary>
/// Base class for table layout objects.
/// </summary>
public abstract class LayoutTable : LayoutBlock
{
    public override bool IsTable() => true;

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Table;
    }
}

/// <summary>
/// Represents a table section (thead, tbody, tfoot) in the layout tree.
/// In LayoutNG, table sections group rows together.
/// </summary>
public class LayoutTableSection : LayoutBox
{
    public override bool IsTableSection() => true;

    /// <summary>
    /// Whether this section is empty (has no rows or only empty rows).
    /// </summary>
    public virtual bool IsEmpty()
    {
        // Check if we have any non-empty rows
        var child = FirstChild;
        while (child != null)
        {
            if (child is LayoutTableRow row && !row.IsEmpty())
                return false;
            child = child.NextSibling;
        }
        return true;
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }
}

/// <summary>
/// Represents a table row in the layout tree.
/// In LayoutNG, table rows contain table cells.
/// </summary>
public class LayoutTableRow : LayoutBox
{
    public override bool IsTableRow() => true;

    /// <summary>
    /// Whether this row is empty (has no cells).
    /// </summary>
    public virtual bool IsEmpty()
    {
        return FirstChild == null;
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }
}

/// <summary>
/// Represents a table cell (td, th) in the layout tree.
/// In LayoutNG, table cells are the content containers in tables.
/// </summary>
public class LayoutTableCell : LayoutBlockFlow
{
    private int _colspan = 1;
    private int _rowspan = 1;

    public override bool IsTableCell() => true;

    /// <summary>
    /// Gets the column span for this cell.
    /// </summary>
    public virtual int ColSpan() => _colspan;

    /// <summary>
    /// Sets the column span for this cell.
    /// </summary>
    public void SetColSpan(int span)
    {
        _colspan = Math.Max(1, span);
    }

    /// <summary>
    /// Gets the computed row span for this cell.
    /// </summary>
    public virtual int ComputedRowSpan() => _rowspan;

    /// <summary>
    /// Sets the row span for this cell.
    /// </summary>
    public void SetRowSpan(int span)
    {
        _rowspan = Math.Max(1, span);
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }
}

/// <summary>
/// Represents a table column (col) or column group (colgroup) in the layout tree.
/// In LayoutNG, columns define width constraints for table cells.
/// </summary>
public class LayoutTableColumn : LayoutBox
{
    private int _span = 1;

    public override bool IsTableCol() => Style?.Display == DisplayMode.TableColumn;
    public override bool IsTableColgroup() => Style?.Display == DisplayMode.TableColumnGroup;

    /// <summary>
    /// Gets the span (number of columns) for this column element.
    /// </summary>
    public virtual int Span() => _span;

    /// <summary>
    /// Sets the span for this column element.
    /// </summary>
    public void SetSpan(int span)
    {
        _span = Math.Max(1, span);
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }
}