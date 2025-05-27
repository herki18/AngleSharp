namespace LayoutEngine.Core.Style.Public;

using System.Collections.Generic;
using AngleSharp.Css.Dom;

/// <summary>
/// Factory for creating CSS style declaration objects.
/// </summary>
public interface ICssStyleDeclarationFactory
{
    /// <summary>
    /// Creates a new empty CSS style declaration.
    /// </summary>
    /// <returns>A new CSS style declaration.</returns>
    ICssStyleDeclaration Create();

    /// <summary>
    /// Creates a new CSS style declaration and parses the provided CSS text.
    /// </summary>
    /// <param name="cssText">The CSS text to parse.</param>
    /// <returns>A new CSS style declaration with the parsed properties.</returns>
    ICssStyleDeclaration Create(string cssText);

    /// <summary>
    /// Creates a new CSS style declaration with the provided properties.
    /// </summary>
    /// <param name="properties">The properties to include in the declaration.</param>
    /// <returns>A new CSS style declaration with the provided properties.</returns>
    ICssStyleDeclaration Create(IEnumerable<ICssProperty> properties);

    /// <summary>
    /// Creates a new CSS style declaration that is a copy of the provided declaration.
    /// </summary>
    /// <param name="source">The source declaration to copy.</param>
    /// <returns>A new CSS style declaration that is a copy of the source.</returns>
    ICssStyleDeclaration CreateCopy(ICssStyleDeclaration source);
}