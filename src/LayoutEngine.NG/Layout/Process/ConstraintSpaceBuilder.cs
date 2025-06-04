namespace LayoutEngine.NG.Layout.Process;

using LayoutEngine.NG.Layout.Fragments;
using Renderer.Platform.Text;

/// <summary>
/// Builder for creating ConstraintSpace instances.
/// In LayoutNG, this provides a fluent API for constraint space construction.
/// </summary>
public class ConstraintSpaceBuilder
{
    private readonly ConstraintSpace _parentSpace;
    private WritingDirectionMode _writingDirection;
    private bool _isNewFormattingContext;
    private bool _isPaintedAtomically;
    private bool _useFirstLineStyle;
    private bool _isHiddenForPaint;
    private BaselineAlgorithmType _baselineAlgorithmType;
    private LogicalSize _availableSize;
    private LogicalSize _percentageResolutionSize;
    private bool _isFixedInlineSize;
    private bool _isFixedBlockSize;
    private bool _isShrinkToFit;

    public ConstraintSpaceBuilder(
        ConstraintSpace parentSpace,
        WritingDirectionMode writingDirection,
        bool isNewFc = false)
    {
        _parentSpace = parentSpace;
        _writingDirection = writingDirection;
        _isNewFormattingContext = isNewFc;
        _availableSize = new LogicalSize(
            parentSpace.AvailableInlineSize,
            parentSpace.AvailableBlockSize);
        _percentageResolutionSize = new LogicalSize(
            parentSpace.PercentageResolutionInlineSize,
            parentSpace.PercentageResolutionBlockSize);
    }

    public void SetIsPaintedAtomically(bool value) => _isPaintedAtomically = value;
    public void SetUseFirstLineStyle(bool value) => _useFirstLineStyle = value;
    public void SetIsHiddenForPaint(bool value) => _isHiddenForPaint = value;
    public void SetBaselineAlgorithmType(BaselineAlgorithmType value) => _baselineAlgorithmType = value;

    public void SetAvailableSize(LogicalSize size) => _availableSize = size;
    public void SetPercentageResolutionSize(LogicalSize size) => _percentageResolutionSize = size;
    public void SetIsFixedSize(bool inlineAxis, bool blockAxis)
    {
        _isFixedInlineSize = inlineAxis;
        _isFixedBlockSize = blockAxis;
    }
    public void SetIsShrinkToFit(bool value) => _isShrinkToFit = value;

    public ConstraintSpace ToConstraintSpace()
    {
        var builder = ConstraintSpace.CreateBuilder();

        builder.AvailableInlineSize = _availableSize.InlineSize;
        builder.AvailableBlockSize = _availableSize.BlockSize;
        builder.IsFixedInlineSize = _isFixedInlineSize;
        builder.IsFixedBlockSize = _isFixedBlockSize;
        builder.IsNewFormattingContext = _isNewFormattingContext;
        builder.PercentageResolutionInlineSize = _percentageResolutionSize.InlineSize;
        builder.PercentageResolutionBlockSize = _percentageResolutionSize.BlockSize;
        builder.IsShrinkToFit = _isShrinkToFit;

        // TODO: Set other properties

        return builder.Build();
    }
}