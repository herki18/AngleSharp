// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/AK/Badge.h

namespace AK;

using System;

/// <summary>
/// Badge is a token type that can only be created by the type it's parameterized with.
/// This is used to restrict access to certain methods to specific classes.
/// </summary>
/// <typeparam name="T">The type that can create badges of this type</typeparam>
public sealed class Badge<T>
{
    // Private constructor prevents external instantiation
    private Badge() { }

    /// <summary>
    /// Creates a new badge. This method is internal, so only types within this assembly can create badges.
    /// In practice, only the type T should create Badge&lt;T&gt; instances.
    /// </summary>
    public static Badge<T> Create() => new Badge<T>();

    /// <summary>
    /// Implicit conversion to allow passing null where Badge is expected
    /// </summary>
    public static implicit operator Badge<T>?(Type? _) => null;
}