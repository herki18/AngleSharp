namespace LayoutEngine.Core.Tests;

using System;
using System.Linq;
using AngleSharp;
using AngleSharp.Css.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Style.Internal;
using NSubstitute;
using Xunit;

public class StyleSheetManagerTests : IDisposable
{
    private readonly IBrowsingContext _context;
    private readonly IEventAggregator _eventAggregator;
    private readonly StyleSheetManager _styleSheetManager;

    public StyleSheetManagerTests()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _eventAggregator = Substitute.For<IEventAggregator>();
        _styleSheetManager = new StyleSheetManager(_context, _eventAggregator);
    }

    [Fact]
    public async void AttachToDocument_LoadsDocumentStylesheets()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .test { color: red; }
                    </style>
                </head>
                <body></body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSheetManager.AttachToDocument(document);

        // Assert
        var authorStylesheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author);
        Assert.NotEmpty(authorStylesheets);
    }

    [Fact]
    public async void AttachToDocument_LoadsInlineStyles()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>
                        .inline-test { background: blue; }
                    </style>
                </head>
                <body></body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));

        // Act
        _styleSheetManager.AttachToDocument(document);

        // Assert
        var authorStylesheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author);
        var hasInlineStylesheet = authorStylesheets.Any(sheet =>
            sheet.Rules.Any(rule => rule is ICssStyleRule styleRule &&
                                    styleRule.SelectorText.Contains("inline-test")));

        Assert.True(hasInlineStylesheet);
    }

    [Fact]
    public void RegisterStylesheet_PublishesStylesheetChangedEvent()
    {
        // Arrange
        var stylesheet = StyleTestHelpers.CreateMockStylesheet();

        // Act
        _styleSheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Assert
        _eventAggregator.Received(1).Publish(
            Arg.Is<StylesheetChangedEvent>(e =>
                e.Stylesheet == stylesheet &&
                e.ChangeType == StyleSheetChangeType.Added));
    }

    [Fact]
    public void UnregisterStylesheet_PublishesStylesheetChangedEvent()
    {
        // Arrange
        var stylesheet = StyleTestHelpers.CreateMockStylesheet();
        _styleSheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Act
        _styleSheetManager.UnregisterStylesheet(stylesheet);

        // Assert
        _eventAggregator.Received(1).Publish(
            Arg.Is<StylesheetChangedEvent>(e =>
                e.Stylesheet == stylesheet &&
                e.ChangeType == StyleSheetChangeType.Removed));
    }

    [Fact]
    public async void DetachFromDocument_ClearsAuthorStylesheets()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>.test { color: red; }</style>
                </head>
                <body></body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        _styleSheetManager.AttachToDocument(document);

        var authorSheetsBeforeDetach = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).Count();
        Assert.True(authorSheetsBeforeDetach > 0);

        // Act
        _styleSheetManager.DetachFromDocument(document);

        // Assert
        var authorSheetsAfterDetach = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).Count();
        Assert.Equal(0, authorSheetsAfterDetach);
    }

    [Fact]
    public void GetStylesheetsByOrigin_ReturnsCorrectStylesheets()
    {
        // Arrange
        var authorSheet = StyleTestHelpers.CreateMockStylesheet();
        var userAgentSheet = StyleTestHelpers.CreateMockStylesheet();

        _styleSheetManager.RegisterStylesheet(authorSheet, StylesheetOrigin.Author);
        _styleSheetManager.RegisterStylesheet(userAgentSheet, StylesheetOrigin.UserAgent);

        // Act
        var authorSheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author);
        var userAgentSheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent);

        // Assert
        Assert.Contains(authorSheet, authorSheets);
        Assert.Contains(userAgentSheet, userAgentSheets);
        Assert.DoesNotContain(authorSheet, userAgentSheets);
        Assert.DoesNotContain(userAgentSheet, authorSheets);
    }

    [Fact]
    public void GetStylesheetOrigin_ReturnsCorrectOrigin()
    {
        // Arrange
        var stylesheet = StyleTestHelpers.CreateMockStylesheet();
        _styleSheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.User);

        // Act
        var origin = _styleSheetManager.GetStylesheetOrigin(stylesheet);

        // Assert
        Assert.Equal(StylesheetOrigin.User, origin);
    }

    [Fact]
    public void LoadAngleSharpUserAgentStylesheets_RegistersBuiltInStyles()
    {
        // Act - This happens in constructor
        var userAgentSheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent);

        // Assert
        Assert.NotEmpty(userAgentSheets);
    }

    [Fact]
    public async void RefreshDocumentStylesheets_ReloadsStylesheets()
    {
        // Arrange
        var html = @"
                <html>
                <head>
                    <style>.original { color: red; }</style>
                </head>
                <body></body>
                </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        _styleSheetManager.AttachToDocument(document);

        var originalCount = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author).Count();

        // Simulate adding a new style element
        var newStyleElement = document.CreateElement("style");
        newStyleElement.TextContent = ".new-style { background: blue; }";
        document.Head?.AppendChild(newStyleElement);

        // Act
        _styleSheetManager.RefreshDocumentStylesheets();

        // Assert
        _eventAggregator.Received(1).Publish(Arg.Any<StylesheetsRefreshedEvent>());
    }

    [Fact]
    public void GetAllRules_ReturnsAllRulesFromAllStylesheets()
    {
        // Arrange
        var stylesheet1 = StyleTestHelpers.CreateMockStylesheetWithRules(new[] { ".rule1 { color: red; }" });
        var stylesheet2 = StyleTestHelpers.CreateMockStylesheetWithRules(new[] { ".rule2 { color: blue; }" });

        _styleSheetManager.RegisterStylesheet(stylesheet1, StylesheetOrigin.Author);
        _styleSheetManager.RegisterStylesheet(stylesheet2, StylesheetOrigin.Author);

        // Act
        var allRules = _styleSheetManager.GetAllRules();

        // Assert
        Assert.True(allRules.Count() >= 2); // At least our two rules
    }

    [Fact]
    public void GetAllStyleRules_ReturnsOnlyStyleRules()
    {
        // Arrange
        var stylesheet = StyleTestHelpers.CreateMockStylesheetWithRules(new[] {
            ".style-rule { color: red; }",
            "@media screen { .media-rule { color: blue; } }"
        });

        _styleSheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        // Act
        var styleRules = _styleSheetManager.GetAllStyleRules();

        // Assert
        Assert.All(styleRules, rule => Assert.IsAssignableFrom<ICssStyleRule>(rule));
    }

    [Fact]
    public void RegisterStylesheet_WithNullStylesheet_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _styleSheetManager.RegisterStylesheet(null!, StylesheetOrigin.Author));
    }

    [Fact]
    public void AttachToDocument_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _styleSheetManager.AttachToDocument(null!));
    }

    public void Dispose()
    {
        _styleSheetManager?.Dispose();
        _context?.Dispose();
    }
}