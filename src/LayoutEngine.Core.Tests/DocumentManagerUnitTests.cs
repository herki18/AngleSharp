using Xunit;
using Xunit.Abstractions;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using LayoutEngine.Core; // Assumes DocumentManager is here
using System;
using MartinCostello.Logging.XUnit;

namespace LayoutEngine.Core.Tests;

using Core; // Corrected Namespace for the test class

public class DocumentManagerUnitTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<DocumentManager> _logger;
    private readonly IConfiguration _testConfiguration;
    private readonly DocumentManager _documentManager;

    public DocumentManagerUnitTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new LoggerFactory()
            .AddXUnit(output)
            .CreateLogger<DocumentManager>();
        _testConfiguration = Configuration.Default.WithCss();
        _documentManager = new DocumentManager(_testConfiguration, _logger);
    }

    [Fact]
    public void Constructor_WithConfiguration_InitializesBrowsingContext()
    {
        var context = _documentManager.BrowsingContext;
        Assert.NotNull(context);
    }

    [Fact]
    public void Document_Property_IsNull_AfterConstruction()
    {
        var document = _documentManager.Document;
        Assert.Null(document);
    }

    [Fact]
    public async Task OpenAsync_WithValidHtml_LogsInformationAndUpdatesDocument()
    {
        var htmlInput = "<html><body><p>Testing Open</p></body></html>";

        var resultDocument = await _documentManager.OpenAsync(htmlInput);
        var documentFromProperty = _documentManager.Document;

        Assert.NotNull(resultDocument);
        Assert.NotNull(documentFromProperty);
        Assert.Same(resultDocument, documentFromProperty);
        Assert.Equal("Testing Open", resultDocument.QuerySelector("p")?.TextContent);
        Assert.Equal(htmlInput, resultDocument.Source.Text);
    }

    [Fact]
    public async Task OpenAsync_CalledMultipleTimes_UpdatesDocumentPropertyEachTime()
    {
        var htmlInput1 = "<html><body><p>First</p></body></html>";
        var htmlInput2 = "<html><body><span>Second</span></body></html>";

        var resultDocument1 = await _documentManager.OpenAsync(htmlInput1);
        var documentFromProperty1 = _documentManager.Document;

        Assert.NotNull(resultDocument1);
        Assert.Same(resultDocument1, documentFromProperty1);
        Assert.Equal("First", resultDocument1.QuerySelector("p")?.TextContent);

        var resultDocument2 = await _documentManager.OpenAsync(htmlInput2);
        var documentFromProperty2 = _documentManager.Document;

        Assert.NotNull(resultDocument2);
        Assert.Same(resultDocument2, documentFromProperty2);
        Assert.NotSame(resultDocument1, resultDocument2);
        Assert.Equal("Second", resultDocument2.QuerySelector("span")?.TextContent);
        Assert.Null(resultDocument2.QuerySelector("p"));
    }
}