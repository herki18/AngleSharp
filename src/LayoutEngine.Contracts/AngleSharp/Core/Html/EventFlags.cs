namespace AngleSharp.Html;

using System;

[Flags]
internal enum EventFlags : Byte
{
    None = 0,
    StopPropagation = 0x1,
    StopImmediatePropagation = 0x2,
    Canceled = 0x4,
    Initialized = 0x8,
    Dispatch = 0x10
}