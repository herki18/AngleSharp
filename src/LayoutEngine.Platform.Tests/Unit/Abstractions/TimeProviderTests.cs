namespace LayoutEngine.Platform.Tests.Unit.Abstractions;

using System;
using System.Threading;
using LayoutEngine.Platform.Abstractions;
using Xunit;

public class TimeProviderTests
{
    [Fact]
    public void SystemTimeProvider_GetCurrentTimeMilliseconds_ShouldIncreaseOverTime()
    {
        // Arrange
        var provider = new SystemTimeProvider();

        // Act
        var time1 = provider.GetCurrentTimeMilliseconds();
        Thread.Sleep(5); // Small delay
        var time2 = provider.GetCurrentTimeMilliseconds();

        // Assert
        Assert.True(time2 > time1, "Time should increase");
    }

    [Fact]
    public void SystemTimeProvider_GetUtcNow_ShouldReturnCurrentTime()
    {
        // Arrange
        var provider = new SystemTimeProvider();
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = provider.GetUtcNow();
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.True(result >= beforeCall, "Result should be greater than or equal to time before call");
        Assert.True(result <= afterCall, "Result should be less than or equal to time after call");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(54321)]
    public void TestTimeProvider_GetCurrentTimeMilliseconds_ShouldReturnInitialValue(double initialTime)
    {
        // Arrange
        var provider = new TestTimeProvider(initialTime);

        // Act
        var result = provider.GetCurrentTimeMilliseconds();

        // Assert
        Assert.Equal(initialTime, result);
    }

    [Fact]
    public void TestTimeProvider_GetUtcNow_ShouldReturnInitialValue()
    {
        // Arrange
        var initialTime = new DateTime(2023, 5, 15, 10, 30, 0, DateTimeKind.Utc);
        var provider = new TestTimeProvider(0, initialTime);

        // Act
        var result = provider.GetUtcNow();

        // Assert
        Assert.Equal(initialTime, result);
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(1000, 500, 1500)]
    [InlineData(3000, -1000, 2000)]  // Testing negative advancement
    public void TestTimeProvider_AdvanceTime_ShouldAdjustCurrentTime(double initialTime, double advancement, double expected)
    {
        // Arrange
        var provider = new TestTimeProvider(initialTime);

        // Act
        provider.AdvanceTime(advancement);
        var result = provider.GetCurrentTimeMilliseconds();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestTimeProvider_AdvanceTime_ShouldAdjustUtcNow()
    {
        // Arrange
        var initialTime = new DateTime(2023, 5, 15, 10, 30, 0, DateTimeKind.Utc);
        var provider = new TestTimeProvider(0, initialTime);
        var advancement = 5000.0; // 5 seconds

        // Act
        provider.AdvanceTime(advancement);
        var result = provider.GetUtcNow();

        // Assert
        var expected = initialTime.AddMilliseconds(advancement);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestTimeProvider_SetCurrentTime_ShouldOverrideCurrentValues()
    {
        // Arrange
        var provider = new TestTimeProvider(1000);
        var newMilliseconds = 5000.0;
        var newDateTime = new DateTime(2023, 6, 15, 15, 45, 30, DateTimeKind.Utc);

        // Act
        provider.SetCurrentTime(newMilliseconds, newDateTime);
        var resultMs = provider.GetCurrentTimeMilliseconds();
        var resultDt = provider.GetUtcNow();

        // Assert
        Assert.Equal(newMilliseconds, resultMs);
        Assert.Equal(newDateTime, resultDt);
    }

    [Fact]
    public void TestTimeProvider_SetCurrentTime_WithNullDateTime_ShouldOnlyUpdateMilliseconds()
    {
        // Arrange
        var initialTime = new DateTime(2023, 5, 15, 10, 30, 0, DateTimeKind.Utc);
        var provider = new TestTimeProvider(1000, initialTime);
        var newMilliseconds = 5000.0;

        // Act
        provider.SetCurrentTime(newMilliseconds);
        var resultMs = provider.GetCurrentTimeMilliseconds();
        var resultDt = provider.GetUtcNow();

        // Assert
        Assert.Equal(newMilliseconds, resultMs);
        Assert.Equal(initialTime, resultDt); // DateTime should remain unchanged
    }

    [Fact]
    public void TestTimeProvider_MultipleCalls_ShouldBehavePredictably()
    {
        // Arrange
        var provider = new TestTimeProvider(0);

        // Act & Assert - Chain of operations
        provider.AdvanceTime(1000);
        Assert.Equal(1000, provider.GetCurrentTimeMilliseconds());

        provider.AdvanceTime(500);
        Assert.Equal(1500, provider.GetCurrentTimeMilliseconds());

        provider.SetCurrentTime(3000);
        Assert.Equal(3000, provider.GetCurrentTimeMilliseconds());

        provider.AdvanceTime(-1000);
        Assert.Equal(2000, provider.GetCurrentTimeMilliseconds());
    }
}