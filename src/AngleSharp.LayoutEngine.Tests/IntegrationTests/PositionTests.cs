namespace AngleSharp.LayoutEngine.Tests.IntegrationTests
{
    using Helpers;

    [TestFixture]
    public class PositionTests : LayoutEngineTestBase
    {
        [Test]
        public async Task Position_WithRelative_OffsetsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .box {
                            width: 100px;
                            height: 100px;
                            background-color: red;
                        }
                        .relative {
                            position: relative;
                            top: 20px;
                            left: 30px;
                        }
                        .relative-negative {
                            position: relative;
                            top: -15px;
                            left: -25px;
                        }
                    </style>
                </head>
                <body>
                    <div class='box'></div>
                    <div class='box relative'></div>
                    <div class='box relative-negative'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var box1 = GetLayoutInfo(".box:not(.relative):not(.relative-negative)");
            var relativeBox = GetLayoutInfo(".relative");
            var negativeBox = GetLayoutInfo(".relative-negative");

            // Normal flow positions
            Assert.That(box1.X, Is.EqualTo(0));
            Assert.That(box1.Y, Is.EqualTo(0));

            // Calculate expected normal flow position for second box
            float normalX = 0;
            float normalY = 100; // Below the first box

            // Relative position should be normal flow position + offsets
            Assert.That(relativeBox.X, Is.EqualTo(normalX + 30));
            Assert.That(relativeBox.Y, Is.EqualTo(normalY + 20));

            // Calculate expected normal flow position for third box
            float normalX3 = 0;
            float normalY3 = 200; // Below the second box

            // Relative position with negative offsets
            Assert.That(negativeBox.X, Is.EqualTo(normalX3 - 25));
            Assert.That(negativeBox.Y, Is.EqualTo(normalY3 - 15));
        }

        [Test]
        public async Task Position_WithAbsolute_PositionsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            position: relative;
                            width: 300px;
                            height: 300px;
                            background-color: #f0f0f0;
                        }
                        .box {
                            width: 100px;
                            height: 100px;
                            background-color: red;
                        }
                        .absolute {
                            position: absolute;
                            top: 50px;
                            left: 50px;
                        }
                        .absolute-bottom-right {
                            position: absolute;
                            bottom: 30px;
                            right: 40px;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='box'></div>
                        <div class='box absolute'></div>
                        <div class='box absolute-bottom-right'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var box1 = GetLayoutInfo(".box:not(.absolute):not(.absolute-bottom-right)");
            var absoluteBox = GetLayoutInfo(".absolute");
            var bottomRightBox = GetLayoutInfo(".absolute-bottom-right");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Width, Is.EqualTo(300));
            Assert.That(container.Height, Is.EqualTo(300));

            // Normal flow box should be positioned at the top of the container
            Assert.That(box1.X, Is.EqualTo(0));
            Assert.That(box1.Y, Is.EqualTo(0));

            // Absolute box should be positioned relative to the container
            Assert.That(absoluteBox.X, Is.EqualTo(50));
            Assert.That(absoluteBox.Y, Is.EqualTo(50));

            // Bottom-right box should be positioned from bottom-right corner
            Assert.That(bottomRightBox.X, Is.EqualTo(300 - 40 - 100)); // container.width - right - box.width
            Assert.That(bottomRightBox.Y, Is.EqualTo(300 - 30 - 100)); // container.height - bottom - box.height
        }

        [Test]
        public async Task Position_WithFixed_PositionsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {
                            margin: 0;
                            padding: 0;
                            height: 2000px; /* Make body tall for scrolling */
                        }
                        .box {
                            width: 100px;
                            height: 100px;
                            background-color: red;
                        }
                        .fixed {
                            position: fixed;
                            top: 50px;
                            left: 50px;
                        }
                        .fixed-bottom-right {
                            position: fixed;
                            bottom: 30px;
                            right: 40px;
                        }
                    </style>
                </head>
                <body>
                    <div class='box'></div>
                    <div class='box fixed'></div>
                    <div class='box fixed-bottom-right'></div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var normalBox = GetLayoutInfo(".box:not(.fixed):not(.fixed-bottom-right)");
            var fixedBox = GetLayoutInfo(".fixed");
            var bottomRightBox = GetLayoutInfo(".fixed-bottom-right");

            // Normal flow box
            Assert.That(normalBox.X, Is.EqualTo(0));
            Assert.That(normalBox.Y, Is.EqualTo(0));

            // Fixed position box should be positioned relative to the viewport
            Assert.That(fixedBox.X, Is.EqualTo(50));
            Assert.That(fixedBox.Y, Is.EqualTo(50));

            // Bottom-right fixed box should be positioned from viewport bottom-right
            Assert.That(bottomRightBox.X, Is.EqualTo(DefaultViewportWidth - 40 - 100)); // viewport.width - right - box.width
            Assert.That(bottomRightBox.Y, Is.EqualTo(DefaultViewportHeight - 30 - 100)); // viewport.height - bottom - box.height
        }

        [Test]
        public async Task Position_WithSticky_BehavesCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {
                            margin: 0;
                            padding: 0;
                        }
                        .container {
                            height: 400px;
                            overflow: auto;
                            background-color: #f0f0f0;
                        }
                        .content {
                            height: 1000px; /* Tall content to enable scrolling */
                        }
                        .box {
                            width: 100px;
                            height: 50px;
                            background-color: red;
                            margin: 10px;
                        }
                        .sticky {
                            position: sticky;
                            top: 20px;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='content'>
                            <div class='box'></div>
                            <div class='box sticky'></div>
                            <div class='box'></div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var normalBox1 = GetLayoutInfo(".box:not(.sticky):first-child");
            var stickyBox = GetLayoutInfo(".sticky");
            var normalBox2 = GetLayoutInfo(".box:not(.sticky):last-child");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Height, Is.EqualTo(400));

            // Initial positions - should be in normal flow at first
            Assert.That(normalBox1.Y, Is.GreaterThanOrEqualTo(10)); // Accounting for margin
            Assert.That(stickyBox.Y, Is.GreaterThan(normalBox1.Y + normalBox1.Height)); // Below first box
            Assert.That(normalBox2.Y, Is.GreaterThan(stickyBox.Y + stickyBox.Height)); // Below sticky box

            // Note: We can't test actual sticky behavior in this test since it depends on scroll position
            // In real-world testing, the sticky element would move with scrolling until it hits its 'top' position
        }

        [Test]
        public async Task Position_WithZIndex_OrdersCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            position: relative;
                            width: 300px;
                            height: 300px;
                        }
                        .box {
                            position: absolute;
                            width: 100px;
                            height: 100px;
                        }
                        .box1 {
                            background-color: red;
                            top: 50px;
                            left: 50px;
                            z-index: 1;
                        }
                        .box2 {
                            background-color: green;
                            top: 80px;
                            left: 80px;
                            z-index: 2;
                        }
                        .box3 {
                            background-color: blue;
                            top: 110px;
                            left: 110px;
                            z-index: 3;
                        }
                        .box4 {
                            background-color: yellow;
                            top: 140px;
                            left: 140px;
                            /* No z-index specified, should default to auto (0) */
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='box box1'></div>
                        <div class='box box2'></div>
                        <div class='box box3'></div>
                        <div class='box box4'></div>
                    </div>
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

            // Verify positions
            Assert.That(box1.X, Is.EqualTo(50));
            Assert.That(box1.Y, Is.EqualTo(50));
            Assert.That(box2.X, Is.EqualTo(80));
            Assert.That(box2.Y, Is.EqualTo(80));
            Assert.That(box3.X, Is.EqualTo(110));
            Assert.That(box3.Y, Is.EqualTo(110));
            Assert.That(box4.X, Is.EqualTo(140));
            Assert.That(box4.Y, Is.EqualTo(140));

            // Note: We can't directly test z-index stacking in this test
            // since the layout engine only computes positions, not rendering.
            // In a full browser, box3 would appear on top, then box2, then box1,
            // and box4 would be behind all of them due to the lack of z-index.
        }
    }
}