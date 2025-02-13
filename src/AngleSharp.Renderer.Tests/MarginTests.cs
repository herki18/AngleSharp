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
            NotNull(containerDiv);
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv);
            AssertContentWidth(childDiv, 160);

            // Final layout
            var containerX = containerDiv.Layout?.X ?? 0f;
            var containerWidth = containerDiv.Layout?.BoxWidth ?? 0f; // 400
            var childX = childDiv.Layout?.X ?? 0f;
            var childWidth = childDiv.Layout?.BoxWidth ?? 0f;

            // Typically your code uses "contentWidth = containerWidth - (paddingLeft + paddingRight + borders + marginLeft + marginRight)"
            // So the leftover for auto margins is computed AFTER container’s padding.
            // Let's see if that’s how your engine is set up.
            var expectedOffsetFromContainerLeft = 20 + 100; // 120
            That(childX - containerX, Is.EqualTo(expectedOffsetFromContainerLeft).Within(1.0));
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
            NotNull(containerDiv);
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv);
            AssertDisplay(childDiv, "block");

            // The child's computed style for width might be "auto",
            // but let's see if your layout engine sets a numeric "computed" width or not.
            // If your code sets it to 400, or 0, or something else, it can differ.
            // Typically we expect the final Layout width to match the container's content box width.
            var childLayout = childDiv.Layout;
            NotNull(childLayout, "Child layout box is null.");

            That(childLayout.BoxWidth, Is.EqualTo(400).Within(1.0),
                "Child should fill the container in normal block flow when width is auto and margins are auto.");

            // X position should be the same as container's X in typical block layout.
            That(childLayout.X, Is.EqualTo(containerDiv.Layout?.X ?? 0).Within(0.5),
                "Expected child to start at the container's X (no horizontal offset).");
        }

        [Test]
        public void test_block_element_takes_full_container_width()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find container <div style="width:300px"> inside <body>
            var container = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(container, "Could not find container <div>.");
            AssertContentWidth(container, 300);

            // 3) Find first child (first block): <div style="height:50px; margin-bottom:20px;">
            var firstBlock = FindChildByIndex(container, 0);
            NotNull(firstBlock, "Could not find first block.");
            // Expect height of 50px
            AssertHeight(firstBlock, 50);

            // 4) Find second child: <div style="height:50px; margin-top:30px;">
            var secondBlock = FindChildByIndex(container, 1);
            NotNull(secondBlock, "Could not find second block.");

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
            NotNull(containerDiv);
            AssertContentWidth(containerDiv, 300);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv);
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
            // 1) Render
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Container <div style="width:200px">
            var container = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(container, "Could not find container <div>.");

            // 3) firstBlock: <div style="height:40px; margin-bottom:20px;">
            var firstBlock = FindChildByIndex(container, 0);
            NotNull(firstBlock, "Could not find first block.");
            AssertHeight(firstBlock, 40);

            // 4) emptyBlock: <div style="margin-top:30px; margin-bottom:10px;">
            var emptyBlock = FindChildByIndex(container, 1);
            NotNull(emptyBlock, "Could not find the empty block.");

            // 5) thirdBlock: <div style="height:40px; margin-top:15px;">
            var thirdBlock = FindChildByIndex(container, 2);
            NotNull(thirdBlock, "Could not find the third block.");
            AssertHeight(thirdBlock, 40);

            // 6) Let's check the Y positions to confirm collapsed margins.
            //    If the container is at Y=8, firstBlock is at 8, then
            //    next sibling's top => firstBlock.Y + firstBlock.Height + collapsedMargin(20 vs 30 => 30).
            //    => 8 + 40 + 30 = 78 => emptyBlock's top
            AssertGlobalPosition(emptyBlock, 8, 78);

            // 7) Then for thirdBlock => emptyBlock.Y + emptyBlock.Height(=0 for empty block) + collapsedMargin(10 vs 15 => 15)
            //    => 78 + 0 + 15 = 93
            AssertGlobalPosition(thirdBlock, 8, 93);
        }

        [Test]
        public void test_margin_collapse_with_first_child()
        {
            // 1) Render
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the parent <div style="margin-top:20px; border:none; padding:0">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> with margin-top:20px.");

            // 3) Find the child <div style="height:50px; margin-top:30px">
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> with margin-top:30px.");

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
            NotNull(container, "Could not find the container <div>.");
            // Expect container positioned at (8,8)
            AssertGlobalPosition(container, 8, 8);

            // 3) Find three sibling divs inside the container.
            //    Each sibling has height:40px and margin:8px (both top and bottom).
            var sibling1 = FindChildByIndex(container, 0);
            var sibling2 = FindChildByIndex(container, 1);
            var sibling3 = FindChildByIndex(container, 2);
            NotNull(sibling1, "Could not find sibling 1.");
            NotNull(sibling2, "Could not find sibling 2.");
            NotNull(sibling3, "Could not find sibling 3.");

            // 4) Expected positions:
            //    - First sibling: with collapsed top margin, its top is at Y = 8.
            AssertGlobalPosition(sibling1, 8, 8);
            //    - The gap between siblings collapses the bottom of sibling1 and top of sibling2:
            //         Collapse(8,8) → 8px gap.
            //         So sibling2's top should be: 8 + 40 + 8 = 56.
            AssertGlobalPosition(sibling2, 8, 56);
            //    - Similarly, sibling3's top should be: 56 + 40 + 8 = 104.
            AssertGlobalPosition(sibling3, 8, 104);
        }

        [Test]
        public void test_negative_margins_allow_overlap()
        {
            // 1) Render the document
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 300px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");
            AssertContentWidth(containerDiv, 300);

            // 3) Find the first child <div style="height: 50px; margin-bottom: -10px">
            var firstBlock = FindChildByIndex(containerDiv, 0);
            NotNull(firstBlock, "Could not find first child.");
            AssertHeight(firstBlock, 50);

            // 4) Find the second child <div style="height: 70px">
            var secondBlock = FindChildByIndex(containerDiv, 1);
            NotNull(secondBlock, "Could not find second child.");
            AssertHeight(secondBlock, 70);

            // 5) Check Y-position for negative margin overlap
            //    The first block is 50px tall, but margin-bottom = -10px
            //    => second block starts at 50 + (-10) = 40,
            //       effectively overlapping the prior 10px
            AssertGlobalPosition(secondBlock, 8 /* or the X offset if relevant */, 40 /* Y expected */);
        }

