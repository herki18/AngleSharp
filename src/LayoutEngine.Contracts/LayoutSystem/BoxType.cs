namespace LayoutEngine.Contracts.LayoutSystem;

/// <summary>
/// Defines the type of layout box.
/// </summary>
public enum BoxType
{
    Block,
    Inline,
    InlineBlock,
    Flex,
    Grid,
    Table,
    TableCell,
    None
}