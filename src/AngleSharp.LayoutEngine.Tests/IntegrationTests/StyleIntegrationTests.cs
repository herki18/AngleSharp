#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace AngleSharp.LayoutEngine.Tests.Style
{
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;
    using AngleSharp.LayoutEngine.Adapters;
    using AngleSharp.LayoutEngine.Box;
    using AngleSharp.LayoutEngine.Core;
    using AngleSharp.LayoutEngine.Style;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class StyleIntegrationTests
    {
        private IBrowsingContext _context;
        private IDocument _document;
        private ViewportSynchronizer _synchronizer;

        [SetUp]
        public void Setup()
        {
            _context = BrowsingContext.New(Configuration.Default.WithCss());
            _synchronizer = new ViewportSynchronizer(_context, 1024, 768);
        }

        [TearDown]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        [Test]
        public async Task DirectStyleResolver_WithPixelValues_ReturnsCorrectPixels()
        {
            // Arrange
            _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #test { width: 100px; height: 200px; padding: 10px; margin: 20px; }
                    </style>
                </head>
                <body>
                    <div id='test'></div>
                </body>
                </html>
            "));

            var element = _document.GetElementById("test");
            var computedStyle = _document.DefaultView.GetComputedStyle(element);
            var context = _synchronizer.CreateLayoutContext();
            var adapter = new AngleSharpRenderDimensionsAdapter(context);
            var resolver = new DirectStyleResolver(computedStyle, adapter);

            // Act
            float width = resolver.ResolveLengthValue("width");
            float height = resolver.ResolveLengthValue("height");
            float padding = resolver.ResolveLengthValue("padding-top");
            float margin = resolver.ResolveLengthValue("margin-left");

            // Assert
            Assert.That(width, Is.EqualTo(100).Within(0.1f));
            Assert.That(height, Is.EqualTo(200).Within(0.1f));
            Assert.That(padding, Is.EqualTo(10).Within(0.1f));
            Assert.That(margin, Is.EqualTo(20).Within(0.1f));
        }

        [Test]
        public async Task DirectStyleResolver_WithPercentageValues_UsesCorrectContainerSize()
        {
            // Arrange
            _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; }
                        #container { width: 500px; height: 400px; }
                        #test { width: 50%; height: 25%; padding: 5%; }
                    </style>
                </head>
                <body>
                    <div id='container'>
                        <div id='test'></div>
                    </div>
                </body>
                </html>
            "));

            var test = _document.GetElementById("test");
            var testStyle = _document.DefaultView.GetComputedStyle(test);

            // Create context and adapter for the element
            var context = _synchronizer.CreateLayoutContext();

            // Create a mock layout node structure to test proper container referencing
            var containerNode = new LayoutNode(null);
            // containerNode.Box = new LayoutBox();
            containerNode.Box.Width = 500;
            containerNode.Box.Height = 400;

            var testNode = new LayoutNode(null);
            testNode.Parent = containerNode;

            var baseAdapter = new AngleSharpRenderDimensionsAdapter(context);
            var elementAdapter = AngleSharpRenderDimensionsAdapter.CreateForNode(testNode, baseAdapter);
            var resolver = new DirectStyleResolver(testStyle, elementAdapter);

            // Act
            float width = resolver.ResolveLengthValue("width");
            float height = resolver.ResolveLengthValue("height");
            float padding = resolver.ResolveLengthValue("padding-top");

            // Assert - percentages should be relative to container
            Assert.That(width, Is.EqualTo(250).Within(0.1f)); // 50% of 500
            Assert.That(height, Is.EqualTo(100).Within(0.1f)); // 25% of 400
            Assert.That(padding, Is.EqualTo(25).Within(0.1f)); // 5% of 500
        }

        [Test]
        public async Task DirectStyleResolver_WithFontRelativeUnits_UsesCorrectFontSize()
        {
            // Arrange
            _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        body { font-size: 16px; }
                        #parent { font-size: 20px; }
                        #test { width: 10em; height: 2rem; }
                    </style>
                </head>
                <body>
                    <div id='parent'>
                        <div id='test'></div>
                    </div>
                </body>
                </html>
            "));

            var test = _document.GetElementById("test");
            var testStyle = _document.DefaultView.GetComputedStyle(test);

            // Create mock layout nodes with font information
            var parentNode = new LayoutNode(null);
            var testNode = new LayoutNode(null);
            testNode.Parent = parentNode;

            // Create context with font size
            var context = _synchronizer.CreateLayoutContext();
            context.DefaultFontSize = 16; // Root font size

            // Create a custom test adapter with the specific font size we want to test
            var baseAdapter = new AngleSharpRenderDimensionsAdapter(context);
            var customAdapter = new TestFontAdapter(20, 16); // Parent font size = 20px, Root font size = 16px

            var resolver = new DirectStyleResolver(testStyle, customAdapter);

            // Act
            float width = resolver.ResolveLengthValue("width");
            float height = resolver.ResolveLengthValue("height");

            // Assert
            Assert.That(width, Is.EqualTo(200).Within(0.1f)); // 10em * 20px
            Assert.That(height, Is.EqualTo(32).Within(0.1f)); // 2rem * 16px (root font size)
        }

        [Test]
        public async Task BoxModelCalculator_WithEnhancedResolver_CalculatesCorrectly()
        {
            // Arrange
            _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #test {
                            width: 200px;
                            height: 150px;
                            padding: 10px 20px 30px 40px;
                            margin: 15px 25px 35px 45px;
                            border-width: 5px 10px 15px 20px;
                            box-sizing: border-box;
                        }
                    </style>
                </head>
                <body>
                    <div id='test'></div>
                </body>
                </html>
            "));

            var element = _document.GetElementById("test");
            var computedStyle = _document.DefaultView.GetComputedStyle(element);

            var context = _synchronizer.CreateLayoutContext();
            var adapter = new AngleSharpRenderDimensionsAdapter(context);
            var resolver = new DirectStyleResolver(computedStyle, adapter);

            // Act
            var calculator = new BoxModelCalculator(computedStyle, resolver);
            var boxValues = calculator.GetBoxValues();

            // Assert
            // With border-box, the content width should be 200 - (40 + 20) - (20 + 10) = 110
            Assert.That(boxValues.ContentWidth, Is.EqualTo(110).Within(0.1f));

            // With border-box, the content height should be 150 - (10 + 30) - (5 + 15) = 90
            Assert.That(boxValues.ContentHeight, Is.EqualTo(90).Within(0.1f));

            // Padding, border, and margin should match the specified values
            Assert.That(boxValues.PaddingTop, Is.EqualTo(10).Within(0.1f));
            Assert.That(boxValues.PaddingRight, Is.EqualTo(20).Within(0.1f));
            Assert.That(boxValues.PaddingBottom, Is.EqualTo(30).Within(0.1f));
            Assert.That(boxValues.PaddingLeft, Is.EqualTo(40).Within(0.1f));

            Assert.That(boxValues.BorderTop, Is.EqualTo(5).Within(0.1f));
            Assert.That(boxValues.BorderRight, Is.EqualTo(10).Within(0.1f));
            Assert.That(boxValues.BorderBottom, Is.EqualTo(15).Within(0.1f));
            Assert.That(boxValues.BorderLeft, Is.EqualTo(20).Within(0.1f));

            Assert.That(boxValues.MarginTop, Is.EqualTo(15).Within(0.1f));
            Assert.That(boxValues.MarginRight, Is.EqualTo(25).Within(0.1f));
            Assert.That(boxValues.MarginBottom, Is.EqualTo(35).Within(0.1f));
            Assert.That(boxValues.MarginLeft, Is.EqualTo(45).Within(0.1f));
        }

        [Test]
        public async Task ViewportSynchronizer_SynchronizesAngleSharpAndLayoutEngine()
        {
            // Arrange - Create a document with viewport-relative units
            _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #test1 { width: 50vw; height: 25vh; }
                        #test2 { width: 10vmin; height: 15vmax; }
                    </style>
                </head>
                <body>
                    <div id='test1'></div>
                    <div id='test2'></div>
                </body>
                </html>
            "));

            var test1 = _document.GetElementById("test1");
            var test2 = _document.GetElementById("test2");
            var test1Style = _document.DefaultView.GetComputedStyle(test1);
            var test2Style = _document.DefaultView.GetComputedStyle(test2);

            // Initial viewport size: 1024x768
            var context1 = _synchronizer.CreateLayoutContext();
            var adapter1 = new AngleSharpRenderDimensionsAdapter(context1);
            var resolver1 = new DirectStyleResolver(test1Style, adapter1);
            var resolver2 = new DirectStyleResolver(test2Style, adapter1);

            // Act - Measure with initial viewport
            float width1_initial = resolver1.ResolveLengthValue("width");
            float height1_initial = resolver1.ResolveLengthValue("height");
            float width2_initial = resolver2.ResolveLengthValue("width");
            float height2_initial = resolver2.ResolveLengthValue("height");

            // Now resize the viewport and measure again
            _synchronizer.ViewportWidth = 800;
            _synchronizer.ViewportHeight = 600;

            var context2 = _synchronizer.CreateLayoutContext();
            var adapter2 = new AngleSharpRenderDimensionsAdapter(context2);
            var resolver1_resized = new DirectStyleResolver(test1Style, adapter2);
            var resolver2_resized = new DirectStyleResolver(test2Style, adapter2);

            float width1_resized = resolver1_resized.ResolveLengthValue("width");
            float height1_resized = resolver1_resized.ResolveLengthValue("height");
            float width2_resized = resolver2_resized.ResolveLengthValue("width");
            float height2_resized = resolver2_resized.ResolveLengthValue("height");

            // Assert
            // Initial viewport: 1024x768
            Assert.That(width1_initial, Is.EqualTo(512).Within(0.1f)); // 50% of 1024
            Assert.That(height1_initial, Is.EqualTo(192).Within(0.1f)); // 25% of 768
            Assert.That(width2_initial, Is.EqualTo(76.8).Within(0.1f)); // 10% of min(1024, 768) = 76.8
            Assert.That(height2_initial, Is.EqualTo(153.6).Within(0.1f)); // 15% of max(1024, 768) = 153.6

            // Resized viewport: 800x600
            Assert.That(width1_resized, Is.EqualTo(400).Within(0.1f)); // 50% of 800
            Assert.That(height1_resized, Is.EqualTo(150).Within(0.1f)); // 25% of 600
            Assert.That(width2_resized, Is.EqualTo(60).Within(0.1f)); // 10% of min(800, 600) = 60
            Assert.That(height2_resized, Is.EqualTo(120).Within(0.1f)); // 15% of max(800, 600) = 120
        }

        /// <summary>
        /// Simple test adapter for font-relative units.
        /// </summary>
        private class TestFontAdapter : AngleSharpRenderDimensionsAdapter
        {
            private readonly double _fontSize;
            private readonly double _rootFontSize;

            public TestFontAdapter(double fontSize, double rootFontSize)
                : base(new LayoutContext(1024, 768))
            {
                _fontSize = fontSize;
                _rootFontSize = rootFontSize;
            }

            public override double FontSize => _fontSize;

            public override double RenderHeight => 768;

            public override double RenderWidth => 1024;
        }
    }
}