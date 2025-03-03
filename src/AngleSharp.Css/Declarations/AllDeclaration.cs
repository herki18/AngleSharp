namespace AngleSharp.Css.Declarations;

using System;
using Common;
using Dom;
using Parser;
using Text;
using Values;

static class AllDeclaration
{
    public static readonly String Name = PropertyNames.All;

    public static readonly IValueConverter Converter = new AllValueConverter();

    public static readonly ICssValue InitialValue = new CssIdentifierValue(CssKeywords.Initial);

    public static readonly PropertyFlags Flags = PropertyFlags.None;

    sealed class AllValueConverter : IValueConverter
    {
        public ICssValue Convert(StringSource source)
        {
            var identifier = source.ParseIdent();

            if (identifier != null)
            {
                if (identifier != null)
                {
                    // Normalize to canonical case versions
                    if (identifier.Equals(CssKeywords.Inherit, StringComparison.OrdinalIgnoreCase))
                        return new CssIdentifierValue(CssKeywords.Inherit);
                    else if (identifier.Equals(CssKeywords.Initial, StringComparison.OrdinalIgnoreCase))
                        return new CssIdentifierValue(CssKeywords.Initial);
                    else if (identifier.Equals(CssKeywords.Unset, StringComparison.OrdinalIgnoreCase))
                        return new CssIdentifierValue(CssKeywords.Unset);
                    else if (identifier.Equals(CssKeywords.Revert, StringComparison.OrdinalIgnoreCase))
                        return new CssIdentifierValue(CssKeywords.Revert);
                }
            }

            return null;
        }
    }
}