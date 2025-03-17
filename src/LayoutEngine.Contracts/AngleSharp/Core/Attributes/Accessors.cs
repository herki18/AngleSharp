namespace AngleSharp.Attributes;

using System;

[Flags]
public enum Accessors : Byte
{
    None = 0x0,
    Getter = 0x1,
    Setter = 0x2,
    Deleter = 0x4,
    Adder = 0x8,
    Remover = 0x10
}