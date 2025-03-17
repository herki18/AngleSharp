namespace AngleSharp.Css.Dom;

using System;
using System.Collections.Generic;

public interface ICssMedium : IStyleFormattable
{
    String Type { get; }
    Boolean IsExclusive { get; }
    Boolean IsInverse { get; }
    String Constraints { get; }
    IEnumerable<IMediaFeature> Features { get; }
}