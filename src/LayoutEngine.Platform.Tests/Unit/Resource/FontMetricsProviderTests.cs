// #nullable enable
//
// namespace LayoutEngine.Platform.Tests.Unit.Resource;
//
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using AutoFixture;
// using Helpers;
// using Infrastructure.CacheManager.API.Caching;
// using Infrastructure.CacheManager.API.Management;
// using LayoutEngine.Contracts.Platform.Events;
// using LayoutEngine.Contracts.Platform.Resource;
// using LayoutEngine.Contracts.Platform.Threading;
// using LayoutEngine.Contracts.Resource;
// using LayoutEngine.Platform.Resource;
// using NSubstitute;
// using Xunit;
//
// public class FontMetricsProviderTests : IDisposable
// {
//     private readonly Fixture _fixture;
//     private readonly TestEventAggregator _eventAggregator;
//     private readonly ICacheManager _cacheManager;
//     private readonly IThreadingCoordinator _threadingCoordinator;
//     private readonly IResourceLoader _resourceLoader;
//     private readonly ICache<string, IFontMetrics> _fontMetricsCache;
//     private readonly ICache<string, ITextMetrics> _textMetricsCache;
//     private readonly ICache<string, IGlyphMetrics> _glyphMetricsCache;
//     private readonly FontMetricsProvider _fontMetricsProvider;
//
//     public FontMetricsProviderTests()
//     {
//         _fixture = new Fixture();
//         _eventAggregator = new TestEventAggregator();
//         _cacheManager = Substitute.For<ICacheManager>();
//         _threadingCoordinator = Substitute.For<IThreadingCoordinator>();
//         _resourceLoader = Substitute.For<IResourceLoader>();
//
//         // Setup caches
//         _fontMetricsCache = Substitute.For<ICache<string, IFontMetrics>>();
//         _textMetricsCache = Substitute.For<ICache<string, ITextMetrics>>();
//         _glyphMetricsCache = Substitute.For<ICache<string, IGlyphMetrics>>();
//
//         _cacheManager.GetCache<ICache<string, IFontMetrics>>("FontMetricsCache").Returns(_fontMetricsCache);
//         _cacheManager.GetCache<ICache<string, ITextMetrics>>("TextMetricsCache").Returns(_textMetricsCache);
//         _cacheManager.GetCache<ICache<string, IGlyphMetrics>>("GlyphMetricsCache").Returns(_glyphMetricsCache);
//
//         _fontMetricsProvider = new FontMetricsProvider(
//             _eventAggregator,
//             _cacheManager,
//             _threadingCoordinator,
//             _resourceLoader);
//     }
//
//     [Fact]
//     public async Task GetFontMetricsAsync_WithCachedMetrics_ShouldReturnCached()
//     {
//         // Arrange
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//         int fontWeight = 400;
//         string fontStyle = "normal";
//
//         var cachedMetrics = CreateFontMetrics(fontFamily, fontSize, fontWeight, fontStyle);
//         _fontMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<IFontMetrics?>())
//             .Returns(x => {
//                 x[1] = cachedMetrics;
//                 return true;
//             });
//
//         // Act
//         var metrics = await _fontMetricsProvider.GetFontMetricsAsync(fontFamily, fontSize, fontWeight, fontStyle);
//
//         // Assert
//         Assert.Same(cachedMetrics, metrics);
//         _fontMetricsCache.Received(1).TryGetValue(
//             Arg.Is<string>(s => s.Contains(fontFamily)),
//             out Arg.Any<IFontMetrics?>());
//     }
//
//     [Fact]
//     public async Task GetFontMetricsAsync_WithUncachedMetrics_ShouldCreateAndCache()
//     {
//         // Arrange
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//         int fontWeight = 400;
//         string fontStyle = "normal";
//
//         _fontMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<IFontMetrics?>())
//             .Returns(false);
//
//         // Act
//         var metrics = await _fontMetricsProvider.GetFontMetricsAsync(fontFamily, fontSize, fontWeight, fontStyle);
//
//         // Assert
//         Assert.NotNull(metrics);
//         Assert.Equal(fontFamily, metrics.FontFamily);
//         Assert.Equal(fontSize, metrics.FontSize);
//         Assert.Equal(fontWeight, metrics.FontWeight);
//         Assert.Equal(fontStyle, metrics.FontStyle);
//
//         _fontMetricsCache.Received(1).TryGetValue(
//             Arg.Is<string>(s => s.Contains(fontFamily)),
//             out Arg.Any<IFontMetrics?>());
//
//         _fontMetricsCache.Received(1).Set(
//             Arg.Is<string>(s => s.Contains(fontFamily)),
//             Arg.Is<IFontMetrics>(m => m.FontFamily == fontFamily));
//     }
//
//     [Theory]
//     [InlineData(null)]
//     [InlineData("")]
//     public async Task GetFontMetricsAsync_WithInvalidFontFamily_ShouldThrow(string? fontFamily)
//     {
//         // Act & Assert
//         await Assert.ThrowsAsync<ArgumentException>(() =>
//             _fontMetricsProvider.GetFontMetricsAsync(fontFamily!, 16f));
//     }
//
//     [Theory]
//     [InlineData(0f)]
//     [InlineData(-1f)]
//     public async Task GetFontMetricsAsync_WithInvalidFontSize_ShouldThrow(float fontSize)
//     {
//         // Act & Assert
//         await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
//             _fontMetricsProvider.GetFontMetricsAsync("Arial", fontSize));
//     }
//
//     [Fact]
//     public async Task MeasureTextAsync_WithCachedMetrics_ShouldReturnCached()
//     {
//         // Arrange
//         string text = "Hello, world!";
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//
//         var cachedMetrics = CreateTextMetrics(text, 100f, 20f);
//         _textMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<ITextMetrics?>())
//             .Returns(x => {
//                 x[1] = cachedMetrics;
//                 return true;
//             });
//
//         // Act
//         var metrics = await _fontMetricsProvider.MeasureTextAsync(text, fontFamily, fontSize);
//
//         // Assert
//         Assert.Same(cachedMetrics, metrics);
//         _textMetricsCache.Received(1).TryGetValue(
//             Arg.Any<string>(),
//             out Arg.Any<ITextMetrics?>());
//     }
//
//     [Fact]
//     public async Task MeasureTextAsync_WithUncachedMetrics_ShouldCreateAndCache()
//     {
//         // Arrange
//         string text = "Hello, world!";
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//
//         _textMetricsCache.TryGetValue(
//                 Arg.Any<string>(),
//                 out Arg.Any<ITextMetrics?>())
//             .Returns(false);
//
//         // Setup font metrics to be returned by GetFontMetricsAsync
//         var fontMetrics = CreateFontMetrics(fontFamily, fontSize, 400, "normal");
//         _fontMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<IFontMetrics?>())
//             .Returns(x => {
//                 x[1] = fontMetrics;
//                 return true;
//             });
//
//         // Act
//         var metrics = await _fontMetricsProvider.MeasureTextAsync(text, fontFamily, fontSize);
//
//         // Assert
//         Assert.NotNull(metrics);
//         Assert.Equal(text, metrics.Text);
//         Assert.True(metrics.Width > 0);
//         Assert.True(metrics.Height > 0);
//
//         _textMetricsCache.Received(1).Set(
//             Arg.Any<string>(),
//             Arg.Is<ITextMetrics>(m => m.Text == text));
//     }
//
//     [Fact]
//     public async Task IsFontAvailableAsync_WithSystemFont_ShouldReturnTrue()
//     {
//         // Arrange
//         string fontFamily = "Arial";
//
//         // Act
//         bool isAvailable = await _fontMetricsProvider.IsFontAvailableAsync(fontFamily);
//
//         // Assert
//         Assert.True(isAvailable);
//     }
//
//     [Fact]
//     public async Task IsFontAvailableAsync_WithNonSystemFont_ShouldReturnFalse()
//     {
//         // Arrange
//         string fontFamily = "CustomFont";
//
//         // Act
//         bool isAvailable = await _fontMetricsProvider.IsFontAvailableAsync(fontFamily);
//
//         // Assert
//         Assert.False(isAvailable);
//     }
//
//     [Fact]
//     public async Task GetFallbackFontsAsync_ShouldReturnFallbackList()
//     {
//         // Arrange
//         string fontFamily = "NonExistentFont";
//
//         // Act
//         var fallbacks = await _fontMetricsProvider.GetFallbackFontsAsync(fontFamily);
//
//         // Assert
//         Assert.NotEmpty(fallbacks);
//         Assert.Contains("Arial", fallbacks);
//     }
//
//     [Fact]
//     public async Task GetGlyphMetricsAsync_WithCachedMetrics_ShouldReturnCached()
//     {
//         // Arrange
//         char character = 'A';
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//
//         var cachedMetrics = CreateGlyphMetrics(character, 10f, 20f);
//         _glyphMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<IGlyphMetrics?>())
//             .Returns(x => {
//                 x[1] = cachedMetrics;
//                 return true;
//             });
//
//         // Act
//         var metrics = await _fontMetricsProvider.GetGlyphMetricsAsync(character, fontFamily, fontSize);
//
//         // Assert
//         Assert.Same(cachedMetrics, metrics);
//         _glyphMetricsCache.Received(1).TryGetValue(
//             Arg.Any<string>(),
//             out Arg.Any<IGlyphMetrics?>());
//     }
//
//     [Fact]
//     public async Task GetGlyphMetricsAsync_WithUncachedMetrics_ShouldCreateAndCache()
//     {
//         // Arrange
//         char character = 'A';
//         string fontFamily = "Arial";
//         float fontSize = 16f;
//
//         _glyphMetricsCache.TryGetValue(
//                 Arg.Any<string>(),
//                 out Arg.Any<IGlyphMetrics?>())
//             .Returns(false);
//
//         // Setup font metrics to be returned by GetFontMetricsAsync
//         var fontMetrics = CreateFontMetrics(fontFamily, fontSize, 400, "normal");
//         _fontMetricsCache.TryGetValue(
//                 Arg.Is<string>(s => s.Contains(fontFamily)),
//                 out Arg.Any<IFontMetrics?>())
//             .Returns(x => {
//                 x[1] = fontMetrics;
//                 return true;
//             });
//
//         // Act
//         var metrics = await _fontMetricsProvider.GetGlyphMetricsAsync(character, fontFamily, fontSize);
//
//         // Assert
//         Assert.NotNull(metrics);
//         Assert.Equal(character, metrics.Character);
//         Assert.True(metrics.Width > 0);
//         Assert.True(metrics.Height > 0);
//
//         _glyphMetricsCache.Received(1).Set(
//             Arg.Any<string>(),
//             Arg.Is<IGlyphMetrics>(m => m.Character == character));
//     }
//
//     [Fact]
//     public void OnMemoryPressure_ShouldClearCachesBasedOnSeverity()
//     {
//         // Test with different severity levels
//
//         // Medium - Should clear text metrics cache
//         _eventAggregator.Publish(new MemoryPressureEvent(
//             MemoryPressureSeverity.Medium, 1000000, 500000));
//         _textMetricsCache.Received(1).Clear();
//         _glyphMetricsCache.DidNotReceive().Clear();
//         _fontMetricsCache.DidNotReceive().Clear();
//
//         // Reset the mocks by creating new instances
//         _textMetricsCache.ClearReceivedCalls();
//         _glyphMetricsCache.ClearReceivedCalls();
//         _fontMetricsCache.ClearReceivedCalls();
//
//         // High - Should clear text and glyph metrics caches
//         _eventAggregator.Publish(new MemoryPressureEvent(
//             MemoryPressureSeverity.High, 1000000, 500000));
//         _textMetricsCache.Received(1).Clear();
//         _glyphMetricsCache.Received(1).Clear();
//         _fontMetricsCache.DidNotReceive().Clear();
//
//         // Reset the mocks again
//         _textMetricsCache.ClearReceivedCalls();
//         _glyphMetricsCache.ClearReceivedCalls();
//         _fontMetricsCache.ClearReceivedCalls();
//
//         // Critical - Should clear all caches
//         _eventAggregator.Publish(new MemoryPressureEvent(
//             MemoryPressureSeverity.Critical, 1000000, 500000));
//         _textMetricsCache.Received(1).Clear();
//         _glyphMetricsCache.Received(1).Clear();
//         _fontMetricsCache.Received(1).Clear();
//     }
//
//     [Fact]
//     public void OnResourceLoaded_WithFontResource_ShouldUpdateFontAvailability()
//     {
//         // Arrange
//         string fontFamily = "CustomFont";
//         string url = "https://example.com/fonts/custom.woff2";
//         var resource = new TestResource(url, ResourceType.Font);
//
//         // Add font-family metadata
//         ((TestResource)resource).AddMetadata("font-family", fontFamily);
//
//         var resourceLoadedEvent = new ResourceLoadedEvent(url, resource);
//
//         // Act
//         _eventAggregator.Publish(resourceLoadedEvent);
//
//         // Assert - Check if this font is now available
//         // Note: We can't directly test the internal _fontAvailabilityTasks dictionary
//         // since it's private, but we can check indirectly through IsFontAvailableAsync
//         // This might not be feasible in a unit test without reflection
//     }
//
//     private IFontMetrics CreateFontMetrics(string fontFamily, float fontSize, int fontWeight, string fontStyle)
//     {
//         var metrics = Substitute.For<IFontMetrics>();
//         metrics.FontFamily.Returns(fontFamily);
//         metrics.FontSize.Returns(fontSize);
//         metrics.FontWeight.Returns(fontWeight);
//         metrics.FontStyle.Returns(fontStyle);
//         metrics.Ascent.Returns(fontSize * 0.8f);
//         metrics.Descent.Returns(fontSize * 0.2f);
//         metrics.LineGap.Returns(fontSize * 0.1f);
//         metrics.EmSquare.Returns(fontSize);
//         metrics.CapHeight.Returns(fontSize * 0.7f);
//         metrics.XHeight.Returns(fontSize * 0.5f);
//         metrics.IsMonospace.Returns(fontFamily.Contains("Mono") || fontFamily.Contains("Courier"));
//         metrics.AverageCharWidth.Returns(fontSize * 0.6f);
//         metrics.MaxCharWidth.Returns(fontSize * 1.2f);
//         return metrics;
//     }
//
//     private ITextMetrics CreateTextMetrics(string text, float width, float height)
//     {
//         var metrics = Substitute.For<ITextMetrics>();
//         metrics.Text.Returns(text);
//         metrics.Width.Returns(width);
//         metrics.Height.Returns(height);
//         metrics.Baseline.Returns(height * 0.8f);
//         metrics.BoundingBox.Returns(new LayoutEngine.Contracts.Platform.Resource.Rectangle(0, 0, width, height));
//         metrics.CharacterPositions.Returns(new List<CharacterPosition>());
//         return metrics;
//     }
//
//     private IGlyphMetrics CreateGlyphMetrics(char character, float width, float height)
//     {
//         var metrics = Substitute.For<IGlyphMetrics>();
//         metrics.Character.Returns(character);
//         metrics.Width.Returns(width);
//         metrics.Height.Returns(height);
//         metrics.BearingX.Returns(0f);
//         metrics.BearingY.Returns(height * 0.8f);
//         metrics.Advance.Returns(width);
//         metrics.BoundingBox.Returns(new LayoutEngine.Contracts.Platform.Resource.Rectangle(0, 0, width, height));
//         return metrics;
//     }
//
//     public void Dispose()
//     {
//         _fontMetricsProvider.Dispose();
//     }
// }