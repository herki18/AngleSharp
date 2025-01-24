namespace AngleSharp.Renderer.Tests
{
    using Core.Tests.Mocks;
    using Css;
    using Dom;
    using Mocks;
    using NUnit.Framework;

    public class Test1
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
test
</body>
</html>

";
            string css = @"

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
        public void Test()
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