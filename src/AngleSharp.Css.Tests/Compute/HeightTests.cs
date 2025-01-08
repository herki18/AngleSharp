using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

namespace AngleSharp.Css.Tests.Compute
{
    using System;

    [TestFixture]
    public class HeightTests
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
        public async Task computes_absolute_height_in_px()
        {
            var css = @"div { height: 100px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("100px", divStyle.Height);
        }

        [Test]
        public async Task computes_absolute_height_in_cm()
        {
            var css = @"div { height: 2cm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2cm = 96px / 2.54 * 2 = 75.59px
            Assert.AreEqual("75.59px", Math.Round(double.Parse(divStyle.Height.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        [Test]
        public async Task computes_absolute_height_in_in()
        {
            var css = @"div { height: 1in; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 1in = 96px
            Assert.AreEqual("96px", divStyle.Height);
        }

        [Test]
        public async Task computes_absolute_height_in_mm()
        {
            var css = @"div { height: 10mm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 10mm = 96px / 2.54 * 10 / 10 = ~37.8px
            Assert.AreEqual("37.8px", Math.Round(double.Parse(divStyle.Height.TrimEnd("px".ToCharArray())), 1) + "px");
        }

        // Relative Units

        [Test]
        public async Task computes_relative_height_with_em()
        {
            var css = @"
            div { font-size: 16px; height: 2em; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2em = font size * 2 = 16px * 2 = 32px
            Assert.AreEqual("32px", divStyle.Height);
        }

        [Test]
        public async Task computes_relative_height_with_rem()
        {
            var css = @"
            html { font-size: 20px; }
            div { height: 1.5rem; }";
            var html = @"
            <html>
                <body>
                    <div>Text</div>
                </body>
            </html>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 1.5rem = root font size * 1.5 = 20px * 1.5 = 30px
            Assert.AreEqual("30px", divStyle.Height);
        }

        [Test]
        public async Task computes_height_with_percentage()
        {
            var css = @"div { height: 50%; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { ViewPortHeight = 400 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 50% of 400px = 200px
            Assert.AreEqual("200px", divStyle.Height);
        }

        // Keywords

        [Test]
        public async Task computes_height_with_auto()
        {
            var css = @"div { height: auto; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // Height for auto should resolve based on content, which depends on implementation
            // Example assumption: "auto" resolves to 0px if no content
            Assert.AreEqual("auto", divStyle.Height);
        }

        // Edge Cases

        [Test]
        public async Task computes_height_with_inherit()
        {
            var css = @"
            div { height: 200px; }
            span { height: inherit; }";
            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits height from parent div = 200px
            Assert.AreEqual("200px", spanStyle.Height);
        }
    }
}
