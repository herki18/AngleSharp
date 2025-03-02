namespace AngleSharp.Css.Dom;

using System;
using Attributes;
using Values;

[DomName("CSSProperty")]
[DomNoInterfaceObject]
public interface ICssProperty : IStyleFormattable
{
    Boolean IsDefault { get; }

    [DomName("name")]
    String Name { get; }

    ICssValue RawValue { get; }

    [DomName("value")]
    String Value { get; set; }

    [DomName("important")]
    Boolean IsImportant { get; set; }

    Boolean IsInherited { get; }

    Boolean IsInitial { get; }

    Boolean IsAnimatable { get; }

    Boolean CanBeInherited { get; }

    Boolean IsShorthand { get; }

    /// <summary>
    ///     Creates a computed version of the property.
    /// </summary>
    /// <param name="context">The context to compute for.</param>
    /// <returns>The computed version of the property if uncomputed, otherwise the same.</returns>
    ICssProperty Compute(ICssComputeContext context);
}