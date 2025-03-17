using System;

namespace LayoutEngine.Contracts.Platform.Threading;

/// <summary>
/// Represents the types of threads that can be used for scheduling work.
/// </summary>
public enum ThreadType
{
    /// <summary>
    /// The main UI thread.
    /// </summary>
    MainThread,

    /// <summary>
    /// The thread dedicated to style calculations.
    /// </summary>
    StyleThread,

    /// <summary>
    /// The thread dedicated to layout calculations.
    /// </summary>
    LayoutThread,

    /// <summary>
    /// The thread dedicated to rendering.
    /// </summary>
    RenderThread,

    /// <summary>
    /// A worker thread from the thread pool.
    /// </summary>
    WorkerThread
}

/// <summary>
/// Coordinates threading and provides thread-safe scheduling.
/// </summary>
public interface IThreadingCoordinator
{
    /// <summary>
    /// Schedules an action to be executed on the specified thread.
    /// </summary>
    /// <param name="threadType">The type of thread to use.</param>
    /// <param name="action">The action to execute.</param>
    void Schedule(ThreadType threadType, Action action);

    /// <summary>
    /// Executes a function on the specified thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="threadType">The type of thread to use.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result of the function.</returns>
    T RunSynchronously<T>(ThreadType threadType, Func<T> func);

    /// <summary>
    /// Executes an action on the specified thread.
    /// </summary>
    /// <param name="threadType">The type of thread to use.</param>
    /// <param name="action">The action to execute.</param>
    void RunSynchronously(ThreadType threadType, Action action);

    /// <summary>
    /// Determines whether the current thread is of the specified type.
    /// </summary>
    /// <param name="threadType">The type of thread to check.</param>
    /// <returns>true if the current thread is of the specified type; otherwise, false.</returns>
    bool IsThread(ThreadType threadType);
}