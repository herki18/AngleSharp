namespace AngleSharp.Css;

using Dom;
using Text;

public interface IValueConverter
{
    ICssValue Convert(IStringSource source);
}