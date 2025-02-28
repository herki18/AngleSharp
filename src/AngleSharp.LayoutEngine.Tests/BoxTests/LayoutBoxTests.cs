namespace AngleSharp.LayoutEngine.Tests.BoxTests;

using AngleSharp.LayoutEngine.Box;
using NUnit.Framework;
using System;

[TestFixture]
public class LayoutBoxTests
{
    [Test]
    public void Clone_CreatesExactCopy_WithIndependentValues()
    {
        // Arrange - Create a layout box with various properties set
        var original = new LayoutBox
        {
            X = 10,
            Y = 20,
            Width = 100,
            Height = 150,
            PaddingTop = 5,
            PaddingRight = 6,
            PaddingBottom = 7,
            PaddingLeft = 8,
            BorderTop = 2,
            BorderRight = 3,
            BorderBottom = 4,
            BorderLeft = 5,
            MarginTop = 10,
            MarginRight = 11,
            MarginBottom = 12,
            MarginLeft = 13,
            IsInMarginCollapsedChain = true,
            HasTopMarginCollapsed = true,
            HasBottomMarginCollapsed = true,
            EffectiveTopMargin = 8,
            EffectiveBottomMargin = 9
        };

        // Act - Clone the layout box
        var clone = original.Clone();

        // Assert - Verify all properties are copied correctly
        Assert.That(clone.X, Is.EqualTo(original.X));
        Assert.That(clone.Y, Is.EqualTo(original.Y));
        Assert.That(clone.Width, Is.EqualTo(original.Width));
        Assert.That(clone.Height, Is.EqualTo(original.Height));

        Assert.That(clone.PaddingTop, Is.EqualTo(original.PaddingTop));
        Assert.That(clone.PaddingRight, Is.EqualTo(original.PaddingRight));
        Assert.That(clone.PaddingBottom, Is.EqualTo(original.PaddingBottom));
        Assert.That(clone.PaddingLeft, Is.EqualTo(original.PaddingLeft));

        Assert.That(clone.BorderTop, Is.EqualTo(original.BorderTop));
        Assert.That(clone.BorderRight, Is.EqualTo(original.BorderRight));
        Assert.That(clone.BorderBottom, Is.EqualTo(original.BorderBottom));
        Assert.That(clone.BorderLeft, Is.EqualTo(original.BorderLeft));

        Assert.That(clone.MarginTop, Is.EqualTo(original.MarginTop));
        Assert.That(clone.MarginRight, Is.EqualTo(original.MarginRight));
        Assert.That(clone.MarginBottom, Is.EqualTo(original.MarginBottom));
        Assert.That(clone.MarginLeft, Is.EqualTo(original.MarginLeft));

        Assert.That(clone.IsInMarginCollapsedChain, Is.EqualTo(original.IsInMarginCollapsedChain));
        Assert.That(clone.HasTopMarginCollapsed, Is.EqualTo(original.HasTopMarginCollapsed));
        Assert.That(clone.HasBottomMarginCollapsed, Is.EqualTo(original.HasBottomMarginCollapsed));
        Assert.That(clone.EffectiveTopMargin, Is.EqualTo(original.EffectiveTopMargin));
        Assert.That(clone.EffectiveBottomMargin, Is.EqualTo(original.EffectiveBottomMargin));

        // Verify that changing the clone doesn't affect the original
        clone.X = 50;
        clone.MarginTop = 25;

        Assert.That(original.X, Is.EqualTo(10));
        Assert.That(original.MarginTop, Is.EqualTo(10));
    }

