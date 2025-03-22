namespace LayoutEngine.Platform.Tests.Unit.DOM;

using System;
using Helpers;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Platform.DOM.Abstractions;
using Xunit;

public class ResizeObserverTests
{
    [Fact]
    public void ResizeObserverFactory_Create_ShouldReturnResizeObserver()
    {
        // Arrange
        var factory = new ResizeObserverFactory();
        Action<ResizeObserverEntry[]> callback = entries => { };

        // Act
        var observer = factory.Create(callback);

        // Assert
        Assert.NotNull(observer);
        Assert.IsType<ResizeObserverWrapper>(observer);
    }

    [Fact]
    public void ResizeObserverFactory_Create_WithNullCallback_ShouldThrow()
    {
        // Arrange
        var factory = new ResizeObserverFactory();
        Action<ResizeObserverEntry[]>? nullCallback = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(nullCallback!));
    }

    [Fact]
    public void ResizeObserverWrapper_Observe_ShouldDelegateToUnderlyingObserver()
    {
        // This test is limited without being able to mock ResizeObserver
        // but we can verify the wrapper doesn't throw

        // Arrange
        var factory = new ResizeObserverFactory();
        Action<ResizeObserverEntry[]> callback = entries => { };
        var observer = factory.Create(callback);

        var document = new TestDocument();
        var element = document.CreateElement("div");

        // Act & Assert
        var exception = Record.Exception(() => observer.Observe(element));
        Assert.Null(exception);
    }

    [Fact]
    public void ResizeObserverWrapper_Disconnect_ShouldNotThrow()
    {
        // Arrange
        var factory = new ResizeObserverFactory();
        Action<ResizeObserverEntry[]> callback = entries => { };
        var observer = factory.Create(callback);

        // Act & Assert
        var exception = Record.Exception(() => observer.Disconnect());
        Assert.Null(exception);
    }

    [Fact]
    public void TestResizeObserver_Observe_ShouldStoreTargetAndUpdateState()
    {
        // Arrange
        var callback = new Action<ResizeObserverEntry[]>(entries => { });
        var observer = new TestResizeObserver(callback);
        var document = new TestDocument();
        var element = document.CreateElement("div");

        // Act
        observer.Observe(element);

        // Assert
        Assert.Same(element, observer.Target);
        Assert.True(observer.IsConnected);
    }

    [Fact]
    public void TestResizeObserver_Disconnect_ShouldClearTargetAndUpdateState()
    {
        // Arrange
        var callback = new Action<ResizeObserverEntry[]>(entries => { });
        var observer = new TestResizeObserver(callback);
        var document = new TestDocument();
        var element = document.CreateElement("div");
        observer.Observe(element);

        // Act
        observer.Disconnect();

        // Assert
        Assert.Null(observer.Target);
        Assert.False(observer.IsConnected);
    }

    [Fact]
    public void TestResizeObserver_SimulateResize_ShouldInvokeCallbackWhenConnected()
    {
        // Arrange
        var entriesReceived = Array.Empty<ResizeObserverEntry>();
        var callbackInvoked = false;
        var callback = new Action<ResizeObserverEntry[]>(entries => {
            entriesReceived = entries;
            callbackInvoked = true;
        });
        var observer = new TestResizeObserver(callback);

        var document = new TestDocument();
        var element = document.CreateElement("div");
        observer.Observe(element);

        var contentRect = new Rectangle(0, 0, 100, 200);

        // Act
        observer.SimulateResize(contentRect);

        // Assert
        Assert.True(callbackInvoked);
        Assert.Single(entriesReceived);
        Assert.Same(element, entriesReceived[0].Target);
        Assert.Equal(contentRect, entriesReceived[0].ContentRect);
    }

    [Fact]
    public void TestResizeObserver_SimulateResize_ShouldNotInvokeCallbackWhenDisconnected()
    {
        // Arrange
        var callbackInvoked = false;
        var callback = new Action<ResizeObserverEntry[]>(entries => { callbackInvoked = true; });
        var observer = new TestResizeObserver(callback);

        // Observer not connected
        var contentRect = new Rectangle(0, 0, 100, 200);

        // Act
        observer.SimulateResize(contentRect);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void TestResizeObserver_SimulateResize_ShouldNotInvokeCallbackAfterDisconnect()
    {
        // Arrange
        var callbackInvoked = false;
        var callback = new Action<ResizeObserverEntry[]>(entries => { callbackInvoked = true; });
        var observer = new TestResizeObserver(callback);

        var document = new TestDocument();
        var element = document.CreateElement("div");

        // Connect then disconnect
        observer.Observe(element);
        observer.Disconnect();

        var contentRect = new Rectangle(0, 0, 100, 200);

        // Act
        observer.SimulateResize(contentRect);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void TestResizeObserver_SimulateResize_WithNullTarget_ShouldNotInvokeCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var callback = new Action<ResizeObserverEntry[]>(entries => { callbackInvoked = true; });
        var observer = new TestResizeObserver(callback);

        // IsConnected = true but Target = null (unusual state)
        var field = typeof(TestResizeObserver).GetField("_isConnected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(observer, true);

        var contentRect = new Rectangle(0, 0, 100, 200);

        // Act
        observer.SimulateResize(contentRect);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void TestResizeObserver_ObserveMultipleTimes_ShouldUpdateTarget()
    {
        // Arrange
        var callback = new Action<ResizeObserverEntry[]>(entries => { });
        var observer = new TestResizeObserver(callback);
        var document = new TestDocument();
        var element1 = document.CreateElement("div");
        var element2 = document.CreateElement("span");

        // Act
        observer.Observe(element1);
        observer.Observe(element2);

        // Assert
        Assert.Same(element2, observer.Target);
        Assert.True(observer.IsConnected);
    }
}