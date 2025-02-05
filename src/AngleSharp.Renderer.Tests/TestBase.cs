namespace AngleSharp.Renderer.Tests
{
    using System.IO;
    using System.Linq;
    using Core.Tests.Mocks;
    using Css;
    using Dom;
    using Mocks;
    using NUnit.Framework;
    using static NUnit.Framework.Assert;

    public abstract class TestBase
    {
        protected IBrowsingContext Context { get; private set; }
        protected IDocument Document { get; private set; }

        protected virtual string TestCaseName => GetSanitizedTestName();
        protected virtual string CssHref => $"{TestCaseName}.css";

        [SetUp]
        public void Setup()
        {
            var html = LoadHtml();
            var css = LoadCss();

            var requester = new MockRequester();
            requester.BuildResponse(request =>
                request.Address.Path.EndsWith(CssHref) ? css : string.Empty);

            var config = Configuration.Default
                .WithCss()
                .WithMockRequester(requester)
                .With(new DefaultRenderDevice());

            Context = BrowsingContext.New(config);
            Document = Context.OpenAsync(req => req.Content(html)).Result;
        }

        protected string LoadHtml()
        {
            var path = GetTestFilePath($"{TestCaseName}.html");
            return File.ReadAllText(path);
        }

        protected string LoadCss()
        {
            var path = GetTestFilePath($"{TestCaseName}.css");
            return FileExists(path) ? File.ReadAllText(path) : string.Empty;
        }

        protected string GetTestFilePath(string fileName)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", fileName);
        }

        protected bool FileExists(string path) => File.Exists(path);

        private string GetSanitizedTestName()
        {
            var name = TestContext.CurrentContext.Test.MethodName
                     ?? TestContext.CurrentContext.Test.Name;

            // Remove parameter information from test name
            var cleanName = name.Split('(')[0];

            // Sanitize for filename
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(cleanName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());
        }

        /// <summary>
        /// Renders the current Document into a tree of <see cref="ElementNode"/>s
        /// and returns (root, html, body). Also prints the render tree.
        /// </summary>
        protected (ElementNode Root, ElementNode Html, ElementNode Body) RenderDocumentAndGetNodes()
        {
            var renderEngine = new DocumentRenderer(
                Document.DefaultView,
                new DefaultRenderDevice(),
                new MockRenderer());
            renderEngine.Update();

            // 1) Get the root node
            var root = renderEngine.GetRoot() as ElementNode;
            TestContext.Out.WriteLine(renderEngine.Print());

            // 2) Find <html> in the render tree
            var htmlNode = FindElementNodeByTagName(root, TagNames.Html, includeParent: true);
            // NotNull(htmlNode, "Could not find <html> element.");

            // 3) Find <body>
            var bodyNode = FindElementNodeByTagName(htmlNode, TagNames.Body);
            // NotNull(bodyNode, "Could not find <body> element.");

            // Optionally check default body margins & display
            // AssertChromiumDefaultBodyMargin(bodyNode);
            // AssertDisplay(bodyNode, "block");

            return (root, htmlNode, bodyNode);
        }

        /// <summary>
        /// A small helper to find the direct child <see cref="ElementNode"/> of <paramref name="parent"/>
        /// at the given 0-based <paramref name="index"/>.
        /// </summary>
        public ElementNode FindChildByIndex(ElementNode parent, int index)
        {
            var child = parent.Children.OfType<ElementNode>().ElementAtOrDefault(index);
            NotNull(child, $"Could not find child element at index={index} under parent <{parent.Ref?.NodeName}>.");
            return child;
        }

        /// <summary>
        /// Finds the first <see cref="ElementNode"/> whose tag name matches <paramref name="tagName"/>.
        /// If <paramref name="includeParent"/> is <c>true</c>, this method checks the <paramref name="parent"/> node
        /// itself first; otherwise, it only searches the parent's direct children (one level deep).
        /// </summary>
        /// <param name="parent">
        /// The starting node. If <paramref name="includeParent"/> is <c>true</c> and the parent matches,
        /// it will be returned.
        /// </param>
        /// <param name="tagName">The tag name to look for (e.g., "html", "body", "div").</param>
        /// <param name="includeParent">
        /// If <c>true</c>, the method checks whether <paramref name="parent"/> is itself a match before
        /// searching its children.
        /// </param>
        /// <returns>The first matching <see cref="ElementNode"/> or <c>null</c> if none is found.</returns>
        protected ElementNode FindElementNodeByTagName(
            ElementNode parent,
            string tagName,
            bool includeParent = false)
        {
            // 1) Check if the parent *itself* has the desired tag name
            if (includeParent && parent.Ref is IElement parentElement && parentElement.GetTagName() == tagName)
            {
                return parent;
            }

            // 2) Otherwise, search among the direct children (one level deep)
            return parent.Children
                .OfType<ElementNode>()
                .FirstOrDefault(x => x.Ref is IElement e && e.GetTagName() == tagName);
        }

        /// <summary>
        /// Asserts that the given <see cref="ElementNode"/> has the expected CSS display property.
        /// </summary>
        protected void AssertDisplay(ElementNode node, string expectedDisplay)
        {
            NotNull(node, $"Node cannot be null when checking display='{expectedDisplay}'.");
            That(node!.ComputedStyle.Display, Is.EqualTo(expectedDisplay),
                $"Expected element display to be '{expectedDisplay}'.");
        }

        /// <summary>
        /// Asserts that the <see cref="ElementNode"/> layout width matches <paramref name="expectedWidth"/>.
        /// Allows for a small floating-point tolerance in case of sub-pixel rendering.
        /// </summary>
        protected void AssertContentWidth(ElementNode node, double expectedWidth, double tolerance = 0.5)
        {
            NotNull(node, $"Node cannot be null when checking expected width={expectedWidth}.");
            That(node!.Layout?.ContentWidth,
                 Is.EqualTo(expectedWidth).Within(tolerance),
                 $"Expected element width to be {expectedWidth}px ± {tolerance}.");
        }

        protected void AssertBoxWidth(ElementNode node, double expectedWidth, double tolerance = 0.5)
        {
            NotNull(node, $"Node cannot be null when checking expected width={expectedWidth}.");
            That(node!.Layout?.BoxWidth,
                Is.EqualTo(expectedWidth).Within(tolerance),
                $"Expected element width to be {expectedWidth}px ± {tolerance}.");
        }

        /// <summary>
        /// Asserts that the <see cref="ElementNode"/> layout height matches <paramref name="expectedHeight"/>.
        /// Allows for a small floating-point tolerance in case of sub-pixel rendering.
        /// </summary>
        protected void AssertHeight(ElementNode node, double expectedHeight, double tolerance = 0.5)
        {
            NotNull(node, $"Node cannot be null when checking expected height={expectedHeight}.");
            That(node!.Layout?.ContentHeight,
                Is.EqualTo(expectedHeight).Within(tolerance),
                $"Expected element height to be {expectedHeight}px ± {tolerance}.");
        }

        protected void AssertPadding(ElementNode node, string expectedLeft, string expectedRight, string expectedTop = null, string expectedBottom = null)
        {
            // Check left / right
            That(node!.ComputedStyle.PaddingLeft, Is.EqualTo(expectedLeft),
                $"Expected padding-left = {expectedLeft}, got {node.ComputedStyle.PaddingLeft}");
            That(node.ComputedStyle.PaddingRight, Is.EqualTo(expectedRight),
                $"Expected padding-right = {expectedRight}, got {node.ComputedStyle.PaddingRight}");

            // Top and bottom are optional
            if (expectedTop != null)
            {
                That(node.ComputedStyle.PaddingTop, Is.EqualTo(expectedTop),
                    $"Expected padding-top = {expectedTop}, got {node.ComputedStyle.PaddingTop}");
            }
            if (expectedBottom != null)
            {
                That(node.ComputedStyle.PaddingBottom, Is.EqualTo(expectedBottom),
                    $"Expected padding-bottom = {expectedBottom}, got {node.ComputedStyle.PaddingBottom}");
            }
        }


        /// <summary>
        /// Asserts that the <see cref="ElementNode"/> is positioned at X/Y coordinates
        /// within a given tolerance.
        /// </summary>
        protected void AssertPosition(
            ElementNode node,
            double expectedX,
            double expectedY,
            double tolerance = 0.5)
        {
            NotNull(node, "Node cannot be null when checking position.");
            That(node!.Layout?.X, Is.EqualTo(expectedX).Within(tolerance),
                $"Expected element X to be {expectedX}px ± {tolerance}.");
            That(node.Layout?.Y, Is.EqualTo(expectedY).Within(tolerance),
                $"Expected element Y to be {expectedY}px ± {tolerance}.");
        }

        /// <summary>
        /// Optional: Asserts default Chromium-like body margins, if your layout engine
        /// provides user-agent styles.
        /// </summary>
        protected void AssertChromiumDefaultBodyMargin(ElementNode bodyNode)
        {
            NotNull(bodyNode, "Body node cannot be null when checking default margin.");

            That(bodyNode!.ComputedStyle.MarginTop, Is.EqualTo("8px"),
                "Expected default user-agent margin of 8px on <body> (top).");
            That(bodyNode.ComputedStyle.MarginBottom, Is.EqualTo("8px"),
                "Expected default user-agent margin of 8px on <body> (bottom).");
            That(bodyNode.ComputedStyle.MarginLeft, Is.EqualTo("8px"),
                "Expected default user-agent margin of 8px on <body> (left).");
            That(bodyNode.ComputedStyle.MarginRight, Is.EqualTo("8px"),
                "Expected default user-agent margin of 8px on <body> (right).");
        }
    }
}