/*
 * Copyright (c) 2021, Andreas Kling <andreas@ladybird.org>
 *
 * SPDX-License-Identifier: BSD-2-Clause
 */

namespace AK;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// This file provides utilities for error handling using Result<T> pattern
// NOTE: In C#, we use Result<T> instead of ErrorOr<T> and handle error propagation differently

public static class TryExtensions
{
    // Helper method to propagate errors similar to TRY macro
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T TryGetValueOrThrow<T>(this Result<T> result, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (result.IsFailure)
        {
            // In C++, this would return the error. In C#, we'll throw for now
            throw new InvalidOperationException($"TRY failed at {filePath}:{lineNumber} in {memberName}", result.Error);
        }
        return result.Value;
    }

    // Helper method similar to MUST macro
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MustGetValue<T>(this Result<T> result, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (result.IsFailure)
        {
            // Similar to AK_HANDLE_UNEXPECTED_ERROR in C++
            Debug.Assert(false, $"MUST failed at {filePath}:{lineNumber} in {memberName}: {result.Error}");
            throw new InvalidOperationException($"MUST failed: {result.Error}");
        }
        return result.Value;
    }
}

// Static assert helper for compile-time checks
public static class StaticAssert
{
    // NOTE: C# doesn't have static_assert like C++, but we can use compile-time constants
    // This is a placeholder for cases where we need compile-time validation
    public static void AssertNotLvalueReference<T>()
    {
        // In C#, we don't have lvalue references like C++
        // This is always valid in C#
    }
}