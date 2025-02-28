namespace AngleSharp.LayoutEngine.Tests.BoxTests;

using AngleSharp.LayoutEngine.Box;
using NUnit.Framework;

[TestFixture]
public class RectTests
{
    [Test]
    public void Constructor_WithDefaultValues_InitializesCorrectly()
    {
        // Act
        var rect = new Rect();

        // Assert
        Assert.That(rect.X, Is.EqualTo(0));
        Assert.That(rect.Y, Is.EqualTo(0));
        Assert.That(rect.Width, Is.EqualTo(0));
        Assert.That(rect.Height, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithParameters_InitializesCorrectly()
    {
        // Act
        var rect = new Rect(10, 20, 100, 50);

        // Assert
        Assert.That(rect.X, Is.EqualTo(10));
        Assert.That(rect.Y, Is.EqualTo(20));
        Assert.That(rect.Width, Is.EqualTo(100));
        Assert.That(rect.Height, Is.EqualTo(50));
    }

    [Test]
    public void EdgeProperties_CalculatedCorrectly()
    {
        // Arrange
        var rect = new Rect(50, 60, 200, 150);

        // Assert
        Assert.That(rect.Left, Is.EqualTo(50));
        Assert.That(rect.Right, Is.EqualTo(250)); // X + Width
        Assert.That(rect.Top, Is.EqualTo(60));
        Assert.That(rect.Bottom, Is.EqualTo(210)); // Y + Height
    }

    [Test]
    public void Clone_CreatesExactCopy_WithIndependentValues()
    {
        // Arrange
        var original = new Rect(25, 35, 150, 120);

        // Act
        var clone = original.Clone();

        // Assert
        Assert.That(clone.X, Is.EqualTo(original.X));
        Assert.That(clone.Y, Is.EqualTo(original.Y));
        Assert.That(clone.Width, Is.EqualTo(original.Width));
        Assert.That(clone.Height, Is.EqualTo(original.Height));

        // Modify clone and verify original is unchanged
        clone.X = 100;
        clone.Y = 100;

        Assert.That(original.X, Is.EqualTo(25));
        Assert.That(original.Y, Is.EqualTo(35));
    }

    [Test]
    public void Contains_WithPointsInsideAndOutside_ReturnsCorrectResults()
    {
        // Arrange
        var rect = new Rect(100, 100, 200, 150);

        // Act & Assert

        // Points inside
        Assert.That(rect.Contains(150, 150), Is.True, "Point inside should be contained");
        Assert.That(rect.Contains(100, 100), Is.True, "Point on top-left corner should be contained");
        Assert.That(rect.Contains(300, 250), Is.True, "Point on bottom-right corner should be contained");

        // Points outside
        Assert.That(rect.Contains(50, 150), Is.False, "Point to the left should not be contained");
        Assert.That(rect.Contains(350, 150), Is.False, "Point to the right should not be contained");
        Assert.That(rect.Contains(150, 50), Is.False, "Point above should not be contained");
        Assert.That(rect.Contains(150, 300), Is.False, "Point below should not be contained");
    }

    [Test]
    public void IntersectsWith_WithOverlappingAndNonOverlappingRects_ReturnsCorrectResults()
    {
        // Arrange
        var rect1 = new Rect(100, 100, 200, 150);
        var rect2 = new Rect();

        // Case 1: Overlapping rectangles
        rect2.X = 200;
        rect2.Y = 150;
        rect2.Width = 200;
        rect2.Height = 200;

        // Act & Assert
        Assert.That(rect1.IntersectsWith(rect2), Is.True, "Rectangles should intersect");
        Assert.That(rect2.IntersectsWith(rect1), Is.True, "Intersection should be symmetric");

        // Case 2: Touching on edge (still considered intersection)
        rect2.X = 300;
        rect2.Y = 100;

        Assert.That(rect1.IntersectsWith(rect2), Is.True, "Rectangles touching on edge should intersect");

        // Case 3: No intersection
        rect2.X = 350;
        rect2.Y = 100;

        Assert.That(rect1.IntersectsWith(rect2), Is.False, "Non-overlapping rectangles should not intersect");

        // Case 4: One rectangle contained in the other
        rect2.X = 150;
        rect2.Y = 125;
        rect2.Width = 100;
        rect2.Height = 75;

        Assert.That(rect1.IntersectsWith(rect2), Is.True, "Container should intersect with contained rectangle");
        Assert.That(rect2.IntersectsWith(rect1), Is.True, "Contained rectangle should intersect with container");
    }

    [Test]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var rect = new Rect(10.5f, 20.75f, 100.25f, 50.5f);

        // Act
        var result = rect.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Rect(10.5,20.8,100.3,50.5)"));
    }
}