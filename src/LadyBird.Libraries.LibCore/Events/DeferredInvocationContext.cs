// Main implementation here, using file-scoped namespace and C# naming conventions

/*
 * Copyright (c) 2018-2020, sin-ack <sin-ack@protonmail.com>
 * Copyright (c) 2022, the SerenityOS developers.
 *
 * SPDX-License-Identifier: BSD-2-Clause
 */

namespace LadyBird.Libraries.LibCore.Events;

// C# translation of C++: Core::DeferredInvocationContext
public sealed class DeferredInvocationContext : EventReceiver
{
    // C++: C_OBJECT(DeferredInvocationContext)
    // In C#, we use a static factory method to mimic the C++ macro.
    public static DeferredInvocationContext Create() => new DeferredInvocationContext();

    // C++: private constructor
    private DeferredInvocationContext()
    {
        // No additional initialization required (matches C++ default constructor)
    }
}