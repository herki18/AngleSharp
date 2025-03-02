namespace AngleSharp.Text;

using System;

public interface IStringSource
{
    Char Current { get; }
    Boolean IsDone { get; }
    Int32 Index { get; }
    String Content { get; }
    Char Next();
    Char Back();
}