using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using NUnit.Framework;
using static AngleSharp.Css.Tests.CssConstructionFunctions;

#pragma warning disable CS0219 // Variable is assigned but its value is never used

namespace AngleSharp.Css.Tests.Compute
{
    using System.Diagnostics;
    using System.Linq;
    using Mocks;

    [TestFixture]
    public class ComputeStylesTests
    {
        private IBrowsingContext _context;
        private IDocument _document;

        [SetUp]
        public void Setup()
        {
            string html = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ComputedStyleEngine Test Cases</title>
    <link rel=""stylesheet"" href=""styles.css"">
</head>
<body>
    <div id=""test-defaults"">Default styles should apply here.</div>

    <div id=""test-inheritance"" style=""color: red; font-size: 20px;"">
        Parent Element
        <div>
            Child Element (should inherit red color and font size)
        </div>
    </div>

    <div id=""test-relative-units"" style=""font-size: 16px;"">
        Relative Units
        <div style=""font-size: 2em;"">2em font size</div>
        <div style=""font-size: 150%;"">150% font size</div>
        <div style=""font-size: 12pt;"">12pt font size</div>
    </div>

    <div id=""test-nested"">
        <div style=""font-size: 16px;"">
            Root Font Size
            <div style=""font-size: 2rem;"">
                2rem font size
                <div style=""font-size: 0.5em;"">0.5em font size</div>
            </div>
        </div>
    </div>

    <div id=""test-complex-selectors"">
        <p class=""test"">Complex Selector Test</p>
        <p class=""test nested"">
            Nested Selector Test
            <span class=""highlighted"">Highlighted text</span>
        </p>
    </div>

    <div id=""test-line-height"" style=""line-height: 1.5;"">
        Line Height Test
        <div style=""line-height: 200%; font-size: 12px;"">200% line height</div>
        <div style=""line-height: normal;"">Normal line height</div>
    </div>

    <div id=""test-edge-cases"">
        Edge Cases
        <div style=""font-size: inherit;"">Font size inherit</div>
        <div style=""font-size: initial;"">Font size initial</div>
        <div style=""font-size: unset;"">Font size unset</div>
    </div>
</body>
</html>

";
            string css = @"
body {
    margin: 0;
    font-family: Arial, sans-serif;
}

#test-defaults {
    margin: 10px;
    padding: 10px;
    background-color: lightgray;
}

#test-inheritance {
    margin: 10px;
    padding: 10px;
    border: 1px solid black;
}

#test-relative-units div {
    margin: 5px 0;
}

#test-nested div {
    margin-left: 10px;
    padding: 5px;
    border: 1px dashed blue;
}

#test-complex-selectors {
    margin: 10px;
}

.test {
    font-size: 14px;
    color: green;
}

.test.nested {
    font-size: 12px;
    color: purple;
}

.highlighted {
    background-color: yellow;
    font-weight: bold;
}

#test-line-height div {
    margin: 5px 0;
}

#test-edge-cases div {
    margin: 5px 0;
    border: 1px solid gray;
}

