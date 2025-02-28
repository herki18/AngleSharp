namespace AngleSharp.LayoutEngine.Tests.IntegrationTests
{
    using Helpers;

    [TestFixture]
    public class FlexLayoutTests : LayoutEngineTestBase
    {
        [Test]
        public async Task FlexLayout_WithRowDirection_PositionsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            display: flex;
                            flex-direction: row;
                            width: 500px;
                            height: 200px;
                            background-color: #f0f0f0;
                        }
                        .item {
                            width: 100px;
                            height: 100px;
                        }
                        .item1 { background-color: red; }
                        .item2 { background-color: green; }
                        .item3 { background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='item item1'></div>
                        <div class='item item2'></div>
                        <div class='item item3'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var item1 = GetLayoutInfo(".item1");
            var item2 = GetLayoutInfo(".item2");
            var item3 = GetLayoutInfo(".item3");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Width, Is.EqualTo(500));
            Assert.That(container.Height, Is.EqualTo(200));

            // Verify items are positioned horizontally in a row
            Assert.That(item1.X, Is.EqualTo(0));
            Assert.That(item2.X, Is.EqualTo(100)); // item1.X + item1.Width
            Assert.That(item3.X, Is.EqualTo(200)); // item2.X + item2.Width

            // All items should have the same Y coordinate
            Assert.That(item1.Y, Is.EqualTo(item2.Y));
            Assert.That(item2.Y, Is.EqualTo(item3.Y));

            // Items should maintain their specified dimensions
            Assert.That(item1.Width, Is.EqualTo(100));
            Assert.That(item1.Height, Is.EqualTo(100));
        }

        [Test]
        public async Task FlexLayout_WithColumnDirection_PositionsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            display: flex;
                            flex-direction: column;
                            width: 300px;
                            height: 500px;
                            background-color: #f0f0f0;
                        }
                        .item {
                            width: 100px;
                            height: 100px;
                        }
                        .item1 { background-color: red; }
                        .item2 { background-color: green; }
                        .item3 { background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='item item1'></div>
                        <div class='item item2'></div>
                        <div class='item item3'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var item1 = GetLayoutInfo(".item1");
            var item2 = GetLayoutInfo(".item2");
            var item3 = GetLayoutInfo(".item3");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Width, Is.EqualTo(300));
            Assert.That(container.Height, Is.EqualTo(500));

            // Verify items are positioned vertically in a column
            Assert.That(item1.Y, Is.EqualTo(0));
            Assert.That(item2.Y, Is.EqualTo(100)); // item1.Y + item1.Height
            Assert.That(item3.Y, Is.EqualTo(200)); // item2.Y + item2.Height

            // All items should have the same X coordinate
            Assert.That(item1.X, Is.EqualTo(item2.X));
            Assert.That(item2.X, Is.EqualTo(item3.X));

            // Items should maintain their specified dimensions
            Assert.That(item1.Width, Is.EqualTo(100));
            Assert.That(item1.Height, Is.EqualTo(100));
        }

        [Test]
        public async Task FlexLayout_WithFlexGrow_DistributesSpace()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            display: flex;
                            width: 600px;
                            height: 100px;
                            background-color: #f0f0f0;
                        }
                        .item {
                            height: 100px;
                        }
                        .item1 {
                            flex-grow: 1;
                            background-color: red;
                        }
                        .item2 {
                            flex-grow: 2;
                            background-color: green;
                        }
                        .item3 {
                            flex-grow: 1;
                            background-color: blue;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='item item1'></div>
                        <div class='item item2'></div>
                        <div class='item item3'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var item1 = GetLayoutInfo(".item1");
            var item2 = GetLayoutInfo(".item2");
            var item3 = GetLayoutInfo(".item3");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Width, Is.EqualTo(600));

            // Verify items are distributed according to flex-grow
            // Total flex-grow = 4, so:
            // item1 width = 600 * (1/4) = 150px
            // item2 width = 600 * (2/4) = 300px
            // item3 width = 600 * (1/4) = 150px
            Assert.That(item1.Width, Is.EqualTo(150));
            Assert.That(item2.Width, Is.EqualTo(300));
            Assert.That(item3.Width, Is.EqualTo(150));

            // Verify items are positioned correctly
            Assert.That(item1.X, Is.EqualTo(0));
            Assert.That(item2.X, Is.EqualTo(150)); // item1.X + item1.Width
            Assert.That(item3.X, Is.EqualTo(450)); // item2.X + item2.Width
        }

        [Test]
        public async Task FlexLayout_WithJustifyContentAndAlignItems_PositionsCorrectly()
        {
            // Arrange
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        .container {
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            width: 600px;
                            height: 400px;
                            background-color: #f0f0f0;
                        }
                        .item {
                            width: 100px;
                            height: 100px;
                            margin: 10px;
                        }
                        .item1 { background-color: red; }
                        .item2 { background-color: green; }
                        .item3 { background-color: blue; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='item item1'></div>
                        <div class='item item2'></div>
                        <div class='item item3'></div>
                    </div>
                </body>
                </html>
            ";

            // Act
            await LoadHtmlAsync(html);

            // Assert
            var container = GetLayoutInfo(".container");
            var item1 = GetLayoutInfo(".item1");
            var item2 = GetLayoutInfo(".item2");
            var item3 = GetLayoutInfo(".item3");

            // Verify container dimensions
            Assert.NotNull(container);
            Assert.That(container.Width, Is.EqualTo(600));
            Assert.That(container.Height, Is.EqualTo(400));

            // Total content width: 3 * (100px + 20px margins) = 360px
            // Remaining space: 600px - 360px = 240px
            // For justify-content: center, space should be distributed evenly on left and right
            // So first item should start at 120px from left
            Assert.That(item1.X, Is.EqualTo(120).Within(1));

            // Items should be centered vertically (align-items: center)
            // Container height: 400px, Item height: 100px
            // So items should be positioned at (400 - 100) / 2 = 150px from top
            Assert.That(item1.Y, Is.EqualTo(150).Within(1));
            Assert.That(item2.Y, Is.EqualTo(150).Within(1));
            Assert.That(item3.Y, Is.EqualTo(150).Within(1));

            // Items should be positioned with their margins
            Assert.That(item2.X, Is.EqualTo(240).Within(1)); // item1.X + item1.Width + margins
            Assert.That(item3.X, Is.EqualTo(360).Within(1)); // item2.X + item2.Width + margins
        }
    }
}