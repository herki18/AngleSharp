namespace AngleSharp.Css.Dom;

using System;
using Values;

public interface ICssValue : IEquatable<ICssValue>
{
    String CssText { get; }

    /// <summary>
    ///     Computes the current value using the given context.
    /// </summary>
    /// <param name="context">The used compute context.</param>
    /// <returns>The computed value or the original value, if already computed.</returns>
    ICssValue Compute(ICssComputeContext context);
}