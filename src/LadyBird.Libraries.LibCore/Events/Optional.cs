namespace LadyBird.Libraries.LibCore.Events;

using System;

/// <summary>
/// Represents an optional value (like std::optional or AK::Optional).
/// </summary>
public readonly struct Optional<T>
{
    private readonly T _value;
    public bool HasValue { get; }

    public T Value
    {
        get
        {
            if (!HasValue)
                throw new InvalidOperationException("No value present");
            return _value;
        }
    }

    public Optional(T value)
    {
        _value = value;
        HasValue = true;
    }

    public static Optional<T> None => new Optional<T>();

    public override string ToString() =>
        HasValue ? $"Optional({(_value == null ? "null" : _value.ToString())})" : "Optional(None)";
}