    [Test]
    public void UpdateFromBoxValues_TransfersAllProperties_Correctly()
    {
        // Arrange
        var box = new LayoutBox();
        var boxValues = new BoxValues
        {
            ContentWidth = 200,
            ContentHeight = 150,
            PaddingTop = 10,
            PaddingRight = 11,
            PaddingBottom = 12,
            PaddingLeft = 13,
            BorderTop = 5,
            BorderRight = 6,
            BorderBottom = 7,
            BorderLeft = 8,
            MarginTop = 15,
            MarginRight = 16,
            MarginBottom = 17,
            MarginLeft = 18
        };

        // Act
        box.UpdateFromBoxValues(boxValues);

        // Assert
        Assert.That(box.Width, Is.EqualTo(boxValues.ContentWidth));
        Assert.That(box.Height, Is.EqualTo(boxValues.ContentHeight));

        Assert.That(box.PaddingTop, Is.EqualTo(boxValues.PaddingTop));
        Assert.That(box.PaddingRight, Is.EqualTo(boxValues.PaddingRight));
        Assert.That(box.PaddingBottom, Is.EqualTo(boxValues.PaddingBottom));
        Assert.That(box.PaddingLeft, Is.EqualTo(boxValues.PaddingLeft));

        Assert.That(box.BorderTop, Is.EqualTo(boxValues.BorderTop));
        Assert.That(box.BorderRight, Is.EqualTo(boxValues.BorderRight));
        Assert.That(box.BorderBottom, Is.EqualTo(boxValues.BorderBottom));
        Assert.That(box.BorderLeft, Is.EqualTo(boxValues.BorderLeft));

        Assert.That(box.MarginTop, Is.EqualTo(boxValues.MarginTop));
        Assert.That(box.MarginRight, Is.EqualTo(boxValues.MarginRight));
        Assert.That(box.MarginBottom, Is.EqualTo(boxValues.MarginBottom));
        Assert.That(box.MarginLeft, Is.EqualTo(boxValues.MarginLeft));
    }

    [Test]
    public void BorderBox_GetBoxMethods_ReturnsCorrectRects()
    {
        // Arrange - Create a layout box with known dimensions
        var box = new LayoutBox
        {
            X = 50,
            Y = 60,
            Width = 100,
            Height = 80,
            PaddingTop = 5,
            PaddingRight = 5,
            PaddingBottom = 5,
            PaddingLeft = 5,
            BorderTop = 10,
            BorderRight = 10,
            BorderBottom = 10,
            BorderLeft = 10
        };

        // Act
        var contentRect = box.ContentRect;
        var paddingBox = box.GetPaddingBox();
        var borderBox = box.GetBorderBox();

        // Assert
        // Content rect
        Assert.That(contentRect.X, Is.EqualTo(50));
        Assert.That(contentRect.Y, Is.EqualTo(60));
        Assert.That(contentRect.Width, Is.EqualTo(100));
        Assert.That(contentRect.Height, Is.EqualTo(80));

        // Padding box
        Assert.That(paddingBox.X, Is.EqualTo(50 - 5)); // X - PaddingLeft
        Assert.That(paddingBox.Y, Is.EqualTo(60 - 5)); // Y - PaddingTop
        Assert.That(paddingBox.Width, Is.EqualTo(100 + 5 + 5)); // Width + PaddingLeft + PaddingRight
        Assert.That(paddingBox.Height, Is.EqualTo(80 + 5 + 5)); // Height + PaddingTop + PaddingBottom

        // Border box
        Assert.That(borderBox.X, Is.EqualTo(50 - 5 - 10)); // X - PaddingLeft - BorderLeft
        Assert.That(borderBox.Y, Is.EqualTo(60 - 5 - 10)); // Y - PaddingTop - BorderTop
        Assert.That(borderBox.Width, Is.EqualTo(100 + 5 + 5 + 10 + 10)); // Width + PaddingLeft + PaddingRight + BorderLeft + BorderRight
        Assert.That(borderBox.Height, Is.EqualTo(80 + 5 + 5 + 10 + 10)); // Height + PaddingTop + PaddingBottom + BorderTop + BorderBottom
    }

