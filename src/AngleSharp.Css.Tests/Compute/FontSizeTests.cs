using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

namespace AngleSharp.Css.Tests.Compute
{
    using System;

    [TestFixture]
    public class FontSizeTests
    {
        private StyleCollection CreateStyleCollection(String css, IRenderDevice device = null)
        {
            var sheet = ParseStyleSheet(css);
            return new StyleCollection(new[] { sheet }, device ?? new DefaultRenderDevice());
        }

        private async Task<IDocument> ParseDocumentAsync(String html)
        {
            var context = BrowsingContext.New(Configuration.Default.WithCss());
            return await context.OpenAsync(req => req.Content(html));
        }

        [Test]
        public async Task computes_absolute_font_size_in_px()
        {
            var css = @"div { font-size: 16px; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            Assert.AreEqual("16px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_absolute_font_size_in_inches()
        {
            var css = @"div { font-size: 1in; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 1in = 96px
            Assert.AreEqual("96px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_absolute_font_size_in_mm()
        {
            var css = @"div { font-size: 10mm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 10mm = 96px/cm * (10 / 10) = ~37.795276px
            Assert.AreEqual("37.8px", Math.Round(double.Parse(divStyle.FontSize.TrimEnd("px".ToCharArray())), 1) + "px");
        }

        [Test]
        public async Task computes_absolute_font_size_in_points()
        {
            var css = @"div { font-size: 12pt; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 12pt = 96px / 72 * 12 = 16px
            Assert.AreEqual("16px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_absolute_font_size_in_picas()
        {
            var css = @"div { font-size: 1pc; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 1pc = 12pt = 16px
            Assert.AreEqual("16px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_relative_font_size_in_em()
        {
            var css = @"
    div { font-size: 16px; }
    span { font-size: 2em; }";
            var html = @"
    <div>
        <span>Text</span>
    </div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // 16px * 2 = 32px
            Assert.AreEqual("32px", spanStyle.FontSize);
        }

        [Test]
        public async Task computes_relative_font_size_in_rem()
        {
            var css = @"
    html { font-size: 20px; }
    div { font-size: 1.5rem; }";
            var html = @"
    <html>
        <body>
            <div>Text</div>
        </body>
    </html>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 20px * 1.5 = 30px
            Assert.AreEqual("30px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_percentage()
        {
            var css = @"
    div { font-size: 16px; }
    span { font-size: 150%; }";
            var html = @"
    <div>
        <span>Text</span>
    </div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // 16px * 150% = 24px
            Assert.AreEqual("24px", spanStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_smaller()
        {
            var css = @"div { font-size: smaller; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // Default root font size = 16px, smaller = 16px / 1.2 = ~13.33px
            Assert.AreEqual("13.3333333333333px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_larger()
        {
            var css = @"div { font-size: larger; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // Default root font size = 16px, larger = 16px * 1.2 = ~19.2px
            Assert.AreEqual("19.2px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_inherit()
        {
            var css = @"
    div { font-size: 20px; }
    span { font-size: inherit; }";
            var html = @"
    <div>
        <span>Text</span>
    </div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits font size from div = 20px
            Assert.AreEqual("20px", spanStyle.FontSize);
        }

        [Test]
        public async Task computes_absolute_font_size_in_cm()
        {
            var css = @"div { font-size: 2cm; }";
            var html = @"<div>Text</div>";
            var device = new DefaultRenderDevice { Resolution = 96 };

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css, device);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // 2cm = 96px/cm * 2 = 75.5905511811024px
            Assert.AreEqual("75.5905511811024px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_percentage_relative_to_parent()
        {
            var css = @"
            div { font-size: 24px; }
            span { font-size: 125%; }";

            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // 24px * 1.25 = 30px
            Assert.AreEqual("30px", spanStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_inheritance()
        {
            var css = @"
            div { font-size: 16px; }
            span { font-size: inherit; }";

            var html = @"
            <div>
                <span>Text</span>
            </div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var spanStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("span"));

            // Inherits font size from div = 16px
            Assert.AreEqual("16px", spanStyle.FontSize);
        }

        [Test]
        public async Task computes_minimum_font_size()
        {
            var css = @"div { font-size: smaller; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // Default root font size = 16px, smaller = 16px / 1.2 = 13.33px
            Assert.AreEqual("13.3333333333333px", divStyle.FontSize);
        }

        [Test]
        public async Task computes_font_size_with_larger_keyword()
        {
            var css = @"div { font-size: larger; }";
            var html = @"<div>Text</div>";

            var document = await ParseDocumentAsync(html);
            var styleCollection = CreateStyleCollection(css);
            var divStyle = styleCollection.ComputeDeclarationsNew(document.QuerySelector("div"));

            // Default root font size = 16px, larger = 16px * 1.2 = 19.2px
            Assert.AreEqual("19.2px", divStyle.FontSize);
        }
    }
}
