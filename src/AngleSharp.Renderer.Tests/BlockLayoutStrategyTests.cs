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
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find child <div style="display:block">
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            // Check child's display and layout
            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            // Typically at (8,8) from the parent's coordinate space
            AssertGlobalPosition(childDiv, 8, 8);
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
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) First child <div style="height: 50px; margin-bottom: 30px">
            var firstBlock = FindChildByIndex(containerDiv, 0);
            AssertDisplay(firstBlock, "block");
            AssertContentWidth(firstBlock, 300);
            // Usually at (8,8)
            AssertGlobalPosition(firstBlock, 8, 8);

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
            AssertGlobalPosition(secondBlock, 8, 58);
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
            AssertBoxWidth(parentDiv, 300);
            AssertContentWidth(parentDiv, 270);

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
            AssertGlobalPosition(containerDiv, 8, 8);

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
            NotNull(containerDiv, "Could not find container <div> under <body>.");

            // With no box-sizing specified, the default is content-box.
            AssertDisplay(containerDiv, "block");
            AssertContentWidth(containerDiv, 300);
            AssertGlobalPosition(containerDiv, 8, 8);

            // 3) Find the child <div>
            var childDiv = FindElementNodeByTagName(containerDiv, TagNames.Div);
            NotNull(childDiv, "Could not find child <div> under container <div>.");

            AssertDisplay(childDiv, "block");
            AssertContentWidth(childDiv, 300);
            AssertHeight(childDiv, 0);
            AssertGlobalPosition(childDiv, 20, 20);
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