#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace LayoutEngine.NG.Layout;

using System;
using System.Collections.Generic;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using LayoutEngine.NG.Style;

/// <summary>
/// Implements the block layout algorithm following LayoutNG principles.
/// This algorithm handles block-level layout including margin collapsing,
/// sizing, and positioning of children.
/// </summary>
public class BlockLayoutAlgorithm
{
    private readonly LayoutBlockFlow _node;
    private readonly ConstraintSpace _constraintSpace;
    private readonly ComputedStyle _style;

    // Layout state
    private PhysicalBoxStrut _borders;
    private PhysicalBoxStrut _padding;
    private PhysicalBoxStrut _margins;
    private float _contentInlineSize;
    private float _contentBlockSize;
    private bool _isFixedBlockSize;

    // Margin collapsing state
    private MarginStrut _marginStrut;
    private float _intrinsicBlockSize;
    private float _previousInflowPosition;
    private bool _hasSeenInFlowChild;

    // Results
    private readonly List<PhysicalFragment> _childFragments = new();

    public BlockLayoutAlgorithm(LayoutBlockFlow node, ConstraintSpace constraintSpace)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
        _constraintSpace = constraintSpace ?? throw new ArgumentNullException(nameof(constraintSpace));
        _style = node.Style ?? throw new InvalidOperationException("Node must have computed style");
    }

    /// <summary>
    /// Performs block layout and returns the result with PhysicalFragment.
    /// </summary>
    public LayoutResult Layout()
    {
        // Step 1: Calculate borders, padding, and margins
        CalculateBoxModelValues();

        // Step 2: Calculate inline size (width)
        CalculateInlineSize();

        // Step 3: Calculate block size if fixed
        CalculateBlockSizeIfFixed();

        // Step 4: Initialize margin collapsing
        InitializeMarginCollapsing();

        // Step 5: Layout children
        LayoutChildren();

        // Step 6: Calculate final block size
        CalculateFinalBlockSize();

        // Step 7: Create and return the fragment using builder
        return CreateLayoutResult();
    }

    private void CalculateBoxModelValues()
    {
        var containingBlockInlineSize = _constraintSpace.PercentageResolutionInlineSize;

        // Convert CSS values to physical pixels
        _borders = new PhysicalBoxStrut(
            ConvertToPixels(_style.Border.Top),
            ConvertToPixels(_style.Border.Right),
            ConvertToPixels(_style.Border.Bottom),
            ConvertToPixels(_style.Border.Left)
        );

        _padding = new PhysicalBoxStrut(
            ConvertToPixels(_style.Padding.Top, containingBlockInlineSize),
            ConvertToPixels(_style.Padding.Right, containingBlockInlineSize),
            ConvertToPixels(_style.Padding.Bottom, containingBlockInlineSize),
            ConvertToPixels(_style.Padding.Left, containingBlockInlineSize)
        );

        _margins = new PhysicalBoxStrut(
            ConvertToPixels(_style.Margin.Top, containingBlockInlineSize),
            ConvertToPixels(_style.Margin.Right, containingBlockInlineSize),
            ConvertToPixels(_style.Margin.Bottom, containingBlockInlineSize),
            ConvertToPixels(_style.Margin.Left, containingBlockInlineSize)
        );
    }

    private void CalculateInlineSize()
    {
        var availableInlineSize = _constraintSpace.AvailableInlineSize;
        var styleWidth = _style.GetProperty(PropertyNames.Width);

        if (styleWidth?.RawValue is CssLengthValue widthValue &&
            !widthValue.Equals(CssLengthValue.Auto))
        {
            // Fixed width
            _contentInlineSize = ConvertToPixels(widthValue, availableInlineSize);
        }
        else if (_constraintSpace.IsFixedInlineSize)
        {
            // Fill available width (block behavior)
            _contentInlineSize = Math.Max(0,
                availableInlineSize - _margins.InlineSum - _borders.InlineSum - _padding.InlineSum);
        }
        else if (_constraintSpace.IsShrinkToFit)
        {
            // Shrink-to-fit (for floats, inline-blocks, etc.)
            // For now, use available size
            _contentInlineSize = Math.Max(0,
                availableInlineSize - _margins.InlineSum - _borders.InlineSum - _padding.InlineSum);
        }
        else
        {
            // Default to available size
            _contentInlineSize = Math.Max(0,
                availableInlineSize - _margins.InlineSum - _borders.InlineSum - _padding.InlineSum);
        }

        // Apply min/max width constraints
        ApplyMinMaxInlineSize();
    }

    private void CalculateBlockSizeIfFixed()
    {
        var styleHeight = _style.GetProperty(PropertyNames.Height);

        if (styleHeight?.RawValue is CssLengthValue heightValue &&
            !heightValue.Equals(CssLengthValue.Auto))
        {
            var containingBlockSize = _constraintSpace.PercentageResolutionBlockSize;
            if (!float.IsNaN(containingBlockSize) || heightValue.Type != CssLengthValue.Unit.Percent)
            {
                _contentBlockSize = ConvertToPixels(heightValue, containingBlockSize);
                _isFixedBlockSize = true;

                // Apply min/max height constraints
                ApplyMinMaxBlockSize();
            }
        }
    }

    private void InitializeMarginCollapsing()
    {
        _marginStrut = _constraintSpace.MarginStrut;

        // If we establish a new formatting context, don't collapse margins
        if (_constraintSpace.IsNewFormattingContext || _node.EstablishesFormattingContext())
        {
            _marginStrut = new MarginStrut();
            _previousInflowPosition = 0;
        }
        else
        {
            // Start with top margin
            _marginStrut.Append(_margins.BlockStart);
            _previousInflowPosition = 0;
        }
    }

    private void LayoutChildren()
    {
        var childAvailableInlineSize = _contentInlineSize;
        var currentBlockOffset = _borders.BlockStart + _padding.BlockStart;

        // Apply margin strut to first position
        if (!_constraintSpace.IsNewFormattingContext)
        {
            currentBlockOffset += _marginStrut.Sum;
        }

        var child = _node.FirstChild;
        while (child != null)
        {
            if (child is LayoutBlockFlow childBlock)
            {
                var childFragment = LayoutBlockChild(
                    childBlock,
                    childAvailableInlineSize,
                    currentBlockOffset);

                if (childFragment != null)
                {
                    _childFragments.Add(childFragment);
                    currentBlockOffset = childFragment.Offset.Top + childFragment.MarginBoxSize.Height;
                    _hasSeenInFlowChild = true;
                }
            }
            else if (child is LayoutText textChild)
            {
                // Simplified text layout for now
                var textFragment = LayoutTextChild(textChild, childAvailableInlineSize, currentBlockOffset);
                if (textFragment != null)
                {
                    _childFragments.Add(textFragment);
                    currentBlockOffset = textFragment.Offset.Top + textFragment.Size.Height;
                    _hasSeenInFlowChild = true;

                    // Text breaks margin collapsing
                    _marginStrut = new MarginStrut();
                }
            }

            child = child.NextSibling;
        }

        // Update intrinsic size
        _intrinsicBlockSize = currentBlockOffset + _padding.BlockEnd + _borders.BlockEnd;
    }

    private LayoutResult CreateLayoutResult()
    {
        // Calculate final size
        var borderBoxSize = new PhysicalSize(
            _contentInlineSize + _borders.InlineSum + _padding.InlineSum,
            _contentBlockSize + _borders.BlockSum + _padding.BlockSum
        );

        // Create fragment using modern builder pattern
        var fragment = PhysicalFragment.CreateBuilder()
            .SetSize(borderBoxSize)
            .SetOffset(PhysicalOffset.Zero) // Parent will position
            .SetLayoutObject(_node)
            .SetMargins(_margins)
            .SetBorders(_borders)
            .SetPadding(_padding)
            .AddChildren(_childFragments)
            .SetIsFormattingContextRoot(_node.EstablishesFormattingContext())
            .SetEndMarginStrut(_marginStrut)
            .SetBaseline(CalculateBaseline())
            .Build();

        // Create result
        return new LayoutResult(
            fragment,
            _intrinsicBlockSize,
            _marginStrut,
            0, // BFC offset
            _isFixedBlockSize && HasPercentageHeight()
        );
    }

    private PhysicalFragment? LayoutBlockChild(
        LayoutBlockFlow child,
        float availableInlineSize,
        float blockOffset)
    {
        // Create constraint space for child
        var childConstraintSpace = ConstraintSpace.CreateBuilder()
            .SetAvailableSize(availableInlineSize, _constraintSpace.AvailableBlockSize)
            .SetIsFixedSize(true, false)
            .SetPercentageResolution(availableInlineSize,
                _isFixedBlockSize ? _contentBlockSize : float.NaN)
            .SetIsNewFormattingContext(child.EstablishesFormattingContext())
            .SetMarginStrut(_marginStrut)
            .Build();

        // Layout child using modern algorithm
        var childAlgorithm = new BlockLayoutAlgorithm(child, childConstraintSpace);
        var childResult = childAlgorithm.Layout();
        var childFragment = childResult.PhysicalFragment;

        // Calculate position with margin collapsing
        var childMargins = childFragment.Margins;
        var collapsedMargin = _marginStrut.Sum;

        // Position child
        var childOffset = new PhysicalOffset(
            _borders.InlineStart + _padding.InlineStart + childMargins.InlineStart,
            blockOffset
        );

        // Create positioned fragment using builder
        var positionedFragment = PhysicalFragment.CreateBuilder()
            .SetSize(childFragment.Size)
            .SetOffset(childOffset)
            .SetLayoutObject(childFragment.LayoutObject)
            .SetMargins(childFragment.Margins)
            .SetBorders(childFragment.Borders)
            .SetPadding(childFragment.Padding)
            .AddChildren(childFragment.Children)
            .SetBaseline(childFragment.Baseline)
            .SetIsFormattingContextRoot(childFragment.IsFormattingContextRoot)
            .SetEndMarginStrut(childFragment.EndMarginStrut)
            .Build();

        // Update margin strut for next child
        if (childResult.PhysicalFragment.IsSelfCollapsing)
        {
            _marginStrut.Merge(childResult.EndMarginStrut);
        }
        else
        {
            _marginStrut = childResult.EndMarginStrut;
        }

        return positionedFragment;
    }

    private PhysicalFragment? LayoutTextChild(
        LayoutText text,
        float availableInlineSize,
        float blockOffset)
    {
        // Very simplified text layout
        if (string.IsNullOrWhiteSpace(text.Text))
            return null;

        var lineHeight = 20f; // Simplified
        var textHeight = lineHeight; // Single line for now

        return PhysicalFragment.CreateBuilder()
            .SetSize(new PhysicalSize(availableInlineSize, textHeight))
            .SetOffset(new PhysicalOffset(
                _borders.InlineStart + _padding.InlineStart,
                blockOffset))
            .SetLayoutObject(text)
            .SetBaseline(lineHeight * 0.8f) // Simplified baseline
            .Build();
    }

    private void CalculateFinalBlockSize()
    {
        if (!_isFixedBlockSize)
        {
            // Use intrinsic size
            _contentBlockSize = Math.Max(0,
                _intrinsicBlockSize - _borders.BlockSum - _padding.BlockSum);
        }
    }

    private float CalculateBaseline()
    {
        // Simplified baseline calculation
        var baseline = _childFragments[0].Baseline;
        if (_childFragments.Count > 0 && _childFragments[0].Baseline.HasValue)
        {
            return baseline != null ? _childFragments[0].Offset.Top + baseline.Value : _contentBlockSize;
        }
        return _contentBlockSize;
    }

    private void ApplyMinMaxInlineSize()
    {
        var minWidth = _style.GetProperty(PropertyNames.MinWidth);
        if (minWidth?.RawValue is CssLengthValue minValue && !minValue.Equals(CssLengthValue.Auto))
        {
            var minPixels = ConvertToPixels(minValue, _constraintSpace.PercentageResolutionInlineSize);
            _contentInlineSize = Math.Max(_contentInlineSize, minPixels);
        }

        var maxWidth = _style.GetProperty(PropertyNames.MaxWidth);
        if (maxWidth?.RawValue is CssLengthValue maxValue && maxValue.Type != CssLengthValue.Unit.None)
        {
            var maxPixels = ConvertToPixels(maxValue, _constraintSpace.PercentageResolutionInlineSize);
            _contentInlineSize = Math.Min(_contentInlineSize, maxPixels);
        }
    }

    private void ApplyMinMaxBlockSize()
    {
        var minHeight = _style.GetProperty(PropertyNames.MinHeight);
        if (minHeight?.RawValue is CssLengthValue minValue && !minValue.Equals(CssLengthValue.Auto))
        {
            var minPixels = ConvertToPixels(minValue, _constraintSpace.PercentageResolutionBlockSize);
            _contentBlockSize = Math.Max(_contentBlockSize, minPixels);
        }

        var maxHeight = _style.GetProperty(PropertyNames.MaxHeight);
        if (maxHeight?.RawValue is CssLengthValue maxValue && maxValue.Type != CssLengthValue.Unit.None)
        {
            var maxPixels = ConvertToPixels(maxValue, _constraintSpace.PercentageResolutionBlockSize);
            _contentBlockSize = Math.Min(_contentBlockSize, maxPixels);
        }
    }

    private bool HasPercentageHeight()
    {
        var height = _style.GetProperty(PropertyNames.Height);
        return height?.RawValue is CssLengthValue lengthValue &&
               lengthValue.Type == CssLengthValue.Unit.Percent;
    }

    private float ConvertToPixels(CssLengthValue length, float percentageBase = 0)
    {
        return length.Type switch
        {
            CssLengthValue.Unit.Px => (float)length.Value,
            CssLengthValue.Unit.Em => (float)(length.Value * 16), // Simplified
            CssLengthValue.Unit.Rem => (float)(length.Value * 16), // Simplified
            CssLengthValue.Unit.Percent => (float)(length.Value / 100 * percentageBase),
            _ => (float)length.Value
        };
    }
}