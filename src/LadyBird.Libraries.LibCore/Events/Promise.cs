// File: Promise.cs

namespace LadyBird.Libraries.LibCore.Events;

using System;
using AK;

/// <summary>
/// Simplified Promise<T> that uses Error as the error type.
/// This is primarily used for Promise<EventReceiver> in the event loop.
/// </summary>
public class Promise<T> : Promise<T, Error>
{
    public Promise() : base() { }
    public Promise(EventReceiver parent) : base(parent) { }
}

/// <summary>
/// Represents a promise that can be resolved or rejected asynchronously.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public class Promise<TResult, TError> : EventReceiver
{
    // Delegate for resolution handler: returns Result<Unit> for parity with ErrorOr<void>
    public delegate Result<Unit> ResolutionHandler(ref TResult result);
    public delegate void RejectionHandler(ref TError error);

    /// <summary>
    /// Called when the promise is resolved.
    /// </summary>
    public ResolutionHandler? OnResolution { get; set; }

    /// <summary>
    /// Called when the promise is rejected.
    /// </summary>
    public RejectionHandler? OnRejection { get; set; }

    private Optional<ResultOrRejection> _resultOrRejection;

    /// <summary>
    /// Resolves the promise with a result.
    /// </summary>
    public void Resolve(TResult result)
    {
        _resultOrRejection = new Optional<ResultOrRejection>(new ResultOrRejection(result));

        if (OnResolution != null)
        {
            var value = _resultOrRejection.Value.Value;
            var handlerResult = OnResolution(ref value);
            _resultOrRejection = new Optional<ResultOrRejection>(new ResultOrRejection(value));
            PossiblyHandleRejection(handlerResult);
        }
    }

    /// <summary>
    /// Rejects the promise with an error.
    /// </summary>
    public void Reject(TError error)
    {
        _resultOrRejection = new Optional<ResultOrRejection>(new ResultOrRejection(error));
        PossiblyHandleRejection(_resultOrRejection.Value.ToResult());
    }

    /// <summary>
    /// Returns true if the promise is rejected.
    /// </summary>
    public bool IsRejected()
    {
        return _resultOrRejection.HasValue && _resultOrRejection.Value.IsError;
    }

    /// <summary>
    /// Returns true if the promise is resolved.
    /// </summary>
    public bool IsResolved()
    {
        return _resultOrRejection.HasValue && !_resultOrRejection.Value.IsError;
    }

    /// <summary>
    /// Waits for the promise to be resolved or rejected and returns the result.
    /// </summary>
    public Result<TResult> Await()
    {
        while (!_resultOrRejection.HasValue)
            EventLoop.Current().Pump();

        return _resultOrRejection.Value.ReleaseValue();
    }

    /// <summary>
    /// Converts a Promise&lt;A&gt; to a Promise&lt;B&gt; using a function func: A -&gt; B.
    /// </summary>
    public Promise<T, TError> Map<T>(Func<TResult, T> func)
    {
        var newPromise = new Promise<T, TError>();

        if (IsResolved())
            newPromise.Resolve(func(_resultOrRejection.Value.Value));
        if (IsRejected())
            newPromise.Reject(_resultOrRejection.Value.ReleaseError());

        OnResolution = (ref TResult result) =>
        {
            newPromise.Resolve(func(result));
            return Result<Unit>.Success(Unit.Default);
        };
        OnRejection = (ref TError error) =>
        {
            newPromise.Reject(error);
        };
        return newPromise;
    }

    /// <summary>
    /// Registers a handler to be called when the promise is resolved.
    /// </summary>
    public Promise<TResult, TError> WhenResolved(Action<TResult> handler)
    {
        return WhenResolved((ref TResult result) =>
        {
            handler(result);
            return Result<Unit>.Success(Unit.Default);
        });
    }

    /// <summary>
    /// Registers a handler to be called when the promise is resolved (with error handling).
    /// </summary>
    public Promise<TResult, TError> WhenResolved(ResolutionHandler handler)
    {
        OnResolution = handler;
        if (IsResolved())
        {
            var value = _resultOrRejection.Value.Value;
            var handlerResult = OnResolution(ref value);
            _resultOrRejection = new Optional<ResultOrRejection>(new ResultOrRejection(value));
            PossiblyHandleRejection(handlerResult);
        }
        return this;
    }

    /// <summary>
    /// Registers a handler to be called when the promise is rejected.
    /// </summary>
    public Promise<TResult, TError> WhenRejected(Action<TError> handler)
    {
        OnRejection = (ref TError error) => handler(error);
        if (IsRejected())
        {
            var error = _resultOrRejection.Value.Error;
            OnRejection(ref error);
        }
        return this;
    }

    private void PossiblyHandleRejection<T>(Result<T> result)
    {
        if (result.IsFailure && OnRejection != null)
        {
            if (result.Error is TError error)
            {
                OnRejection(ref error);
            }
            else if (result.Error is Exception)
            {
                var errorValue = _resultOrRejection.Value.Error;
                OnRejection(ref errorValue);
            }
        }
    }

    public Promise() : base() { }
    public Promise(EventReceiver parent) : base(parent) { }

    // Internal structure to hold either a result or an error
    private struct ResultOrRejection
    {
        public TResult Value { get; }
        public TError Error { get; }
        public bool IsError { get; }

        public ResultOrRejection(TResult value)
        {
            Value = value;
            Error = default!;
            IsError = false;
        }

        public ResultOrRejection(TError error)
        {
            Value = default!;
            Error = error;
            IsError = true;
        }

        public Result<TResult> ReleaseValue()
        {
            if (IsError)
            {
                if (Error is Exception ex)
                    return Result<TResult>.Failure(ex);
                else
                    return Result<TResult>.Failure(new Exception(Error?.ToString() ?? "Unknown error"));
            }
            return Result<TResult>.Success(Value);
        }

        public TError ReleaseError() => Error;

        public Result<Unit> ToResult()
        {
            if (IsError)
            {
                if (Error is Exception ex)
                    return Result<Unit>.Failure(ex);
                else
                    return Result<Unit>.Failure(new Exception(Error?.ToString() ?? "Unknown error"));
            }
            return Result<Unit>.Success(Unit.Default);
        }
    }
}