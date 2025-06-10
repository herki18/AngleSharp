namespace AK;

using System;

/// <summary>
/// Represents the result of an operation, which may be a value or an error.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Exception? _error;

    /// <summary>
    /// True if the result is a success (contains a value).
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True if the result is a failure (contains an error).
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Alias for IsSuccess (for ErrorOr<T> parity).
    /// </summary>
    public bool HasValue => IsSuccess;

    /// <summary>
    /// Alias for IsFailure (for ErrorOr<T> parity).
    /// </summary>
    public bool HasError => IsFailure;

    /// <summary>
    /// Gets the value if the result is a success; throws otherwise.
    /// </summary>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("No value present. Check IsSuccess/HasValue before accessing Value.");
            return _value!;
        }
    }

    /// <summary>
    /// Gets the error if the result is a failure; throws otherwise.
    /// </summary>
    public Exception Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("No error present. Check IsFailure/HasError before accessing Error.");
            return _error!;
        }
    }

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Exception error)
    {
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result containing the given value.
    /// </summary>
    public static Result<T> Success(T value) => new Result<T>(value);

    /// <summary>
    /// Creates a failed result containing the given error.
    /// </summary>
    public static Result<T> Failure(Exception error) => new Result<T>(error);

    /// <summary>
    /// Tries to get the value. Returns true if successful, false otherwise.
    /// </summary>
    public bool TryGetValue(out T? value)
    {
        value = IsSuccess ? _value : default;
        return IsSuccess;
    }

    /// <summary>
    /// Tries to get the error. Returns true if failed, false otherwise.
    /// </summary>
    public bool TryGetError(out Exception? error)
    {
        error = IsFailure ? _error : null;
        return IsFailure;
    }

    /// <summary>
    /// Deconstructs the result into its state, value, and error.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out T? value, out Exception? error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = _error;
    }

    public override string ToString()
    {
        return IsSuccess
            ? $"Success({(_value == null ? "null" : _value.ToString())})"
            : $"Failure({Error.GetType().Name}: {Error.Message})";
    }
}

/// <summary>
/// Represents a void value for use in generic contexts (like Result<Unit>).
/// This is the idiomatic .NET/functional programming approach.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The single value of type Unit.
    /// </summary>
    public static readonly Unit Default = new Unit();

    public override string ToString() => "()";
    public override int GetHashCode() => 0;
    public override bool Equals(object? obj) => obj is Unit;
    public bool Equals(Unit other) => true;
}