using NUnit.Framework;
using AngleSharp.Dom;

namespace AngleSharp.Renderer.Tests
{
    using System;
    using System.Diagnostics;
    using Core.Tests.Mocks;
    using Css;
    using Mocks;

    [TestFixture]
    public class Tests
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

            var configuration = Configuration
                .Default
                .WithCss()
                .WithMockRequester(requester);
            configuration = configuration.With(new DefaultRenderDevice());

            _context = BrowsingContext.New(configuration);
            _document = _context.OpenAsync(req => req.Content(html)).Result;
        }

        [Test]
        public void Test1()
        {
            var renderEngine = new DocumentRenderer(
                _document!.DefaultView!, new DefaultRenderDevice(), new MockRenderer());

            renderEngine.Update();

            var node = renderEngine.GetRoot();

            var print = renderEngine.Print();
            TestContext.Out.WriteLine(print);
        }
    }
}