    [Test]
    public void MarginBox_GetBoxMethods_ReturnsCorrectRects()
    {
        // Arrange - Create a layout box with known dimensions
        var box = new LayoutBox
        {
            X = 50,
            Y = 60,
            Width = 100,
            Height = 80,
            PaddingTop = 5,
            PaddingRight = 5,
            PaddingBottom = 5,
            PaddingLeft = 5,
            BorderTop = 10,
            BorderRight = 10,
            BorderBottom = 10,
            BorderLeft = 10,
            MarginTop = 15,
            MarginRight = 15,
            MarginBottom = 15,
            MarginLeft = 15
        };

        // Act
        var marginBox = box.GetMarginBox();

        // Assert
        Assert.That(marginBox.X, Is.EqualTo(50 - 5 - 10 - 15)); // X - PaddingLeft - BorderLeft - MarginLeft
        Assert.That(marginBox.Y, Is.EqualTo(60 - 5 - 10 - 15)); // Y - PaddingTop - BorderTop - MarginTop
        Assert.That(marginBox.Width, Is.EqualTo(100 + 5 + 5 + 10 + 10 + 15 + 15)); // Width + PaddingLeft + PaddingRight + BorderLeft + BorderRight + MarginLeft + MarginRight
        Assert.That(marginBox.Height, Is.EqualTo(80 + 5 + 5 + 10 + 10 + 15 + 15)); // Height + PaddingTop + PaddingBottom + BorderTop + BorderBottom + MarginTop + MarginBottom
    }

    [Test]
    public void Overlaps_WithDifferentScenarios_ReturnsCorrectResults()
    {
        // Arrange - Create two boxes
        var box1 = new LayoutBox
        {
            X = 10,
            Y = 10,
            Width = 100,
            Height = 100
        };

        var box2 = new LayoutBox();

        // Case 1: Boxes overlap
        box2.X = 50;
        box2.Y = 50;
        box2.Width = 100;
        box2.Height = 100;

        // Act & Assert
        Assert.That(box1.Overlaps(box2), Is.True, "Boxes should overlap");
        Assert.That(box2.Overlaps(box1), Is.True, "Overlap check should be symmetric");

        // Case 2: Boxes touch on edge (still considered overlap)
        box2.X = 110;
        box2.Y = 10;
        box2.Width = 100;
        box2.Height = 100;

        Assert.That(box1.Overlaps(box2), Is.True, "Boxes that touch on edge should overlap");

        // Case 3: Boxes don't overlap
        box2.X = 120;
        box2.Y = 10;

        Assert.That(box1.Overlaps(box2), Is.False, "Boxes should not overlap");

        // Case 4: One box contains the other
        box2.X = 20;
        box2.Y = 20;
        box2.Width = 50;
        box2.Height = 50;

        Assert.That(box1.Overlaps(box2), Is.True, "Containing box should overlap with contained box");
    }

    [Test]
    public void ContainsPoint_WithDifferentPoints_ReturnsCorrectResults()
    {
        // Arrange
        var box = new LayoutBox
        {
            X = 100,
            Y = 100,
            Width = 200,
            Height = 150
        };

        // Act & Assert

        // Point inside the box
        Assert.That(box.ContainsPoint(150, 150), Is.True, "Point inside box should be contained");

        // Point on the edge of the box (should be contained)
        Assert.That(box.ContainsPoint(100, 100), Is.True, "Point on top-left edge should be contained");
        Assert.That(box.ContainsPoint(300, 100), Is.True, "Point on top-right edge should be contained");
        Assert.That(box.ContainsPoint(100, 250), Is.True, "Point on bottom-left edge should be contained");
        Assert.That(box.ContainsPoint(300, 250), Is.True, "Point on bottom-right edge should be contained");

        // Point outside the box
        Assert.That(box.ContainsPoint(50, 150), Is.False, "Point to the left should not be contained");
        Assert.That(box.ContainsPoint(350, 150), Is.False, "Point to the right should not be contained");
        Assert.That(box.ContainsPoint(150, 50), Is.False, "Point above should not be contained");
        Assert.That(box.ContainsPoint(150, 300), Is.False, "Point below should not be contained");
    }
}