namespace AngleSharp.LayoutEngine.Tests.Style;

using AngleSharp.Dom;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using FormattingContexts.Enums;
using AngleSharp.LayoutEngine.Style;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestFixture]
public class StyleEngineTests
{
    private IBrowsingContext _context;
    private IDocument? _document;
    private StyleEngine _styleEngine;
    private LayoutTree _layoutTree;
    private RenderTreeBuilder _renderTreeBuilder;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _styleEngine = new StyleEngine();
        _layoutTree = new LayoutTree();
        _renderTreeBuilder = new RenderTreeBuilder();
    }

    [TearDown]
    public void Cleanup()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task StyleEngine_WithVariousDisplayValues_SetsCorrectDisplayTypes()
    {
        // Arrange - Create a document with different display properties
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #block { display: block; }
                        #inline { display: inline; }
                        #flex { display: flex; }
                        #inlineBlock { display: inline-block; }
                        #none { display: none; }
                    </style>
                </head>
                <body>
                    <div id='block'></div>
                    <div id='inline'></div>
                    <div id='flex'></div>
                    <div id='inlineBlock'></div>
                    <div id='none'></div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if display types are set correctly
        var blockNode = _layoutTree.FindNodeById("block");
        var inlineNode = _layoutTree.FindNodeById("inline");
        var flexNode = _layoutTree.FindNodeById("flex");
        var inlineBlockNode = _layoutTree.FindNodeById("inlineBlock");
        var noneNode = _layoutTree.FindNodeById("none");

        Assert.That(blockNode.Display, Is.EqualTo(DisplayType.Block));
        Assert.That(inlineNode.Display, Is.EqualTo(DisplayType.Inline));
        Assert.That(flexNode.Display, Is.EqualTo(DisplayType.Flex));
        Assert.That(inlineBlockNode.Display, Is.EqualTo(DisplayType.InlineBlock));

        // Display:none nodes might not be in the layout tree depending on implementation
        if (noneNode != null)
        {
            Assert.That(noneNode.Display, Is.EqualTo(DisplayType.None));
        }
    }

    [Test]
    public async Task StyleEngine_WithVariousPositionValues_SetsCorrectPositionTypes()
    {
        // Arrange - Create a document with different position properties
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #static { position: static; }
                        #relative { position: relative; }
                        #absolute { position: absolute; }
                        #fixed { position: fixed; }
                        #sticky { position: sticky; }
                    </style>
                </head>
                <body>
                    <div id='static'></div>
                    <div id='relative'></div>
                    <div id='absolute'></div>
                    <div id='fixed'></div>
                    <div id='sticky'></div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if position types are set correctly
        var staticNode = _layoutTree.FindNodeById("static");
        var relativeNode = _layoutTree.FindNodeById("relative");
        var absoluteNode = _layoutTree.FindNodeById("absolute");
        var fixedNode = _layoutTree.FindNodeById("fixed");
        var stickyNode = _layoutTree.FindNodeById("sticky");

        Assert.That(staticNode.Position, Is.EqualTo(PositionType.Static));
        Assert.That(relativeNode.Position, Is.EqualTo(PositionType.Relative));
        Assert.That(absoluteNode.Position, Is.EqualTo(PositionType.Absolute));
        Assert.That(fixedNode.Position, Is.EqualTo(PositionType.Fixed));
        Assert.That(stickyNode.Position, Is.EqualTo(PositionType.Sticky));
    }

    [Test]
    public async Task StyleEngine_WithInheritedProperties_InheritsCorrectly()
    {
        // Arrange - Create a document with inherited properties
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        body {
                            color: red;
                            font-size: 16px;
                            font-family: Arial, sans-serif;
                        }
                        #parent {
                            color: blue;
                            border: 1px solid black;
                        }
                        /* No color specified for child - should inherit */
                    </style>
                </head>
                <body>
                    <div id='parent'>
                        <div id='child'></div>
                    </div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if inherited properties are correctly applied
        // Since StyleEngine doesn't directly expose color properties (it focuses on layout),
        // we need to check the underlying DOM element's computed styles via AngleSharp

        var parent = _document.GetElementById("parent") as IElement;
        var child = _document.GetElementById("child") as IElement;

        var parentComputedStyle = _document.DefaultView.GetComputedStyle(parent);
        var childComputedStyle = _document.DefaultView.GetComputedStyle(child);

        // Child should inherit color from parent (blue), not from body (red)
        Assert.That(childComputedStyle.Color, Is.EqualTo(parentComputedStyle.Color));
        Assert.That(childComputedStyle.Color, Is.EqualTo("rgba(0, 0, 255, 1)")
            .Or.EqualTo("rgb(0, 0, 255)"));

        // Child should inherit font-size from body (16px) since parent doesn't override it
        Assert.That(childComputedStyle.FontSize, Is.EqualTo("16px"));

        // Child should inherit font-family from body
        Assert.That(childComputedStyle.FontFamily, Is.EqualTo("Arial, sans-serif"));

        // Child should NOT inherit border from parent (border is not an inherited property)
        Assert.That(childComputedStyle.BorderWidth, Is.Not.EqualTo(parentComputedStyle.BorderWidth));
    }

    [Test]
    public async Task StyleEngine_WithSpecificityRules_AppliesCorrectPrecedence()
    {
        // Arrange - Create a document with specificity challenges
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        /* Least specific */
                        div { color: black; }

                        /* More specific */
                        .test { color: green; }

                        /* Even more specific */
                        #target { color: blue; }

                        /* Most specific */
                        #target.test { color: red; }
                    </style>
                </head>
                <body>
                    <div id='target' class='test'>Test element</div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if specificity rules are correctly applied
        var target = _document.GetElementById("target") as IElement;
        var targetComputedStyle = _document.DefaultView.GetComputedStyle(target);

        // The most specific rule (#target.test) should win
        Assert.That(targetComputedStyle.Color, Is.EqualTo("rgba(255, 0, 0, 1)")
            .Or.EqualTo("rgb(255, 0, 0)"));
    }

    [Test]
    public async Task StyleEngine_WithImportantRules_OverridesNormalSpecificity()
    {
        // Arrange - Create a document with !important rules
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        /* Most specific, but not important */
                        #target.test { color: red; }

                        /* Less specific, but important */
                        .test { color: green !important; }
                    </style>
                </head>
                <body>
                    <div id='target' class='test'>Test element</div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if !important rules override normal specificity
        var target = _document.GetElementById("target") as IElement;
        var targetComputedStyle = _document.DefaultView.GetComputedStyle(target);

        // The !important rule should win despite lower specificity
        Assert.That(targetComputedStyle.Color, Is.EqualTo("rgba(0, 128, 0, 1)")
            .Or.EqualTo("rgb(0, 128, 0)"));
    }

    [Test]
    public async Task StyleEngine_WithInlineStyles_OverridesStylesheetRules()
    {
        // Arrange - Create a document with inline styles
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        #target { width: 100px; height: 200px; }
                    </style>
                </head>
                <body>
                    <div id='target' style='width: 300px;'>Test element</div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Act - Compute styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Assert - Check if inline styles override stylesheet rules
        var targetNode = _layoutTree.FindNodeById("target");

        // The Width should come from the inline style, not the stylesheet
        Assert.That(targetNode.Width, Is.InstanceOf<StyleLengthValue>());
        var widthValue = targetNode.Width as StyleLengthValue;
        Assert.That(widthValue, Is.Not.Null);
        Assert.That(widthValue.Value, Is.EqualTo(300));

        // The Height should come from the stylesheet
        Assert.That(targetNode.Height, Is.InstanceOf<StyleLengthValue>());
        var heightValue = targetNode.Height as StyleLengthValue;
        Assert.That(heightValue, Is.Not.Null);
        Assert.That(heightValue.Value, Is.EqualTo(200));
    }

    [Test]
    public async Task StyleEngine_WithStyleUpdate_UpdatesOnlyChangedNodes()
    {
        // Arrange - Create a document
        _document = await _context.OpenAsync(req => req.Content(@"
                <html>
                <head>
                    <style>
                        div { width: 100px; height: 100px; }
                    </style>
                </head>
                <body>
                    <div id='target1'></div>
                    <div id='target2'></div>
                </body>
                </html>
            "));

        // Build render tree and layout tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(_document);
        _layoutTree.BuildFromDOM(renderTree.Root);

        // Initial style computation
        _styleEngine.ComputeStyles(_layoutTree);

        // Get initial state
        var target1Before = _layoutTree.FindNodeById("target1");
        var target2Before = _layoutTree.FindNodeById("target2");
        var target1WidthBefore = ((StyleLengthValue)target1Before.Width).Value;
        var target2WidthBefore = ((StyleLengthValue)target2Before.Width).Value;

        // Act - Change style of one node and update
        var target1Element = _document.GetElementById("target1");
        Assert.That(target1Element, Is.Not.Null);
        target1Element.SetAttribute("style", "width: 200px;");

        // Get the DOM node and update just target1
        var renderNode = _renderTreeBuilder.FindRenderNode(target1Element);
        var dirtyLayoutNodes = new List<LayoutNode> { _layoutTree.FindNodeForDomNode(renderNode) };

        // Update styles for changed nodes
        _styleEngine.UpdateStyles(dirtyLayoutNodes);

        // Assert - Check that only the changed node was updated
        var target1After = _layoutTree.FindNodeById("target1");
        var target2After = _layoutTree.FindNodeById("target2");
        var target1WidthAfter = ((StyleLengthValue)target1After.Width).Value;
        var target2WidthAfter = ((StyleLengthValue)target2After.Width).Value;

        // Target1 width should have changed
        Assert.That(target1WidthAfter, Is.EqualTo(200));

        // Target2 width should remain the same
        Assert.That(target2WidthAfter, Is.EqualTo(target2WidthBefore));
        Assert.That(target2WidthAfter, Is.EqualTo(100));
    }
}