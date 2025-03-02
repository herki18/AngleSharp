namespace AngleSharp.Css.Values;

using System;
using Dom;

public interface ICssComputeContext
{
    IRenderDevice Device { get; }
    IBrowsingContext Context { get; }
    IValueConverter Converter { get; }

    /// <summary>
    ///     Resolves a CSS variable by its name.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <returns>The value of the variable or null if no such variable exists.</returns>
    ICssValue Resolve(String name);
}