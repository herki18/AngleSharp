using System;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem;
using AngleSharp.StyleSystem.Interfaces;
using NUnit.Framework;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

namespace AngleSharp.StyleSystem.Tests;

[TestFixture]
public class StyleSystemE2ETests
{
    [Test]
    public async Task ComputedStyles_ShouldReflectCascadedValues()
    {
        // Arrange - Setup configuration with StyleSystem enabled
        var config = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
            .WithStyleSystem();

        // Create a new context with the configuration
        using var context = BrowsingContext.New(config);

        // Define HTML with embedded CSS that tests cascade, inheritance, and specificity
        var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        /* Base styles */
                        body {
                            font-family: Arial, sans-serif;
                            color: #333;
                            margin: 20px;
                            --custom-padding: 15px;
                        }

                        /* Element selector - lower specificity */
                        p {
                            font-size: 16px;
                            line-height: 1.5;
                            margin-bottom: 1em;
                        }

                        /* Class selector - medium specificity */
                        .highlighted {
                            background-color: #ffff00;
                            padding: var(--custom-padding);
                            border: 1px solid #ccc;
                        }

                        /* ID selector - highest specificity */
                        #special {
                            color: #0000ff;
                            font-weight: bold;
                        }

                        /* Testing logical properties */
                        .box {
                            margin-block: 10px;
                            padding-inline: 20px;
                            border-block-width: 2px;
                            border-block-style: solid;
                            border-block-color: #ff0000;
                        }

                        /* Child styles for inheritance testing */
                        .parent {
                            color: green;
                            font-family: 'Times New Roman', serif;
                        }

                        /* !important rule for testing priority */
                        .important-rule {
                            color: orange !important;
                        }
                    </style>
                </head>
                <body>
                    <div class='parent'>
                        <p>This paragraph inherits styles from its parent.</p>
                        <p class='highlighted'>This paragraph has a class applied.</p>
                        <p id='special' class='highlighted'>This paragraph has both a class and ID.</p>
                    </div>
                    <div class='box'>This tests logical properties.</div>
                    <p class='important-rule' id='special'>This tests !important rules.</p>
                </body>
                </html>";

        // Act - Parse the document
        var document = await context.OpenAsync(req => req.Content(html));
        Assert.That(document, Is.Not.Null);

        // Test 1: Basic style computation
        var paragraph = document.QuerySelector("p") as IElement;
        var computedStyle = paragraph.GetComputedStyle();

        Assert.That(computedStyle, Is.Not.Null);
        Assert.That(computedStyle.GetPropertyValue("font-size"), Is.EqualTo("16px"));
        // Line-height should be computed as 24px (1.5 * 16px)
        Assert.That(computedStyle.GetPropertyValue("line-height"), Is.EqualTo("24px"));
        // Margin-bottom should be computed as 16px (1em = font-size)
        Assert.That(computedStyle.GetPropertyValue("margin-bottom"), Is.EqualTo("16px"));

