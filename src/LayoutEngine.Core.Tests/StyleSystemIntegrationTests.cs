namespace LayoutEngine.Core.Tests;

using System.Threading.Tasks;
using AngleSharp;
using Core;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class StyleSystemIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStyleSystem _styleSystem;
    private readonly IBrowsingContext _context;
    private readonly IEventAggregator _eventAggregator;

    public StyleSystemIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLayoutEngine();

        _serviceProvider = services.BuildServiceProvider();
        _styleSystem = _serviceProvider.GetRequiredService<IStyleSystem>();
        _context = _serviceProvider.GetRequiredService<IBrowsingContext>();
        _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
    }

    [Fact]
    public async Task CompleteStyleComputation_WithRealHTML_ComputesAllStyles()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        body { font-family: Arial; color: #333; }
                        .header { font-size: 24px; color: blue; }
                        .content { margin: 20px; padding: 10px; }
                        #special { background: yellow; }
                    </style>
                </head>
                <body>
                    <div class='header'>Title</div>
                    <div class='content'>
                        <p id='special'>Special paragraph</p>
                        <p>Normal paragraph</p>
                    </div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var bodyStyle = _styleSystem.GetComputedStyle(document.Body!);
        var headerStyle = _styleSystem.GetComputedStyle(document.QuerySelector(".header")!);
        var specialStyle = _styleSystem.GetComputedStyle(document.QuerySelector("#special")!);
        var normalParagraph = _styleSystem.GetComputedStyle(document.QuerySelector("p:not(#special)")!);

        Assert.NotNull(bodyStyle);
        Assert.NotNull(headerStyle);
        Assert.NotNull(specialStyle);
        Assert.NotNull(normalParagraph);

        // Verify cascade works correctly
        Assert.Equal("blue", headerStyle.GetPropertyValue("color"));
        Assert.Equal("24px", headerStyle.GetPropertyValue("font-size"));

        // Verify inheritance works
        Assert.Equal("Arial", headerStyle.GetPropertyValue("font-family")); // Inherited from body

        // Verify ID selector specificity
        Assert.Equal("yellow", specialStyle.GetPropertyValue("background-color"));
    }

    [Fact]
    public async Task StyleInvalidation_TriggersCorrectRecomputation()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .test { color: red; }
                    </style>
                </head>
                <body>
                    <div class='test' id='target'>Test</div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var targetElement = document.QuerySelector("#target")!;

        _styleSystem.ComputeDocumentStyles(document);
        var originalStyle = _styleSystem.GetComputedStyle(targetElement);
        Assert.Equal("red", originalStyle!.GetPropertyValue("color"));

        // Track events
        bool styleInvalidatedEventReceived = false;
        bool styleComputedEventReceived = false;
        _eventAggregator.Subscribe<StyleInvalidatedEvent>(e => styleInvalidatedEventReceived = true);
        _eventAggregator.Subscribe<StyleComputedEvent>(e => styleComputedEventReceived = true);

        // Act
        _styleSystem.InvalidateStyle(targetElement);
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        Assert.True(styleInvalidatedEventReceived);
        Assert.True(styleComputedEventReceived);
    }

    [Fact]
    public async Task DynamicStyleChanges_UpdateComputedStyles()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .red { color: red; }
                        .blue { color: blue; }
                    </style>
                </head>
                <body>
                    <div class='red' id='target'>Test</div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var targetElement = document.QuerySelector("#target")!;

        _styleSystem.ComputeDocumentStyles(document);
        var originalStyle = _styleSystem.GetComputedStyle(targetElement);
        Assert.Equal("red", originalStyle!.GetPropertyValue("color"));

        // Act - Change class
        targetElement.ClassName = "blue";
        _styleSystem.InvalidateStyle(targetElement);
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var updatedStyle = _styleSystem.GetComputedStyle(targetElement);
        Assert.Equal("blue", updatedStyle!.GetPropertyValue("color"));
    }

    [Fact]
    public async Task ComplexSelectors_MatchCorrectElements()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .container > .item { color: red; }
                        .container .item.special { color: blue; }
                        .container .item:first-child { font-weight: bold; }
                        div + p { margin-top: 0; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='item special'>First Item</div>
                        <div class='item'>Second Item</div>
                        <p>Paragraph after div</p>
                    </div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var firstItem = document.QuerySelector(".item.special")!;
        var secondItem = document.QuerySelector(".item:not(.special)")!;
        var paragraph = document.QuerySelector("p")!;

        var firstItemStyle = _styleSystem.GetComputedStyle(firstItem);
        var secondItemStyle = _styleSystem.GetComputedStyle(secondItem);
        var paragraphStyle = _styleSystem.GetComputedStyle(paragraph);

        // More specific selector should win
        Assert.Equal("blue", firstItemStyle!.GetPropertyValue("color")); // .special is more specific
        Assert.Equal("red", secondItemStyle!.GetPropertyValue("color"));
        Assert.Equal("0px", paragraphStyle!.GetPropertyValue("margin-top")); // Adjacent sibling selector
    }

    [Fact]
    public async Task NestedElements_InheritPropertiesCorrectly()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        body {
                            font-family: 'Times New Roman';
                            color: #333;
                            line-height: 1.6;
                        }
                        .container {
                            font-size: 18px;
                            color: blue;
                        }
                        .nested {
                            font-weight: bold;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='nested'>
                            <span>Deep nested content</span>
                        </div>
                    </div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var spanElement = document.QuerySelector("span")!;
        var spanStyle = _styleSystem.GetComputedStyle(spanElement);

        // Should inherit from ancestors
        Assert.Equal("Times New Roman", spanStyle!.GetPropertyValue("font-family")); // From body
        Assert.Equal("1.6", spanStyle.GetPropertyValue("line-height"));             // From body
        Assert.Equal("18px", spanStyle.GetPropertyValue("font-size"));             // From .container
        Assert.Equal("blue", spanStyle.GetPropertyValue("color"));                 // From .container
        Assert.Equal("bold", spanStyle.GetPropertyValue("font-weight"));           // From .nested
    }

    [Fact]
    public async Task InlineStyles_OverrideStylesheetRules()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .test {
                            color: red !important;
                            font-size: 16px;
                            background: blue;
                        }
                    </style>
                </head>
                <body>
                    <div class='test' style='color: green; font-size: 20px;' id='target'>Test</div>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var targetElement = document.QuerySelector("#target")!;

        // Act
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var style = _styleSystem.GetComputedStyle(targetElement);

        // Important stylesheet rule should win over inline style
        Assert.Equal("red", style!.GetPropertyValue("color"));

        // Inline style should win over normal stylesheet rule
        Assert.Equal("20px", style.GetPropertyValue("font-size"));

        // Stylesheet rule should apply where no inline style exists
        Assert.Equal("blue", style.GetPropertyValue("background-color"));
    }

    [Fact]
    public async Task UserAgentStyles_ApplyAsDefault()
    {
        // Arrange
        var html = @"
                <html>
                <body>
                    <div>Regular div</div>
                    <h1>Heading</h1>
                    <p>Paragraph</p>
                </body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSystem.ComputeDocumentStyles(document);

        // Assert
        var divStyle = _styleSystem.GetComputedStyle(document.QuerySelector("div")!);
        var h1Style = _styleSystem.GetComputedStyle(document.QuerySelector("h1")!);
        var pStyle = _styleSystem.GetComputedStyle(document.QuerySelector("p")!);

        // These should have user agent styles applied
        Assert.NotNull(divStyle);
        Assert.NotNull(h1Style);
        Assert.NotNull(pStyle);

        // User agent styles should provide defaults
        Assert.NotEmpty(divStyle!.GetPropertyValue("display") ?? "");
    }

    [Fact]
    public async Task LargeDocument_ComputesStylesEfficiently()
    {
        // Arrange
        var htmlBuilder = new System.Text.StringBuilder();
        htmlBuilder.AppendLine("<html><head>");
        htmlBuilder.AppendLine("<style>");
        for (int i = 0; i < 50; i++)
        {
            htmlBuilder.AppendLine($".class-{i} {{ color: rgb({i * 5}, 0, 0); }}");
        }
        htmlBuilder.AppendLine("</style></head><body>");

        for (int i = 0; i < 100; i++)
        {
            htmlBuilder.AppendLine($"<div class='class-{i % 50}' id='element-{i}'>Element {i}</div>");
        }
        htmlBuilder.AppendLine("</body></html>");

        var document = await _context.OpenAsync(req => req.Content(htmlBuilder.ToString()));

        // Act
        var startTime = System.DateTime.UtcNow;
        _styleSystem.ComputeDocumentStyles(document);
        var endTime = System.DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        Assert.True(duration.TotalMilliseconds < 2000,
            $"Style computation took {duration.TotalMilliseconds}ms for large document, expected < 2000ms");

        // Verify some styles were computed correctly
        var firstElement = document.QuerySelector("#element-0")!;
        var lastElement = document.QuerySelector("#element-99")!;

        var firstStyle = _styleSystem.GetComputedStyle(firstElement);
        var lastStyle = _styleSystem.GetComputedStyle(lastElement);

        Assert.NotNull(firstStyle);
        Assert.NotNull(lastStyle);
    }
}