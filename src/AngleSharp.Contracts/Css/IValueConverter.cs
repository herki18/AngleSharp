namespace AngleSharp.Css;

using Dom;
using Text;

public interface IValueConverter
{
    /// <summary>
    ///     Tries to convert the given source to a value.
    /// </summary>
    /// <param name="source">The source to convert.</param>
    /// <returns>The value if valid, otherwise null.</returns>
    ICssValue Convert(IStringSource source);
}