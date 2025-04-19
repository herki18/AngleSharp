using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LayoutEngine.Core.Tests;

using MartinCostello.Logging.XUnit;
using Xunit.Abstractions;

public class EngineFacadeIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    private ITestOutputHelper _output;
    private readonly IEngine _engine;

    public EngineFacadeIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var serviceCollection = new ServiceCollection();



        serviceCollection.AddLayoutEngine();

        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddXUnit(_output);
        });

        // Add other necessary AddXyzSystem() calls here if not included in AddLayoutEngine

        _serviceProvider = serviceCollection.BuildServiceProvider();
        _engine = _serviceProvider.GetRequiredService<IEngine>();
    }

    [Fact]
    public async Task OpenAsync_Facade_LoadsHtmlAndUpdatesDocument()
    {
        // Arrange
        var html = "<html><body><h1>Hello Integration Test!</h1></body></html>";

        // Act
        var loadedDocument = await _engine.OpenAsync(html);
        var documentFromProperty = _engine.Document;

        // Assert
        Assert.NotNull(loadedDocument);
        Assert.Same(loadedDocument, documentFromProperty);
        Assert.Equal("Hello Integration Test!", documentFromProperty?.QuerySelector("h1")?.TextContent);
    }

    [Fact]
    public void DocumentProperty_Facade_IsNullBeforeOpen()
    {
        Assert.Null(_engine.Document);
    }

    [Fact]
    public void BrowsingContext_Facade_IsAvailableAfterConstruction()
    {
        Assert.NotNull(_engine.BrowsingContext);
    }
}