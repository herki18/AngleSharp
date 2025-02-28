#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace AngleSharp.LayoutEngine.Tests.Helpers
{
    using System.Threading.Tasks;
    using AngleSharp;
    using AngleSharp.Dom;
    using AngleSharp.LayoutEngine.API;
    using NUnit.Framework;

    /// <summary>
    /// Base class for layout engine tests providing common setup and utilities.
    /// </summary>
    public abstract class LayoutEngineTestBase
    {
        protected IBrowsingContext Context { get; private set; }
        protected AngleSharpLayoutProvider LayoutProvider { get; private set; }
        protected IDocument Document { get; private set; }
        protected LayoutResult LayoutResult { get; private set; }

        /// <summary>
        /// Default viewport width used for tests.
        /// </summary>
        protected const float DefaultViewportWidth = 1024;

        /// <summary>
        /// Default viewport height used for tests.
        /// </summary>
        protected const float DefaultViewportHeight = 768;

        [SetUp]
        public void Setup()
        {
            Context = BrowsingContext.New(Configuration.Default.WithCss());
            LayoutProvider = new AngleSharpLayoutProvider();
            LayoutProvider.ViewportWidth = DefaultViewportWidth;
            LayoutProvider.ViewportHeight = DefaultViewportHeight;
        }

        [TearDown]
        public void TearDown()
        {
            Context?.Dispose();
        }

        /// <summary>
        /// Loads HTML content and performs initial layout.
        /// </summary>
        /// <param name="html">The HTML content to load.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async Task LoadHtmlAsync(string html)
        {
            Document = await Context.OpenAsync(req => req.Content(html));
            LayoutResult = LayoutProvider.PerformLayout(Document);
        }

        /// <summary>
        /// Gets a DOM element by its CSS selector.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <returns>The element, or null if not found.</returns>
        protected IElement QuerySelector(string selector)
        {
            return Document?.QuerySelector(selector);
        }

        /// <summary>
        /// Gets layout information for an element.
        /// </summary>
        /// <param name="element">The element to get layout for.</param>
        /// <returns>The layout info, or null if not available.</returns>
        protected LayoutInfo GetLayoutInfo(IElement element)
        {
            return LayoutProvider.GetLayoutForElement(element);
        }

        /// <summary>
        /// Gets layout information for an element selected by CSS selector.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <returns>The layout info, or null if not available.</returns>
        protected LayoutInfo GetLayoutInfo(string selector)
        {
            var element = QuerySelector(selector);
            return element != null ? LayoutProvider.GetLayoutForElement(element) : null;
        }

        /// <summary>
        /// Asserts that an element has expected position and size.
        /// </summary>
        /// <param name="selector">The CSS selector for the element.</param>
        /// <param name="expectedX">The expected X coordinate.</param>
        /// <param name="expectedY">The expected Y coordinate.</param>
        /// <param name="expectedWidth">The expected width.</param>
        /// <param name="expectedHeight">The expected height.</param>
        /// <param name="tolerance">The allowable difference in values.</param>
        protected void AssertLayout(
            string selector,
            float expectedX,
            float expectedY,
            float expectedWidth,
            float expectedHeight,
            float tolerance = 0.1f)
        {
            var layout = GetLayoutInfo(selector);
            Assert.NotNull(layout, $"Layout info for '{selector}' not found");

            Assert.That(layout.X, Is.EqualTo(expectedX).Within(tolerance),
                $"X coordinate for '{selector}' is {layout.X} but expected {expectedX}");

            Assert.That(layout.Y, Is.EqualTo(expectedY).Within(tolerance),
                $"Y coordinate for '{selector}' is {layout.Y} but expected {expectedY}");

            Assert.That(layout.Width, Is.EqualTo(expectedWidth).Within(tolerance),
                $"Width for '{selector}' is {layout.Width} but expected {expectedWidth}");

            Assert.That(layout.Height, Is.EqualTo(expectedHeight).Within(tolerance),
                $"Height for '{selector}' is {layout.Height} but expected {expectedHeight}");
        }

        /// <summary>
        /// Asserts that two elements have the expected relationship.
        /// </summary>
        /// <param name="selector1">The CSS selector for the first element.</param>
        /// <param name="selector2">The CSS selector for the second element.</param>
        /// <param name="expectedHorizontalRelation">The expected horizontal relation (1 if first is to the right, -1 if to the left, 0 if aligned).</param>
        /// <param name="expectedVerticalRelation">The expected vertical relation (1 if first is below, -1 if above, 0 if aligned).</param>
        protected void AssertElementRelation(
            string selector1,
            string selector2,
            int expectedHorizontalRelation,
            int expectedVerticalRelation)
        {
            var layout1 = GetLayoutInfo(selector1);
            var layout2 = GetLayoutInfo(selector2);

            Assert.NotNull(layout1, $"Layout info for '{selector1}' not found");
            Assert.NotNull(layout2, $"Layout info for '{selector2}' not found");

            // Check horizontal relation
            if (expectedHorizontalRelation > 0)
            {
                Assert.That(layout1.X, Is.GreaterThan(layout2.X),
                    $"'{selector1}' should be to the right of '{selector2}'");
            }
            else if (expectedHorizontalRelation < 0)
            {
                Assert.That(layout1.X, Is.LessThan(layout2.X),
                    $"'{selector1}' should be to the left of '{selector2}'");
            }
            else
            {
                Assert.That(layout1.X, Is.EqualTo(layout2.X).Within(0.1f),
                    $"'{selector1}' should be horizontally aligned with '{selector2}'");
            }

            // Check vertical relation
            if (expectedVerticalRelation > 0)
            {
                Assert.That(layout1.Y, Is.GreaterThan(layout2.Y),
                    $"'{selector1}' should be below '{selector2}'");
            }
            else if (expectedVerticalRelation < 0)
            {
                Assert.That(layout1.Y, Is.LessThan(layout2.Y),
                    $"'{selector1}' should be above '{selector2}'");
            }
            else
            {
                Assert.That(layout1.Y, Is.EqualTo(layout2.Y).Within(0.1f),
                    $"'{selector1}' should be vertically aligned with '{selector2}'");
            }
        }

        /// <summary>
        /// Creates a document from HTML string input.
        /// </summary>
        /// <param name="html">The HTML content to parse.</param>
        /// <returns>The created document.</returns>
        protected IDocument CreateDocument(string html)
        {
            return Context.OpenAsync(req => req.Content(html)).Result;
        }

        /// <summary>
        /// Updates the layout after modifying the document.
        /// </summary>
        /// <param name="modifiedElement">The element that was modified.</param>
        protected void UpdateLayout(IElement modifiedElement)
        {
            LayoutProvider.UpdateLayoutForElement(modifiedElement);
        }

        /// <summary>
        /// Resizes the viewport and updates the layout.
        /// </summary>
        /// <param name="width">The new viewport width.</param>
        /// <param name="height">The new viewport height.</param>
        protected void ResizeViewport(float width, float height)
        {
            LayoutResult = LayoutProvider.ResizeViewport(width, height);
        }
    }
}