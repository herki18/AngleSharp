namespace AngleSharp.Renderer.Tests
{
    using Dom;
    using NUnit.Framework;
    using static NUnit.Framework.Assert;

    public class BoxSizingTests : TestBase
    {
        protected override string TestPath => "BoxSizing";

        // <div style="width:300px; padding:10px; border:2px solid black; box-sizing: content-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_content_box()
        {
            // 1) Render and get (root, html, body) from the auto-loaded contentBox.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // The container has box-sizing: content-box.
            // In content-box, the specified width (300px) is the content width.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should span the full container content width (300px) and have zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
        }

// <div style="width:300px; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_border_box()
        {
            // 1) Render and get (root, html, body) from the auto-loaded borderBox.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // For border-box, the specified width (300px) includes padding and border.
            // Thus, the computed content width is 300 - (10+10+2+2) = 276px.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 276);
            AssertGlobalPosition(containerDiv, 0, 0);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should also have a computed width of 276px and zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 276);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
        }

// <div style="width:300px; padding:10px; border:2px solid black">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_default()
        {
            // 1) Render and get (root, html, body) from the auto-loaded defaultBoxSizing.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);

            // With no box-sizing specified, the default is content-box.
            AssertDisplay(containerDiv, "block");
            AssertLayoutPadding(containerDiv, 10, 10, 10, 10);
            AssertLayoutBorder(containerDiv, 10, 10, 10, 10);
            AssertContentWidth(containerDiv, 300);
            AssertGlobalPosition(containerDiv, 0, 0);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 12, 12);
        }

// <div style="height:200px; padding:10px; border:2px solid black; box-sizing: content-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_height_content_box()
        {
            // 1) Render and get (root, html, body) from the auto-loaded heightContentBox.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // In content-box, the specified height is the content height.
            AssertDisplay(containerDiv, "block");
            // Expected content height: 200px.
            AssertHeight(containerDiv, 200);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should have zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, containerDiv.Layout.BoxWidth); // width might be inherited or auto (if width not set)
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 8, 8);
        }

// <div style="height:200px; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_height_border_box()
        {
            // 1) Render and get (root, html, body) from the auto-loaded heightBorderBox.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // For border-box, the overall height is 200px, so the computed content height is 200 - (10+10+2+2) = 176px.
            AssertDisplay(containerDiv, "block");
            AssertHeight(containerDiv, 176);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, containerDiv.Layout.BoxWidth);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 8, 8);
        }

// <div style="width:auto; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_auto_width_border_box()
        {
            // 1) Render and get (root, html, body) from the auto-loaded autoWidthBorderBox.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // In this test, assume the parent (viewport) available width is 500px.
            // For border-box with width:auto, the container takes the full available width.
            // Thus, the computed content width is 500 - (10+10+2+2) = 500 - 24 = 476px.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 476);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 476);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 8, 8);
        }

// <div style="width:20px; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_edge_case_clamped()
        {
            // 1) Render and get (root, html, body) from the auto-loaded edgeCaseClamped.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // For border-box, computed content width = 20 - (10+10+2+2) = 20 - 24 = -4, clamped to 0.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 0);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 0);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
        }


// <div style="width:300px; min-width:350px; max-width:400px; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="display:block"></div>
// </div>
        [Test]
        public void test_box_sizing_with_min_max()
        {
            // 1) Render and get (root, html, body) from the auto-loaded withMinMax.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // Even though the specified width is 300px, the min-width forces the width to be at least 350px.
            // In border-box mode, the computed content width is 350 - (10+10+2+2) = 350 - 24 = 326px.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 326);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 326);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
        }

// <div style="width:300px; padding:10px; border:2px solid black; box-sizing: border-box">
//     <div style="width:100px; padding:5px; border:1px solid black; box-sizing: content-box"></div>
// </div>
        [Test]
        public void test_box_sizing_nested_elements()
        {
            // 1) Render and get (root, html, body) from the auto-loaded nestedElements.html.
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the outer container <div>
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find outer container <div> under <body>.");

            // For border-box, outer container computed content width = 300 - (10+10+2+2) = 276px.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 276);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the inner child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find inner child <div> under outer container <div>.");

            // The inner child uses content-box, so its specified width (100px) is its content width.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 100);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
        }
    }
}