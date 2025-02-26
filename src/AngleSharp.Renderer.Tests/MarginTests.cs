namespace AngleSharp.Renderer.Tests
{
    using Dom;
    using NUnit.Framework;
    using static NUnit.Framework.Assert;

    [TestFixture]
    public class MarginTests : TestBase
    {
        protected override string TestPath => "Margins";

        [Test]
        public void test_auto_margins_center_element_horizontally()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width:400px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);

            // Assert it's block-level and 400px wide
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 400);
            // Typically at (8,8)
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div style="width:200px; margin-left:auto; margin-right:auto">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);

            // Assert block display and 200px width
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 200);

            // 4) Check that margin-left/margin-right remain 'auto'
            That(childDiv.ComputedStyle.MarginLeft, Is.EqualTo("auto"),
                "Expected child’s margin-left to be 'auto'.");
            That(childDiv.ComputedStyle.MarginRight, Is.EqualTo("auto"),
                "Expected child’s margin-right to be 'auto'.");

            // 5) Verify horizontal centering
            var containerX = containerDiv.Layout?.X ?? 0.0;
            var containerWidth = containerDiv.Layout?.BoxWidth ?? 0.0;
            var childX = childDiv.Layout?.X ?? 0.0;
            var childWidth = childDiv.Layout?.BoxWidth ?? 0.0;

            var expectedOffset = (containerWidth - childWidth) / 2.0; // => 100
            var actualOffset = childX - containerX;

            That(actualOffset, Is.EqualTo(expectedOffset).Within(0.5),
                "Expected child <div> to be horizontally centered in the container.");
        }

        [Test]
        public void test_auto_margins_center_within_padded_container()
        {
            // HTML:
            // <div style="width: 400px; padding: 20px;">
            //     <div style="width: 160px; margin-left: auto; margin-right: auto;"></div>
            // </div>

            // Container content box = 400 - 20px left padding - 20px right padding = 360
            // So if child is 160 wide, leftover = 360 - 160 = 200 => margin-left & right = 100 each
            // Then child X should be containerX + 20 + 100

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(containerDiv, 400);
            AssertBoxWidth(containerDiv, 440);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            AssertContentWidth(childDiv, 160);
            AssertBoxWidth(childDiv, 160);

            // Child X=Container X+Parent Padding(Left)+Margin-Left(Auto)
            AssertGlobalPosition(childDiv, 148, 28);
        }

        [Test]
        public void test_auto_margins_no_explicit_width_fill_container()
        {
            // HTML:
            // <div style="width: 400px; border: 1px solid black;">
            //     <div style="margin-left: auto; margin-right: auto;"></div>
            // </div>

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            AssertDisplay(childDiv, "block");

            // When a block element has auto margins but no explicit width,
            // it should fill its container's width
            var childLayout = childDiv.Layout;

            That(childLayout.BoxWidth, Is.EqualTo(containerDiv.Layout.ContentWidth).Within(1.0),
                "Child with no explicit width should fill its container's width, regardless of auto margins.");

            // X position should match container's content box
            var expectedX = containerDiv.Layout?.X + containerDiv.Layout.BorderLeft ?? 0;
            That(childLayout.X, Is.EqualTo(expectedX).Within(0.5),
                "Child should align with container's content box when filling container width.");
        }

        [Test]
        public void test_vertical_margin_collapsing_between_block_elements()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find container <div style="width:300px"> inside <body>
            var container = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(container, 300);

            // 3) Find first child (first block): <div style="height:50px; margin-bottom:20px;">
            var firstBlock = FindChildByIndex(container, 0);
            // Expect height of 50px
            AssertHeight(firstBlock, 50);

            // 4) Find second child: <div style="height:50px; margin-top:30px;">
            var secondBlock = FindChildByIndex(container, 1);

            // 5) Check positions:
            //    The container is expected at Y = 8 (body's margin applied once)
            AssertGlobalPosition(container, 8, 8);
            //    First block should start at Y = 8 (its top margin collapses with container's position)
            AssertGlobalPosition(firstBlock, 8, 8);
            //    The gap between first and second blocks should be the collapse of
            //       first block's bottom margin (20) and second block's top margin (30) → 30.
            //    Thus, second block's top should be: 8 + 50 (first block's height) + 30 = 88.
            AssertGlobalPosition(secondBlock, 8, 88);
        }

        [Test]
        public void test_child_wider_than_container_auto_margins()
        {
            // HTML:
            // <div style="width: 300px">
            //     <div style="width: 500px; margin-left: auto; margin-right: auto;"></div>
            // </div>

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(containerDiv, 300);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            AssertContentWidth(childDiv, 500);

            // leftoverSpace = 300 - 500 = -200 => 0 after clamp
            // Typically, child ends up at containerX, simply overflowing to the right
            var containerX = containerDiv.Layout?.X ?? 0f;
            var containerWidth = containerDiv.Layout?.BoxWidth ?? 0f;
            var childX = childDiv.Layout?.X ?? 0f;

            That(childX, Is.EqualTo(containerX).Within(0.5),
                "When leftover space is negative, expect child to align at container’s left (i.e. no negative margin).");
        }

        [Test]
        public void test_empty_block_still_collapses_margins()
        {
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            var container = FindElementNodeByTagName(bodyNode, TagNames.Div);

            var firstBlock = FindChildByIndex(container, 0);
            AssertHeight(firstBlock, 40);

            var emptyBlock = FindChildByIndex(container, 1);

            var thirdBlock = FindChildByIndex(container, 2);
            AssertHeight(thirdBlock, 40);

            AssertGlobalPosition(emptyBlock, 8, 78);

            AssertGlobalPosition(thirdBlock, 8, 78);
        }

        [Test]
        public void test_margin_collapse_with_first_child()
        {
            // 1) Render
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the parent <div style="margin-top:20px; border:none; padding:0">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);

            // 3) Find the child <div style="height:50px; margin-top:30px">
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);

            // 4) The parent's top margin (20px) + child's top margin (30px)
            //    collapse to 30px total offset from the page top.
            //    So the child should effectively sit 30px down from the top of the page (plus any body margin).
            //    Let's see how your engine tracks absolute Y. Possibly parent's Y is 8, child is at 38, etc.
            //    If there's no default body margin or other offsets, it might be 30.
            //    We'll assume the net offset is 30 from the page, ignoring body margin:
            AssertGlobalPosition(childDiv, 8, 30);
            // Adjust if your engine has a body margin of 8 => childDiv might be at (8, 38).

            // The key is that it's 30px total, not 50px.
        }

        [Test]
        public void test_multiple_sibling_margin_collapse()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the outer container <div> inside <body>
            var container = FindElementNodeByTagName(bodyNode, TagNames.Div);
            // Expect container positioned at (8,8)
            AssertGlobalPosition(container, 8, 8);

            // 3) Find three sibling divs inside the container.
            //    Each sibling has height:40px and margin:8px (both top and bottom).
            var sibling1 = FindChildByIndex(container, 0);
            var sibling2 = FindChildByIndex(container, 1);
            var sibling3 = FindChildByIndex(container, 2);
            That(sibling1, Is.Not.Null, "Could not find sibling 1.");
            That(sibling2, Is.Not.Null, "Could not find sibling 2.");
            That(sibling3, Is.Not.Null, "Could not find sibling 3.");

            // 4) Expected positions:
            //    - First sibling: with collapsed top margin, its top is at Y = 8.
            AssertGlobalPosition(sibling1, 16, 8);
            //    - The gap between siblings collapses the bottom of sibling1 and top of sibling2:
            //         Collapse(8,8) → 8px gap.
            //         So sibling2's top should be: 8 + 40 + 8 = 56.
            AssertGlobalPosition(sibling2, 16, 56);
            //    - Similarly, sibling3's top should be: 56 + 40 + 8 = 104.
            AssertGlobalPosition(sibling3, 16, 104);
        }

        [Test]
        public void test_negative_margins_allow_overlap()
        {
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(containerDiv, 300);

            var firstBlock = FindChildByIndex(containerDiv, 0);
            AssertHeight(firstBlock, 50);

            var secondBlock = FindChildByIndex(containerDiv, 1);
            AssertHeight(secondBlock, 70);

            AssertGlobalPosition(secondBlock, 8, 48);
        }


        [Test]
        public void test_only_left_margin_auto()
        {
            // HTML:
            // <div style="width: 400px">
            //     <div style="width: 100px; margin-left: auto; margin-right: 20px;"></div>
            // </div>

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            AssertContentWidth(childDiv, 100);

            // Check computed style
            That(childDiv.ComputedStyle.MarginLeft, Is.EqualTo("auto"));
            That(childDiv.ComputedStyle.MarginRight, Is.EqualTo("20px"));

            // Check final layout
            var containerX = containerDiv.Layout?.X ?? 0.0;
            var containerWidth = containerDiv.Layout?.BoxWidth ?? 0.0;
            var childX = childDiv.Layout?.X ?? 0.0;
            var childWidth = childDiv.Layout?.BoxWidth ?? 0.0;

            // The leftover space is 400 - 100 - 20 => 280
            // With margin-left:auto, child should have x= containerX + 280
            var expectedX = containerX + 280;
            That(childX, Is.EqualTo(expectedX).Within(0.5));
        }

        [Test]
        public void test_only_right_margin_auto()
        {
            // HTML:
            // <div style="width: 400px">
            //     <div style="width: 150px; margin-left: 10px; margin-right: auto;"></div>
            // </div>

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            AssertContentWidth(childDiv, 150);

            // Check computed style
            That(childDiv.ComputedStyle.MarginLeft, Is.EqualTo("10px"));
            That(childDiv.ComputedStyle.MarginRight, Is.EqualTo("auto"));

            // Check final layout
            var containerX = containerDiv.Layout?.X ?? 0.0;
            var containerWidth = containerDiv.Layout?.BoxWidth ?? 0.0;
            var childX = childDiv.Layout?.X ?? 0.0;
            var childWidth = childDiv.Layout?.BoxWidth ?? 0.0;

            // leftover = 400 - (150 + 10) => 240
            // margin-right is auto => child remains left-aligned with 10px margin-left
            var expectedX = containerX + 10; //
            That(childX, Is.EqualTo(expectedX).Within(0.5),
                "Child should sit 10px from the container's left edge.");
        }

        [Test]
        public void test_single_child_margin_collapse()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the single child <div style="width:300px; margin:8px;"> under <body>
            var singleChild = FindElementNodeByTagName(bodyNode, TagNames.Div);

            // 3) Assert that the child's content width is 300.
            AssertContentWidth(singleChild, 300);

            // 4) With body's margin of 8px and the child's own margin of 8px,
            //    proper collapse should result in the child being positioned (16,8).
            AssertGlobalPosition(singleChild, 16, 8);
        }

        [TestCaseSource(typeof(MarginTestData), nameof(MarginTestData.ParentChildTopMarginCases))]
        public void test_nested_margin_collapse_parent_child_top(MarginTestCase testCase)
        {
            ReplaceBody(testCase.Html);
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var parentDiv = FindElementNodeById(bodyNode, "d1");
            var childDiv = FindElementNodeById(parentDiv, "d2");
            AssertGlobalPosition(childDiv, testCase.ExpectedX, testCase.ExpectedY);
        }

        [TestCaseSource(typeof(MarginTestData), nameof(MarginTestData.ParentChildBottomMarginCases))]
        public void test_nested_margin_collapse_parent_child_bottom(MarginTestCase testCase)
        {
            ReplaceBody(testCase.Html);
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var parentDiv = FindElementNodeById(bodyNode, "d1");
            var childDiv = FindElementNodeById(parentDiv, "d2");
            var siblingDiv = FindElementNodeById(bodyNode, "d1.1");
            AssertGlobalPosition(siblingDiv, testCase.ExpectedX, testCase.ExpectedY);
        }

        [TestCaseSource(typeof(MarginTestData), nameof(MarginTestData.DeepHierarchyMarginCases))]
        public void test_nested_margin_collapse_with_deep_hierarchy(MarginTestCase testCase)
        {
            ReplaceBody(testCase.Html);
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var d1 = FindElementNodeById(bodyNode, "d1");
            var d2 = FindElementNodeById(d1, "d2");
            var d3 = FindElementNodeById(d2, "d3");
            AssertGlobalPosition(d3, testCase.ExpectedX, testCase.ExpectedY);
        }


        [Test]
        public void test_vertical_margins_collapse_between_siblings()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 300px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);

            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) First child <div style="height: 50px; margin-bottom: 30px">
            var firstBlock = FindElementNodeById(containerDiv, "d2.1");
            AssertDisplay(firstBlock, "block");
            AssertContentWidth(firstBlock, 300);
            // Usually at (8,8)
            AssertGlobalPosition(firstBlock, 8, 8);

            // 4) Second child <div style="height: 70px; margin-top: 20px">
            var secondBlock = FindElementNodeById(containerDiv, "d2.2");
            AssertDisplay(secondBlock, "block");
            AssertContentWidth(secondBlock, 300);

            // 5) Verify margin collapsing:
            //    bottom margin of first block = 30px
            //    top margin of second block   = 20px
            //    => collapsed gap = max(30, 20) = 30px
            //
            // So secondBlock’s top = firstBlock.Y + firstBlock.Height + 30 = 8 + 50 + 30 = 88
            AssertGlobalPosition(secondBlock, 8, 88);
        }

        [Test]
        public void test_global_positions_and_dimensions()
        {
            // 1) Render the document and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // --- BODY (ID: d0) ---
            // Chrome reports BODY at (0, 40) (relative to document)
            AssertDisplay(bodyNode, "block");
            AssertGlobalPosition(bodyNode, 0, 40);

            // --- Container d0.1 ---
            // Chrome: X=498, Y=40; Expected content width = 600px (from CSS)
            var d01 = FindElementNodeById(bodyNode, "d0.1");
            AssertDisplay(d01, "block");
            AssertContentWidth(d01, 600);
            AssertGlobalPosition(d01, 498, 40);

            // --- d0.1.1 ---
            // Chrome: X=520, Y=82
            var d011 = FindElementNodeById(d01, "d0.1.1");
            AssertDisplay(d011, "block");
            AssertGlobalPosition(d011, 520, 82);

            // --- <p> inside d0.1.1, ID: d0.1.1-p ---
            // Chrome: X=531, Y=109
            var p_d011 = FindElementNodeById(d011, "d0.1.1-p");
            AssertDisplay(p_d011, "block");
            AssertGlobalPosition(p_d011, 531, 109);

            // --- d0.1.2 ---
            // Chrome: X=520, Y=174
            var d012 = FindElementNodeById(d01, "d0.1.2");
            AssertDisplay(d012, "block");
            AssertGlobalPosition(d012, 520, 174);

            // --- <p> inside d0.1.2, ID: d0.1.2-p ---
            // Chrome: X=531, Y=201
            var p_d012 = FindElementNodeById(d012, "d0.1.2-p");
            AssertDisplay(p_d012, "block");
            AssertGlobalPosition(p_d012, 531, 201);

            // --- d0.1.2.1 ---
            // Chrome: X=531, Y=239
            var d0121 = FindElementNodeById(d012, "d0.1.2.1");
            AssertDisplay(d0121, "block");
            AssertGlobalPosition(d0121, 531, 239);

            // --- <p> inside d0.1.2.1, ID: d0.1.2.1-p ---
            // Chrome: X=542, Y=266
            var p_d0121 = FindElementNodeById(d0121, "d0.1.2.1-p");
            AssertDisplay(p_d0121, "block");
            AssertGlobalPosition(p_d0121, 542, 266);

            // --- d0.1.2.2 (empty block) ---
            // Chrome: X=531, Y=331
            var d0122 = FindElementNodeById(d012, "d0.1.2.2");
            AssertDisplay(d0122, "block");
            AssertGlobalPosition(d0122, 531, 331);

            // --- d0.1.2.3 ---
            // Chrome: X=531, Y=331
            var d0123 = FindElementNodeById(d012, "d0.1.2.3");
            AssertDisplay(d0123, "block");
            AssertGlobalPosition(d0123, 531, 331);

            // --- <p> inside d0.1.2.3, ID: d0.1.2.3-p ---
            // Chrome: X=542, Y=358
            var p_d0123 = FindElementNodeById(d0123, "d0.1.2.3-p");
            AssertDisplay(p_d0123, "block");
            AssertGlobalPosition(p_d0123, 542, 358);

            // --- d0.1.2.3.1 ---
            // Chrome: X=542, Y=396
            var d01231 = FindElementNodeById(d0123, "d0.1.2.3.1");
            AssertDisplay(d01231, "block");
            AssertGlobalPosition(d01231, 542, 396);

            // --- <p> inside d0.1.2.3.1, ID: d0.1.2.3.1-p ---
            // Chrome: X=553, Y=423
            var p_d01231 = FindElementNodeById(d01231, "d0.1.2.3.1-p");
            AssertDisplay(p_d01231, "block");
            AssertGlobalPosition(p_d01231, 553, 423);

            // --- d0.1.3 ---
            // Chrome: X=520, Y=550
            var d013 = FindElementNodeById(d01, "d0.1.3");
            AssertDisplay(d013, "block");
            AssertGlobalPosition(d013, 520, 550);

            // --- d0.1.4 ---
            // Chrome: X=520, Y=550
            var d014 = FindElementNodeById(d01, "d0.1.4");
            AssertDisplay(d014, "block");
            AssertGlobalPosition(d014, 520, 550);

            // --- Container d0.2 (side-by-side empty blocks container) ---
            // Chrome: X=498, Y=612
            var d02 = FindElementNodeById(bodyNode, "d0.2");
            AssertDisplay(d02, "block");
            AssertGlobalPosition(d02, 498, 612);

            // --- d0.2.1 ---
            // Chrome: X=520, Y=654
            var d021 = FindElementNodeById(d02, "d0.2.1");
            AssertDisplay(d021, "block");
            AssertGlobalPosition(d021, 520, 654);

            // --- d0.2.2 ---
            // Chrome: X=825, Y=654
            var d022 = FindElementNodeById(d02, "d0.2.2");
            AssertDisplay(d022, "block");
            AssertGlobalPosition(d022, 825, 654);

            // --- Container d0.3 ---
            // Chrome: X=498, Y=736
            var d03 = FindElementNodeById(bodyNode, "d0.3");
            AssertDisplay(d03, "block");
            AssertGlobalPosition(d03, 498, 736);

            // --- d0.3.1 ---
            // Chrome: X=520, Y=778
            var d031 = FindElementNodeById(d03, "d0.3.1");
            AssertDisplay(d031, "block");
            AssertGlobalPosition(d031, 520, 778);

            // --- <p> inside d0.3.1, ID: d0.3.1-p ---
            // Chrome: X=531, Y=805
            var p_d031 = FindElementNodeById(d031, "d0.3.1-p");
            AssertDisplay(p_d031, "block");
            AssertGlobalPosition(p_d031, 531, 805);

            // --- d0.3.1.1 ---
            // Chrome: X=531, Y=843
            var d0311 = FindElementNodeById(d031, "d0.3.1.1");
            AssertDisplay(d0311, "block");
            AssertGlobalPosition(d0311, 531, 843);

            // --- <p> inside d0.3.1.1, ID: d0.3.1.1-p ---
            // Chrome: X=542, Y=870
            var p_d0311 = FindElementNodeById(d0311, "d0.3.1.1-p");
            AssertDisplay(p_d0311, "block");
            AssertGlobalPosition(p_d0311, 542, 870);

            // --- d0.3.1.1.1 ---
            // Chrome: X=542, Y=908
            var d03111 = FindElementNodeById(d0311, "d0.3.1.1.1");
            AssertDisplay(d03111, "block");
            AssertGlobalPosition(d03111, 542, 908);

            // --- <p> inside d0.3.1.1.1, ID: d0.3.1.1.1-p ---
            // Chrome: X=553, Y=935
            var p_d03111 = FindElementNodeById(d03111, "d0.3.1.1.1-p");
            AssertDisplay(p_d03111, "block");
            AssertGlobalPosition(p_d03111, 553, 935);

            // --- d0.3.1.1.1.1 ---
            // Chrome: X=553, Y=973
            var d031111 = FindElementNodeById(d03111, "d0.3.1.1.1.1");
            AssertDisplay(d031111, "block");
            AssertGlobalPosition(d031111, 553, 973);

            // --- <p> inside d0.3.1.1.1.1, ID: d0.3.1.1.1.1-p ---
            // Chrome: X=564, Y=1000
            var p_d031111 = FindElementNodeById(d031111, "d0.3.1.1.1.1-p");
            AssertDisplay(p_d031111, "block");
            AssertGlobalPosition(p_d031111, 564, 1000);
        }
    }
}