namespace AngleSharp.Css.Dom;

using System;

public interface IMediaFeature : IStyleFormattable
{
    String Name { get; }
    Boolean IsMinimum { get; }
    Boolean IsMaximum { get; }
    String Value { get; }
    Boolean HasValue { get; }
}