        // Test 2: Inheritance
        // The paragraph should inherit color from parent div which has color:green
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));

        // Test 3: Style cascade and specificity
        var highlightedParagraph = document.QuerySelector("p.highlighted") as IElement;
        var highlightedStyle = highlightedParagraph.GetComputedStyle();

        Assert.That(highlightedStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(255, 255, 0, 1)"));
        Assert.That(highlightedStyle.GetPropertyValue("padding"), Is.EqualTo("15px"));
        // Border should be computed with absolute values
        Assert.That(highlightedStyle.GetPropertyValue("border"), Is.EqualTo("1px solid rgba(204, 204, 204, 1)"));

        // Test 4: Highest specificity (ID)
        var specialParagraph = document.QuerySelector("#special") as IElement;
        var specialStyle = specialParagraph.GetComputedStyle();

        Assert.That(specialStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // ID overrides inherited color
        Assert.That(specialStyle.GetPropertyValue("font-weight"), Is.EqualTo("700")); // bold = 700
        Assert.That(specialStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(255, 255, 0, 1)")); // Still has class styles

        // Test 5: Logical properties conversion
        var boxElement = document.QuerySelector(".box") as IElement;
        var boxStyle = boxElement.GetComputedStyle();

        // Logical properties should be converted to physical ones
        Assert.That(boxStyle.GetPropertyValue("margin-top"), Is.EqualTo("10px"));
        Assert.That(boxStyle.GetPropertyValue("margin-bottom"), Is.EqualTo("10px"));
        Assert.That(boxStyle.GetPropertyValue("padding-left"), Is.EqualTo("20px"));
        Assert.That(boxStyle.GetPropertyValue("padding-right"), Is.EqualTo("20px"));
        Assert.That(boxStyle.GetPropertyValue("border-top-width"), Is.EqualTo("2px"));
        Assert.That(boxStyle.GetPropertyValue("border-top-style"), Is.EqualTo("solid"));
        Assert.That(boxStyle.GetPropertyValue("border-top-color"), Is.EqualTo("rgba(255, 0, 0, 1)"));

        // Test 6: CSS Variables
        Assert.That(highlightedStyle.GetPropertyValue("padding"), Is.EqualTo("15px"));

        // Test 7: !important rule wins over specificity
        var importantElement = document.QuerySelector(".important-rule") as IElement;
        var importantStyle = importantElement.GetComputedStyle();

        Assert.That(importantStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 165, 0, 1)")); // !important overrides ID

        // Test 8: Dynamic style updates
        // Add a new inline style to an element and verify it's applied
        paragraph.SetAttribute("style", "margin-left: 30px; color: purple;");

        // Force a recalculation of styles
        context.RecalculateStyles();

        // Get fresh computed style
        computedStyle = paragraph.GetComputedStyle();
        Assert.That(computedStyle.GetPropertyValue("margin-left"), Is.EqualTo("30px"));
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(128, 0, 128, 1)")); // purple in rgba

        // Test 9: Style sharing verification
        // Create similar elements and check if style optimization works
        var parent = document.QuerySelector(".parent") as IElement;
        for (int i = 0; i < 5; i++)
        {
            var newP = document.CreateElement("p");
            newP.TextContent = $"Similar paragraph {i}";
            parent.AppendChild(newP);
        }

        // Force a recalculation of styles
        context.RecalculateStyles();

        // All similar paragraphs should compute to the same styles
        var similarParagraphs = document.QuerySelectorAll(".parent > p:not([class]):not([id]):not([style])");
        Assert.That(similarParagraphs.Length, Is.GreaterThan(1));

        var firstStyle = (similarParagraphs[0] as IElement).GetComputedStyle();
        var secondStyle = (similarParagraphs[1] as IElement).GetComputedStyle();

        Assert.That(secondStyle.GetPropertyValue("font-size"), Is.EqualTo(firstStyle.GetPropertyValue("font-size")));
        Assert.That(secondStyle.GetPropertyValue("line-height"), Is.EqualTo(firstStyle.GetPropertyValue("line-height")));
        Assert.That(secondStyle.GetPropertyValue("color"), Is.EqualTo(firstStyle.GetPropertyValue("color")));

        // Test 10: Pseudo-element styles
        // Get the style for a pseudo-element
        var pseudoStyle = specialParagraph.GetComputedStyle("::before");
        Assert.That(pseudoStyle, Is.Not.Null);
    }

    [Test]
    public async Task DynamicStyleChanges_ShouldUpdateComputedStyles()
    {
        // Arrange
        var config = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
            .WithStyleSystem();

        using var context = BrowsingContext.New(config);

        var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style id='mainStyle'>
                        .dynamic { color: blue; }
                    </style>
                </head>
                <body>
                    <div id='target' class='dynamic'>Target element</div>
                </body>
                </html>";

        // Act
        var document = await context.OpenAsync(req => req.Content(html));

        // Initial style check
        var target = document.GetElementById("target");
        var initialStyle = target.GetComputedStyle();
        Assert.That(initialStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));

        // Modify the stylesheet
        var styleElement = document.GetElementById("mainStyle") as IHtmlElement;
        styleElement.InnerHtml = ".dynamic { color: red; font-weight: bold; }";

        // Force style recalculation
        context.RecalculateStyles();

        // Get updated style
        var updatedStyle = target.GetComputedStyle();
        Assert.That(updatedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(updatedStyle.GetPropertyValue("font-weight"), Is.EqualTo("700")); // bold = 700

        // Change the class
        target.ClassName = "other";
        context.RecalculateStyles();

        // Class changed, so styles should revert
        var revertedStyle = target.GetComputedStyle();
        Assert.That(revertedStyle.GetPropertyValue("color"), Is.Not.EqualTo("rgba(255, 0, 0, 1)"));

        // Add a new style element
        var newStyle = document.CreateElement("style");
        newStyle.TextContent = ".other { color: green; text-decoration: underline; }";
        document.Head.AppendChild(newStyle);

        context.RecalculateStyles();

        // New style should be applied
        var finalStyle = target.GetComputedStyle();
        Assert.That(finalStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));
        Assert.That(finalStyle.GetPropertyValue("text-decoration"), Is.EqualTo("underline"));
    }

    [Test]
    public async Task MediaQueries_ShouldApplyBasedOnDeviceSettings()
    {
        // Arrange
        var config = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
            .WithStyleSystem();

        using var context = BrowsingContext.New(config);

        var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        #responsive { color: black; }

                        @media (max-width: 600px) {
                            #responsive { color: green; }
                        }

                        @media (min-width: 601px) {
                            #responsive { color: blue; }
                        }
                    </style>
                </head>
                <body>
                    <div id='responsive'>Responsive element</div>
                </body>
                </html>";

        // Act - Load with default viewport (typically larger than 600px)
        var document = await context.OpenAsync(req => req.Content(html));
        var responsive = document.GetElementById("responsive");

        // Get the style engine service
        var styleSystem = context.GetStyleSystem();
        Assert.That(styleSystem, Is.Not.Null);

        // Test with default viewport (large)
        var initialStyle = responsive.GetComputedStyle();
        Assert.That(initialStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));

        // Change viewport to small screen
        styleSystem.StyleEngine.RenderDevice.SetViewport(400, 800);
        context.RecalculateStyles();

        // Get updated style
        var smallScreenStyle = responsive.GetComputedStyle();
        Assert.That(smallScreenStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));

        // Change viewport back to large screen
        styleSystem.StyleEngine.RenderDevice.SetViewport(1024, 768);
        context.RecalculateStyles();

        // Get updated style
        var largeScreenStyle = responsive.GetComputedStyle();
        Assert.That(largeScreenStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }

    [Test]
    public async Task StyleOptimization_ShouldReduceMemoryUsage()
    {
        // Arrange
        var config = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
            .WithStyleSystem();

        using var context = BrowsingContext.New(config);

        // Enable optimization metrics collection
        var styleSystem = context.GetStyleSystem();
        styleSystem.SetOptimization(true, true); // Enable optimization and metrics

        // Create a document with many similar elements
        var html = "<html><head><style>.test { color: blue; font-size: 12px; margin: 10px; padding: 5px; }</style></head><body>";

        // Add 100 similar elements
        for (int i = 0; i < 100; i++)
        {
            html += $"<div class='test'>Test element {i}</div>";
        }
        html += "</body></html>";

        // Act
        var document = await context.OpenAsync(req => req.Content(html));

        // Force style computation for all elements
        foreach (var element in document.QuerySelectorAll(".test"))
        {
            (element as IElement).GetComputedStyle();
        }

        // Get optimization metrics
        var metrics = context.GetStyleOptimizationMetrics();

        // Assert
        Assert.That(metrics, Is.Not.Null);

        // We should see some memory savings from optimization
        Assert.That(metrics.MemorySavingsPercentage, Is.GreaterThan(0));
        Assert.That(metrics.TotalPropertiesBeforeOptimization, Is.GreaterThan(metrics.TotalPropertiesAfterOptimization));

        // Some properties should be shared
        Assert.That(metrics.SharedNodeCount, Is.GreaterThan(0));

        // Common properties should be in the most frequent list
        Assert.That(metrics.MostFrequentProperties.ContainsKey("color"), Is.True);
        Assert.That(metrics.MostFrequentProperties.ContainsKey("font-size"), Is.True);
    }
}