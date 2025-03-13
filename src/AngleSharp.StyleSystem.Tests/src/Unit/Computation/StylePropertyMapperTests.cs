namespace AngleSharp.StyleSystem.Tests.Unit.Computation;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Models;

[TestFixture]
public class StylePropertyMapperTests
{
    private StylePropertyMapper _mapper;

    [SetUp]
    public void Setup()
    {
        _mapper = new StylePropertyMapper();
    }

    #region Identification Tests

    [Test]
    public void IsLogicalProperty_LogicalProperties_ReturnsTrue()
    {
        // Act & Assert
        Assert.IsTrue(_mapper.IsLogicalProperty("margin-block-start"));
        Assert.IsTrue(_mapper.IsLogicalProperty("padding-inline-end"));
        Assert.IsTrue(_mapper.IsLogicalProperty("border-block"));
        Assert.IsTrue(_mapper.IsLogicalProperty("inline-size"));
        Assert.IsTrue(_mapper.IsLogicalProperty("inset-block"));
        Assert.IsTrue(_mapper.IsLogicalProperty("border-inline-width"));
        Assert.IsTrue(_mapper.IsLogicalProperty("min-inline-size"));
        Assert.IsTrue(_mapper.IsLogicalProperty("max-block-size"));
    }

    [Test]
    public void IsLogicalProperty_PhysicalProperties_ReturnsFalse()
    {
        // Act & Assert
        Assert.IsFalse(_mapper.IsLogicalProperty("margin-top"));
        Assert.IsFalse(_mapper.IsLogicalProperty("padding-right"));
        Assert.IsFalse(_mapper.IsLogicalProperty("border-bottom"));
        Assert.IsFalse(_mapper.IsLogicalProperty("width"));
        Assert.IsFalse(_mapper.IsLogicalProperty("top"));
        Assert.IsFalse(_mapper.IsLogicalProperty("min-height"));
        Assert.IsFalse(_mapper.IsLogicalProperty("max-width"));
    }

    [Test]
    public void GetPhysicalProperties_LogicalProperty_ReturnsCorrectProperties()
    {
        // Act & Assert for various logical properties
        var marginBlockProps = _mapper.GetPhysicalProperties("margin-block").ToList();
        Assert.IsNotNull(marginBlockProps);
        Assert.IsTrue(marginBlockProps.Contains("margin-top"));
        Assert.IsTrue(marginBlockProps.Contains("margin-bottom"));

        var borderInlineProps = _mapper.GetPhysicalProperties("border-inline-width").ToList();
        Assert.IsNotNull(borderInlineProps);
        Assert.IsTrue(borderInlineProps.Contains("border-left-width"));
        Assert.IsTrue(borderInlineProps.Contains("border-right-width"));

        var insetBlockProps = _mapper.GetPhysicalProperties("inset-block-start").ToList();
        Assert.IsNotNull(insetBlockProps);
        Assert.IsTrue(insetBlockProps.Contains("top"));
    }

    #endregion

    #region Horizontal LTR Mode Tests

