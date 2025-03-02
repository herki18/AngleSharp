namespace AngleSharp;

using System.IO;

public interface IMarkupFormattable
{
    void ToHtml(TextWriter writer, IMarkupFormatter formatter);
}