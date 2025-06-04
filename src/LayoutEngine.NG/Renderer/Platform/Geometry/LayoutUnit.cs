// Copyright 2016 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

// For MethodImplOptions.AggressiveInlining

namespace LayoutEngine.NG.Renderer.Platform.Geometry;

using System;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Represents a layout unit, typically a multiple of 1/64th of a pixel.
/// This C# version uses a float for storage, providing a similar API surface
/// to Blink's fixed-point LayoutUnit.
/// See: https://trac.webkit.org/wiki/LayoutUnit
/// </summary>
public readonly struct LayoutUnit : IEquatable<LayoutUnit>, IComparable<LayoutUnit>, IComparable
{
    private readonly float _value;

    public const int FractionalBits = 6; // For LayoutUnit (1/64)
    public const int FixedPointDenominator = 1 << FractionalBits; // 64

    // kIndefiniteSize is a special value used within layout code.
    public static readonly LayoutUnit Indefinite = new LayoutUnit(-1.0f);
    public static readonly LayoutUnit Zero = new LayoutUnit(0.0f);
    public static readonly LayoutUnit MaxValue = new LayoutUnit(float.MaxValue / FixedPointDenominator); // Approximate
    public static readonly LayoutUnit MinValue = new LayoutUnit(float.MinValue / FixedPointDenominator); // Approximate

    // Approximate NearlyMax/NearlyMin for float representation
    public static readonly LayoutUnit NearlyMax = new LayoutUnit((float.MaxValue / FixedPointDenominator) - 0.5f);
    public static readonly LayoutUnit NearlyMin = new LayoutUnit((float.MinValue / FixedPointDenominator) + 0.5f);


    private LayoutUnit(float value)
    {
        _value = value;
    }

    // Constructors from numeric types
    public LayoutUnit(int value)
    {
        // C++ SaturatedSet logic is complex due to fixed-point representation.
        // For float, direct assignment is usually fine, but we can clamp if needed.
        // This simplified version doesn't perfectly replicate the saturation of raw_value.
        _value = value;
    }

    public LayoutUnit(uint value)
    {
        _value = value;
    }

    public LayoutUnit(long value)
    {
        _value = value; // Potential precision loss if value is very large
    }


    public LayoutUnit(double value)
    {
        // C++ clamps raw_value. Here we assign the double directly.
        _value = (float)value;
    }

    // Static factory methods mirroring C++
    public static LayoutUnit FromFloatCeil(float value)
    {
        return new LayoutUnit((float)Math.Ceiling(value * FixedPointDenominator) / FixedPointDenominator);
    }

    public static LayoutUnit FromFloatFloor(float value)
    {
        return new LayoutUnit((float)Math.Floor(value * FixedPointDenominator) / FixedPointDenominator);
    }

    public static LayoutUnit FromFloatRound(float value)
    {
        return new LayoutUnit((float)Math.Round(value * FixedPointDenominator) / FixedPointDenominator);
    }

    public static LayoutUnit FromDoubleRound(double value)
    {
        return new LayoutUnit((float)Math.Round(value * FixedPointDenominator) / FixedPointDenominator);
    }

    /// <summary>
    /// Creates a LayoutUnit from its raw fixed-point representation (scaled integer).
    /// This is for internal use or when interacting with systems that use the raw value.
    /// </summary>
    public static LayoutUnit FromRawValue(int rawValue)
    {
        return new LayoutUnit(rawValue / (float)FixedPointDenominator);
    }

    /// <summary>
    /// Gets the raw fixed-point value (scaled integer).
    /// For float-based LayoutUnit, this converts it back to the scaled int representation.
    /// </summary>
    public int RawValue()
    {
        // This is an approximation. For true fixed-point, _value would be the raw int.
        return (int)Math.Round(_value * FixedPointDenominator);
    }

    // SetRawValue is not idiomatic for a readonly struct.
    // If mutation is needed, it should be a class or mutable struct.

    public int ToInt() => (int)_value;
    public float ToFloat() => _value;
    public double ToDouble() => _value;

    public uint ToUnsigned()
    {
        Debug.Assert(_value >= 0, "ToUnsigned called on negative LayoutUnit");
        return (uint)_value;
    }

    // Implicit and explicit conversions
    public static implicit operator float(LayoutUnit lu) => lu._value;
    public static implicit operator double(LayoutUnit lu) => lu._value;
    public static explicit operator LayoutUnit(float f) => new LayoutUnit(f);
    public static explicit operator LayoutUnit(double d) => new LayoutUnit(d);
    public static explicit operator LayoutUnit(int i) => new LayoutUnit(i);

    public LayoutUnit Abs() => new LayoutUnit(Math.Abs(_value));
    public int Ceil() => (int)Math.Ceiling(_value);
    public int Round() => (int)Math.Round(_value);
    public int Floor() => (int)Math.Floor(_value);

    public LayoutUnit ClampNegativeToZero() => _value < 0 ? Zero : this;
    public LayoutUnit ClampPositiveToZero() => _value > 0 ? Zero : this;

    public LayoutUnit ClampIndefiniteToZero()
    {
        // C++ compares raw value with -kFixedPointDenominator for kIndefiniteSize.
        // For float, we check against our Indefinite.Value.
        if (this == Indefinite) return Zero;
        Debug.Assert(_value >= 0 || float.IsNaN(_value), "ClampIndefiniteToZero called on a negative, non-indefinite value.");
        return this;
    }

    public bool HasFraction()
    {
        // For float, this means it's not a whole number.
        return _value != Math.Floor(_value);
    }
    public bool IsInteger() => !HasFraction();

    public LayoutUnit Fraction()
    {
        // C++ uses RawValue() % kFixedPointDenominator.
        // For float, it's the fractional part.
        return new LayoutUnit(_value - (float)Math.Truncate(_value));
    }

    // MightBeSaturated is less meaningful for float unless we define strict min/max.
    public bool MightBeSaturated() => _value == MaxValue._value || _value == MinValue._value;

    public static float Epsilon() => 1.0f / FixedPointDenominator;

    public LayoutUnit AddEpsilon() => new LayoutUnit(_value + Epsilon());


    // C++ MulDiv: (this * m) / d without intermediate overflow.
    // For floats, direct computation is usually fine unless extreme values are involved.
    public LayoutUnit MulDiv(LayoutUnit m, LayoutUnit d)
    {
        if (d._value == 0)
        {
            // Handle division by zero: could throw, return NaN, or a max value.
            // Blink's fixed point might saturate.
            return float.IsNaN(_value * m._value) ? new LayoutUnit(float.NaN) : (_value >= 0 == m._value >= 0 == d._value >= 0 ? MaxValue : MinValue);
        }
        return new LayoutUnit((_value * m._value) / d._value);
    }

    public LayoutUnit? NullOptIf(LayoutUnit nullValue) => this == nullValue ? (LayoutUnit?)null : this;
    public LayoutUnit? NullOptIfMin() => NullOptIf(MinValue);


    // Operators
    public static LayoutUnit operator +(LayoutUnit a, LayoutUnit b) => new LayoutUnit(a._value + b._value);
    public static LayoutUnit operator -(LayoutUnit a, LayoutUnit b) => new LayoutUnit(a._value - b._value);
    public static LayoutUnit operator -(LayoutUnit a) => new LayoutUnit(-a._value);
    public static LayoutUnit operator *(LayoutUnit a, LayoutUnit b) => new LayoutUnit(a._value * b._value); // Note: C++ BoundedMultiply is different
    public static LayoutUnit operator /(LayoutUnit a, LayoutUnit b)
    {
        if (b._value == 0)
        {
            // Consistent with MulDiv for division by zero
            return float.IsNaN(a._value) ? new LayoutUnit(float.NaN) : (a._value >= 0 == b._value >= 0 ? MaxValue : MinValue);
        }
        return new LayoutUnit(a._value / b._value);
    }

    public static LayoutUnit operator *(LayoutUnit a, float b) => new LayoutUnit(a._value * b);
    public static LayoutUnit operator *(float a, LayoutUnit b) => new LayoutUnit(a * b._value);
    public static LayoutUnit operator /(LayoutUnit a, float b) => new LayoutUnit(a._value / b);


    public static bool operator ==(LayoutUnit a, LayoutUnit b) => a._value == b._value;
    public static bool operator !=(LayoutUnit a, LayoutUnit b) => a._value != b._value;
    public static bool operator <(LayoutUnit a, LayoutUnit b) => a._value < b._value;
    public static bool operator >(LayoutUnit a, LayoutUnit b) => a._value > b._value;
    public static bool operator <=(LayoutUnit a, LayoutUnit b) => a._value <= b._value;
    public static bool operator >=(LayoutUnit a, LayoutUnit b) => a._value >= b._value;

    // Operators with int (mirroring C++ global operators)
    public static bool operator ==(LayoutUnit a, int b) => a._value == b;
    public static bool operator ==(int a, LayoutUnit b) => a == b._value;
    public static bool operator !=(LayoutUnit a, int b) => a._value != b;
    public static bool operator !=(int a, LayoutUnit b) => a != b._value;
    public static bool operator <(LayoutUnit a, int b) => a._value < b;
    public static bool operator <(int a, LayoutUnit b) => a < b._value;
    public static bool operator >(LayoutUnit a, int b) => a._value > b;
    public static bool operator >(int a, LayoutUnit b) => a > b._value;
    public static bool operator <=(LayoutUnit a, int b) => a._value <= b;
    public static bool operator <=(int a, LayoutUnit b) => a <= b._value;
    public static bool operator >=(LayoutUnit a, int b) => a._value >= b;
    public static bool operator >=(int a, LayoutUnit b) => a >= b._value;

    public static LayoutUnit operator +(LayoutUnit a, int b) => new LayoutUnit(a._value + b);
    public static LayoutUnit operator +(int a, LayoutUnit b) => new LayoutUnit(a + b._value);
    public static LayoutUnit operator -(LayoutUnit a, int b) => new LayoutUnit(a._value - b);
    public static LayoutUnit operator -(int a, LayoutUnit b) => new LayoutUnit(a - b._value);
    public static LayoutUnit operator *(LayoutUnit a, int b) => new LayoutUnit(a._value * b);
    public static LayoutUnit operator *(int a, LayoutUnit b) => new LayoutUnit(a * b._value);
    public static LayoutUnit operator /(LayoutUnit a, int b) => new LayoutUnit(a._value / b);
    public static LayoutUnit operator /(int a, LayoutUnit b) => new LayoutUnit(a / b._value);


    public bool Equals(LayoutUnit other) => _value.Equals(other._value);
    public override bool Equals(object obj) => obj is LayoutUnit other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public int CompareTo(LayoutUnit other) => _value.CompareTo(other._value);
    public int CompareTo(object obj)
    {
        if (obj is null) return 1;
        if (obj is LayoutUnit other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(LayoutUnit)}");
    }

    public override string ToString()
    {
        // Mimic C++ FixedPoint::ToString()
        if (_value == MaxValue._value) return $"Max({ToRoundedFloatString(this)})";
        if (_value == MinValue._value) return $"Min({ToRoundedFloatString(this)})";
        if (_value == NearlyMax._value) return $"NearlyMax({ToRoundedFloatString(this)})";
        if (_value == NearlyMin._value) return $"NearlyMin({ToRoundedFloatString(this)})";
        if (this == Indefinite) return "indefinite"; // Match previous LayoutUnit.ToString(float)

        return ToRoundedFloatString(this);
    }

    private static string ToRoundedFloatString(LayoutUnit lu)
    {
        // C++ FromLayoutUnit uses String::Number(value.ToDouble(), 14)
        // This aims for a similar representation, typically a few decimal places for 1/64.
        // For 1/64, 2-3 decimal places are usually enough to represent exactly.
        // E.g., 1/64 = 0.015625.
        // Using "G" format specifier with a reasonable precision.
        // Or, more precisely:
        double val = lu.ToDouble();
        if (val == Math.Floor(val)) // if it's a whole number
            return ((long)val).ToString(CultureInfo.InvariantCulture);
        return val.ToString("0.######", CultureInfo.InvariantCulture); // Up to 6 decimal places
    }

    // Static helper functions from C++ global scope
    public static LayoutUnit IntMod(LayoutUnit a, LayoutUnit b)
    {
        if (b._value == 0) throw new DivideByZeroException("LayoutUnit.IntMod divisor is zero.");
        // C++ uses RawValue() % RawValue(). For float, it's fmod.
        return new LayoutUnit((float)(a._value % b._value));
    }

    public static int SnapSizeToPixel(LayoutUnit size, LayoutUnit location)
    {
        LayoutUnit fraction = location.Fraction();
        int result = (fraction + size).Round() - fraction.Round();
        // C++: if (result == 0 && (size.RawValue() > 4 || size.RawValue() < -4))
        // RawValue > 4 means size > 4/64 = 0.0625
        if (result == 0 && (size._value > Epsilon() * 4 || size._value < -Epsilon() * 4))
        {
            return size._value > 0 ? 1 : -1;
        }
        return result;
    }

    public static int SnapSizeToPixelAllowingZero(LayoutUnit size, LayoutUnit location)
    {
        LayoutUnit fraction = location.Fraction();
        return (fraction + size).Round() - fraction.Round();
    }

    public static int RoundToInt(LayoutUnit value) => value.Round();
    public static int FloorToInt(LayoutUnit value) => value.Floor();
    public static int CeilToInt(LayoutUnit value) => value.Ceil();
    public static LayoutUnit AbsoluteValue(LayoutUnit value) => value.Abs();
}

// Define TextRunLayoutUnit and InlineLayoutUnit if needed.
// They could also be LayoutUnit if the precision difference (1/64 vs 1/65536)
// can be handled by context or if float precision is deemed sufficient.
// If strict fixed-point with different fractional bits is required,
// a more complex FixedPoint<TStorage, TFractionalBits> struct would be needed.

// public readonly struct TextRunLayoutUnit { /* ... similar to LayoutUnit but with different scaling ... */ }
// public readonly struct InlineLayoutUnit { /* ... similar to LayoutUnit but with different scaling ... */ }