";

            var requester = new MockRequester();

            requester.BuildResponse(request =>
            {
                if (request.Address.Path.EndsWith("styles.css"))
                {
                    return css;
                }

                return string.Empty;
            });

            var configuration = Configuration.Default.WithCss().WithMockRequester(requester);
            configuration = configuration.With(new DefaultRenderDevice());

            _context = BrowsingContext.New(configuration);
            _document = _context.OpenAsync(req => req.Content(html)).Result;
        }

        [Test]
        public void TestInheritance()
        {
            var parent = _document.QuerySelector("#test-inheritance");
            var child = parent.Children[0];

            var childStyle = child.ComputeStyleNew();


            var colorProperty = childStyle.GetProperty("color");

            Assert.AreEqual("rgba(255, 0, 0, 1)", colorProperty.Value);
            var fontSizeProperty = childStyle.GetProperty("font-size");
            Assert.AreEqual("20px", fontSizeProperty.Value);
        }

        [Test]
        public void compute_styles_inheritance()
        {
            var parent = _document.QuerySelector("#test-inheritance");
            var child = parent.Children[0];

            var parentStyle = parent.ComputeStyleNew();
            var childStyle = child.ComputeStyleNew();

            var colorProperty = childStyle.GetProperty("color");

            Assert.AreEqual("rgba(255, 0, 0, 1)", colorProperty.Value);
            var fontSizeProperty = childStyle.GetProperty("font-size");
            Assert.AreEqual("20px", fontSizeProperty.Value);
        }

        [Test]
        public void compute_styles_relative_units()
        {
            var container = _document.QuerySelector("#test-relative-units");
            var child1 = container.Children[0]; // 2em
            var child2 = container.Children[1]; // 150%
            var child3 = container.Children[2]; // 12pt

            var style1 = child1.ComputeStyleNew();
            var style2 = child2.ComputeStyleNew();
            var style3 = child3.ComputeStyleNew();

            Assert.AreEqual("32px", style1.GetProperty("font-size").Value);
            Assert.AreEqual("24px", style2.GetProperty("font-size").Value);
            Assert.AreEqual("16px", style3.GetProperty("font-size").Value); // Assuming 12pt = 16px
        }

        [Test]
        public void compute_styles_nested_elements()
        {
            var nested = _document.QuerySelector("#test-nested");
            var root = nested.Children[0];
            var child = root.Children[0];
            var grandchild = child.Children[0];

            var rootStyle = root.ComputeStyleNew();
            var childStyle = child.ComputeStyleNew();
            var grandchildStyle = grandchild.ComputeStyleNew();

            Assert.AreEqual("16px", rootStyle.GetProperty("font-size").Value);
            Assert.AreEqual("32px", childStyle.GetProperty("font-size").Value);
            Assert.AreEqual("16px", grandchildStyle.GetProperty("font-size").Value);
        }

        [Test]
        public void compute_styles_complex_selectors()
        {
            var test = _document.QuerySelector(".test");
            var nested = _document.QuerySelector(".test.nested");
            var highlighted = nested.QuerySelector(".highlighted");

            var testStyle = test.ComputeStyleNew();
            var nestedStyle = nested.ComputeStyleNew();
            var highlightedStyle = highlighted.ComputeStyleNew();

            Assert.AreEqual("14px", testStyle.GetProperty("font-size").Value);
            Assert.AreEqual("rgba(0, 128, 0, 1)", testStyle.GetProperty("color").Value);

            Assert.AreEqual("12px", nestedStyle.GetProperty("font-size").Value);
            Assert.AreEqual("rgba(128, 0, 128, 1)", nestedStyle.GetProperty("color").Value);

            Assert.AreEqual("bold", highlightedStyle.GetProperty("font-weight").Value);
            Assert.AreEqual("rgba(255, 255, 0, 1)", highlightedStyle.GetProperty("background-color").Value);
        }

        [Test]
        public void compute_styles_line_height()
        {
            var container = _document.QuerySelector("#test-line-height");
            var child1 = container.Children[0]; // 200%
            var child2 = container.Children[1]; // normal

            // var style1 = child1.ComputeStyleNew();
            var style2 = child2.ComputeStyleNew();

            // Assert.AreEqual("24px", style1.GetProperty("line-height").Value); // 200% of 12px font-size
            Assert.AreEqual("18px", style2.GetProperty("line-height").Value); // normal = 1.5 * font-size (12px)
        }

        [Test]
        public void compute_styles_edge_cases()
        {
            var container = _document.QuerySelector("#test-edge-cases");
            var inherit = container.Children[0];
            var initial = container.Children[1];
            var unset = container.Children[2];

            var inheritStyle = inherit.ComputeStyleNew();
            var initialStyle = initial.ComputeStyleNew();
            var unsetStyle = unset.ComputeStyleNew();

            Assert.AreEqual("16px", inheritStyle.GetProperty("font-size").Value); // Should inherit from parent
            Assert.AreEqual("16px", initialStyle.GetProperty("font-size").Value); // Should reset to initial value
            Assert.AreEqual("16px", unsetStyle.GetProperty("font-size").Value); // Should reset to initial value
        }
    }
}