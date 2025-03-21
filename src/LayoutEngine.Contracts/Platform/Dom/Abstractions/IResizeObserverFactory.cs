namespace LayoutEngine.Contracts.Platform.Dom.Abstractions;

using System;
using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Factory interface for creating resize observers
/// </summary>
public interface IResizeObserverFactory
{
    /// <summary>
    /// Creates a new resize observer
    /// </summary>
    /// <param name="callback">Callback to execute when resize events occur</param>
    /// <returns>Resize observer instance</returns>
    IResizeObserver Create(Action<ResizeObserverEntry[]> callback);
}

/// <summary>
/// Interface for window provider to make testing easier
/// </summary>
public interface IWindowProvider
{
    /// <summary>
    /// Gets the current window
    /// </summary>
    /// <returns>Window interface or null</returns>
    IWindow? GetWindow();
}