namespace LayoutEngine.NG.Layout.Fragments;

using System;
using System.Collections.Generic;

/// <summary>
/// Similar to PhysicalFragmentLink but with LogicalOffset instead of
/// PhysicalOffset.
/// Represents a link to a PhysicalFragment at a specific LogicalOffset.
/// Corresponds to Blink's LogicalFragmentLink.
/// </summary>
public readonly struct LogicalFragmentLink : IEquatable<LogicalFragmentLink>
{
    /// <summary>
    /// The linked physical fragment.
    /// In C++, this is Member<const PhysicalFragment>.
    /// </summary>
    public PhysicalFragment Fragment { get; }

    /// <summary>
    /// The logical offset of the fragment.
    /// </summary>
    public LogicalOffset Offset { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalFragmentLink"/> struct.
    /// </summary>
    /// <param name="fragment">The physical fragment.</param>
    /// <param name="offset">The logical offset.</param>
    public LogicalFragmentLink(PhysicalFragment fragment, LogicalOffset offset)
    {
        Fragment = fragment;
        Offset = offset;
    }

    /// <summary>
    /// Gets a value indicating whether this link points to a valid fragment.
    /// Corresponds to C++ explicit operator bool().
    /// </summary>
    public bool IsValid => Fragment != null;

    // C++ operator* and operator-> are not directly translated.
    // Access members directly via the Fragment property, e.g., myLink.Fragment.SomeProperty.

    // C++ Trace method is for Blink's GC and not needed in C#.

    public override bool Equals(object obj)
    {
        return obj is LogicalFragmentLink link && Equals(link);
    }

    public bool Equals(LogicalFragmentLink other)
    {
        return EqualityComparer<PhysicalFragment>.Default.Equals(Fragment, other.Fragment) &&
               Offset.Equals(other.Offset);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Fragment, Offset);
    }

    public static bool operator ==(LogicalFragmentLink left, LogicalFragmentLink right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogicalFragmentLink left, LogicalFragmentLink right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"Fragment: {(Fragment?.ToString() ?? "null")}, Offset: {Offset}";
    }
}

// The C++ alias "using LogicalFragmentLinkVector = HeapVector<LogicalFragmentLink, 4>;"
// would typically be represented in C# by using List<LogicalFragmentLink>.
// For example: var vector = new List<LogicalFragmentLink>();
// The small-size optimization (4) in HeapVector is an internal detail of that C++ container.