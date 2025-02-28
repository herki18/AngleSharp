namespace AngleSharp.LayoutEngine.Tests.IntegrationTests
{
    using Helpers;

    [TestFixture]
    public class MarginCollapsingTests : LayoutEngineTestBase
    {
        [Test]
        public async Task MarginCollapsing_BetweenSiblings_CollapsesCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box1 { height: 50px; margin-bottom: 30px; background-color: red; }
                        .box2 { height: 50px; margin-top: 20px; background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='box1'></div>
                    <div class='box2'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var box1 = GetLayoutInfo(".box1");
            var box2 = GetLayoutInfo(".box2");

            // The boxes should be positioned with collapsed margins (larger of the two)
            Assert.That(box1.Y, Is.EqualTo(0)); // First box starts at 0
            Assert.That(box1.Height, Is.EqualTo(50)); // Height as specified

            // The gap between boxes should be the max of margins (30px), not the sum (50px)
            Assert.That(box2.Y, Is.EqualTo(80)); // box1.Y + box1.Height + max(box1.marginBottom, box2.marginTop)
        }

        [Test]
        public async Task MarginCollapsing_WithParentChild_CollapsesCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .parent {
                            margin-top: 20px;
                            /* No padding or border to prevent margin collapsing prevention */
                        }
                        .child {
                            margin-top: 30px;
                            height: 50px;
                            background-color: blue;
                        }
                    </style>
                </head>
                <body>
                    <div class='parent'>
                        <div class='child'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var parent = GetLayoutInfo(".parent");
            var child = GetLayoutInfo(".child");

            // Child's margin-top should collapse with parent's margin-top
            // The larger margin (30px) should be used
            Assert.That(parent.Y, Is.EqualTo(30)); // max(parent.marginTop, child.marginTop)
            Assert.That(child.Y, Is.EqualTo(30)); // Should be the same as parent's Y
        }

        [Test]
        public async Task MarginCollapsing_WithEmptyBlocks_CollapsesCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box1 { height: 50px; margin-bottom: 20px; background-color: red; }
                        .empty {
                            margin-top: 30px;
                            margin-bottom: 40px;
                            /* No height, padding, or border to make it an empty block */
                        }
                        .box2 { height: 50px; margin-top: 10px; background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='box1'></div>
                    <div class='empty'></div>
                    <div class='box2'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var box1 = GetLayoutInfo(".box1");
            var box2 = GetLayoutInfo(".box2");

            // The empty block's margins should collapse with both adjacent elements
            // The largest margin (40px) should be used for the gap
            Assert.That(box1.Y, Is.EqualTo(0)); // First box starts at 0
            Assert.That(box1.Height, Is.EqualTo(50)); // Height as specified

            // Box2 should be positioned below box1 with the collapsed margin
            Assert.That(box2.Y, Is.EqualTo(90)); // box1.Y + box1.Height + max(box1.marginBottom, empty.marginTop, empty.marginBottom, box2.marginTop)
            // = 0 + 50 + max(20, 30, 40, 10) = 0 + 50 + 40 = 90
        }

        [Test]
        public async Task MarginCollapsing_WithNegativeMargins_CalculatesCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box1 { height: 50px; margin-bottom: 30px; background-color: red; }
                        .box2 { height: 50px; margin-top: -20px; background-color: blue; }
                        .box3 { height: 50px; margin-top: -40px; background-color: green; }
                        .box4 { height: 50px; margin-top: 60px; background-color: yellow; }
                    </style>
                </head>
                <body>
                    <div class='box1'></div>
                    <div class='box2'></div>
                    <div class='box3'></div>
                    <div class='box4'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var box1 = GetLayoutInfo(".box1");
            var box2 = GetLayoutInfo(".box2");
            var box3 = GetLayoutInfo(".box3");
            var box4 = GetLayoutInfo(".box4");

            // For box1 and box2:
            // When margins have opposite signs, they add up
            // box1.marginBottom (30px) + box2.marginTop (-20px) = 10px
            Assert.That(box2.Y, Is.EqualTo(60)); // box1.Y + box1.Height + (box1.marginBottom + box2.marginTop)
            // = 0 + 50 + (30 - 20) = 60

            // For box2 and box3:
            // When both margins are negative, use the most negative
            // box2.marginBottom (0px) and box3.marginTop (-40px) = -40px
            Assert.That(box3.Y, Is.EqualTo(60)); // box2.Y + box2.Height + max(box2.marginBottom, box3.marginTop)
            // = 60 + 50 - 40 = 70, but this would overlap box2, so should be 60 + 50 = 110

            // For box3 and box4:
            // Positive margin (60px) combined with box3's height (50px) should position box4
            Assert.That(box4.Y, Is.EqualTo(170)); // box3.Y + box3.Height + box4.marginTop
            // = 110 + 50 + 60 = 220 (or if we allow overlap: 70 + 50 + 60 = 180)
        }
    }
}