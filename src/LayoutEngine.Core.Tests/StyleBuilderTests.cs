namespace LayoutEngine.Core.Tests;

using System;
using System.Collections.Generic;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class StyleBuilderTests : IDisposable
{
    private readonly ICssStyleDeclarationFactory _styleDeclarationFactory;
    private readonly ICascadeResolver _cascadeResolver;
    private readonly IInheritanceResolver _inheritanceResolver;
    private readonly ILogger<StyleBuilder> _logger;
    private readonly StyleBuilder _styleBuilder;
    private readonly IBrowsingContext _context;

    public StyleBuilderTests()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);

        _styleDeclarationFactory = new CssStyleDeclarationFactory(_context);
        _cascadeResolver = Substitute.For<ICascadeResolver>();
        _inheritanceResolver = Substitute.For<IInheritanceResolver>();
        _logger = Substitute.For<ILogger<StyleBuilder>>();

        _styleBuilder = new StyleBuilder(
            _styleDeclarationFactory,
            _cascadeResolver,
            _inheritanceResolver,
            _logger);
    }

    [Fact]
    public async void ApplyMatchedProperties_BuildsCorrectDeclaration()
    {
        // Arrange
        var html = "<div class='test'>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".test") as IElement;

        var matchResult = CreateMockMatchResult();
        var context = CreateMockStyleRecalcContext(document, element);
        var resolvedDeclaration = CreateStyleDeclaration(new[] { ("color", "red") });

        _cascadeResolver.ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!)
            .Returns(resolvedDeclaration);

        // Act
        _styleBuilder.ApplyMatchedProperties(matchResult, context);

        // Assert
        _cascadeResolver.Received(1).ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!);
    }

    [Fact]
    public async void ApplyInheritance_IntegratesWithMatchedProperties()
    {
        // Arrange
        var html = "<div><span>Test</span></div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("span") as IElement;

        var matchResult = CreateMockMatchResult();
        var context = CreateMockStyleRecalcContext(document, element);
        var parentDeclaration = CreateStyleDeclaration(new[] { ("color", "blue") });
        var resolvedDeclaration = CreateStyleDeclaration(new[] { ("font-size", "16px") });

        _cascadeResolver.ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!)
            .Returns(resolvedDeclaration);

        // Act
        _styleBuilder.ApplyMatchedProperties(matchResult, context);
        _styleBuilder.ApplyInheritance(parentDeclaration);

        // Assert
        _inheritanceResolver.Received(1).ApplyInheritance(Arg.Any<ICssStyleDeclaration>(), parentDeclaration);
    }

    [Fact]
    public async void TakeStyle_ReturnsComputedStyleAndClearsBuilder()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var matchResult = CreateMockMatchResult();
        var context = CreateMockStyleRecalcContext(document, element);
        var resolvedDeclaration = CreateStyleDeclaration(new[] { ("color", "red") });

        _cascadeResolver.ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!)
            .Returns(resolvedDeclaration);

        _styleBuilder.ApplyMatchedProperties(matchResult, context);

        // Act
        var computedStyle = _styleBuilder.TakeStyle(element!);

        // Assert
        Assert.NotNull(computedStyle);
        Assert.Equal(element, computedStyle.Element);
        Assert.NotNull(computedStyle.Declaration);

        // Verify builder is cleared by trying to take style again
        Assert.Throws<InvalidOperationException>(() => _styleBuilder.TakeStyle(element!));
    }

    [Fact]
    public void TakeStyle_WithoutApplyMatchedProperties_ThrowsException()
    {
        // Arrange
        var element = TestHelpers.CreateMockElement();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _styleBuilder.TakeStyle(element));
    }

    [Fact]
    public void ApplyInheritance_WithoutApplyMatchedProperties_ThrowsException()
    {
        // Arrange
        var parentDeclaration = CreateStyleDeclaration(new[] { ("color", "blue") });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _styleBuilder.ApplyInheritance(parentDeclaration));
    }

    [Fact]
    public async void CompleteWorkflow_BuildsCorrectComputedStyle()
    {
        // Arrange
        var html = "<div class='parent'><span class='child'>Test</span></div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector(".child") as IElement;

        var matchResult = CreateMockMatchResult();
        var context = CreateMockStyleRecalcContext(document, element);
        var parentDeclaration = CreateStyleDeclaration(new[] { ("color", "blue"), ("font-family", "Arial") });
        var resolvedDeclaration = CreateStyleDeclaration(new[] { ("font-size", "16px"), ("margin", "10px") });

        _cascadeResolver.ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!)
            .Returns(resolvedDeclaration);

        // Setup inheritance resolver to copy inherited properties
        _inheritanceResolver.When(x => x.ApplyInheritance(Arg.Any<ICssStyleDeclaration>(), parentDeclaration))
            .Do(callInfo =>
            {
                var childDecl = callInfo.Arg<ICssStyleDeclaration>();
                childDecl.SetProperty("color", "blue");
                childDecl.SetProperty("font-family", "Arial");
            });

        // Act
        _styleBuilder.ApplyMatchedProperties(matchResult, context);
        _styleBuilder.ApplyInheritance(parentDeclaration);
        var computedStyle = _styleBuilder.TakeStyle(element!);

        // Assert
        Assert.Equal("16px", computedStyle.GetPropertyValue("font-size"));  // From matched properties
        Assert.Equal("10px", computedStyle.GetPropertyValue("margin"));     // From matched properties
        Assert.Equal("blue", computedStyle.GetPropertyValue("color"));      // From inheritance
        Assert.Equal("Arial", computedStyle.GetPropertyValue("font-family")); // From inheritance
    }

    [Fact]
    public async void ApplyMatchedProperties_CallsCascadeResolverWithCorrectParameters()
    {
        // Arrange
        var html = "<div>Test</div>";
        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.QuerySelector("div") as IElement;

        var userAgentRules = new List<MatchedRule> { CreateMockMatchedRule() };
        var authorRules = new List<MatchedRule> { CreateMockMatchedRule() };
        var matchResult = CreateMockMatchResult(userAgentRules, authorRules);
        var context = CreateMockStyleRecalcContext(document, element);

        _cascadeResolver.ResolveCascade(Arg.Any<IEnumerable<MatchedRule>>(), element!)
            .Returns(CreateStyleDeclaration(new (string, string)[0]));

        // Act
        _styleBuilder.ApplyMatchedProperties(matchResult, context);

        // Assert
        _cascadeResolver.Received(1).ResolveCascade(
            Arg.Is<IEnumerable<MatchedRule>>(rules =>
                rules != null &&
                System.Linq.Enumerable.Count(rules) == 2), // userAgent + author rules
            element!);
    }

    private ICssStyleDeclaration CreateStyleDeclaration((string property, string value)[] properties)
    {
        var declaration = new CssStyleDeclaration(_context);
        foreach (var (property, value) in properties)
        {
            declaration.SetProperty(property, value);
        }
        return declaration;
    }

    private IMatchResult CreateMockMatchResult(
        IReadOnlyList<MatchedRule>? userAgentRules = null,
        IReadOnlyList<MatchedRule>? authorRules = null)
    {
        var matchResult = Substitute.For<IMatchResult>();
        matchResult.UserAgentRules.Returns(userAgentRules ?? new List<MatchedRule>());
        matchResult.AuthorRules.Returns(authorRules ?? new List<MatchedRule>());
        matchResult.InlineStyle.Returns((ICssStyleDeclaration?)null);
        return matchResult;
    }

    private MatchedRule CreateMockMatchedRule()
    {
        var rule = Substitute.For<ICssStyleRule>();
        return new MatchedRule(rule, new AngleSharp.Css.Priority(0, 0, 1, 0), StylesheetOrigin.Author, 0);
    }

    private IStyleRecalcContext CreateMockStyleRecalcContext(IDocument document, IElement? element = null)
    {
        var context = Substitute.For<IStyleRecalcContext>();
        context.Document.Returns(document);
        context.CurrentElement.Returns(element);
        return context;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}