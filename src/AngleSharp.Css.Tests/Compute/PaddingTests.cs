using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

namespace AngleSharp.Css.Tests.Compute
{
    using System;

    [TestFixture]
    public class PaddingTests
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
        public async Task computes_absolute_padding_in_px()
        {
            var css = @"div { padding: 10px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.PaddingTop);
            Assert.AreEqual("10px", divStyle.PaddingRight);
            Assert.AreEqual("10px", divStyle.PaddingBottom);
            Assert.AreEqual("10px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_absolute_padding_in_cm()
        {
            var css = @"div { padding: 2cm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2cm = 96px / 2.54 * 2 = 75.59px
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.PaddingTop.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.PaddingRight.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.PaddingBottom.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.PaddingLeft.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        [Test]
        public async Task computes_absolute_padding_in_mm()
        {
            var css = @"div { padding: 10mm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 10mm = 96px / 2.54 * 1 = ~37.8px
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.PaddingTop.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.PaddingRight.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.PaddingBottom.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.PaddingLeft.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        // Relative Units

        [Test]
        public async Task computes_relative_padding_in_em()
        {
            var css = @"
            div { font-size: 16px; padding: 2em; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2em = font size * 2 = 16px * 2 = 32px
            Assert.AreEqual("32px", divStyle.PaddingTop);
            Assert.AreEqual("32px", divStyle.PaddingRight);
            Assert.AreEqual("32px", divStyle.PaddingBottom);
            Assert.AreEqual("32px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_relative_padding_in_rem()
        {
            var css = @"
            html { font-size: 20px; }
            div { padding: 1.5rem; }";
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
            Assert.AreEqual("30px", divStyle.PaddingTop);
            Assert.AreEqual("30px", divStyle.PaddingRight);
            Assert.AreEqual("30px", divStyle.PaddingBottom);
            Assert.AreEqual("30px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_padding_with_percentage()
        {
            var css = @"div { padding: 50%; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { ViewPortWidth = 800 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 50% of 800px = 400px
            Assert.AreEqual("400px", divStyle.PaddingTop);
            Assert.AreEqual("400px", divStyle.PaddingRight);
            Assert.AreEqual("400px", divStyle.PaddingBottom);
            Assert.AreEqual("400px", divStyle.PaddingLeft);
        }

        // Shorthand Properties

        [Test]
        public async Task computes_shorthand_padding_with_four_values()
        {
            var css = @"div { padding: 10px 20px 30px 40px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.PaddingTop);
            Assert.AreEqual("20px", divStyle.PaddingRight);
            Assert.AreEqual("30px", divStyle.PaddingBottom);
            Assert.AreEqual("40px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_shorthand_padding_with_three_values()
        {
            var css = @"div { padding: 10px 20px 30px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.PaddingTop);
            Assert.AreEqual("20px", divStyle.PaddingRight);
            Assert.AreEqual("30px", divStyle.PaddingBottom);
            Assert.AreEqual("20px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_shorthand_padding_with_two_values()
        {
            var css = @"div { padding: 10px 20px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.PaddingTop);
            Assert.AreEqual("20px", divStyle.PaddingRight);
            Assert.AreEqual("10px", divStyle.PaddingBottom);
            Assert.AreEqual("20px", divStyle.PaddingLeft);
        }

        [Test]
        public async Task computes_shorthand_padding_with_one_value()
        {
            var css = @"div { padding: 10px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.PaddingTop);
            Assert.AreEqual("10px", divStyle.PaddingRight);
            Assert.AreEqual("10px", divStyle.PaddingBottom);
            Assert.AreEqual("10px", divStyle.PaddingLeft);
        }

        // Edge Cases

        [Test]
        public async Task computes_padding_with_inherit()
        {
            var css = @"
            div { padding: 20px; }
            span { padding: inherit; }";
            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits padding from parent div = 20px
            Assert.AreEqual("20px", spanStyle.PaddingTop);
            Assert.AreEqual("20px", spanStyle.PaddingRight);
            Assert.AreEqual("20px", spanStyle.PaddingBottom);
            Assert.AreEqual("20px", spanStyle.PaddingLeft);
        }
    }
}
