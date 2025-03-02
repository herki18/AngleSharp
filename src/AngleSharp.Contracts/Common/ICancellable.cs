namespace AngleSharp.Common;

using System;
using System.Threading.Tasks;

public interface ICancellable<T> : ICancellable
{
    Task<T> Task { get; }
}

public interface ICancellable
{
    Boolean IsCompleted { get; }

    Boolean IsRunning { get; }

    void Cancel();
}