    [Test]
    public void MapLogicalToPhysical_MarginBlockStart_HorizontalLTR_MapsToMarginTop()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-top"));
        Assert.That(result["margin-top"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginBlockEnd_HorizontalLTR_MapsToMarginBottom()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block-end", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-bottom"));
        Assert.That(result["margin-bottom"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginInlineStart_HorizontalLTR_MapsToMarginLeft()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-left"));
        Assert.That(result["margin-left"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginInlineEnd_HorizontalLTR_MapsToMarginRight()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline-end", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-right"));
        Assert.That(result["margin-right"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_InlineSize_HorizontalLTR_MapsToWidth()
    {
        // Arrange
        var value = new CssLengthValue(200, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("inline-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("width"));
        Assert.That(result["width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BlockSize_HorizontalLTR_MapsToHeight()
    {
        // Arrange
        var value = new CssLengthValue(100, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("block-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("height"));
        Assert.That(result["height"], Is.EqualTo(value));
    }

    #endregion

    #region Horizontal RTL Mode Tests

    [Test]
    public void MapLogicalToPhysical_MarginInlineStart_HorizontalRTL_MapsToMarginRight()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalRtl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-right"));
        Assert.That(result["margin-right"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginInlineEnd_HorizontalRTL_MapsToMarginLeft()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalRtl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline-end", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-left"));
        Assert.That(result["margin-left"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginBlock_HorizontalRTL_MapsToBothTopAndBottom()
    {
        // Arrange - In RTL, block axis is still top-bottom
        var value = new CssLengthValue(20, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalRtl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-top"));
        Assert.IsTrue(result.ContainsKey("margin-bottom"));
        Assert.That(result["margin-top"], Is.EqualTo(value));
        Assert.That(result["margin-bottom"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_PaddingInline_HorizontalRTL_MapsToBothLeftAndRight()
    {
        // Arrange
        var value = new CssLengthValue(15, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalRtl;

        // Act
        var result = _mapper.MapLogicalToPhysical("padding-inline", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("padding-left"));
        Assert.IsTrue(result.ContainsKey("padding-right"));
        Assert.That(result["padding-left"], Is.EqualTo(value));
        Assert.That(result["padding-right"], Is.EqualTo(value));
    }

    #endregion

    #region Vertical Writing Mode Tests

    [Test]
    public void MapLogicalToPhysical_InlineSize_VerticalRL_MapsToHeight()
    {
        // Arrange
        var value = new CssLengthValue(200, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("inline-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("height"));
        Assert.That(result["height"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BlockSize_VerticalRL_MapsToWidth()
    {
        // Arrange
        var value = new CssLengthValue(100, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("block-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("width"));
        Assert.That(result["width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginBlockStart_VerticalRL_MapsToMarginRight()
    {
        // Arrange - In vertical-rl, block-start is right
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-right"));
        Assert.That(result["margin-right"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginBlockEnd_VerticalRL_MapsToMarginLeft()
    {
        // Arrange - In vertical-rl, block-end is left
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block-end", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-left"));
        Assert.That(result["margin-left"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginInlineStart_VerticalRL_MapsToMarginTop()
    {
        // Arrange - In vertical-rl, inline-start is top
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-top"));
        Assert.That(result["margin-top"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_InlineSize_VerticalLR_MapsToHeight()
    {
        // Arrange
        var value = new CssLengthValue(200, CssLengthValue.Unit.Px);
        var writingMode = new WritingMode(DirectionMode.Ltr, WritingModeType.VerticalLeftToRight);

        // Act
        var result = _mapper.MapLogicalToPhysical("inline-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("height"));
        Assert.That(result["height"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginBlockStart_VerticalLR_MapsToMarginLeft()
    {
        // Arrange - In vertical-lr, block-start is left
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = new WritingMode(DirectionMode.Ltr, WritingModeType.VerticalLeftToRight);

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block-start", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-left"));
        Assert.That(result["margin-left"], Is.EqualTo(value));
    }

    #endregion

    #region Shorthand Property Tests

    [Test]
    public void MapLogicalToPhysical_MarginBlock_HorizontalLTR_MapsToBothTopAndBottom()
    {
        // Arrange
        var value = new CssLengthValue(20, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-block", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-top"));
        Assert.IsTrue(result.ContainsKey("margin-bottom"));
        Assert.That(result["margin-top"], Is.EqualTo(value));
        Assert.That(result["margin-bottom"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MarginInline_HorizontalLTR_MapsToBothLeftAndRight()
    {
        // Arrange
        var value = new CssLengthValue(20, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("margin-inline", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("margin-left"));
        Assert.IsTrue(result.ContainsKey("margin-right"));
        Assert.That(result["margin-left"], Is.EqualTo(value));
        Assert.That(result["margin-right"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_PaddingBlock_HorizontalLTR_MapsToBothTopAndBottom()
    {
        // Arrange
        var value = new CssLengthValue(15, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("padding-block", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("padding-top"));
        Assert.IsTrue(result.ContainsKey("padding-bottom"));
        Assert.That(result["padding-top"], Is.EqualTo(value));
        Assert.That(result["padding-bottom"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderBlockWidth_HorizontalLTR_MapsToBothTopAndBottomWidth()
    {
        // Arrange
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-block-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-top-width"));
        Assert.IsTrue(result.ContainsKey("border-bottom-width"));
        Assert.That(result["border-top-width"], Is.EqualTo(value));
        Assert.That(result["border-bottom-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderInlineWidth_HorizontalLTR_MapsToBothLeftAndRightWidth()
    {
        // Arrange
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-inline-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-left-width"));
        Assert.IsTrue(result.ContainsKey("border-right-width"));
        Assert.That(result["border-left-width"], Is.EqualTo(value));
        Assert.That(result["border-right-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_Inset_HorizontalLTR_MapsToAllFourSides()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("inset", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("top"));
        Assert.IsTrue(result.ContainsKey("right"));
        Assert.IsTrue(result.ContainsKey("bottom"));
        Assert.IsTrue(result.ContainsKey("left"));
        Assert.That(result["top"], Is.EqualTo(value));
        Assert.That(result["right"], Is.EqualTo(value));
        Assert.That(result["bottom"], Is.EqualTo(value));
        Assert.That(result["left"], Is.EqualTo(value));
    }

    #endregion

    #region Physical to Logical Mapping Tests

    [Test]
    public void MapPhysicalToLogical_WidthAndHeight_HorizontalLTR_MapsToInlineAndBlockSize()
    {
        // Arrange
        var width = new CssLengthValue(300, CssLengthValue.Unit.Px);
        var height = new CssLengthValue(150, CssLengthValue.Unit.Px);
        var physicalProps = new Dictionary<string, ICssValue>
        {
            { "width", width },
            { "height", height }
        };
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var inlineSize = _mapper.MapPhysicalToLogical(physicalProps, "inline-size", writingMode);
        var blockSize = _mapper.MapPhysicalToLogical(physicalProps, "block-size", writingMode);

        // Assert
        Assert.IsNotNull(inlineSize);
        Assert.IsNotNull(blockSize);
        Assert.That(inlineSize, Is.EqualTo(width));
        Assert.That(blockSize, Is.EqualTo(height));
    }

    [Test]
    public void MapPhysicalToLogical_WidthAndHeight_VerticalRL_MapsToInlineAndBlockSize()
    {
        // Arrange
        var width = new CssLengthValue(300, CssLengthValue.Unit.Px);
        var height = new CssLengthValue(150, CssLengthValue.Unit.Px);
        var physicalProps = new Dictionary<string, ICssValue>
        {
            { "width", width },
            { "height", height }
        };
        var writingMode = WritingMode.VerticalRl;

        // Act
        var inlineSize = _mapper.MapPhysicalToLogical(physicalProps, "inline-size", writingMode);
        var blockSize = _mapper.MapPhysicalToLogical(physicalProps, "block-size", writingMode);

        // Assert
        Assert.IsNotNull(inlineSize);
        Assert.IsNotNull(blockSize);
        Assert.That(inlineSize, Is.EqualTo(height)); // In vertical mode, height is inline-size
        Assert.That(blockSize, Is.EqualTo(width));   // In vertical mode, width is block-size
    }

    [Test]
    public void MapPhysicalToLogical_BorderWidths_HorizontalLTR_MapsToBorderBlock()
    {
        // Arrange
        var topWidth = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var bottomWidth = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var physicalProps = new Dictionary<string, ICssValue>
        {
            { "border-top-width", topWidth },
            { "border-bottom-width", bottomWidth }
        };
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var borderBlockWidth = _mapper.MapPhysicalToLogical(physicalProps, "border-block-width", writingMode);

        // Assert
        Assert.IsNotNull(borderBlockWidth);
        // Note: In a real implementation, if top and bottom are the same, it would return a single value
        // If they differ, it might return a combined value or just the first one based on implementation
    }

    [Test]
    public void MapPhysicalToLogical_Paddings_VerticalRL_MapsToPaddingBlock()
    {
        // Arrange
        var rightPadding = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var leftPadding = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var physicalProps = new Dictionary<string, ICssValue>
        {
            { "padding-right", rightPadding },
            { "padding-left", leftPadding }
        };
        var writingMode = WritingMode.VerticalRl;

        // Act
        var paddingBlock = _mapper.MapPhysicalToLogical(physicalProps, "padding-block", writingMode);

        // Assert
        Assert.IsNotNull(paddingBlock);
        // In vertical-rl, right/left map to block-start/block-end
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Test]
    public void MapLogicalToPhysical_NonExistentProperty_ReturnsOriginalProperty()
    {
        // Arrange
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("non-existent-property", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("non-existent-property"));
        Assert.That(result["non-existent-property"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MinInlineSize_HorizontalLTR_MapsToMinWidth()
    {
        // Arrange
        var value = new CssLengthValue(200, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("min-inline-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("min-width"));
        Assert.That(result["min-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_MaxBlockSize_VerticalRL_MapsToMaxWidth()
    {
        // Arrange
        var value = new CssLengthValue(500, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("max-block-size", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("max-width"));
        Assert.That(result["max-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_InsetBlock_VerticalRL_MapsToRightAndLeft()
    {
        // Arrange - In vertical-rl, inset-block maps to right and left
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("inset-block", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("right"));
        Assert.IsTrue(result.ContainsKey("left"));
        Assert.That(result["right"], Is.EqualTo(value)); // block-start
        Assert.That(result["left"], Is.EqualTo(value));  // block-end
    }

    [Test]
    public void MapLogicalToPhysical_InsetInline_VerticalRL_MapsToTopAndBottom()
    {
        // Arrange - In vertical-rl, inset-inline maps to top and bottom
        var value = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("inset-inline", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("top"));
        Assert.IsTrue(result.ContainsKey("bottom"));
        Assert.That(result["top"], Is.EqualTo(value));     // inline-start
        Assert.That(result["bottom"], Is.EqualTo(value));  // inline-end
    }

    #endregion

    #region Border-Specific Tests

    [Test]
    public void MapLogicalToPhysical_BorderBlockWidth_HorizontalLTR_MapsToBorderTopWidthAndBorderBottomWidth()
    {
        // Arrange
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-block-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-top-width"));
        Assert.IsTrue(result.ContainsKey("border-bottom-width"));
        Assert.That(result["border-top-width"], Is.EqualTo(value));
        Assert.That(result["border-bottom-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderInlineWidth_HorizontalRTL_MapsToBorderLeftWidthAndBorderRightWidth()
    {
        // Arrange - In RTL, the inline axis directions are reversed but still maps to left/right
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalRtl;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-inline-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-left-width"));
        Assert.IsTrue(result.ContainsKey("border-right-width"));
        Assert.That(result["border-left-width"], Is.EqualTo(value));
        Assert.That(result["border-right-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderBlockStartWidth_HorizontalLTR_MapsToBorderTopWidth()
    {
        // Arrange
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-block-start-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-top-width"));
        Assert.That(result["border-top-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderInlineEndWidth_HorizontalLTR_MapsToBorderRightWidth()
    {
        // Arrange
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.HorizontalLtr;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-inline-end-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-right-width"));
        Assert.That(result["border-right-width"], Is.EqualTo(value));
    }

    [Test]
    public void MapLogicalToPhysical_BorderBlockStartWidth_VerticalRL_MapsToBorderRightWidth()
    {
        // Arrange - In vertical-rl, block-start is right
        var value = new CssLengthValue(5, CssLengthValue.Unit.Px);
        var writingMode = WritingMode.VerticalRl;

        // Act
        var result = _mapper.MapLogicalToPhysical("border-block-start-width", value, writingMode);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("border-right-width"));
        Assert.That(result["border-right-width"], Is.EqualTo(value));
    }

    #endregion
}