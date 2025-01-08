using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

namespace AngleSharp.Css.Tests.Compute
{
    using System;

    [TestFixture]
    public class MarginTests
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
        public async Task computes_absolute_margin_in_px()
        {
            var css = @"div { margin: 10px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.MarginTop);
            Assert.AreEqual("10px", divStyle.MarginRight);
            Assert.AreEqual("10px", divStyle.MarginBottom);
            Assert.AreEqual("10px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_absolute_margin_in_cm()
        {
            var css = @"div { margin: 2cm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2cm = 96px / 2.54 * 2 = 75.59px
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.MarginTop.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.MarginRight.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.MarginBottom.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("75.59px", Math.Round(Double.Parse(divStyle.MarginLeft.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        [Test]
        public async Task computes_absolute_margin_in_mm()
        {
            var css = @"div { margin: 10mm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 10mm = 96px / 2.54 * 1 = ~37.8px
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.MarginTop.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.MarginRight.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.MarginBottom.TrimEnd("px".ToCharArray())), 2) + "px");
            Assert.AreEqual("37.8px", Math.Round(Double.Parse(divStyle.MarginLeft.TrimEnd("px".ToCharArray())), 2) + "px");
        }

        // Relative Units

        [Test]
        public async Task computes_relative_margin_in_em()
        {
            var css = @"
            div { font-size: 16px; margin: 2em; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2em = font size * 2 = 16px * 2 = 32px
            Assert.AreEqual("32px", divStyle.MarginTop);
            Assert.AreEqual("32px", divStyle.MarginRight);
            Assert.AreEqual("32px", divStyle.MarginBottom);
            Assert.AreEqual("32px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_relative_margin_in_rem()
        {
            var css = @"
            html { font-size: 20px; }
            div { margin: 1.5rem; }";
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
            Assert.AreEqual("30px", divStyle.MarginTop);
            Assert.AreEqual("30px", divStyle.MarginRight);
            Assert.AreEqual("30px", divStyle.MarginBottom);
            Assert.AreEqual("30px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_margin_with_percentage()
        {
            var css = @"div { margin: 50%; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { ViewPortWidth = 800 };

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 50% of 800px = 400px
            Assert.AreEqual("400px", divStyle.MarginTop);
            Assert.AreEqual("400px", divStyle.MarginRight);
            Assert.AreEqual("400px", divStyle.MarginBottom);
            Assert.AreEqual("400px", divStyle.MarginLeft);
        }

        // Shorthand Properties

        [Test]
        public async Task computes_shorthand_margin_with_four_values()
        {
            var css = @"div { margin: 10px 20px 30px 40px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.MarginTop);
            Assert.AreEqual("20px", divStyle.MarginRight);
            Assert.AreEqual("30px", divStyle.MarginBottom);
            Assert.AreEqual("40px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_shorthand_margin_with_three_values()
        {
            var css = @"div { margin: 10px 20px 30px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.MarginTop);
            Assert.AreEqual("20px", divStyle.MarginRight);
            Assert.AreEqual("30px", divStyle.MarginBottom);
            Assert.AreEqual("20px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_shorthand_margin_with_two_values()
        {
            var css = @"div { margin: 10px 20px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.MarginTop);
            Assert.AreEqual("20px", divStyle.MarginRight);
            Assert.AreEqual("10px", divStyle.MarginBottom);
            Assert.AreEqual("20px", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_shorthand_margin_with_one_value()
        {
            var css = @"div { margin: 10px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("10px", divStyle.MarginTop);
            Assert.AreEqual("10px", divStyle.MarginRight);
            Assert.AreEqual("10px", divStyle.MarginBottom);
            Assert.AreEqual("10px", divStyle.MarginLeft);
        }

        // Edge Cases

        [Test]
        public async Task computes_margin_with_auto()
        {
            var css = @"div { margin: auto; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("auto", divStyle.MarginTop);
            Assert.AreEqual("auto", divStyle.MarginRight);
            Assert.AreEqual("auto", divStyle.MarginBottom);
            Assert.AreEqual("auto", divStyle.MarginLeft);
        }

        [Test]
        public async Task computes_margin_with_inherit()
        {
            var css = @"
            div { margin: 20px; }
            span { margin: inherit; }";
            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(css, html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits margin from parent div = 20px
            Assert.AreEqual("20px", spanStyle.MarginTop);
            Assert.AreEqual("20px", spanStyle.MarginRight);
            Assert.AreEqual("20px", spanStyle.MarginBottom);
            Assert.AreEqual("20px", spanStyle.MarginLeft);
        }
    }
}
