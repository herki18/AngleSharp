namespace LayoutEngine.Platform.Tests.Unit.DOM;

using System;
using Helpers;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Platform.DOM.Abstractions;
using Xunit;

public class MutationObserverTests
{
    [Fact]
    public void MutationObserverFactory_Create_ShouldReturnMutationObserver()
    {
        // Arrange
        var factory = new MutationObserverFactory();
        Action<MutationRecord[]> callback = records => { };

        // Act
        var observer = factory.Create(callback);

        // Assert
        Assert.NotNull(observer);
        Assert.IsType<MutationObserverWrapper>(observer);
    }

    [Fact]
    public void MutationObserverFactory_Create_WithNullCallback_ShouldThrow()
    {
        // Arrange
        var factory = new MutationObserverFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void MutationObserverWrapper_Observe_ShouldDelegateToUnderlyingObserver()
    {
        // This test is limited without being able to mock MutationObserver
        // but we can verify the wrapper doesn't throw

        // Arrange
        var factory = new MutationObserverFactory();
        Action<MutationRecord[]> callback = records => { _ = true; };
        var observer = factory.Create(callback);

        var document = new TestDocument();
        var options = new MutationObserverInit
        {
            Attributes = true,
            ChildList = true,
            Subtree = true
        };

        // Act & Assert
        var exception = Record.Exception(() => observer.Observe(document, options));
        Assert.Null(exception);
    }

    [Fact]
    public void MutationObserverWrapper_Disconnect_ShouldNotThrow()
    {
        // Arrange
        var factory = new MutationObserverFactory();
        Action<MutationRecord[]> callback = records => { };
        var observer = factory.Create(callback);

        // Act & Assert
        var exception = Record.Exception(() => observer.Disconnect());
        Assert.Null(exception);
    }

    [Fact]
    public void TestMutationObserver_Observe_ShouldStoreTargetAndOptions()
    {
        // Arrange
        var callback = new Action<MutationRecord[]>(records => { });
        var observer = new TestMutationObserver(callback);
        var document = new TestDocument();
        var options = new MutationObserverInit
        {
            Attributes = true,
            ChildList = true,
            Subtree = true
        };

        // Act
        observer.Observe(document, options);

        // Assert
        Assert.Same(document, observer.Target);
        Assert.Same(options, observer.Options);
        Assert.True(observer.IsConnected);
    }

    [Fact]
    public void TestMutationObserver_Disconnect_ShouldClearTargetAndOptions()
    {
        // Arrange
        var callback = new Action<MutationRecord[]>(records => { });
        var observer = new TestMutationObserver(callback);
        var document = new TestDocument();
        var options = new MutationObserverInit();
        observer.Observe(document, options);

        // Act
        observer.Disconnect();

        // Assert
        Assert.Null(observer.Target);
        Assert.Null(observer.Options);
        Assert.False(observer.IsConnected);
    }

    [Fact]
    public void TestMutationObserver_SimulateMutation_ShouldInvokeCallbackWhenConnected()
    {
        // Arrange
        var records = Array.Empty<MutationRecord>();
        var callbackExecuted = false;
        var callback = new Action<MutationRecord[]>(r => { records = r; callbackExecuted = true; });
        var observer = new TestMutationObserver(callback);
        var document = new TestDocument();
        var options = new MutationObserverInit();
        observer.Observe(document, options);

        var mutation = new MutationRecord(
            "attributes",
            document,
            "id",
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        observer.SimulateMutation(mutation);

        // Assert
        Assert.True(callbackExecuted);
        Assert.Single(records);
        Assert.Same(mutation, records[0]);
    }

    [Fact]
    public void TestMutationObserver_SimulateMutation_ShouldNotInvokeCallbackWhenDisconnected()
    {
        // Arrange
        var callbackExecuted = false;
        var callback = new Action<MutationRecord[]>(r => { callbackExecuted = true; });
        var observer = new TestMutationObserver(callback);

        // Observer not connected
        var document = new TestDocument();
        var mutation = new MutationRecord(
            "attributes",
            document,
            "id",
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        observer.SimulateMutation(mutation);

        // Assert
        Assert.False(callbackExecuted);
    }

    [Fact]
    public void TestMutationObserver_SimulateMutation_ShouldNotInvokeCallbackAfterDisconnect()
    {
        // Arrange
        var callbackExecuted = false;
        var callback = new Action<MutationRecord[]>(r => { callbackExecuted = true; });
        var observer = new TestMutationObserver(callback);
        var document = new TestDocument();
        var options = new MutationObserverInit();

        // Connect then disconnect
        observer.Observe(document, options);
        observer.Disconnect();

        var mutation = new MutationRecord(
            "attributes",
            document,
            "id",
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        observer.SimulateMutation(mutation);

        // Assert
        Assert.False(callbackExecuted);
    }
}