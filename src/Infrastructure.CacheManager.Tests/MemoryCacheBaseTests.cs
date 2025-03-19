using AutoFixture;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Models;
using Infrastructure.CacheManager.Internal.Caches;

namespace Infrastructure.CacheManager.Tests;

public class MemoryCacheBaseTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly MemoryCacheBase<string, string> _sut;
    private readonly string _cacheName;
    private readonly CacheEntryOptions _defaultOptions;
    private bool _callbackInvoked;
    private object? _callbackObj;

    public MemoryCacheBaseTests()
    {
        _fixture = new Fixture();
        _cacheName = _fixture.Create<string>();
        _sut = new MemoryCacheBase<string, string>(_cacheName, CachePriority.Normal, TimeSpan.FromMilliseconds(500));
        _defaultOptions = new CacheEntryOptions();
        _callbackInvoked = false;
    }

    private void OnEntryEvicted(object obj)
    {
        _callbackInvoked = true;
        _callbackObj = obj;
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MemoryCacheBase<string, string>(null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSetNameAndPriority()
    {
        // Arrange & Act in constructor

        // Assert
        Assert.Equal(_cacheName, _sut.Name);
        Assert.Equal(CachePriority.Normal, _sut.Priority);
    }

    [Fact]
    public void GetOrCreate_WithExistingKey_ShouldReturnCachedValue()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        _sut.Set(key, value);

        // Act
        int factoryCallCount = 0;
        var result = _sut.GetOrCreate(key, k =>
        {
            factoryCallCount++;
            return "New Value";
        });

        // Assert
        Assert.Equal(value, result);
        Assert.Equal(0, factoryCallCount);
    }

    [Fact]
    public void GetOrCreate_WithNewKey_ShouldUseFactoryAndCacheResult()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var expectedValue = _fixture.Create<string>();

        // Act
        int factoryCallCount = 0;
        var result = _sut.GetOrCreate(key, k =>
        {
            factoryCallCount++;
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(1, factoryCallCount);
        Assert.True(_sut.TryGetValue(key, out var cachedValue));
        Assert.Equal(expectedValue, cachedValue);
    }

    [Fact]
    public void GetOrCreate_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.GetOrCreate(null!, k => "value"));
    }

    [Fact]
    public void GetOrCreate_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = _fixture.Create<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.GetOrCreate(key, null!));
    }

    [Fact]
    public void Set_ThenTryGetValue_ShouldReturnCachedValue()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        // Act
        _sut.Set(key, value);
        var success = _sut.TryGetValue(key, out var retrievedValue);

        // Assert
        Assert.True(success);
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public void Set_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var value = _fixture.Create<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.Set(null!, value));
    }

    [Fact]
    public void Set_WithNewKey_ShouldIncrementCountAndUpdateSize()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        int initialCount = _sut.Count;
        long initialSize = _sut.EstimatedSize;

        // Act
        _sut.Set(key, value);

        // Assert
        Assert.Equal(initialCount + 1, _sut.Count);
        Assert.True(_sut.EstimatedSize > initialSize);
    }

    [Fact]
    public void Set_WithExpirationOptions_ShouldExpireEntry()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        };

        // Act
        _sut.Set(key, value, options);
        bool beforeExpiry = _sut.TryGetValue(key, out _);

        // Wait for expiration
        Thread.Sleep(200);
        bool afterExpiry = _sut.TryGetValue(key, out _);

        // Assert
        Assert.True(beforeExpiry);
        Assert.False(afterExpiry);
    }

    [Fact]
    public void Set_WithPostEvictionCallback_ShouldInvokeCallbackOnRemoval()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var options = new CacheEntryOptions
        {
            PostEvictionCallback = OnEntryEvicted
        };

        // Act
        _sut.Set(key, value, options);
        _sut.Remove(key);

        // Assert
        Assert.True(_callbackInvoked);
        Assert.Equal(key, _callbackObj);
    }

    [Fact]
    public void Remove_WithExistingKey_ShouldRemoveEntryAndDecrementCount()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        _sut.Set(key, value);
        int initialCount = _sut.Count;

        // Act
        bool result = _sut.Remove(key);
        bool exists = _sut.TryGetValue(key, out _);

        // Assert
        Assert.True(result);
        Assert.False(exists);
        Assert.Equal(initialCount - 1, _sut.Count);
    }

    [Fact]
    public void Remove_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var key = _fixture.Create<string>();

        // Act
        bool result = _sut.Remove(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_WithNullKey_ShouldReturnFalse()
    {
        // Act
        bool result = _sut.Remove(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        _sut.Set(key, value);

        // Act
        bool result = _sut.Contains(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var key = _fixture.Create<string>();

        // Act
        bool result = _sut.Contains(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullKey_ShouldReturnFalse()
    {
        // Act
        bool result = _sut.Contains(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Trim_WithZeroPercentage_ShouldNotRemoveEntries()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _sut.Set($"key{i}", $"value{i}");
        }
        int initialCount = _sut.Count;

        // Act
        int removed = _sut.Trim(0);

        // Assert
        Assert.Equal(0, removed);
        Assert.Equal(initialCount, _sut.Count);
    }

    [Fact]
    public void Trim_With100Percentage_ShouldRemoveAllEntries()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _sut.Set($"key{i}", $"value{i}");
        }
        int initialCount = _sut.Count;
        Assert.True(initialCount > 0);

        // Act
        int removed = _sut.Trim(100);

        // Assert
        Assert.Equal(initialCount, removed);
        Assert.Equal(0, _sut.Count);
    }

    [Fact]
    public void Trim_With50Percentage_ShouldRemoveHalfOfEntries()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _sut.Set($"key{i}", $"value{i}");
        }
        int initialCount = _sut.Count;
        Assert.Equal(10, initialCount);

        // Act
        int removed = _sut.Trim(50);

        // Assert
        Assert.Equal(5, removed);
        Assert.Equal(5, _sut.Count);
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntriesAndResetCountAndSize()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _sut.Set($"key{i}", $"value{i}");
        }
        Assert.True(_sut.Count > 0);
        Assert.True(_sut.EstimatedSize > 0);

        // Act
        _sut.Clear();

        // Assert
        Assert.Equal(0, _sut.Count);
        Assert.Equal(0, _sut.EstimatedSize);
        for (int i = 0; i < 5; i++)
        {
            Assert.False(_sut.Contains($"key{i}"));
        }
    }

    [Fact]
    public void CleanupTimer_ShouldRemoveExpiredEntries()
    {
        // Arrange
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        };
        _sut.Set(key, value, options);

        // Act - Wait for cleanup timer
        Thread.Sleep(700); // Cleanup interval was set to 500ms in constructor

        // Assert
        Assert.False(_sut.Contains(key));
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}