//         [Test]
// public void test_nested_margin_collapse_with_parent_and_siblings()
// {
//     // 1) Render and get (root, html, body)
//     var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
//
//     // 2) Find the outer container <div> (which has its own margin and padding)
//     var outerContainer = FindElementNodeByTagName(bodyNode, TagNames.Div);
//     NotNull(outerContainer, "Could not find outer container <div>.");
//     // For this test, assume outer container is positioned at (8,8)
//     AssertPosition(outerContainer, 8, 8);
//
//     // 3) Within the outer container, there are three nested sibling divs.
//     //    Each nested div has height:30px and margin:10px.
//     var nested1 = FindChildByIndex(outerContainer, 0);
//     var nested2 = FindChildByIndex(outerContainer, 1);
//     var nested3 = FindChildByIndex(outerContainer, 2);
//     NotNull(nested1, "Could not find nested div 1.");
//     NotNull(nested2, "Could not find nested div 2.");
//     NotNull(nested3, "Could not find nested div 3.");
//
//     // 4) Assume the outer container has a top padding of 5px; therefore,
//     //    its content area's Y origin is 8 (container) + 5 (padding) = 13.
//     //    For the first nested div, if its top margin (10px) collapses with the container's margin,
//     //    then its top should be at Y = 13.
//     AssertPosition(nested1, 8, 13);
//     // 5) For nested siblings, the gap is the collapse of 10px margins → 10px gap.
//     //    So nested2's top should be: 13 + 30 + 10 = 53.
//     AssertPosition(nested2, 8, 53);
//     //    And nested3's top should be: 53 + 30 + 10 = 93.
//     AssertPosition(nested3, 8, 93);
//
//     // 6) Finally, find additional sibling divs outside the outer container.
//     //    For example, these may be the second and third divs in <body>.
//     var sibling1 = FindElementNodeByTagName(bodyNode, TagNames.Div, 1);
//     var sibling2 = FindElementNodeByTagName(bodyNode, TagNames.Div, 2);
//     NotNull(sibling1, "Could not find sibling div 1.");
//     NotNull(sibling2, "Could not find sibling div 2.");
//
//     // 7) Assume the outer container’s bottom is computed around Y ~110.
//     //    With sibling1's margin of 8, its top should be approximately 110 + 8 = 118.
//     AssertPosition(sibling1, 8, 118);
//     // 8) Sibling2 should then be positioned at: 118 + 50 (sibling1 height) + 8 = 176.
//     AssertPosition(sibling2, 8, 176);
// }


        [Test]
        public void test_only_left_margin_auto()
        {
            // HTML:
            // <div style="width: 400px">
            //     <div style="width: 100px; margin-left: auto; margin-right: 20px;"></div>
            // </div>

            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv);
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv);
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
            NotNull(containerDiv);
            AssertContentWidth(containerDiv, 400);

            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv);
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
            NotNull(singleChild, "Could not find the single child <div>.");

            // 3) Assert that the child's content width is 300.
            AssertContentWidth(singleChild, 300);

            // 4) With body's margin of 8px and the child's own margin of 8px,
            //    proper collapse should result in the child being positioned (16,8).
            AssertGlobalPosition(singleChild, 16, 8);
        }

        [TestCase("<body style='margin: 0px;'><div style='margin-top: 20px;'><div style='margin-top: 30px;'></div></div></body>", 0, 30)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: 0px;'><div style='margin-top: 30px;'></div></div></body>", 0, 30)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: 20px;'><div style='margin-top: -10px;'></div></div></body>", 0, 10)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: -20px;'><div style='margin-top: -10px;'></div></div></body>", 0, -10)]
        public void test_nested_margin_collapse_parent_child(string html, double expectedX, double expectedY)
        {
            ReplaceBody(html);
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            AssertGlobalPosition(childDiv, expectedX, expectedY);
        }

        [TestCase("<body style='margin: 0px;'><div style='margin-top: 20px;'><div style='margin-top: 30px;'><div style='margin-top: 10px;'></div></div></div></body>", 8, 30)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: 20px;'><div style='margin-top: -10px;'><div style='margin-top: 15px;'></div></div></div></body>", 8, 15)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: 0px;'><div style='margin-top: 30px;'><div style='margin-top: 0px;'></div></div></div></body>", 8, 30)]
        [TestCase("<body style='margin: 0px;'><div style='margin-top: 20px; padding: 10px;'><div style='margin-top: 30px;'><div style='margin-top: 10px;'></div></div></div></body>", 8, 40)]
        public void test_nested_margin_collapse_with_deep_hierarchy(string html, double expectedX, double expectedY)
        {
            ReplaceBody(html);
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();
            var outerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            var middleDiv = FindElementNodeByTagName(outerDiv, TagNames.Div);
            var innerDiv = FindElementNodeByTagName(middleDiv, TagNames.Div);
            AssertGlobalPosition(innerDiv, expectedX, expectedY);
        }
    }
}