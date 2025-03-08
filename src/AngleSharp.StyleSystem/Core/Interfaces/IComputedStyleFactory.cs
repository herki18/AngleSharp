namespace AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Factory for creating computed style objects.
/// </summary>
public interface IComputedStyleFactory
{
    /// <summary>
    /// Creates a new computed style object.
    /// </summary>
    /// <returns>A new computed style object.</returns>
    IComputedStyle CreateComputedStyle();

    /// <summary>
    /// Creates a computed style by copying another.
    /// </summary>
    /// <param name="source">The source style to copy.</param>
    /// <returns>A new computed style object with copied values.</returns>
    IComputedStyle CopyComputedStyle(IComputedStyle source);
}