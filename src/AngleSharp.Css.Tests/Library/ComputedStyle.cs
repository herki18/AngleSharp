namespace AngleSharp.Css.Tests.Library
{
    using System.Linq;
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class ComputedStyleTests
    {
        [Test]
        public async Task TransformEmToPx_Issue136()
        {
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var source = "<p>This is <span>only</span> a test.</p>";
            var cssSheet = "p { font-size: 1.5em }";
            var document = await context.OpenAsync(req => req.Content(source));
            var style = document.CreateElement<IHtmlStyleElement>();
            style.TextContent = cssSheet;
            document.Head.AppendChild(style);
            var span = document.QuerySelector("span");
            var fontSize = span.ComputeCurrentStyle().GetProperty("font-size");

            Assert.AreEqual("24px", fontSize.Value);
        }

        [Test]
        public async Task computes_default_font_size_when_no_css_is_defined()
        {
            var html = "<p><span>Text</span></p>";
            // var css = "";

            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));
            var span = document.QuerySelector("span");
            var fontSize = span.ComputeCurrentStyle().GetProperty("font-size");

            Assert.AreEqual("16px", fontSize.Value);
        }

        [Test]
        public async Task computes_font_size_with_rem_units()
        {
            var html = "<p>Text</p>";
            var css = @"
            html {
                font-size: 10px;
            }
            p {
                font-size: 2rem;
            }";

            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html));
            // document.StyleSheets

            var style = document.CreateElement<IHtmlStyleElement>();
            style.TextContent = css;
            document.Head.AppendChild(style);
            var p = document.QuerySelector("p");
            var fontSize = p.ComputeCurrentStyleNew().GetProperty("font-size");

            Assert.AreEqual("20px", fontSize.Value);
        }
    }
}
