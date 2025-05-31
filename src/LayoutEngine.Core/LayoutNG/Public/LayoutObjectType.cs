namespace LayoutEngine.Core.LayoutNG.Public;

/// <summary>
/// Types of layout objects in the layout tree, matching Blink's LayoutObject types.
/// </summary>
public enum LayoutObjectType
{
    /// <summary>
    /// Block-level layout object (div, p, section, etc.)
    /// </summary>
    Block,

    /// <summary>
    /// Inline layout object (span, a, em, etc.)
    /// </summary>
    Inline,

    /// <summary>
    /// Inline-block layout object
    /// </summary>
    InlineBlock,

    /// <summary>
    /// Text layout object for text nodes
    /// </summary>
    Text,

    /// <summary>
    /// Flexbox container
    /// </summary>
    Flex,

    /// <summary>
    /// Grid container
    /// </summary>
    Grid,

    /// <summary>
    /// Table layout object
    /// </summary>
    Table,

    /// <summary>
    /// Table row layout object
    /// </summary>
    TableRow,

    /// <summary>
    /// Table cell layout object
    /// </summary>
    TableCell,

    /// <summary>
    /// Replaced element (img, video, iframe, etc.)
    /// </summary>
    Replaced,

    /// <summary>
    /// List item layout object
    /// </summary>
    ListItem,

    /// <summary>
    /// Anonymous block created by CSS rules
    /// </summary>
    AnonymousBlock,

    /// <summary>
    /// Anonymous inline created by CSS rules
    /// </summary>
    AnonymousInline,

    /// <summary>
    /// Line box (contains inline content on a single line)
    /// </summary>
    LineBox,

    /// <summary>
    /// Block flow root (establishes new block formatting context)
    /// </summary>
    BlockFlowRoot
}