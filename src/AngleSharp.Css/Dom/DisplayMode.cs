namespace AngleSharp.Css.Dom;

/// <summary>
/// A list of all pre-defined display settings, aligned with BlinkNG's DisplayType enum.
/// </summary>
public enum DisplayMode : byte
{
    /// <summary>
    /// Turns off the display of an element (it has no effect on layout);
    /// all descendant elements also have their display turned off. The
    /// document is rendered as though the element did not exist.
    /// </summary>
    None,

    /// <summary>
    /// The element generates a block element box.
    /// </summary>
    Block,

    /// <summary>
    /// The element generates one or more inline element boxes.
    /// </summary>
    Inline,

    /// <summary>
    /// The element generates a block element box that will be flowed with
    /// surrounding content as if it were a single inline box (behaving much
    /// like a replaced element would).
    /// </summary>
    InlineBlock,

    /// <summary>
    /// The element behaves like a block element and lays out its content
    /// according to the flexbox model.
    /// </summary>
    Flex,

    /// <summary>
    /// The element behaves like a block element and lays out its content
    /// according to the grid model.
    /// </summary>
    Grid,

    /// <summary>
    /// Behaves like the table HTML element. It defines a block-level box.
    /// </summary>
    Table,

    /// <summary>
    /// Behaves like the tr HTML element.
    /// </summary>
    TableRow,

    /// <summary>
    /// Behaves like the td HTML element.
    /// </summary>
    TableCell,

    /// <summary>
    /// The element generates a block box for the content and a separate
    /// list-item inline box.
    /// </summary>
    ListItem,

    /// <summary>
    /// The element itself does not generate any boxes, but its children do.
    /// </summary>
    Contents,

    /// <summary>
    /// The element generates a block container box, establishing a new block formatting context.
    /// </summary>
    FlowRoot,

    /// <summary>
    /// The element behaves like an inline element and lays out its content
    /// according to the flexbox model.
    /// </summary>
    InlineFlex,

    /// <summary>
    /// The element behaves like an inline element and lays out its content
    /// according to the grid model.
    /// </summary>
    InlineGrid,

    /// <summary>
    /// The inline-table value does not have a direct mapping in HTML. It
    /// behaves like a table HTML element, but as an inline box, rather than
    /// a block-level box. Inside the table box is a block-level context.
    /// </summary>
    InlineTable,

    /// <summary>
    /// These elements behave like the corresponding tbody HTML elements.
    /// </summary>
    TableRowGroup,

    /// <summary>
    /// These elements behave like the corresponding thead HTML elements.
    /// </summary>
    TableHeaderGroup,

    /// <summary>
    /// These elements behave like the corresponding tfoot HTML elements.
    /// </summary>
    TableFooterGroup,

    /// <summary>
    /// These elements behave like the corresponding col HTML elements.
    /// </summary>
    TableColumn,

    /// <summary>
    /// These elements behave like the corresponding colgroup HTML elements.
    /// </summary>
    TableColumnGroup,

    /// <summary>
    /// Behaves like the caption HTML element.
    /// </summary>
    TableCaption
}