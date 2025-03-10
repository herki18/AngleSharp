using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core;

namespace AngleSharp.StyleSystem.Tests
{
    using Computation;
    using Models;

    [TestFixture]
    public class StylePropertyMapperTests
    {
        private StylePropertyMapper _mapper;

        [SetUp]
        public void Setup()
        {
            _mapper = new StylePropertyMapper();
        }

        [Test]
        public void IsLogicalProperty_LogicalProperties_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_mapper.IsLogicalProperty("margin-block-start"));
            Assert.IsTrue(_mapper.IsLogicalProperty("padding-inline-end"));
            Assert.IsTrue(_mapper.IsLogicalProperty("border-block"));
            Assert.IsTrue(_mapper.IsLogicalProperty("inline-size"));
        }

        [Test]
        public void IsLogicalProperty_PhysicalProperties_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_mapper.IsLogicalProperty("margin-top"));
            Assert.IsFalse(_mapper.IsLogicalProperty("padding-right"));
            Assert.IsFalse(_mapper.IsLogicalProperty("border-bottom"));
            Assert.IsFalse(_mapper.IsLogicalProperty("width"));
        }

        [Test]
        public void GetPhysicalProperties_LogicalProperty_ReturnsCorrectProperties()
        {
            // Act
            var properties = _mapper.GetPhysicalProperties("margin-block").ToList();

            // Assert
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.Contains("margin-top"));
            Assert.IsTrue(properties.Contains("margin-bottom"));
        }

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
        public void MapPhysicalToLogical_MarginLeftRight_HorizontalLTR_MapsToMarginInline()
        {
            // Arrange
            var marginLeft = new CssLengthValue(10, CssLengthValue.Unit.Px);
            var marginRight = new CssLengthValue(20, CssLengthValue.Unit.Px);
            var physicalProps = new Dictionary<string, ICssValue>
            {
                { "margin-left", marginLeft },
                { "margin-right", marginRight }
            };
            var writingMode = WritingMode.HorizontalLtr;

            // Act
            var marginInline = _mapper.MapPhysicalToLogical(physicalProps, "margin-inline", writingMode);

            // Assert
            Assert.IsNotNull(marginInline);
            // In a real implementation, this would create a combined value of both margins
        }
    }
}