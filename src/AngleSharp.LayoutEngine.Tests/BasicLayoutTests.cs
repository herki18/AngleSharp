namespace AngleSharp.LayoutEngine.Tests
{
    [TestFixture]
    public class BasicLayoutTests : LayoutEngineTestBase
    {
        [Test]
        public async Task BasicBlockLayout_ShouldPositionElementsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box1 { width: 100px; height: 100px; background-color: red; }
                        .box2 { width: 150px; height: 80px; background-color: blue; }
                        .box3 { width: 200px; height: 120px; background-color: green; }
                    </style>
                </head>
                <body>
                    <div class='box1'></div>
                    <div class='box2'></div>
                    <div class='box3'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            LayoutTreePrinter.PrintToConsole(LayoutResult.LayoutTree);

            // Assert
            AssertLayout(".box1", 0, 0, 100, 100);
            AssertLayout(".box2", 0, 100, 150, 80);
            AssertLayout(".box3", 0, 180, 200, 120);
        }

        [Test]
        public async Task MarginsAndPadding_ShouldAffectLayout()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container { width: 400px; padding: 20px; border: 5px solid black; }
                        .box { height: 50px; margin: 10px; background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='box' id='box1'></div>
                        <div class='box' id='box2'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            Assert.NotNull(container, "Container not found");

            // Container should have width of 400px plus padding of 40px (20px each side) plus border of 10px (5px each side)
            Assert.That(container.BorderBoxWidth, Is.EqualTo(450).Within(1));

            // Boxes should have container width minus padding (360px) and be 50px tall
            AssertLayout("#box1", 25, 25, 360, 50);

            // Second box should be below first box accounting for margins
            // First box top: 25 (container padding) + 10 (box margin)
            // First box height: 50
            // First box bottom margin: 10
            // Second box top margin: 10 (collapses with first box bottom margin)
            // So second box should be at y = 25 + 10 + 50 + 10 = 95
            AssertLayout("#box2", 25, 95, 360, 50);
        }

        [Test]
        public async Task MarginCollapsing_ShouldWorkCorrectly()
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
                        .box3 { height: 50px; margin-top: 40px; margin-bottom: 10px; background-color: green; }
                        .box4 { height: 50px; margin-top: 30px; background-color: yellow; }
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
            // Margins should collapse when adjacent vertically
            AssertLayout(".box1", 0, 0, DefaultViewportWidth, 50);

            // The larger of the two margins (30px) should be used
            AssertLayout(".box2", 0, 80, DefaultViewportWidth, 50);

            // box3 should be 40px below box2 (using the larger of the margins)
            AssertLayout(".box3", 0, 170, DefaultViewportWidth, 50);

            // box4 should be 40px below box3 (using the larger of the margins)
            AssertLayout(".box4", 0, 260, DefaultViewportWidth, 50);
        }

        [Test]
        public async Task PositionRelative_ShouldOffsetFromNormalFlow()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box { width: 100px; height: 100px; }
                        .normal { background-color: gray; }
                        .relative { position: relative; top: 20px; left: 30px; background-color: blue; }
                        .after { background-color: green; }
                    </style>
                </head>
                <body>
                    <div class='box normal'></div>
                    <div class='box relative'></div>
                    <div class='box after'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            AssertLayout(".normal", 0, 0, 100, 100);

            // Relative positioning should offset from normal flow position
            AssertLayout(".relative", 30, 120, 100, 100);

            // Elements after a relatively positioned element should be positioned as if
            // the relatively positioned element were in its normal flow position
            AssertLayout(".after", 0, 200, 100, 100);
        }

        [Test]
        public async Task PositionAbsolute_ShouldRemoveFromNormalFlow()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container { position: relative; width: 300px; height: 300px; border: 1px solid black; }
                        .normal { width: 100px; height: 50px; background-color: gray; }
                        .absolute { position: absolute; top: 50px; left: 50px; width: 100px; height: 100px; background-color: blue; }
                        .after { width: 100px; height: 50px; background-color: green; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='normal'></div>
                        <div class='absolute'></div>
                        <div class='after'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            Assert.NotNull(container, "Container not found");

            // Normal element should be at the top of the container
            AssertLayout(".normal", container.X, container.Y, 100, 50);

            // Absolute element should be positioned relative to the container
            AssertLayout(".absolute", container.X + 50, container.Y + 50, 100, 100);

            // After element should be positioned as if the absolute element didn't exist
            AssertLayout(".after", container.X, container.Y + 50, 100, 50);
        }

        [Test]
        public async Task InlineElements_ShouldFlowHorizontally()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; font-family: sans-serif; font-size: 16px; }
                        .container { width: 500px; }
                        .inline { display: inline; background-color: yellow; padding: 5px; margin: 5px; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <span class='inline' id='span1'>First</span>
                        <span class='inline' id='span2'>Second</span>
                        <span class='inline' id='span3'>Third</span>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var span1 = GetLayoutInfo("#span1");
            var span2 = GetLayoutInfo("#span2");
            var span3 = GetLayoutInfo("#span3");

            Assert.NotNull(span1, "span1 not found");
            Assert.NotNull(span2, "span2 not found");
            Assert.NotNull(span3, "span3 not found");

            // Spans should be next to each other horizontally
            Assert.That(span2.X, Is.GreaterThan(span1.X), "span2 should be to the right of span1");
            Assert.That(span3.X, Is.GreaterThan(span2.X), "span3 should be to the right of span2");

            // Spans should be on the same line (approximately same Y coordinate)
            Assert.That(span2.Y, Is.EqualTo(span1.Y).Within(1), "span2 should be on the same line as span1");
            Assert.That(span3.Y, Is.EqualTo(span1.Y).Within(1), "span3 should be on the same line as span1");
        }

        [Test]
        public async Task IncrementalLayout_ShouldUpdateChangedElements()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container { width: 400px; }
                        .box { height: 100px; margin: 10px; background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='box' id='box1'></div>
                        <div class='box' id='box2'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Record initial positions
            var box1Before = GetLayoutInfo("#box1");
            var box2Before = GetLayoutInfo("#box2");

            Assert.NotNull(box1Before, "box1 not found before update");
            Assert.NotNull(box2Before, "box2 not found before update");

            // Modify box1's height
            var box1 = QuerySelector("#box1");
            box1.SetAttribute("style", "height: 200px; margin: 10px; background-color: blue;");

            // Update layout
            UpdateLayout(box1);

            // Get updated positions
            var box1After = GetLayoutInfo("#box1");
            var box2After = GetLayoutInfo("#box2");

            Assert.NotNull(box1After, "box1 not found after update");
            Assert.NotNull(box2After, "box2 not found after update");

            // Box1 height should have changed
            Assert.That(box1After.Height, Is.EqualTo(200).Within(1), "box1 height should be 200px after update");

            // Box2 should have moved down
            float expectedY = box1After.Y + box1After.Height + 20; // 20px accounts for margins
            Assert.That(box2After.Y, Is.EqualTo(expectedY).Within(1),
                $"box2 should be at Y={expectedY} after box1 height change");
        }
    }
}