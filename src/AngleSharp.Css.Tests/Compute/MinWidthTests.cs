using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

namespace AngleSharp.Css.Tests.Compute
{
    using System;

    [TestFixture]
    public class MinWidthTests
    {
        private StyleCollection CreateStyleCollection(string css, IRenderDevice device = null)
        {
            var sheet = ParseStyleSheet(css);
            return new StyleCollection(new[] { sheet }, device ?? new DefaultRenderDevice());
        }

        private async Task<IDocument> ParseDocumentAsync(string css, string html)
        {
            var context = BrowsingContext.New(Configuration.Default.WithCss());
            var sheet = ParseStyleSheet(css);
            return await context.OpenAsync(req => req.Content(html));
        }

        // Absolute Units

        [Test]
        public async Task computes_absolute_min_width_in_px()
        {
            var css = @"div { min-width: 300px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("300px", divStyle.MinWidth);
        }

        [Test]
        public async Task computes_absolute_min_width_in_cm()
        {
            var css = @"div { min-width: 5cm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 5cm = 96px / 2.54 * 5 = 188.976px
            Assert.AreEqual("188.98px", Math.Round(double.Parse(divStyle.MinWidth.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        [Test]
        public async Task computes_absolute_min_width_in_mm()
        {
            var css = @"div { min-width: 50mm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 50mm = 96px / 2.54 * 5 = 188.976px
            Assert.AreEqual("188.98px", Math.Round(double.Parse(divStyle.MinWidth.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        [Test]
        public async Task computes_absolute_min_width_in_in()
        {
            var css = @"div { min-width: 2in; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2in = 96px/in * 2 = 192px
            Assert.AreEqual("192px", divStyle.MinWidth);
        }

        // Relative Units

        [Test]
        public async Task computes_relative_min_width_with_em()
        {
            var css = @"
            div { font-size: 16px; min-width: 3em; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 3em = font size * 3 = 16px * 3 = 48px
            Assert.AreEqual("48px", divStyle.MinWidth);
        }

        [Test]
        public async Task computes_relative_min_width_with_rem()
        {
            var css = @"
            html { font-size: 20px; }
            div { min-width: 2rem; }";
            var html = @"
            <html>
                <body>
                    <div>Text</div>
                </body>
            </html>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2rem = root font size * 2 = 20px * 2 = 40px
            Assert.AreEqual("40px", divStyle.MinWidth);
        }

        [Test]
        public async Task computes_min_width_with_percentage()
        {
            var css = @"div { min-width: 50%; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { ViewPortWidth = 800 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 50% of 800px = 400px
            Assert.AreEqual("400px", divStyle.MinWidth);
        }

        [Test]
        public async Task computes_min_width_with_viewport_units()
        {
            var css = @"div { min-width: 10vw; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { ViewPortWidth = 1920 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 10vw = 10% of viewport width = 192px
            Assert.AreEqual("192px", divStyle.MinWidth);
        }

        // Keywords

        [Test]
        public async Task computes_min_width_with_auto()
        {
            var css = @"div { min-width: auto; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // The "auto" keyword behavior depends on the browser's default logic; typically resolves to the intrinsic content width
            Assert.AreEqual("auto", divStyle.MinWidth);
        }

        // Edge Cases

        [Test]
        public async Task computes_min_width_with_inherit()
        {
            var css = @"
            div { min-width: 400px; }
            span { min-width: inherit; }";
            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits min-width from parent div = 400px
            Assert.AreEqual("400px", spanStyle.MinWidth);
        }
    }
}
