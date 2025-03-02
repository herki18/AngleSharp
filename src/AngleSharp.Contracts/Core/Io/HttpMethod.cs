namespace AngleSharp.Io;

using System;

/// <summary>
///     Represents the usable methods for transmitting HTTP forms.
/// </summary>
public enum HttpMethod : Byte
{
    Get,
    Post,
    Put,
    Delete,
    Options,
    Head,
    Trace,
    Connect
}