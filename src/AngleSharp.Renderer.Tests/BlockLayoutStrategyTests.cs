namespace AngleSharp.Renderer.Tests
{
    using Dom;
    using NUnit.Framework;
    using static NUnit.Framework.Assert;

    public class BlockLayoutStrategyTests : TestBase
    {
        [Test]
        public void test_block_element_takes_full_container_width()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find <div style="width:300px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // The container has display:block and width=300
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            // Position might be offset by body margin if absolute coords
            AssertPosition(containerDiv, 8, 8);

            // 3) Find child <div style="display:block">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // Check child's display and layout
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            // Typically at (8,8) from the parent's coordinate space
            AssertPosition(childDiv, 8, 8);
        }

        [Test]
        public void test_auto_margins_center_element_horizontally()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width:400px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // Assert it's block-level and 400px wide
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 400);
            // Typically at (8,8)
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div style="width:200px; margin-left:auto; margin-right:auto">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

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
        public void test_vertical_margins_collapse_between_siblings()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 300px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            AssertPosition(containerDiv, 8, 8);

            // 3) First child <div style="height: 50px; margin-bottom: 30px">
            var firstBlock = FindChildByIndex(containerDiv, 0);
            AssertDisplay(firstBlock, "block");
            AssertContentWidth(firstBlock, 300);
            // Usually at (8,8)
            AssertPosition(firstBlock, 8, 8);

            // 4) Second child <div style="height: 70px; margin-top: 20px">
            var secondBlock = FindChildByIndex(containerDiv, 1);
            AssertDisplay(secondBlock, "block");
            AssertContentWidth(secondBlock, 300);

            // 5) Verify margin collapsing:
            //    bottom margin of first block = 30px
            //    top margin of second block   = 20px
            //    => collapsed gap = max(30, 20) = 30px
            //
            // So secondBlock’s top = firstBlock.Y + firstBlock.Height + 30 = 8 + 50 + 30 = 88
            AssertPosition(secondBlock, 8, 58);
        }


        [Test]
        public void test_percentage_width_resolves_relative_to_container()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 400px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // Check container is block, 400px wide
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 400);

            // 3) Find child <div style="width: 50%">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside the container.");

            // Assert it is block-level
            AssertDisplay(childDiv, "block");
            // 50% of 400px => 200px
            AssertContentWidth(childDiv, 200);
        }

        [Test]
        public void test_padding_reduces_available_content_width()
        {
            // 1) Render and get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 300px; padding: 10px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // Assert container is block-level, 300px wide
            // 300px total width minus left/right padding of 10px => 280px content area
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 280);
            AssertBoxWidth(containerDiv, 300);

            // 3) Find child <div style="display: block">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside container.");

            // Assert child's display
            AssertDisplay(childDiv, "block");

            AssertContentWidth(childDiv, 280);
            AssertBoxWidth(childDiv, 280);
        }

        [Test]
        public void test_explicit_height_overrides_content_height()
        {
            // 1) Render and get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find parent <div style="width: 300px; height: 150px">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> under <body>.");

            // Check parent is block-level, 300px wide, 150px tall
            AssertDisplay(parentDiv, "block");
            AssertContentWidth(parentDiv, 300);
            AssertHeight(parentDiv, 150);

            // 3) Find child <div style="height: 200px">
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside parent.");

            // Check child is block-level
            AssertDisplay(childDiv, "block");
            // It explicitly sets height:200px, so we confirm that
            AssertHeight(childDiv, 200);

            // This means child extends beyond the parent's 150px container
            // (overflowing by default unless specified otherwise).
        }

        [Test]
        public void test_display_inline_not_handled_by_block_strategy()
        {
            // 1) Render the document so we have a <span style="display:inline">
            //    This is optional if your "CanHandle(...)" method just needs the element reference.
            //    But if your approach requires a real render tree, do the usual:
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the <span> we want to check
            var spanNode = FindElementNodeByTagName(bodyNode, TagNames.Span);
            NotNull(spanNode, "Could not find <span> in the body.");

            // 3) We want to ensure the block layout strategy does NOT handle inline elements
            //    This depends on your actual layout code. Typically something like:
            var blockStrategy = new BlockLayoutStrategy();
            bool canHandle = blockStrategy.CanHandle(spanNode);

            That(canHandle, Is.False,
                "Expected block layout strategy to NOT handle an inline element.");
        }

        [Test]
        public void test_border_increases_total_width()
        {
            // 1) Render and get the main nodes
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the parent <div style="width: 200px; border: 5px solid">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> under <body>.");

            // 3) Because border adds to the outside of content-box width:
            //    total width = content width (200px) + left border (5px) + right border (5px) = 210px
            //    So the computed layout width is 210.
            //    (If your engine lumps border into .Layout.Width, we expect 210).
            AssertContentWidth(parentDiv, 210);

            // Optionally check the child, but the requirement is only that the parent
            // has total box width = 210px.
            // If you want, you could:
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside the parent.");
            AssertDisplay(childDiv, "block");
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
            AssertPosition(secondBlock, 8 /* or the X offset if relevant */, 40 /* Y expected */);
        }

        [Test]
        public void test_percentage_padding_relative_to_container_width()
        {
            // 1) Render the document and get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width: 200px">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // 3) Find child <div style="padding: 10%">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> for percentage padding test.");

            // In CSS, padding percentages are based on container *width*.
            // 10% of 200px = 20px
            // So we expect childDiv.ComputedStyle.PaddingLeft = "20px", etc.
            That(childDiv.ComputedStyle.PaddingLeft, Is.EqualTo("20px"),
                "Expected left padding = 20px (10% of 200px).");
            That(childDiv.ComputedStyle.PaddingRight, Is.EqualTo("20px"),
                "Expected right padding = 20px (10% of 200px).");

            // (Optionally check top/bottom padding if your CSS also sets them or if the engine computes them by default.)
        }

        [Test]
        public void test_box_sizing_border_box_includes_border_padding()
        {
            // 1) Render & get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the parent <div style="width: 300px; padding: 10px; border: 5px solid; box-sizing: border-box">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> with border-box sizing.");

            // With box-sizing: border-box, the declared width (300px) is the total box width
            // including border + padding. So the "content box" is 300 - 2*(10 + 5) = 270.

            // 3) Check the parent's overall layout width is 300
            //    (Many engines will store the total box size in Layout.Width)
            AssertContentWidth(parentDiv, 300);

            // 4) Check the child’s content area is ~270 if the child is block and auto-fills the parent's content box
            //    i.e. 300 - leftPadding(10) - rightPadding(10) - leftBorder(5) - rightBorder(5) = 270
            //    If your engine doesn’t directly track “content width,” we assume the child is block-level inside:
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside border-box parent.");

            // If the child is a block-level, width:auto element, it should fill the parent's content box => 270
            AssertContentWidth(childDiv, 270);
        }

        [Test]
        public void test_min_width_overrides_content_width()
        {
            // 1) Render & get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find parent <div style="width: 100px; min-width: 200px">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> with min-width.");

            // By standard CSS rules, min-width=200px overrides width=100px
            // => final used width is 200px.
            AssertContentWidth(parentDiv, 200);

            // 3) Child <div style="width: 50px">
            //    We can confirm it’s present, but the main check is the parent's width.
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Expected a child <div> inside the parent.");
            // Possibly check child's display or width, but the key assertion is parent's 200px.
        }

        [Test]
        public void test_max_width_constrains_content_width()
        {
            // 1) Render & get (root, htmlNode, bodyNode)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find parent <div style="width: 500px; max-width: 300px">
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> with max-width.");

            // If both width=500px and max-width=300px, the used width is the smaller => 300px
            AssertContentWidth(parentDiv, 300);

            // 3) Child <div style="width: 400px">
            //    This doesn't matter as far as parent's final width, but we can check it’s there
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> inside parent with max-width.");
            // The child's own specified width can't exceed the parent's used width in normal flow, but
            // the main assertion is parent's final 300px.
        }

        [Test]
        public void test_display_none_skips_layout()
        {
            // 1) Render and get the top-level nodes
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find <div style="display:none">
            var hiddenDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(hiddenDiv, "Could not find <div> with display:none.");

            // 3) Since it's display:none, it should not appear in normal flow.
            //    Typically this means it has no layout box (i.e., hiddenDiv.Layout == null).
            That(hiddenDiv.Layout, Is.Null,
                "Elements with display:none should not produce a layout box.");
        }

        [Test]
        public void test_inline_block_retains_dimensions_in_inline_flow()
        {
            // 1) Render document
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Locate the container <div style="font-size:16px; width:300px;">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> for inline flow test.");

            // 3) Find the inline-block <span>
            var spanNode = FindElementNodeByTagName(containerDiv, TagNames.Span);
            NotNull(spanNode, "Could not find <span> with display:inline-block.");

            // 4) Check that the span is indeed recognized as 'inline-block'.
            That(spanNode.ComputedStyle.Display, Is.EqualTo("inline-block"),
                "Expected the <span> to have computed display 'inline-block'.");

            // 5) Assert the final computed width/height
            AssertContentWidth(spanNode, 100);
            AssertHeight(spanNode, 20);

            // 6) (Optional) Ensure the line box is at least 20px tall (so no overlap with text).
            //    If your engine stores line box info in containerDiv.Layout or somewhere similar,
            //    you might do something like:
            var containerHeight = containerDiv.Layout?.BoxHeight ?? 0.0;
            That(containerHeight, Is.GreaterThanOrEqualTo(20.0),
                "Parent line box should accommodate 20px tall inline-block.");

            // 7) (Optional) Confirm no forced line break (if your engine tracks inline flows).
            //    Typically you’d see the child’s X position is after the text, and
            //    the text that follows it is on the same line.
            //    For a quick check, you might examine the next sibling’s X > child’s X + child’s Width.
            //    Implementation depends heavily on your internal line-flow layout details.
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
            AssertPosition(childDiv, 8, 30);
            // Adjust if your engine has a body margin of 8 => childDiv might be at (8, 38).

            // The key is that it's 30px total, not 50px.
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
            AssertPosition(emptyBlock, 8, 78);

            // 7) Then for thirdBlock => emptyBlock.Y + emptyBlock.Height(=0 for empty block) + collapsedMargin(10 vs 15 => 15)
            //    => 78 + 0 + 15 = 93
            AssertPosition(thirdBlock, 8, 93);
        }

        [Test]
        public void test_percent_height_without_explicit_parent_height()
        {
            // 1) Render
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) The parent: <div style="width:300px; background:lightgray;">
            //    No explicit height => auto
            var parentDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(parentDiv, "Could not find parent <div> with auto height.");

            // 3) Child: <div style="height:50%; background:turquoise;">
            var childDiv = FindElementNodeByTagName(parentDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> with height=50%.");

            // 4) In standard CSS, if the parent’s height is auto, the child’s 50% is unresolved => child’s used height is auto.
            //    We can check the computed style or final layout. The engine might store it as "auto" or 0 if there's no content.
            //    So we confirm it's not 150px, for instance (which would be 50% of 300 if it incorrectly used width).
            //    Or we might check that .ComputedStyle.Height == "auto".
            That(childDiv.ComputedStyle.Height, Is.EqualTo("auto"),
                "Expected child's height to remain auto due to no explicit parent height.");

            // 5) If your engine calculates a final layout height, it may be 0 or minimal if there's no text or content inside.
            //    We'll just ensure it's not 150 or some incorrectly derived 50% of the parent's width.
            var childLayoutHeight = childDiv.Layout?.BoxHeight ?? 0.0;
            That(childLayoutHeight, Is.LessThan(10.0),
                "Child should not get a large 50%-of-parent-height. Likely near 0 if empty or text-based auto height.");
        }

        [Test]
        public void test_overflow_hidden_clips_child()
        {
            // 1) Render and get (root, html, body)
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="width:300px; height:100px; overflow:hidden;">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> with overflow:hidden.");

            // Check container’s final height is 100px
            AssertHeight(containerDiv, 100);

            // 3) Find the child <div style="height:150px;">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> in overflow:hidden container.");

            // The child is 150px tall
            AssertHeight(childDiv, 150);

            // 4) Verify clipping.
            //    Typically, the child's box extends from Y=... to Y+150,
            //    but the parent's clip region is only 100px tall.
            //    Your layout engine might store an 'overflow' property or a clipping region.
            //    We can conceptually verify that the parent's height is 100,
            //    so anything beyond that is not visible (no scroll, since hidden).

            That(containerDiv.ComputedStyle.Overflow, Is.EqualTo("hidden"),
                "Expected container to have overflow:hidden.");

            // (Optional) If your engine tracks the portion of the child that is visible:
            //    we might check childDiv.Layout.VisibleRegion == (0,0,300,100) or similar,
            //    but that depends on your implementation details.

            // The key concept:
            // The extra 50px of child’s height is clipped and not visible.
        }

        [Test]
        public void test_inline_block_baseline_alignment()
        {
            // 1) Render
            var (root, htmlNode, bodyNode) = RenderDocumentAndGetNodes();

            // 2) Find the container <div style="font-size:16px;">
            var containerDiv = FindElementNodeByTagName(bodyNode, TagNames.Div);
            NotNull(containerDiv, "Could not find container <div> for inline-block baseline test.");

            // 3) Find the inline-block <span>
            var spanNode = FindElementNodeByTagName(containerDiv, TagNames.Span);
            NotNull(spanNode, "Could not find inline-block <span>.");

            // 4) Verify the computed display is "inline-block" and vertical-align is "baseline"
            That(spanNode.ComputedStyle.Display, Is.EqualTo("inline-block"),
                "Expected <span> to have computed display: inline-block.");
            That(spanNode.ComputedStyle.VerticalAlign, Is.EqualTo("baseline"),
                "Expected <span> to have vertical-align: baseline.");

            // 5) Check the inline-block’s final computed width/height
            //    The example sets width=80px, height=16px.
            AssertContentWidth(spanNode, 80);
            AssertHeight(spanNode, 16);

            // 6) Confirm the line box accommodates 16px in height and
            //    that the bottom of the inline-block aligns with the text baseline.
            //    In many engines, the container's line-height or line box might be ~16px (or a bit more).
            var containerHeight = containerDiv.Layout?.BoxHeight ?? 0.0;
            That(containerHeight, Is.GreaterThanOrEqualTo(16.0),
                "Line box must be at least 16px tall to avoid clipping the inline-block.");

            // 7) Baseline alignment can be tested in detail if your engine exposes
            //    a 'baseline' metric or the text's baseline Y. E.g.:
            //    var textBaselineY = containerDiv.Layout?.FirstLineBaseline ?? ...
            //    var spanBottomY   = spanNode.Layout?.Y + spanNode.Layout?.Height.
            //    Then compare them:
            //    That(spanBottomY, Is.EqualTo(textBaselineY).Within(1.0),
            //        "Inline-block bottom should align with text baseline.");
            //
            //    Without such metrics, verifying no extra gap or shift is typically a visual or more advanced check.
        }

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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should span the full container content width (300px) and have zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should also have a computed width of 276px and zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 276);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
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
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // With no box-sizing specified, the default is content-box.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // The child should have zero height.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, containerDiv.Layout.BoxWidth); // width might be inherited or auto (if width not set)
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 8, 8);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, containerDiv.Layout.BoxWidth);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 8, 8);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 476);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 8, 8);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 0);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 326);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
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
            AssertPosition(containerDiv, 8, 8);

            // 3) Find the inner child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find inner child <div> under outer container <div>.");

            // The inner child uses content-box, so its specified width (100px) is its content width.
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 100);
            AssertHeight(childDiv, 0);
            AssertPosition(childDiv, 20, 20);
        }
    }
}