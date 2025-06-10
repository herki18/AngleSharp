// File: ConsoleOutput.cs
/*
 * Copyright (c) 2025, Tim Flynn <trflynn89@ladybird.org>
 *
 * SPDX-License-Identifier: BSD-2-Clause
 */

namespace LadyBird.Libraries.WebView;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

// C# translation of C++ structs from ConsoleOutput.h

public interface IConsoleOutputEntry
{
    // Optionally, add common members if needed
}

public struct ConsoleLog : IConsoleOutputEntry
{
    public JS.Console.LogLevel Level { get; set; }
    public List<JsonValue> Arguments { get; set; }
}

public struct StackFrame
{
    public string? Function { get; set; }
    public string? File { get; set; }
    public ulong? Line { get; set; }
    public ulong? Column { get; set; }
}

public struct ConsoleError : IConsoleOutputEntry
{
    public string Name { get; set; }
    public string Message { get; set; }
    public List<StackFrame> Trace { get; set; }
    public bool InsidePromise { get; set; }
}

public struct ConsoleOutput
{
    public DateTimeOffset Timestamp { get; set; }
    public IConsoleOutputEntry Output { get; set; }
}
