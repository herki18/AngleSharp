namespace LayoutEngine.NG.Layout;

using System.Collections.Generic;
using Inputs;

/// <summary>
/// Represents a break token for block fragmentation.
/// In LayoutNG, this tracks where to resume layout after a fragmentation break.
/// </summary>
public class BlockBreakToken : BreakToken
{
    public bool IsRepeated { get; private set; }
    public bool IsAtBlockStart { get; private set; }
    public float ConsumedBlockSizeForLegacy { get; set; }
    public IList<BreakToken> ChildBreakTokens { get; } = new List<BreakToken>();

    public BlockBreakToken(BlockNode node, int sequenceNumber = 0) : base(node)
    {
        SetSequenceNumber(sequenceNumber);
    }

    /// <summary>
    /// Creates a repeated break token.
    /// </summary>
    public static BlockBreakToken CreateRepeated(BlockNode node, int index)
    {
        return new BlockBreakToken(node, index) { IsRepeated = true };
    }

    public override bool IsFinished() => false;
}

/// <summary>
/// Base class for all break tokens.
/// </summary>
public abstract class BreakToken
{
    public LayoutInputNode InputNode { get; }
    private int _sequenceNumber;

    protected BreakToken(LayoutInputNode inputNode)
    {
        InputNode = inputNode;
    }

    public int SequenceNumber() => _sequenceNumber;
    protected void SetSequenceNumber(int value) => _sequenceNumber = value;

    public abstract bool IsFinished();
}