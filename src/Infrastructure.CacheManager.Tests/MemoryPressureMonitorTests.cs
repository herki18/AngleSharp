using System.Reflection;
using Infrastructure.CacheManager.API.Monitoring;
using Infrastructure.CacheManager.Internal.Monitoring;

namespace Infrastructure.CacheManager.Tests;

public class MemoryPressureMonitorTests : IDisposable
{
    private readonly MemoryPressureMonitor _sut;
    private bool _eventRaised;
    private PressureSeverity _reportedSeverity;

    public MemoryPressureMonitorTests()
    {
        // Using a shorter check interval for testing
        _sut = new MemoryPressureMonitor(
            lowThresholdPercentage: 60,
            mediumThresholdPercentage: 70,
            highThresholdPercentage: 80,
            criticalThresholdPercentage: 90,
            checkInterval: TimeSpan.FromMilliseconds(100));

        _sut.MemoryPressureDetected += OnMemoryPressureDetected;
        _eventRaised = false;
    }

    private void OnMemoryPressureDetected(object? sender, MemoryPressureEventArgs e)
    {
        _eventRaised = true;
        _reportedSeverity = e.Severity;
    }

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldNotStartMonitoring()
    {
        Assert.False(_sut.IsMonitoring);
    }

    [Fact]
    public void StartMonitoring_ShouldSetIsMonitoringToTrue()
    {
        // Act
        _sut.StartMonitoring();

        // Assert
        Assert.True(_sut.IsMonitoring);
    }

    [Fact]
    public void StopMonitoring_AfterStartMonitoring_ShouldSetIsMonitoringToFalse()
    {
        // Arrange
        _sut.StartMonitoring();

        // Act
        _sut.StopMonitoring();

        // Assert
        Assert.False(_sut.IsMonitoring);
    }

    [Fact]
    public void StopMonitoring_WithoutStartingMonitoring_ShouldNotThrowException()
    {
        // Act & Assert (no exception)
        _sut.StopMonitoring();
        Assert.False(_sut.IsMonitoring);
    }

    [Fact]
    public void CheckMemoryPressure_ShouldUpdateCurrentMemoryUsage()
    {
        // Act
        _sut.CheckMemoryPressure();

        // Assert
        Assert.True(_sut.CurrentMemoryUsage > 0);
        Assert.True(_sut.CurrentMemoryUsagePercentage > 0);
    }

    [Fact]
    public void Dispose_ShouldStopMonitoring()
    {
        // Arrange
        _sut.StartMonitoring();
        Assert.True(_sut.IsMonitoring);

        // Act
        _sut.Dispose();

        // Assert - Using reflection to check disposed state since it's a private field
        bool isDisposed = GetPrivateField<bool>(_sut, "_isDisposed");
        Assert.True(isDisposed);
    }

    [Fact]
    public void StartMonitoring_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.StartMonitoring());
    }

    [Fact]
    public void StopMonitoring_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.StopMonitoring());
    }

    [Fact]
    public void CheckMemoryPressure_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.CheckMemoryPressure());
    }

    [Fact]
    public void SimulatePressure_ShouldRaiseEventWithCorrectSeverity()
    {
        _sut.StartMonitoring();
        SetPrivateField(_sut, "_currentPressure", PressureSeverity.None);

        // Get the method info safely
        MethodInfo? determineMethod = typeof(MemoryPressureMonitor).GetMethod(
            "DeterminePressureSeverity",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (determineMethod != null)
        {
            var mockMethodInfo = new MockMethodInfo(determineMethod);
            mockMethodInfo.SetReturnValue(PressureSeverity.Critical);

            // Call method with null parameters (which is valid in this case)
            InvokePrivateMethod(_sut, "CheckMemoryPressure", null);

            Assert.True(_eventRaised);
            Assert.Equal(PressureSeverity.Critical, _reportedSeverity);
        }
        else
        {
            // Handle the case where the method isn't found
            Assert.Fail("Could not find DeterminePressureSeverity method via reflection");
        }
    }

    public void Dispose()
    {
        _sut.MemoryPressureDetected -= OnMemoryPressureDetected;
        _sut.Dispose();
    }

    #region Helper Methods for Reflection

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? (T)field.GetValue(obj)! : default!;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(obj, value);
    }

    private object? InvokePrivateMethod(object obj, string methodName, object[]? parameters)
    {
        var method = obj.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return method?.Invoke(obj, parameters);
    }

    public class MockMethodInfo : MethodInfo
    {
        private readonly MethodInfo _originalMethod;
        private object? _returnValue;

        public MockMethodInfo(MethodInfo originalMethod)
        {
            _originalMethod = originalMethod ?? throw new ArgumentNullException(nameof(originalMethod));
        }

        public void SetReturnValue(object? returnValue)
        {
            _returnValue = returnValue;
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
        {
            return _returnValue;
        }

        public override MethodAttributes Attributes => _originalMethod.Attributes;
        public override RuntimeMethodHandle MethodHandle => _originalMethod.MethodHandle;
        public override Type? DeclaringType => _originalMethod.DeclaringType;
        public override string Name => _originalMethod.Name;
        public override Type? ReflectedType => _originalMethod.ReflectedType;
        public override ParameterInfo[] GetParameters() => _originalMethod.GetParameters();
        public override object[] GetCustomAttributes(bool inherit) => _originalMethod.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _originalMethod.GetCustomAttributes(attributeType, inherit);
        public override bool IsDefined(Type attributeType, bool inherit) => _originalMethod.IsDefined(attributeType, inherit);
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => _originalMethod.ReturnTypeCustomAttributes;
        public override MethodImplAttributes GetMethodImplementationFlags() => _originalMethod.GetMethodImplementationFlags();
        public override MethodInfo GetBaseDefinition() => _originalMethod.GetBaseDefinition();
        public override Type ReturnType => _originalMethod.ReturnType;
    }

    #endregion
}