// #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
// namespace AngleSharp.LayoutEngine.Tests.BoxTests;
//
// using Adapters;
// using AngleSharp.LayoutEngine.Box;
// using Dom;
// using Helpers;
// using LayoutEngine.Core;
// using LayoutEngine.Style;
// using NUnit.Framework;
//
// [TestFixture]
// public class BoxValuesTests
// {
//     private IBrowsingContext _context;
//
//     [SetUp]
//     public void Setup()
//     {
//         _context = BrowsingContext.New(Configuration.Default.WithCss().WithTestRenderDevice(baseFontSize: 16.0));
//     }
//
//     [TearDown]
//     public void Cleanup()
//     {
//         _context?.Dispose();
//     }
//
//     [Test]
//     public void BoxValues_WithPixelMeasurements_CalculatesCorrectDimensions()
//     {
//         // Create a document to give us access to AngleSharp's CSS infrastructure
//         var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         // Set inline styles for testing
//         element.SetAttribute("style", "width: 100px; padding: 10px; border-width: 5px; margin: 15px;");
//
//         // Get the computed style
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Now test our BoxModelCalculator with the real style
//         var calculator = new BoxModelCalculator(computedStyle, 800);
//         var boxValues = calculator.GetBoxValues();
//
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(100));
//         Assert.That(boxValues.PaddingLeft, Is.EqualTo(10));
//         Assert.That(boxValues.BorderRight, Is.EqualTo(5));
//         Assert.That(boxValues.MarginTop, Is.EqualTo(15));
//         Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(130)); // 100 + 10*2 + 5*2
//         Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(160)); // 130 + 15*2
//     }
//
//     [Test]
//     public void BoxValues_WithMixedUnits_CalculatesCorrectDimensions()
//     {
//         var document = _context.OpenAsync(req => req.Content(@"
//             <div style='font-size: 16px;'>
//                 <div id='test' style='width: 300px; height: 10em; padding: 1em 5%; border-width: 5px; margin: 10px 5%;'></div>
//             </div>
//         ")).Result;
//
//         var element = document.QuerySelector("#test");
//
//         // Get the computed style
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Print actual values for debugging
//         Console.WriteLine($"Computed padding-right: {computedStyle.PaddingRight}");
//         Console.WriteLine($"Computed margin-right: {computedStyle.MarginRight}");
//
//         // Now test our BoxModelCalculator with the real style
//         var calculator = new BoxModelCalculator(computedStyle, 1000); // Parent width is 1000px
//         var boxValues = calculator.GetBoxValues();
//
//         // Print calculated values for debugging
//         Console.WriteLine($"Calculated padding-right: {boxValues.PaddingRight}");
//         Console.WriteLine($"Calculated margin-right: {boxValues.MarginRight}");
//
//         // Test dimensions with mixed units
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(300).Within(1)); // 300px
//
//         // em values (1em = 16px in this case)
//         Assert.That(boxValues.PaddingTop, Is.EqualTo(16).Within(1)); // 1em = 16px
//
//         // Percentage values - AngleSharp uses device width (1024px) not container width
//         Assert.That(boxValues.PaddingRight, Is.InRange(50, 52)); // ~5% of device width
//
//         // Mixed margins (10px top/bottom, 5% of device width left/right)
//         Assert.That(boxValues.MarginTop, Is.EqualTo(10).Within(1)); // 10px
//         Assert.That(boxValues.MarginRight, Is.InRange(50, 52)); // ~5% of device width
//
//         // Height should be 10em = 160px
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(160).Within(1)); // 10em = 10 * 16px
//     }
//
//     [Test]
//     public void BoxValues_WithPercentageMeasurements_CalculatesCorrectDimensions()
//     {
//         var document = _context.OpenAsync(req => req.Content(@"<div style='width: 1000px; height: 800px;'></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         // Set percentage-based styles for testing
//         element.SetAttribute("style", "width: 50%; height: 25%; padding: 5%; margin: 10%; border-width: 5px;");
//
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Test with direct style resolver to ensure consistent percentage calculations
//         var layoutContext = new LayoutContext(1000, 800); // Make this match the parent container dimensions
//         var adapter = new AngleSharpRenderDimensionsAdapter(layoutContext);
//         var styleResolver = new DirectStyleResolver(computedStyle, adapter);
//
//         var calculator = new BoxModelCalculator(computedStyle, styleResolver);
//         var boxValues = calculator.GetBoxValues();
//
//         // 50% of 1000px = 500px
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(500).Within(0.1));
//
//         // 5% of 1000px = 50px
//         Assert.That(boxValues.PaddingLeft, Is.EqualTo(50).Within(0.1));
//         Assert.That(boxValues.BorderRight, Is.EqualTo(5));
//
//         // 10% of 1000px = 100px (corrected from 30px which was inconsistent)
//         Assert.That(boxValues.MarginTop, Is.EqualTo(100).Within(0.1));
//
//         // BorderBox: 500 + (50*2) + (5*2) = 610px
//         Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(610).Within(0.1));
//
//         // MarginBox: 610 + (100*2) = 810px
//         Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(810).Within(0.1));
//     }
//
//     [Test]
//     public void CalculateContentWidthFromBorderBox_WithDifferentBoxSizing_ReturnsCorrectValue()
//     {
//         // Create a BoxValues instance with some predefined values
//         var boxValues = new BoxValues
//         {
//             PaddingLeft = 10,
//             PaddingRight = 10,
//             BorderLeft = 5,
//             BorderRight = 5
//         };
//
//         // Calculate content width from border box width of 200px
//         boxValues.CalculateContentWidthFromBorderBox(200);
//
//         // Content width should be: border-box width - padding - border
//         // 200 - (10*2) - (5*2) = 200 - 20 - 10 = 170
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(170));
//
//         // Test with border-box of 0 (should clamp to 0)
//         boxValues.CalculateContentWidthFromBorderBox(0);
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(0));
//
//         // Test with border-box smaller than insets (should clamp to 0)
//         boxValues.CalculateContentWidthFromBorderBox(20);
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(0));
//     }
//
//     [Test]
//     public void CalculateContentHeightFromBorderBox_WithDifferentBoxSizing_ReturnsCorrectValue()
//     {
//         // Create a BoxValues instance with some predefined values
//         var boxValues = new BoxValues
//         {
//             PaddingTop = 15,
//             PaddingBottom = 15,
//             BorderTop = 8,
//             BorderBottom = 8
//         };
//
//         // Calculate content height from border box height of 250px
//         boxValues.CalculateContentHeightFromBorderBox(250);
//
//         // Content height should be: border-box height - padding - border
//         // 250 - (15*2) - (8*2) = 250 - 30 - 16 = 204
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(204));
//
//         // Test with border-box of 0 (should clamp to 0)
//         boxValues.CalculateContentHeightFromBorderBox(0);
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(0));
//
//         // Test with border-box smaller than insets (should clamp to 0)
//         boxValues.CalculateContentHeightFromBorderBox(40);
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(0));
//     }
//
//     [Test]
//     public void HorizontalInsets_WithVariousValues_CalculatesCorrectly()
//     {
//         var boxValues = new BoxValues
//         {
//             PaddingLeft = 10,
//             PaddingRight = 15,
//             BorderLeft = 5,
//             BorderRight = 8
//         };
//
//         // Horizontal insets should be sum of left and right padding and border
//         // 10 + 15 + 5 + 8 = 38
//         Assert.That(boxValues.HorizontalInsets, Is.EqualTo(38));
//
//         // Test with zero values
//         boxValues.PaddingLeft = 0;
//         boxValues.PaddingRight = 0;
//         boxValues.BorderLeft = 0;
//         boxValues.BorderRight = 0;
//         Assert.That(boxValues.HorizontalInsets, Is.EqualTo(0));
//
//         // Test with negative values (should not happen in practice, but testing for robustness)
//         boxValues.PaddingLeft = -5; // In real CSS, this would be clamped to 0
//         Assert.That(boxValues.HorizontalInsets, Is.EqualTo(-5));
//     }
//
//     [Test]
//     public void VerticalInsets_WithVariousValues_CalculatesCorrectly()
//     {
//         var boxValues = new BoxValues
//         {
//             PaddingTop = 12,
//             PaddingBottom = 18,
//             BorderTop = 6,
//             BorderBottom = 9
//         };
//
//         // Vertical insets should be sum of top and bottom padding and border
//         // 12 + 18 + 6 + 9 = 45
//         Assert.That(boxValues.VerticalInsets, Is.EqualTo(45));
//
//         // Test with zero values
//         boxValues.PaddingTop = 0;
//         boxValues.PaddingBottom = 0;
//         boxValues.BorderTop = 0;
//         boxValues.BorderBottom = 0;
//         Assert.That(boxValues.VerticalInsets, Is.EqualTo(0));
//     }
//
//     [Test]
//     public void BoxValues_WithBorderBoxSizing_CalculatesCorrectDimensions()
//     {
//         var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         // Set box-sizing: border-box with dimensions
//         element.SetAttribute("style", "box-sizing: border-box; width: 200px; height: 150px; padding: 20px; border: 5px solid black;");
//
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         var calculator = new BoxModelCalculator(computedStyle, 1000);
//         var boxValues = calculator.GetBoxValues();
//
//         // Content width should be: width - padding - border
//         // 200 - (20*2) - (5*2) = 200 - 40 - 10 = 150
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(150).Within(0.1));
//
//         // BorderBoxWidth should be the specified width
//         Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(200).Within(0.1));
//     }
//
//     [Test]
//     public void BoxValues_WithLayoutContext_CalculatesCorrectDimensions()
//     {
//         // Create a document to give us access to AngleSharp's CSS infrastructure
//         var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         element.SetAttribute("style", "width: 100px; padding: 10px; border-width: 5px; margin: 15px;");
//
//         // Get the computed style
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Create a layout context with viewport dimensions
//         var layoutContext = new LayoutContext(800, 600);
//
//         // Create calculator with new constructor
//         var calculator = new BoxModelCalculator(computedStyle, layoutContext);
//         var boxValues = calculator.GetBoxValues();
//
//         // Should produce same results as original constructor
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(100));
//         Assert.That(boxValues.PaddingLeft, Is.EqualTo(10));
//         Assert.That(boxValues.BorderRight, Is.EqualTo(5));
//         Assert.That(boxValues.MarginTop, Is.EqualTo(15));
//         Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(130));
//         Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(160));
//     }
//
//     [Test]
//     public void BoxValues_WithDirectStyleResolver_CalculatesCorrectDimensions()
//     {
//         // Create a document to give us access to AngleSharp's CSS infrastructure
//         var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         element.SetAttribute("style", "width: 100px; padding: 10px; border-width: 5px; margin: 15px;");
//
//         // Get the computed style
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Create components for direct resolution
//         var layoutContext = new LayoutContext(800, 600);
//         var adapter = new AngleSharpRenderDimensionsAdapter(layoutContext);
//         var styleResolver = new DirectStyleResolver(computedStyle, adapter);
//
//         // Create calculator with resolver constructor
//         var calculator = new BoxModelCalculator(computedStyle, styleResolver);
//         var boxValues = calculator.GetBoxValues();
//
//         // Should produce same results as original constructor
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(100));
//         Assert.That(boxValues.PaddingLeft, Is.EqualTo(10));
//         Assert.That(boxValues.BorderRight, Is.EqualTo(5));
//         Assert.That(boxValues.MarginTop, Is.EqualTo(15));
//         Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(130));
//         Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(160));
//     }
//
//     [Test]
//     public void BoxValues_WithViewportRelativeUnits_CalculatesCorrectly()
//     {
//         var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
//         var element = document.QuerySelector("div");
//
//         Assert.That(element, Is.Not.Null);
//         element.SetAttribute("style", "width: 50vw; height: 25vh; padding: 5vmin; margin: 2vmax;");
//
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Use our enhanced calculation with viewport dimensions
//         var layoutContext = new LayoutContext(1000, 800);
//         var adapter = new AngleSharpRenderDimensionsAdapter(layoutContext);
//         var styleResolver = new DirectStyleResolver(computedStyle, adapter);
//
//         var calculator = new BoxModelCalculator(computedStyle, styleResolver);
//         var boxValues = calculator.GetBoxValues();
//
//         // 50% of viewport width (1000px) = 500px
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(500).Within(0.1));
//
//         // 25% of viewport height (800px) = 200px
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(200).Within(0.1));
//
//         // 5% of min(1000, 800) = 5% of 800 = 40px
//         Assert.That(boxValues.PaddingTop, Is.EqualTo(40).Within(0.1));
//
//         // 2% of max(1000, 800) = 2% of 1000 = 20px
//         Assert.That(boxValues.MarginTop, Is.EqualTo(20).Within(0.1));
//     }
//
//     [Test]
//     public void BoxValues_WithFontRelativeUnits_CalculatesCorrectly()
//     {
//         var document = _context.OpenAsync(req => req.Content(@"
//         <div style='font-size: 20px;'>
//             <div id='test' style='width: 10em; height: 5rem; padding: 0.5em; margin: 1rem;'></div>
//         </div>
//     ")).Result;
//
//         var element = document.QuerySelector("#test");
//         Assert.That(element, Is.Not.Null);
//
//         var computedStyle = document.DefaultView.GetComputedStyle(element);
//
//         // Create a layout context with font information
//         var layoutContext = new LayoutContext(1000, 800)
//         {
//             DefaultFontSize = 16 // Root font size is 16px
//         };
//
//         // Create a custom adapter to provide parent font size
//         var parentElement = document.QuerySelector("div");
//         var parentStyle = document.DefaultView.GetComputedStyle(parentElement);
//
//         // Create node structure to test em units correctly
//         var parentNode = new LayoutNode(null);
//         var testNode = new LayoutNode(null) { Parent = parentNode };
//
//         var baseAdapter = new AngleSharpRenderDimensionsAdapter(layoutContext);
//         var elementAdapter = AngleSharpRenderDimensionsAdapter.CreateForNode(testNode, baseAdapter);
//         var styleResolver = new DirectStyleResolver(computedStyle, elementAdapter);
//
//         var calculator = new BoxModelCalculator(computedStyle, styleResolver);
//         var boxValues = calculator.GetBoxValues();
//
//         // 10em * 20px = 200px (em is relative to parent font size)
//         Assert.That(boxValues.ContentWidth, Is.EqualTo(200).Within(0.1));
//
//         // 5rem * 16px = 80px (rem is relative to root font size)
//         Assert.That(boxValues.ContentHeight, Is.EqualTo(80).Within(0.1));
//
//         // 0.5em * 20px = 10px
//         Assert.That(boxValues.PaddingTop, Is.EqualTo(10).Within(0.1));
//
//         // 1rem * 16px = 16px
//         Assert.That(boxValues.MarginTop, Is.EqualTo(16).Within(0.1));
//     }
// }