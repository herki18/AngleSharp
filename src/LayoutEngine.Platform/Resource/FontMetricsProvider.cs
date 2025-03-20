using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Platform.Resource;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.Threading;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Platform.Resource;

using Contracts.Platform.Events;
using Infrastructure.CacheManager.API.Caching;

/// <summary>
/// Provides font metrics information for text layout calculations.
/// </summary>
public sealed class FontMetricsProvider : IFontMetricsProvider, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IResourceLoader _resourceLoader;
    private readonly ICache<string, IFontMetrics> _fontMetricsCache;
    private readonly ICache<string, ITextMetrics> _textMetricsCache;
    private readonly ICache<string, IGlyphMetrics> _glyphMetricsCache;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly ConcurrentDictionary<string, Task<bool>> _fontAvailabilityTasks = new();
    private readonly List<string> _systemFonts = new();

    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontMetricsProvider"/> class.
    /// </summary>
    public FontMetricsProvider(
        IEventAggregator eventAggregator,
        ICacheManager cacheManager,
        IThreadingCoordinator threadingCoordinator,
        IResourceLoader resourceLoader)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));

        // Create caches
        _fontMetricsCache = _cacheManager.GetOrCreateCache<ICache<string, IFontMetrics>>(
            "FontMetricsCache",
            new CacheOptions { Priority = CachePriority.Normal });

        _textMetricsCache = _cacheManager.GetOrCreateCache<ICache<string, ITextMetrics>>(
            "TextMetricsCache",
            new CacheOptions { Priority = CachePriority.Low });

        _glyphMetricsCache = _cacheManager.GetOrCreateCache<ICache<string, IGlyphMetrics>>(
            "GlyphMetricsCache",
            new CacheOptions { Priority = CachePriority.Normal });

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceLoadedEvent>(OnResourceLoaded));

        // Initialize system fonts asynchronously
        InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<IFontMetrics> GetFontMetricsAsync(string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal")
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(fontFamily))
            throw new ArgumentException("Font family cannot be null or empty", nameof(fontFamily));

        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than zero");

        // Create cache key
        var cacheKey = CreateFontMetricsCacheKey(fontFamily, fontSize, fontWeight, fontStyle);

        // Try to get from cache
        if (_fontMetricsCache.TryGetValue(cacheKey, out var metrics))
        {
            return metrics;
        }

        // Check if font is available
        if (!await IsFontAvailableAsync(fontFamily).ConfigureAwait(false))
        {
            // Use a fallback font
            var fallbackFonts = await GetFallbackFontsAsync(fontFamily).ConfigureAwait(false);
            if (fallbackFonts.Count > 0)
            {
                return await GetFontMetricsAsync(fallbackFonts[0], fontSize, fontWeight, fontStyle).ConfigureAwait(false);
            }
        }

        // Get or compute the metrics
        metrics = await CreateFontMetricsAsync(fontFamily, fontSize, fontWeight, fontStyle).ConfigureAwait(false);

        // Cache the result
        _fontMetricsCache.Set(cacheKey, metrics);

        return metrics;
    }

    /// <inheritdoc />
    public async Task<ITextMetrics> MeasureTextAsync(string text, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal")
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        if (string.IsNullOrEmpty(fontFamily))
            throw new ArgumentException("Font family cannot be null or empty", nameof(fontFamily));

        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than zero");

        // Create cache key
        var cacheKey = CreateTextMetricsCacheKey(text, fontFamily, fontSize, fontWeight, fontStyle);

        // Try to get from cache
        if (_textMetricsCache.TryGetValue(cacheKey, out var metrics))
        {
            return metrics;
        }

        // Get font metrics
        var fontMetrics = await GetFontMetricsAsync(fontFamily, fontSize, fontWeight, fontStyle).ConfigureAwait(false);

        // Measure the text
        metrics = await MeasureTextWithMetricsAsync(text, fontMetrics).ConfigureAwait(false);

        // Cache the result
        _textMetricsCache.Set(cacheKey, metrics);

        return metrics;
    }

    /// <inheritdoc />
    public async Task<bool> IsFontAvailableAsync(string fontFamily)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(fontFamily))
            throw new ArgumentException("Font family cannot be null or empty", nameof(fontFamily));

        // Check system fonts first
        if (_systemFonts.Contains(fontFamily, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if we already have a task for this font
        if (_fontAvailabilityTasks.TryGetValue(fontFamily, out var existingTask))
        {
            return await existingTask.ConfigureAwait(false);
        }

        // Create new task to check font availability
        var tcs = new TaskCompletionSource<bool>();
        var newTask = tcs.Task;

        if (_fontAvailabilityTasks.TryAdd(fontFamily, newTask))
        {
            try
            {
                // Perform actual check asynchronously
                var isAvailable = await CheckFontAvailabilityAsync(fontFamily).ConfigureAwait(false);
                tcs.SetResult(isAvailable);
                return isAvailable;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            // Another task was added in the meantime
            if (_fontAvailabilityTasks.TryGetValue(fontFamily, out var concurrentTask))
            {
                return await concurrentTask.ConfigureAwait(false);
            }

            // Fallback to direct check
            return await CheckFontAvailabilityAsync(fontFamily).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetFallbackFontsAsync(string fontFamily)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(fontFamily))
            throw new ArgumentException("Font family cannot be null or empty", nameof(fontFamily));

        // For simplicity, return a fixed list of fallback fonts
        // In a real implementation, this would depend on the font family and platform
        return new List<string>
        {
            "Arial",
            "Helvetica",
            "Times New Roman",
            "Courier New",
            "Georgia"
        };
    }

    /// <inheritdoc />
    public async Task<IGlyphMetrics> GetGlyphMetricsAsync(char character, string fontFamily, float fontSize, int fontWeight = 400, string fontStyle = "normal")
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(fontFamily))
            throw new ArgumentException("Font family cannot be null or empty", nameof(fontFamily));

        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than zero");

        // Create cache key
        var cacheKey = CreateGlyphMetricsCacheKey(character, fontFamily, fontSize, fontWeight, fontStyle);

        // Try to get from cache
        if (_glyphMetricsCache.TryGetValue(cacheKey, out var metrics))
        {
            return metrics;
        }

        // Get font metrics
        var fontMetrics = await GetFontMetricsAsync(fontFamily, fontSize, fontWeight, fontStyle).ConfigureAwait(false);

        // Get glyph metrics
        metrics = await GetGlyphMetricsWithFontAsync(character, fontMetrics).ConfigureAwait(false);

        // Cache the result
        _glyphMetricsCache.Set(cacheKey, metrics);

        return metrics;
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized || _isDisposed)
            return;

        try
        {
            // Initialize system fonts list
            // In a real implementation, this would query the system for available fonts
            _systemFonts.AddRange(new[]
            {
                "Arial",
                "Arial Black",
                "Arial Narrow",
                "Calibri",
                "Cambria",
                "Comic Sans MS",
                "Courier New",
                "Georgia",
                "Impact",
                "Segoe UI",
                "Tahoma",
                "Times New Roman",
                "Trebuchet MS",
                "Verdana"
            });

            _isInitialized = true;
        }
        catch (Exception)
        {
            // Initialization failed, but we can still operate with limited capabilities
            _isInitialized = true;
        }
    }

    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        if (e.Severity >= MemoryPressureSeverity.Medium)
        {
            // Clear text metrics cache on medium or higher pressure
            _textMetricsCache.Clear();

            if (e.Severity >= MemoryPressureSeverity.High)
            {
                // Clear glyph metrics cache on high or critical pressure
                _glyphMetricsCache.Clear();

                if (e.Severity >= MemoryPressureSeverity.Critical)
                {
                    // Clear font metrics cache only on critical pressure
                    _fontMetricsCache.Clear();
                }
            }
        }
    }

    private void OnResourceLoaded(ResourceLoadedEvent e)
    {
        // Check if the loaded resource is a font
        if (e.Resource.ResourceType == ResourceType.Font)
        {
            // Clear font availability task to force re-check
            string? fontFamily = null;

            // Extract font family from metadata
            if (e.Resource.Metadata.TryGetValue("font-family", out var family))
            {
                fontFamily = family;
                _fontAvailabilityTasks.TryRemove(fontFamily, out _);
            }
        }
    }

    private async Task<bool> CheckFontAvailabilityAsync(string fontFamily)
    {
        // In a real implementation, this would check if the font is available in the system
        // or try to load it from a font provider

        // For simplicity, check if it's in our predefined list or a web font that's been loaded
        var isSystemFont = _systemFonts.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);

        if (isSystemFont)
        {
            return true;
        }

        // Try to load the font if it's not a system font
        try
        {
            // This is a placeholder for actual font loading logic
            // In a real implementation, this would attempt to load the font
            await Task.Delay(10).ConfigureAwait(false);
            return false;
        }
        catch
        {
            return false;
        }
    }

    private Task<IFontMetrics> CreateFontMetricsAsync(string fontFamily, float fontSize, int fontWeight, string fontStyle)
    {
        // In a real implementation, this would measure actual font metrics
        // This is a placeholder implementation that returns reasonable defaults

        var metrics = new FontMetrics(
            fontFamily,
            fontSize,
            fontWeight,
            fontStyle,
            ascent: fontSize * 0.8f,
            descent: fontSize * 0.2f,
            lineGap: fontSize * 0.1f,
            emSquare: fontSize,
            capHeight: fontSize * 0.7f,
            xHeight: fontSize * 0.5f,
            isMonospace: fontFamily.Contains("Mono") || fontFamily.Contains("Courier"),
            averageCharWidth: fontSize * 0.6f,
            maxCharWidth: fontSize * 1.2f);

        return Task.FromResult<IFontMetrics>(metrics);
    }

    private Task<ITextMetrics> MeasureTextWithMetricsAsync(string text, IFontMetrics fontMetrics)
    {
        // In a real implementation, this would measure actual text metrics
        // This is a placeholder implementation that returns reasonable defaults

        var characterPositions = new List<CharacterPosition>();
        float currentX = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var charWidth = fontMetrics.IsMonospace
                ? fontMetrics.AverageCharWidth
                : GetApproximateCharacterWidth(text[i], fontMetrics);

            characterPositions.Add(new CharacterPosition(text[i], currentX, charWidth));
            currentX += charWidth;
        }

        var width = currentX;
        var height = fontMetrics.Ascent + fontMetrics.Descent;
        var baseline = fontMetrics.Ascent;
        var boundingBox = new Rectangle(0, -fontMetrics.Ascent, width, height);

        var metrics = new TextMetrics(
            text,
            width,
            height,
            baseline,
            boundingBox,
            characterPositions);

        return Task.FromResult<ITextMetrics>(metrics);
    }

    private Task<IGlyphMetrics> GetGlyphMetricsWithFontAsync(char character, IFontMetrics fontMetrics)
    {
        // In a real implementation, this would get actual glyph metrics
        // This is a placeholder implementation that returns reasonable defaults

        var width = GetApproximateCharacterWidth(character, fontMetrics);
        var height = fontMetrics.Ascent + fontMetrics.Descent;
        var bearingY = fontMetrics.Ascent;
        var bearingX = 0f;
        var advance = width;
        var boundingBox = new Rectangle(0, -fontMetrics.Ascent, width, height);

        var metrics = new GlyphMetrics(
            character,
            width,
            height,
            bearingX,
            bearingY,
            advance,
            boundingBox);

        return Task.FromResult<IGlyphMetrics>(metrics);
    }

    private float GetApproximateCharacterWidth(char c, IFontMetrics fontMetrics)
    {
        // Simple approximation of character width
        if (fontMetrics.IsMonospace)
        {
            return fontMetrics.AverageCharWidth;
        }

        if (char.IsWhiteSpace(c))
        {
            return fontMetrics.AverageCharWidth * 0.5f;
        }

        if (char.IsUpper(c) || c == 'W' || c == 'M')
        {
            return fontMetrics.AverageCharWidth * 1.2f;
        }

        if (c == 'i' || c == 'l' || c == 'I' || c == '.' || c == ',')
        {
            return fontMetrics.AverageCharWidth * 0.5f;
        }

        return fontMetrics.AverageCharWidth;
    }

    private string CreateFontMetricsCacheKey(string fontFamily, float fontSize, int fontWeight, string fontStyle)
    {
        return $"font:{fontFamily}:{fontSize}:{fontWeight}:{fontStyle}";
    }

    private string CreateTextMetricsCacheKey(string text, string fontFamily, float fontSize, int fontWeight, string fontStyle)
    {
        // Use a hash of the text to keep the key length reasonable
        var textHash = Math.Abs(text.GetHashCode());
        return $"text:{textHash}:{fontFamily}:{fontSize}:{fontWeight}:{fontStyle}";
    }

    private string CreateGlyphMetricsCacheKey(char character, string fontFamily, float fontSize, int fontWeight, string fontStyle)
    {
        return $"glyph:{(int)character}:{fontFamily}:{fontSize}:{fontWeight}:{fontStyle}";
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(FontMetricsProvider));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();
        _fontAvailabilityTasks.Clear();
    }

    #region Implementation Classes

    private sealed class FontMetrics : IFontMetrics
    {
        public string FontFamily { get; }
        public float FontSize { get; }
        public int FontWeight { get; }
        public string FontStyle { get; }
        public float Ascent { get; }
        public float Descent { get; }
        public float LineGap { get; }
        public float EmSquare { get; }
        public float CapHeight { get; }
        public float XHeight { get; }
        public bool IsMonospace { get; }
        public float AverageCharWidth { get; }
        public float MaxCharWidth { get; }

        public FontMetrics(
            string fontFamily,
            float fontSize,
            int fontWeight,
            string fontStyle,
            float ascent,
            float descent,
            float lineGap,
            float emSquare,
            float capHeight,
            float xHeight,
            bool isMonospace,
            float averageCharWidth,
            float maxCharWidth)
        {
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
            EmSquare = emSquare;
            CapHeight = capHeight;
            XHeight = xHeight;
            IsMonospace = isMonospace;
            AverageCharWidth = averageCharWidth;
            MaxCharWidth = maxCharWidth;
        }
    }

    private sealed class TextMetrics : ITextMetrics
    {
        public string Text { get; }
        public float Width { get; }
        public float Height { get; }
        public float Baseline { get; }
        public Rectangle BoundingBox { get; }
        public IReadOnlyList<CharacterPosition> CharacterPositions { get; }

        public TextMetrics(
            string text,
            float width,
            float height,
            float baseline,
            Rectangle boundingBox,
            IReadOnlyList<CharacterPosition> characterPositions)
        {
            Text = text;
            Width = width;
            Height = height;
            Baseline = baseline;
            BoundingBox = boundingBox;
            CharacterPositions = characterPositions;
        }
    }

    private sealed class GlyphMetrics : IGlyphMetrics
    {
        public char Character { get; }
        public float Width { get; }
        public float Height { get; }
        public float BearingX { get; }
        public float BearingY { get; }
        public float Advance { get; }
        public Rectangle BoundingBox { get; }

        public GlyphMetrics(
            char character,
            float width,
            float height,
            float bearingX,
            float bearingY,
            float advance,
            Rectangle boundingBox)
        {
            Character = character;
            Width = width;
            Height = height;
            BearingX = bearingX;
            BearingY = bearingY;
            Advance = advance;
            BoundingBox = boundingBox;
        }
    }

    #endregion
}